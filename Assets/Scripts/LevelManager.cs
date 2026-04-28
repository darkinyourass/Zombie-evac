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
	[SerializeField] private GameObject indicatorPrefab; // Перетащи сюда SpawnIndicator_Prefab
	[SerializeField] private float indicatorHeight = 1.5f; // Высота над землей

	[Header("Освещение")]
	public Light sunLight; // Перетащи сюда Directional Light
	public Color nightColor = new Color(0.1f, 0.1f, 0.3f); // Темно-синий
	public float nightIntensity = 0.2f;

	private Color dayColor;
	private float dayIntensity;

	public List<LevelData> allLevels = new List<LevelData>();
	private int currentLevelIndex = 0;
	private List<Transform> daySpawnPoints = new List<Transform>();
	private List<Transform> nightSpawnPoints = new List<Transform>();

	// НОВЫЙ СПИСОК: Для хранения созданных иконок
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

		currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);
		if (currentLevelIndex >= allLevels.Count) currentLevelIndex = 0;
		if (allLevels.Count > 0) LoadLevel(allLevels[currentLevelIndex]);
	}

	public void LoadLevel(LevelData data)
	{
		// Очищаем старые индикаторы, если уровень перезагружается
		ClearIndicators();

		currentData = data;
		if (currentLevelEnvironment != null) Destroy(currentLevelEnvironment);
		currentLevelEnvironment = Instantiate(data.levelPrefab, Vector3.zero, Quaternion.identity);

		if (Camera.main != null)
		{
			Camera.main.transform.position = data.cameraPosition;
			Camera.main.transform.rotation = Quaternion.Euler(data.cameraRotation);
			Camera.main.fieldOfView = data.cameraFieldOfView;
			if (Camera.main.orthographic) Camera.main.orthographicSize = data.orthographicSize;
		}

		if (navSurface != null) navSurface.BuildNavMesh();

		daySpawnPoints.Clear();
		foreach (GameObject sp in GameObject.FindGameObjectsWithTag("SpawnPoint")) daySpawnPoints.Add(sp.transform);

		nightSpawnPoints.Clear();
		foreach (GameObject sp in GameObject.FindGameObjectsWithTag("NightSpawn")) nightSpawnPoints.Add(sp.transform);
		if (nightSpawnPoints.Count == 0) nightSpawnPoints.AddRange(daySpawnPoints);

		// --- СОЗДАЕМ ИНДИКАТОРЫ ДЛЯ ПЛАНИРОВАНИЯ ---
		SpawnPlanningIndicators();

		SpawnHumans(data.humanCount);
		GameManager.Instance.SetTotalHumans(GameObject.FindGameObjectsWithTag("Human").Length);
		GameManager.Instance.SetupTimer(data.levelTimer);
	}

	// НОВЫЙ МЕТОД: Спавн иконок над точками
	private void SpawnPlanningIndicators()
	{
		if (indicatorPrefab == null) return;

		// Мы показываем только "дневные" спавны в начале, так как SuddenDeath (ночные)
		// начинаются позже. Но можно заспавнить и те, и другие, если нужно.
		foreach (Transform sp in daySpawnPoints)
		{
			Vector3 pos = sp.position + Vector3.up * indicatorHeight;
			GameObject indicator = Instantiate(indicatorPrefab, pos, indicatorPrefab.transform.rotation);
			activeIndicators.Add(indicator);
		}
	}

	// НОВЫЙ МЕТОД: Очистка списка
	private void ClearIndicators()
	{
		foreach (GameObject ind in activeIndicators)
		{
			if (ind != null) Destroy(ind);
		}
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

	// ТОЧКА ВХОДА ИЗ GAMEMANAGER при старте боя
	public void StartInitialSpawns()
	{
		// --- ПЕРВЫМ ДЕЛОМ УДАЛЯЕМ ИКОНКИ ПЛАНИРОВАНИЯ ---
		ClearIndicators();

		StartCoroutine(InitialSpawnRoutine());
	}

	private IEnumerator InitialSpawnRoutine()
	{
		for (int i = 0; i < currentData.initialZombies; i++)
		{
			yield return new WaitForSeconds(currentData.initialSpawnDelay);
			if (daySpawnPoints.Count > 0)
				Instantiate(zombiePrefab, daySpawnPoints[Random.Range(0, daySpawnPoints.Count)].position, Quaternion.identity);
		}
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

		// Запоминаем, насколько сильно светило небо днем
		float startAmbient = RenderSettings.ambientIntensity;

		while (t < 1)
		{
			t += Time.deltaTime / transitionTime;
			sunLight.color = Color.Lerp(dayColor, nightColor, t);
			sunLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);

			// Плавно гасим свечение самого неба (от дневного до 0.1)
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
				Instantiate(zombiePrefab, nightSpawnPoints[Random.Range(0, nightSpawnPoints.Count)].position, Quaternion.identity);
		}
	}
}