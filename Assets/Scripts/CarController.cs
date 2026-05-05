using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class CarController : MonoBehaviour
{
	public enum CarState { DrivingToTarget, Loading, Leaving }
	public CarState currentState;

	[Header("Связь с карточкой")]
	public CardData myCardData;

	[Header("Технические настройки")]
	public float crushRadius = 2.5f;
	public float boardingRadius = 1.8f;
	public int boardPerTick = 1;
	public float dangerRadius = 2.0f;

	[Header("UI и Визуал")]
	public TMP_Text statusText;
	public GameObject sirenRingPrefab;
	public GameObject humanAlertPrefab;
	public float alertDuration = 2.0f;

	private int maxCapacity;
	private float loadTime;
	private float sirenRadius;
	private float boardingCooldown;
	private float moveSpeed;

	private NavMeshAgent agent;
	private int currentLoad = 0;
	private int loadedHumanCount = 0;
	private int loadedScientistCount = 0;
	private Transform exitWaypoint;
	private bool isTooHot = false;
	private bool sirenFired = false;

	private List<GameObject> loadedUnits = new List<GameObject>();

	private void Awake() => agent = GetComponent<NavMeshAgent>();

	private void Start()
	{
		int currentLevel = 1;
		if (PlayerProfile.Instance != null && myCardData != null)
		{
			var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == myCardData.name);
			if (progress != null) currentLevel = progress.currentLevel;

			maxCapacity = (int)myCardData.GetCalculatedStat(StatType.Capacity, currentLevel);
			loadTime = myCardData.GetCalculatedStat(StatType.Duration, currentLevel);
			sirenRadius = myCardData.GetCalculatedStat(StatType.Radius, currentLevel);
			boardingCooldown = myCardData.GetCalculatedStat(StatType.Cooldown, currentLevel);
			moveSpeed = myCardData.GetCalculatedStat(StatType.Speed, currentLevel);
		}

		if (loadTime <= 0) loadTime = 15f;
		if (sirenRadius <= 0) sirenRadius = 20f;
		if (boardingCooldown <= 0) boardingCooldown = 0.5f;
		if (maxCapacity <= 0) maxCapacity = 5;
		if (moveSpeed <= 0) moveSpeed = 3.5f;

		agent.speed = moveSpeed;
	}

	public void Launch(Vector3 targetPos)
	{
		if (maxCapacity == 0) Start();

		GameObject[] waypoints = GameObject.FindGameObjectsWithTag("CarWaypoint");
		if (waypoints.Length < 2) { Destroy(gameObject); return; }

		Transform entryWaypoint = waypoints[0].transform;
		exitWaypoint = waypoints[1].transform;
		float minDist = float.MaxValue;
		float maxDist = float.MinValue;

		foreach (var wp in waypoints)
		{
			float d = Vector3.Distance(targetPos, wp.transform.position);
			if (d < minDist) { minDist = d; entryWaypoint = wp.transform; }
			if (d > maxDist) { maxDist = d; exitWaypoint = wp.transform; }
		}

		agent.Warp(entryWaypoint.position);
		currentState = CarState.DrivingToTarget;
		agent.SetDestination(targetPos);

		UpdateUI();
		StartCoroutine(CarRoutine());
	}

	private void Update()
	{
		if (statusText != null)
			statusText.transform.rotation = Quaternion.LookRotation(statusText.transform.position - Camera.main.transform.position);

		if (agent.velocity.magnitude > 1f)
		{
			Collider[] hits = Physics.OverlapSphere(transform.position, crushRadius);
			foreach (var h in hits)
			{
				if (h.CompareTag("Zombie")) h.GetComponent<Zombie>()?.TakeDamage(1000);
			}
		}
	}

	private void UpdateUI()
	{
		if (statusText == null) return;

		if (currentLoad == 0) statusText.text = "";
		else statusText.text = currentLoad.ToString();

		if (isTooHot) statusText.color = Color.red;
		else statusText.color = Color.green;
	}

	private IEnumerator CarRoutine()
	{
		while (agent.pathPending || agent.remainingDistance > 1.5f) yield return null;

		currentState = CarState.Loading;
		agent.isStopped = true;
		agent.velocity = Vector3.zero;

		if (!sirenFired)
		{
			if (sirenRingPrefab != null)
			{
				GameObject ring = Instantiate(sirenRingPrefab, transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
				ring.GetComponent<SirenEffect>()?.Setup(sirenRadius, 1.2f);
			}

			foreach (var h in Human.AllHumans)
			{
				if (h != null && Vector3.Distance(transform.position, h.transform.position) <= sirenRadius)
				{
					h.SetRescueTarget(transform);
					if (humanAlertPrefab != null)
					{
						GameObject alert = Instantiate(humanAlertPrefab, h.transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0), h.transform);
						Destroy(alert, alertDuration);
					}
				}
			}

			foreach (var s in Scientist.AllScientists)
			{
				if (s != null && Vector3.Distance(transform.position, s.transform.position) <= sirenRadius)
				{
					s.SetRescueTarget(transform);
					if (humanAlertPrefab != null)
					{
						GameObject alert = Instantiate(humanAlertPrefab, s.transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0), s.transform);
						Destroy(alert, alertDuration);
					}
				}
			}

			sirenFired = true;
		}

		float nextBoardTime = 0;
		float waitTimer = 0;

		while (currentLoad < maxCapacity && !isTooHot && waitTimer < loadTime)
		{
			agent.velocity = Vector3.zero;
			waitTimer += Time.deltaTime;

			Collider[] dangerZone = Physics.OverlapSphere(transform.position, dangerRadius);
			foreach (var d in dangerZone)
			{
				if (d.CompareTag("Zombie"))
				{
					isTooHot = true;
					break;
				}
			}

			if (isTooHot) break;

			if (Time.time >= nextBoardTime)
			{
				int boarded = 0;
				Collider[] unitsAtDoors = Physics.OverlapSphere(transform.position, boardingRadius);

				foreach (var unit in unitsAtDoors)
				{
					Human human = unit.GetComponent<Human>();
					if (human != null && currentLoad < maxCapacity)
					{
						var nav = human.GetComponent<NavMeshAgent>();
						if (nav != null) nav.enabled = false;
						human.transform.position = new Vector3(0, -1000, 0);
						loadedUnits.Add(human.gameObject);

						currentLoad++;
						loadedHumanCount++;
						boarded++;

						if (boarded >= boardPerTick || currentLoad >= maxCapacity) break;
					}

					Scientist scientist = unit.GetComponent<Scientist>();
					if (scientist != null && currentLoad < maxCapacity)
					{
						var nav = scientist.GetComponent<NavMeshAgent>();
						if (nav != null) nav.enabled = false;
						scientist.transform.position = new Vector3(0, -1000, 0);
						loadedUnits.Add(scientist.gameObject);

						currentLoad++;
						loadedScientistCount++;
						boarded++;

						if (boarded >= boardPerTick || currentLoad >= maxCapacity) break;
					}
				}

				if (boarded > 0)
				{
					UpdateUI();
					nextBoardTime = Time.time + boardingCooldown;
				}
			}

			yield return null;
		}

		UpdateUI();
		if (isTooHot) yield return new WaitForSeconds(0.8f);

		foreach (var h in Human.AllHumans)
		{
			if (h != null && h.rescueTarget == transform) h.CancelRescue();
		}

		foreach (var s in Scientist.AllScientists)
		{
			if (s != null && s.rescueTarget == transform) s.CancelRescue();
		}

		currentState = CarState.Leaving;
		agent.isStopped = false;
		agent.SetDestination(exitWaypoint.position);

		if (loadedHumanCount > 0 || loadedScientistCount > 0)
		{
			GameManager.Instance.AddRescuedFromTransport(
				loadedHumanCount,
				loadedScientistCount,
				transform.position
			);

			foreach (var unit in loadedUnits)
			{
				if (unit != null) Destroy(unit);
			}

			loadedUnits.Clear();
		}

		while (agent.pathPending || agent.remainingDistance > 2f) yield return null;
		Destroy(gameObject);
	}
}