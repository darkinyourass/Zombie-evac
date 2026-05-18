using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WaveAction
{
	[Tooltip("Префаб зомби. Если пусто - возьмется дефолтный из LevelManager")]
	public GameObject zombiePrefab;

	[Tooltip("Количество зомби в этой пачке")]
	public int count = 5;

	[Tooltip("Интервал между появлением каждого зомби в секундах")]
	public float spawnInterval = 0.5f;

	[Tooltip("Из какой группы точек спавнить эту пачку")]
	public SpawnGroup spawnGroup = SpawnGroup.Any;
}

[CreateAssetMenu(fileName = "NewWave", menuName = "ZombieGame/WaveData")]
public class WaveData : ScriptableObject
{
	[Header("Тайминг")]
	[Tooltip("Секунда от начала уровня, когда начнется спавн")]
	public float startTime = 10f;

	[Tooltip("За сколько секунд до старта показать UI-предупреждение над точкой спавна")]
	public float warningDuration = 3f;

	[Header("Действия волны")]
	[Tooltip("Список пачек зомби, которые появятся в эту волну")]
	public List<WaveAction> actions = new List<WaveAction>();
}