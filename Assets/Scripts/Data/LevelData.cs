using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "ZombieGame/LevelData")]
public class LevelData : ScriptableObject
{
	public enum CameraType { Perspective, Orthographic }

	[Header("Базовые настройки")]
	public GameObject levelPrefab;
	public int humanCount = 25;
	public float levelTimer = 60f;
	public int initialZombies = 5;
	public float initialSpawnDelay = 0.5f;
	public float suddenDeathSpawnRate = 0.3f;

	[Header("Условия победы")]
	[Tooltip("Минимальное количество спасённых людей для победы")]
	public int requiredRescuedHumans = 10;

	[Header("3 звезды")]
	[Tooltip("1 звезда. Если 0, будет использоваться requiredRescuedHumans")]
	public int star1RequiredHumans = 0;

	[Tooltip("2 звезды. Если 0, будет использоваться requiredRescuedHumans + 5")]
	public int star2RequiredHumans = 0;

	[Tooltip("Описание 3 звезды. Сама логика = perfect clear")]
	public string star3Description = "Пройти идеально";

	[Header("Отображение на Mission Popup")]
	public string missionTitleOverride = "";
	public Sprite missionIcon;

	[TextArea(2, 4)]
	public string missionDescription;

	[Header("Настройки Камеры Уровня")]
	public CameraType cameraType = CameraType.Perspective;
	public Vector3 cameraPosition = new Vector3(0, 20, -15);
	public Vector3 cameraRotation = new Vector3(60, 0, 0);
	public float cameraFieldOfView = 60f;
	public float orthographicSize = 10f;

	[Header("Награда за первое прохождение")]
	public int currencyReward = 50;
	public LootboxData levelRewardLootbox;

	[Header("Учёные на уровне")]
	[Tooltip("Сколько учёных будет на уровне")]
	public int scientistCount = 0;

	[Tooltip("Префаб учёного (Scientist) для этого уровня (если не задан — возьмём из LevelManager)")]
	public GameObject scientistPrefab;

	[Header("Настройки босса")]
	[Tooltip("Нужно ли спавнить босса на этом уровне")]
	public bool spawnBoss = false;

	[Tooltip("Префаб босса")]
	public GameObject bossPrefab;

	[Tooltip("Сколько боссов появится на уровне")]
	public int bossCount = 1;

	[Tooltip("Через сколько секунд после старта уровня начнётся спаун боссов")]
	public float bossSpawnDelay = 5f;

	[Tooltip("Пауза между спауном нескольких боссов")]
	public float bossSpawnStepDelay = 0.5f;

	[Tooltip("Раз в сколько секунд босс входит в ярость")]
	public float bossRageInterval = 8f;

	[Tooltip("Сколько секунд длится ярость")]
	public float bossRageDuration = 2.5f;

	[Tooltip("Радиус ломания зданий во время ярости")]
	public float bossBreakRadius = 4f;

	[Tooltip("Сколько максимум зданий может сломать за одну ярость")]
	public int bossMaxBuildingsPerRage = 1;

	public int GetStar1Target()
	{
		return star1RequiredHumans > 0 ? star1RequiredHumans : requiredRescuedHumans;
	}

	public int GetStar2Target()
	{
		return star2RequiredHumans > 0 ? star2RequiredHumans : requiredRescuedHumans + 5;
	}

	public string GetMissionTitle(int regionIndex, int levelIndex)
	{
		if (!string.IsNullOrEmpty(missionTitleOverride))
			return missionTitleOverride;

		return $"MISSION {regionIndex + 1}-{levelIndex + 1}";
	}
}