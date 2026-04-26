using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class CarController : MonoBehaviour
{
	public static List<CarController> AllCars = new List<CarController>();
	public enum CarState { DrivingToPoint, Loading, Escaping }
	public CarState currentState;

	[Header("Настройки")]
	public int hp = 3;
	public int maxCapacity = 8;
	public float attractRadius = 12f;
	public float fireRange = 8f;
	public float fireRate = 0.5f;

	[Header("Ссылки")]
	public GameObject zombiePrefab;
	public TextMeshProUGUI loadText;
	public ParticleSystem exhaustFire;
	public LineRenderer pathLine;

	private NavMeshAgent agent;
	private int currentLoad = 0;
	private bool isDestroyed = false;
	private Vector3 escapeDir;
	private float nextFire;

	// Пулемет
	private LineRenderer tracerLine;

	private void Awake() => agent = GetComponent<NavMeshAgent>();
	private void OnEnable() => AllCars.Add(this);
	private void OnDisable() => AllCars.Remove(this);

	public void Launch(Vector3 target)
	{
		agent.enabled = false;
		Vector3 spawnDir = Vector3.back;
		if (Zombie.AllZombies.Count > 0)
		{
			Vector3 avgZ = Vector3.zero;
			foreach (var z in Zombie.AllZombies) if (z != null) avgZ += z.transform.position;
			spawnDir = (avgZ / Zombie.AllZombies.Count - target).normalized;
		}

		Vector3 spawnPos = target + spawnDir * 25f;
		if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 15f, NavMesh.AllAreas)) transform.position = hit.position;
		else transform.position = target;

		escapeDir = -spawnDir;
		agent.enabled = true;
		agent.SetDestination(target);
		currentState = CarState.DrivingToPoint;
		if (exhaustFire != null) exhaustFire.Play();
		UpdateUI();

		// ИСПРАВЛЕНИЕ: Создаем пулемет на отдельном дочернем объекте, чтобы не конфликтовать с линией пути!
		GameObject tracerObj = new GameObject("MachineGunTracer");
		tracerObj.transform.SetParent(transform); // Делаем его частью машины
		tracerObj.transform.localPosition = Vector3.zero;

		tracerLine = tracerObj.AddComponent<LineRenderer>();
		tracerLine.startWidth = 0.1f;
		tracerLine.endWidth = 0.02f;
		tracerLine.material = new Material(Shader.Find("Sprites/Default"));
		tracerLine.startColor = Color.yellow;
		tracerLine.endColor = Color.yellow;
		tracerLine.enabled = false;
	}

	private void Update()
	{
		if (isDestroyed || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

		// Стрельба пулемета
		if (Time.time > nextFire) { Shoot(); nextFire = Time.time + fireRate; }

		if (currentState == CarState.DrivingToPoint && !agent.pathPending && agent.remainingDistance < 1f) StartLoading();

		if (currentState == CarState.Escaping)
		{
			if (pathLine != null) DrawPath();
			if (!agent.pathPending && agent.remainingDistance < 1f)
			{
				GameManager.Instance.AddRescuedHumans(currentLoad);
				Destroy(gameObject);
			}
		}
	}

	private void Shoot()
	{
		Zombie best = null; float minD = fireRange;
		foreach (var z in Zombie.AllZombies)
		{
			if (z == null) continue;
			float d = Vector3.Distance(transform.position, z.transform.position);
			if (d < minD) { minD = d; best = z; }
		}
		if (best != null && tracerLine != null)
		{
			best.TakeDamage(10);

			// Визуальный выстрел из машины
			tracerLine.SetPosition(0, transform.position + Vector3.up);
			tracerLine.SetPosition(1, best.transform.position + Vector3.up);
			tracerLine.enabled = true;
			StartCoroutine(HideTracer());
		}
	}

	private IEnumerator HideTracer()
	{
		yield return new WaitForSeconds(0.1f);
		if (tracerLine != null) tracerLine.enabled = false;
	}

	private void StartLoading()
	{
		currentState = CarState.Loading;
		agent.isStopped = true;
		foreach (var h in Human.AllHumans) if (Vector3.Distance(transform.position, h.transform.position) < attractRadius) h.SetRescueTarget(transform);
		StartCoroutine(LoadRoutine());
	}

	private IEnumerator LoadRoutine()
	{
		while (currentLoad < maxCapacity)
		{
			Human h = null;
			foreach (var hum in Human.AllHumans) if (Vector3.Distance(transform.position, hum.transform.position) < 2.5f) { h = hum; break; }
			if (h != null) { Destroy(h.gameObject); currentLoad++; UpdateUI(); yield return new WaitForSeconds(0.3f); }
			else { bool anyone = false; foreach (var hum in Human.AllHumans) if (hum.rescueTarget == transform) anyone = true; if (!anyone) break; yield return null; }
		}
		Exit();
	}

	private void Exit()
	{
		currentState = CarState.Escaping;
		agent.isStopped = false;
		Vector3 ex = transform.position + escapeDir * 40f;
		if (NavMesh.SamplePosition(ex, out NavMeshHit hit, 20f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
		else { GameManager.Instance.AddRescuedHumans(currentLoad); Destroy(gameObject); }
	}

	public void TakeDamage(int dmg) { hp -= dmg; if (hp <= 0) Die(); }

	private void Die()
	{
		if (isDestroyed) return;
		isDestroyed = true;

		if (agent.isOnNavMesh) agent.isStopped = true;
		if (exhaustFire != null) exhaustFire.Stop();
		if (tracerLine != null) tracerLine.enabled = false;

		int totalZombiesToSpawn = 3 + currentLoad;
		for (int i = 0; i < totalZombiesToSpawn; i++)
		{
			Instantiate(zombiePrefab, transform.position + Random.insideUnitSphere * 2f, Quaternion.identity);
		}

		GetComponent<Renderer>().material.color = Color.black;
		Destroy(gameObject, 3f);
	}

	private void UpdateUI() { if (loadText != null) loadText.text = $"{currentLoad}/{maxCapacity}"; }
	private void DrawPath() { if (agent.path.corners.Length > 0 && pathLine != null) { pathLine.positionCount = agent.path.corners.Length; pathLine.SetPositions(agent.path.corners); } }
	private void OnTriggerEnter(Collider other) { if (other.CompareTag("Zombie") && currentState != CarState.Loading) Destroy(other.gameObject); }
}