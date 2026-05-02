using UnityEngine;
using System.Collections.Generic;

// [ГДЕ ИСПОЛЬЗОВАТЬ]: В папке Data (наши ScriptableObjects)
[CreateAssetMenu(fileName = "NewRegion", menuName = "ZombieGame/RegionData")]
public class RegionConfig : ScriptableObject
{
	public string regionName = "Nevada";

	[Tooltip("Префаб с визуалом карты (со скриптом RegionMapVisual)")]
	public GameObject regionUIPrefab; // <-- ТЕПЕРЬ ТУТ ПРЕФАБ

	public LootboxData regionRewardLootbox;
	public List<LevelData> levels = new List<LevelData>();
}