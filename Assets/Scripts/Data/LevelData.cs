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

	[Header("Настройки Камеры Уровня")]
	public CameraType cameraType = CameraType.Perspective; // <-- Тот самый переключатель
	public Vector3 cameraPosition = new Vector3(0, 20, -15);
	public Vector3 cameraRotation = new Vector3(60, 0, 0);
	public float cameraFieldOfView = 60f; // Для Perspective
	public float orthographicSize = 10f;  // Для Orthographic

	[Header("Награда за первое прохождение")]
	public int currencyReward = 50; // Сколько валюты (софта) даем
	public LootboxData levelRewardLootbox;

}