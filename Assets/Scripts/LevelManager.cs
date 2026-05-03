using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
	public static LevelManager Instance;

	[Header("Ссылки на мир")]
	[SerializeField] private NavMeshSurface navSurface;
	[SerializeField] private GameObject humanPrefab;
	[SerializeField] private GameObject zombiePrefab;

	[Header("Визуализация Планирования")]
	[SerializeField] private GameObject indicatorPrefab;
	[SerializeField] private float indicatorHeight = 1.5f;

	[Header("Освещение")]
	public Light sunLight;
	public Color nightColor = new Color(0.1f, 0.1f, 0.3f);
	public float nightIntensity = 0.2f;

	private Color dayColor;
	private float dayIntensity;

	private int currentLevelIndex = 0;
	private List<Transform> daySpawnPoints = new List<Transform>();
	private List<Transform> nightSpawnPoints = new List<Transform>();
	private List<GameObject> activeIndicators = new List<GameObject>();

	public LevelData currentData;
	private GameObject currentLevelEnvironment;

	private void Awake() => Instance = this;

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
		{
			LoadLevel(currentRegion.levels[currentLevelIndex]);
		}
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

		daySpawnPoints.Clear();
		foreach (GameObject sp in GameObject.FindGameObjectsWithTag("SpawnPoint"))
			daySpawnPoints.Add(sp.transform);

		nightSpawnPoints.Clear();
		foreach (GameObject sp in GameObject.FindGameObjectsWithTag("NightSpawn"))
			nightSpawnPoints.Add(sp.transform);

		if (nightSpawnPoints.Count == 0)
			nightSpawnPoints.AddRange(daySpawnPoints);

		SpawnPlanningIndicators();
		SpawnHumans(data.humanCount);

		GameManager.Instance.SetTotalHumans(GameObject.FindGameObjectsWithTag("Human").Length);
		GameManager.Instance.SetupTimer(data.levelTimer);
	}

	private void SpawnPlanningIndicators()
	{
		if (indicatorPrefab == null) return;

		foreach (Transform sp in daySpawnPoints)
		{
			Vector3 pos = sp.position + Vector3.up * indicatorHeight;
			GameObject indicator = Instantiate(indicatorPrefab, pos, indicatorPrefab.transform.rotation);
			activeIndicators.Add(indicator);
		}
	}

	private void ClearIndicators()
	{
		foreach (GameObject ind in activeIndicators)
			if (ind != null)
				Destroy(ind);

		activeIndicators.Clear();
	}

	private void SpawnHumans(int count)
	{
		for (int i = 0; i < count; i++)
		{
			Vector3 randomPos = Random.insideUnitSphere * 20f;
			randomPos.y = 0;

			if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
				Instantiate(humanPrefab, hit.position, Quaternion.identity);
		}
	}

	public void StartInitialSpawns()
	{
		ClearIndicators();
		StartCoroutine(InitialSpawnRoutine());

		if (currentData.spawnBoss && currentData.bossPrefab != null)
			StartCoroutine(SpawnBossRoutine());
	}

	private IEnumerator InitialSpawnRoutine()
	{
		for (int i = 0; i < currentData.initialZombies; i++)
		{
			yield return new WaitForSeconds(currentData.initialSpawnDelay);

			if (daySpawnPoints.Count > 0)
			{
				Instantiate(
					zombiePrefab,
					daySpawnPoints[Random.Range(0, daySpawnPoints.Count)].position,
					Quaternion.identity
				);
			}
		}
	}

	private IEnumerator SpawnBossRoutine()
	{
		yield return new WaitForSeconds(currentData.bossSpawnDelay);

		if (daySpawnPoints.Count == 0)
			yield break;

		Transform spawnPoint = daySpawnPoints[Random.Range(0, daySpawnPoints.Count)];
		GameObject bossObj = Instantiate(currentData.bossPrefab, spawnPoint.position, Quaternion.identity);

		ZombieBoss boss = bossObj.GetComponent<ZombieBoss>();
		if (boss != null)
		{
			boss.rageInterval = currentData.bossRageInterval;
			boss.rageDuration = currentData.bossRageDuration;
			boss.rageBreakRadius = currentData.bossBreakRadius;
			boss.maxBuildingsPerRage = currentData.bossMaxBuildingsPerRage;
		}

		Debug.Log("[LevelManager] Босс заспавнен");
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
		float t = 0;
		float startAmbient = RenderSettings.ambientIntensity;

		while (t < 1)
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

			if (nightSpawnPoints.Count > 0)
			{
				Instantiate(
					zombiePrefab,
					nightSpawnPoints[Random.Range(0, nightSpawnPoints.Count)].position,
					Quaternion.identity
				);
			}
		}
	}
}