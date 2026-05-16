using UnityEngine;

public class LevelCell : MonoBehaviour
{
	public enum CellType
	{
		Empty = 0,
		Road = 1,
		House = 2,
		SpawnDay = 3,
		SpawnNight = 4,
		HumanSpawn = 5,
		ScientistSpawn = 6
	}

	[Header("Cell Settings")]
	public CellType cellType = CellType.Empty;
}