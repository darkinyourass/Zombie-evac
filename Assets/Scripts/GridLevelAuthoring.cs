using System.Collections.Generic;
using UnityEngine;

public class GridLevelAuthoring : MonoBehaviour
{
	[Header("Grid")]
	public int width = 16;
	public int height = 24;
	public float cellSize = 2f;
	public bool drawGizmos = true;
	public Color gizmoColor = new Color(0f, 0.8f, 1f, 0.35f);

	[Header("Optional roots")]
	public Transform cellsRoot;
	public Transform roadsRoot;
	public Transform housesRoot;
	public Transform spawnsRoot;

	public List<Transform> GetCellsOfType(LevelCell.CellType type)
	{
		List<Transform> result = new List<Transform>();
		LevelCell[] all = GetComponentsInChildren<LevelCell>(true);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] != null && all[i].cellType == type)
				result.Add(all[i].transform);
		}

		return result;
	}

	private void OnDrawGizmos()
	{
		if (!drawGizmos) return;

		Gizmos.color = gizmoColor;

		Vector3 origin = transform.position;
		float totalW = width * cellSize;
		float totalH = height * cellSize;

		for (int x = 0; x <= width; x++)
		{
			Vector3 from = origin + new Vector3(x * cellSize, 0f, 0f);
			Vector3 to = origin + new Vector3(x * cellSize, 0f, totalH);
			Gizmos.DrawLine(from, to);
		}

		for (int y = 0; y <= height; y++)
		{
			Vector3 from = origin + new Vector3(0f, 0f, y * cellSize);
			Vector3 to = origin + new Vector3(totalW, 0f, y * cellSize);
			Gizmos.DrawLine(from, to);
		}
	}
}