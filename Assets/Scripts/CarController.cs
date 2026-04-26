using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class CarController : MonoBehaviour
{
	public enum CarState { DrivingToTarget, Loading, Leaving }
	public CarState currentState;

	[Header("Настройки Времени")]
	public float loadTime = 15f;          // Тот самый таймер, который ты просил вернуть

	[Header("Настройки Эвакуации")]
	public int maxCapacity = 5;
	public float sirenRadius = 20f;
	public float crushRadius = 2.5f;

	[Header("Посадка и Опасность")]
	public float boardingRadius = 1.8f;
	public float boardingCooldown = 0.5f;
	public int boardPerTick = 1;
	public float dangerRadius = 2.0f;

	[Header("UI и Визуал")]
	public TMP_Text statusText;
	public GameObject sirenRingPrefab;

	private NavMeshAgent agent;
	private int currentLoad = 0;
	private Transform exitWaypoint;
	private bool isTooHot = false;
	private bool sirenFired = false; // Предохранитель для сирены

	private void Awake() => agent = GetComponent<NavMeshAgent>();

	public void Launch(Vector3 targetPos)
	{
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
		// 1. Доезжаем
		while (agent.pathPending || agent.remainingDistance > 1.5f) yield return null;

		// 2. ФАЗА ЗАГРУЗКИ
		currentState = CarState.Loading;
		agent.isStopped = true;
		agent.velocity = Vector3.zero; // Сразу гасим инерцию

		// --- СИРЕНА (ОДИН РАЗ!) ---
		if (!sirenFired && sirenRingPrefab != null)
		{
			GameObject ring = Instantiate(sirenRingPrefab, transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
			// Передаем радиус из настроек машины в эффект!
			ring.GetComponent<SirenEffect>()?.Setup(sirenRadius, 1.2f);

			Collider[] humansInRange = Physics.OverlapSphere(transform.position, sirenRadius);
			foreach (var h in humansInRange)
			{
				if (h.CompareTag("Human")) h.GetComponent<NavMeshAgent>()?.SetDestination(transform.position);
			}
			sirenFired = true;
		}

		float nextBoardTime = 0;
		float waitTimer = 0; // Таймер нахождения на точке

		while (currentLoad < maxCapacity && !isTooHot && waitTimer < loadTime)
		{
			agent.velocity = Vector3.zero; // Не даем людям её толкать
			waitTimer += Time.deltaTime;

			// Проверка на зомби
			Collider[] dangerZone = Physics.OverlapSphere(transform.position, dangerRadius);
			foreach (var d in dangerZone) if (d.CompareTag("Zombie")) { isTooHot = true; break; }

			if (isTooHot) break;

			// Посадка
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

		// 3. УЕЗЖАЕМ
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