using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
	public static LevelManager Instance;

	[Header("References")]
	[SerializeField] private NavMeshSurface navSurface;
	[SerializeField] private GameObject humanPrefab;
	[SerializeField] private GameObject zombiePrefab;
	[SerializeField] private GameObject defaultScientistPrefab;

	[Header("Planning Indicators")]
	[SerializeField] private GameObject indicatorPrefab;
	[SerializeField] private float indicatorHeight = 1.5f;

	[Header("Lighting")]
	public Light sunLight;
	public Color nightColor = new Color(0.1f, 0.1f, 0.3f);
	public float nightIntensity = 0.2f;

	private Color dayColor;
	private float dayIntensity;

	private int currentLevelIndex = 0;

	private readonly List<Transform> daySpawnPoints = new List<Transform>();
	private readonly List<Transform> nightSpawnPoints = new List<Transform>();
	private readonly List<Transform> humanSpawnPoints = new List<Transform>();
	private readonly List<Transform> scientistSpawnPoints = new List<Transform>();

	private readonly List<GameObject> activeIndicators = new List<GameObject>();

	public LevelData currentData;
	private GameObject currentLevelEnvironment;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		if (sunLight != null)
		{
			dayColor = sunLight.color;
			dayIntensity = sunLight.intensity;
		}

		int regionIdx = PlayerProfile.Instance.currentRegionIndex;
		regionIdx = Mathf.Clamp(regionIdx, 0, PlayerProfile.Instance.allRegions.Count - 1);

		RegionConfig currentRegion = PlayerProfile.Instance.allRegions[regionIdx];

		currentLevelIndex = PlayerPrefs.GetInt("SelectedLevelToPlay", 0);
		if (currentLevelIndex >= currentRegion.levels.Count)
			currentLevelIndex = 0;

		if (currentRegion.levels.Count > 0)
			LoadLevel(currentRegion.levels[currentLevelIndex]);
	}

	public void LoadLevel(LevelData data)
	{
		ClearIndicators();

		currentData = data;

		if (currentLevelEnvironment != null)
			Destroy(currentLevelEnvironment);

		currentLevelEnvironment = Instantiate(data.levelPrefab, Vector3.zero, Quaternion.identity);

		if (CameraController.Instance != null)
			CameraController.Instance.SetupCamera(data);

		if (navSurface != null)
			navSurface.BuildNavMesh();

		CollectSpawnPoints();

		SpawnPlanningIndicators();
		SpawnHumans(data.humanCount);
		SpawnScientists(data.scientistCount);

		GameManager.Instance.SetTotalHumans(Human.AllHumans.Count);
		GameManager.Instance.SetupTimer(data.levelTimer);
	}

	private void CollectSpawnPoints()
	{
		daySpawnPoints.Clear();
		nightSpawnPoints.Clear();
		humanSpawnPoints.Clear();
		scientistSpawnPoints.Clear();

		if (currentLevelEnvironment == null)
		{
			Debug.LogWarning("[LevelManager] currentLevelEnvironment is null.");
			return;
		}

		LevelCell[] cells = currentLevelEnvironment.GetComponentsInChildren<LevelCell>(true);

		foreach (LevelCell cell in cells)
		{
			if (cell == null) continue;

			switch (cell.cellType)
			{
				case LevelCell.CellType.SpawnDay:
					daySpawnPoints.Add(cell.transform);
					break;

				case LevelCell.CellType.SpawnNight:
					nightSpawnPoints.Add(cell.transform);
					break;

				case LevelCell.CellType.HumanSpawn:
					humanSpawnPoints.Add(cell.transform);
					break;

				case LevelCell.CellType.ScientistSpawn:
					scientistSpawnPoints.Add(cell.transform);
					break;
			}
		}

		if (nightSpawnPoints.Count == 0)
			nightSpawnPoints.AddRange(daySpawnPoints);

		Debug.Log($"[LevelManager] Spawn points collected | Day: {daySpawnPoints.Count}, Night: {nightSpawnPoints.Count}, Human: {humanSpawnPoints.Count}, Scientist: {scientistSpawnPoints.Count}");
	}

	private void SpawnPlanningIndicators()
	{
		if (indicatorPrefab == null) return;

		foreach (Transform sp in daySpawnPoints)
		{
			if (sp == null) continue;

			Vector3 pos = sp.position + Vector3.up * indicatorHeight;
			GameObject indicator = Instantiate(indicatorPrefab, pos, indicatorPrefab.transform.rotation);
			activeIndicators.Add(indicator);
		}
	}

	private void ClearIndicators()
	{
		foreach (GameObject ind in activeIndicators)
		{
			if (ind != null)
				Destroy(ind);
		}

		activeIndicators.Clear();
	}

	private void SpawnHumans(int count)
	{
		if (humanPrefab == null)
		{
			Debug.LogWarning("[LevelManager] Human prefab is missing.");
			return;
		}

		if (count <= 0)
			return;

		if (currentData.useHumanSpawnMarkers && humanSpawnPoints.Count > 0)
		{
			float spawnRadius = 2.5f;
			int attemptsPerHuman = 8;

			for (int i = 0; i < count; i++)
			{
				Transform anchor = humanSpawnPoints[Random.Range(0, humanSpawnPoints.Count)];
				if (anchor == null) continue;

				bool spawned = false;

				for (int attempt = 0; attempt < attemptsPerHuman; attempt++)
				{
					Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
					Vector3 candidate = anchor.position + new Vector3(offset2D.x, 0f, offset2D.y);

					if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
					{
						Instantiate(humanPrefab, hit.position, Quaternion.identity);
						spawned = true;
						break;
					}
				}

				if (!spawned)
				{
					if (NavMesh.SamplePosition(anchor.position, out NavMeshHit fallbackHit, 2f, NavMesh.AllAreas))
					{
						Instantiate(humanPrefab, fallbackHit.position, Quaternion.identity);
					}
					else
					{
						Debug.LogWarning("[LevelManager] Failed to spawn human near anchor: " + anchor.name);
					}
				}
			}

			return;
		}

		for (int i = 0; i < count; i++)
		{
			Vector3 randomPos = Random.insideUnitSphere * 20f;
			randomPos.y = 0f;

			if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
			{
				Instantiate(humanPrefab, hit.position, Quaternion.identity);
			}
		}
	}

	private void SpawnScientists(int count)
	{
		if (count <= 0)
			return;

		GameObject prefabToUse = currentData.scientistPrefab != null
			? currentData.scientistPrefab
			: defaultScientistPrefab;

		if (prefabToUse == null)
		{
			Debug.LogWarning("[LevelManager] Scientist prefab is missing.");
			return;
		}

		if (currentData.useScientistSpawnMarkers && scientistSpawnPoints.Count > 0)
		{
			float spawnRadius = 2.5f;
			int attemptsPerScientist = 8;

			for (int i = 0; i < count; i++)
			{
				Transform anchor = scientistSpawnPoints[Random.Range(0, scientistSpawnPoints.Count)];
				if (anchor == null) continue;

				bool spawned = false;

				for (int attempt = 0; attempt < attemptsPerScientist; attempt++)
				{
					Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
					Vector3 candidate = anchor.position + new Vector3(offset2D.x, 0f, offset2D.y);

					if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
					{
						Instantiate(prefabToUse, hit.position, Quaternion.identity);
						spawned = true;
						break;
					}
				}

				if (!spawned)
				{
					if (NavMesh.SamplePosition(anchor.position, out NavMeshHit fallbackHit, 2f, NavMesh.AllAreas))
					{
						Instantiate(prefabToUse, fallbackHit.position, Quaternion.identity);
					}
					else
					{
						Debug.LogWarning("[LevelManager] Failed to spawn scientist near anchor: " + anchor.name);
					}
				}
			}

			return;
		}

		for (int i = 0; i < count; i++)
		{
			Vector3 randomPos = Random.insideUnitSphere * 20f;
			randomPos.y = 0f;

			if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
			{
				Instantiate(prefabToUse, hit.position, Quaternion.identity);
			}
		}
	}

	public void StartInitialSpawns()
	{
		ClearIndicators();

		StartCoroutine(InitialSpawnRoutine());

		if (currentData.spawnBoss && currentData.bossPrefab != null && currentData.bossCount > 0)
			StartCoroutine(SpawnBossesRoutine());
	}

	private IEnumerator InitialSpawnRoutine()
	{
		for (int i = 0; i < currentData.initialZombies; i++)
		{
			yield return new WaitForSeconds(currentData.initialSpawnDelay);

			if (daySpawnPoints.Count == 0)
			{
				Debug.LogWarning("[LevelManager] No day spawn points found.");
				continue;
			}

			Transform spawnPoint = daySpawnPoints[Random.Range(0, daySpawnPoints.Count)];

			if (spawnPoint == null)
				continue;

			if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
			{
				Instantiate(zombiePrefab, hit.position, Quaternion.identity);
			}
			else
			{
				Debug.LogWarning("[LevelManager] Day spawn point is not on NavMesh: " + spawnPoint.name);
			}
		}
	}

	private IEnumerator SpawnBossesRoutine()
	{
		yield return new WaitForSeconds(currentData.bossSpawnDelay);

		int bossesToSpawn = Mathf.Max(0, currentData.bossCount);

		for (int i = 0; i < bossesToSpawn; i++)
		{
			SpawnOneBoss(i + 1);

			if (i < bossesToSpawn - 1)
				yield return new WaitForSeconds(currentData.bossSpawnStepDelay);
		}
	}

	private void SpawnOneBoss(int bossNumber)
	{
		if (daySpawnPoints.Count == 0) return;
		if (currentData.bossPrefab == null) return;

		Transform spawnPoint = daySpawnPoints[Random.Range(0, daySpawnPoints.Count)];
		if (spawnPoint == null) return;

		Vector3 spawnPos = spawnPoint.position;

		if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
			spawnPos = hit.position;

		GameObject bossObj = Instantiate(currentData.bossPrefab, spawnPos, Quaternion.identity);
		bossObj.name = currentData.bossPrefab.name + "_" + bossNumber;

		ZombieBoss boss = bossObj.GetComponent<ZombieBoss>();
		if (boss != null)
		{
			boss.rageInterval = currentData.bossRageInterval;
			boss.rageDuration = currentData.bossRageDuration;
			boss.rageBreakRadius = currentData.bossBreakRadius;
			boss.maxBuildingsPerRage = currentData.bossMaxBuildingsPerRage;
		}

		Debug.Log("[LevelManager] Boss spawned: " + bossObj.name);
	}

	public void StartSuddenDeath()
	{
		StartCoroutine(SuddenDeathRoutine());
		StartCoroutine(NightTransitionRoutine());
	}

	private IEnumerator NightTransitionRoutine()
	{
		if (sunLight == null) yield break;

		float transitionTime = 2f;
		float t = 0f;
		float startAmbient = RenderSettings.ambientIntensity;

		while (t < 1f)
		{
			t += Time.deltaTime / transitionTime;
			sunLight.color = Color.Lerp(dayColor, nightColor, t);
			sunLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
			RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, 0.1f, t);
			yield return null;
		}
	}

	private IEnumerator SuddenDeathRoutine()
	{
		while (GameManager.Instance.State == GameManager.GameState.SuddenDeath)
		{
			yield return new WaitForSeconds(currentData.suddenDeathSpawnRate);

			if (nightSpawnPoints.Count == 0)
			{
				Debug.LogWarning("[LevelManager] No night spawn points found.");
				continue;
			}

			Transform spawnPoint = nightSpawnPoints[Random.Range(0, nightSpawnPoints.Count)];
			if (spawnPoint == null) continue;

			if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
			{
				Instantiate(zombiePrefab, hit.position, Quaternion.identity);
			}
			else
			{
				Debug.LogWarning("[LevelManager] Night spawn point is not on NavMesh: " + spawnPoint.name);
			}
		}
	}
}