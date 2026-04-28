using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "ZombieGame/LevelData")]
public class LevelData : ScriptableObject
{
	[Header("Базовые настройки")]
	public GameObject levelPrefab;
	public int humanCount = 25;
	public float levelTimer = 60f;
	public int initialZombies = 5;
	public float initialSpawnDelay = 0.5f;
	public float suddenDeathSpawnRate = 0.3f;

	[Header("Настройки Камеры (Туториал)")]
	public Vector3 cameraPosition = new Vector3(0, 20, -15);
	public Vector3 cameraRotation = new Vector3(60, 0, 0);
	public float cameraFieldOfView = 60f; // Для обычного зума
	public float orthographicSize = 10f;  // Если используешь Orthographic камеру

	[Header("Награда за первое прохождение (Лутбокс)")]
	public int currencyReward = 50; // Сколько выживших (монет) даем
	public LootboxData levelRewardLootbox;
	public bool hasCardReward = false; // Есть ли в коробке карта?
	public CardManager.CardType cardReward = CardManager.CardType.None; // Какая именно
}