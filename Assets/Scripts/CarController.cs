using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro;

// [ГДЕ ВИСИТ]: На префабе Машины.
// [НАСТРОЙКИ]: В инспекторе нужно будет назначить Human Alert Prefab (маркер под ногами людей).
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
	[Tooltip("Эффект, который появляется под ногами людей, когда они услышали машину")]
	public GameObject humanAlertPrefab; // <-- НОВОЕ ПОЛЕ

	private int maxCapacity;
	private float loadTime;
	private float sirenRadius;
	private float boardingCooldown;

	private NavMeshAgent agent;
	private int currentLoad = 0;
	private Transform exitWaypoint;
	private bool isTooHot = false;
	private bool sirenFired = false;

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
		}
		else
		{
			maxCapacity = 5; loadTime = 15f; sirenRadius = 20f; boardingCooldown = 0.5f;
		}

		if (loadTime <= 0) loadTime = 15f;
		if (sirenRadius <= 0) sirenRadius = 20f;
		if (boardingCooldown <= 0) boardingCooldown = 0.5f;
		if (maxCapacity <= 0) maxCapacity = 5;
	}

	public void Launch(Vector3 targetPos)
	{
		if (maxCapacity == 0) Start();

		GameObject[] waypoints = GameObject.FindGameObjectsWithTag("CarWaypoint");
		if (waypoints.Length < 2) { Destroy(gameObject); return; }

		Transform entryWaypoint = waypoints[0].transform;
		exitWaypoint = waypoints[1].transform;
		float minDist = float.MaxValue; float maxDist = float.MinValue;

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
		if (isTooHot) { statusText.text = "TOO HOT!"; statusText.color = Color.red; }
		else if (currentLoad >= maxCapacity) { statusText.text = "FULL!"; statusText.color = Color.green; }
		else { statusText.text = $"{currentLoad} / {maxCapacity}"; statusText.color = Color.white; }
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

			Collider[] humansInRange = Physics.OverlapSphere(transform.position, sirenRadius);
			foreach (var h in humansInRange)
			{
				if (h.CompareTag("Human"))
				{
					h.GetComponent<NavMeshAgent>()?.SetDestination(transform.position);

					// --- НОВОЕ: Спавним маркер под человеком ---
					if (humanAlertPrefab != null)
					{
						// Делаем маркер дочерним объектом человека, чтобы он бежал вместе с ним
						Instantiate(humanAlertPrefab, h.transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0), h.transform);
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
			foreach (var d in dangerZone) if (d.CompareTag("Zombie")) { isTooHot = true; break; }

			if (isTooHot) break;

			if (Time.time >= nextBoardTime)
			{
				Collider[] humansAtDoors = Physics.OverlapSphere(transform.position, boardingRadius);
				int boarded = 0;
				foreach (var h in humansAtDoors)
				{
					if (h.CompareTag("Human"))
					{
						Destroy(h.gameObject);
						currentLoad++; boarded++;
						if (boarded >= boardPerTick || currentLoad >= maxCapacity) break;
					}
				}
				if (boarded > 0) { UpdateUI(); nextBoardTime = Time.time + boardingCooldown; }
			}
			yield return null;
		}

		UpdateUI();
		if (isTooHot) yield return new WaitForSeconds(0.8f);

		currentState = CarState.Leaving;
		agent.isStopped = false;
		agent.SetDestination(exitWaypoint.position);

		while (agent.pathPending || agent.remainingDistance > 2f) yield return null;

		GameManager.Instance.AddRescuedHumans(currentLoad);
		Destroy(gameObject);
	}
}