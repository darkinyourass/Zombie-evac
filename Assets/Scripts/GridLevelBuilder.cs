using System.Collections.Generic;
using UnityEngine;

public static class GridLevelBuilder
{
	public static GameObject Build(GridLevelData data)
	{
		if (data == null)
		{
			Debug.LogError("[GridLevelBuilder] GridLevelData is null.");
			return null;
		}

		GameObject root = new GameObject("GridLevel_" + data.name);

		GameObject roadsRoot = new GameObject("Roads");
		roadsRoot.transform.SetParent(root.transform);

		GameObject housesRoot = new GameObject("Houses");
		housesRoot.transform.SetParent(root.transform);

		GameObject markersRoot = new GameObject("Markers");
		markersRoot.transform.SetParent(root.transform);

		GridCellType[,] cells = ParseGrid(data);
		if (cells == null)
		{
			Debug.LogError("[GridLevelBuilder] Failed to parse grid.");
			Object.Destroy(root);
			return null;
		}

		Vector3 originOffset = new Vector3(
			-(data.width - 1) * data.cellSize * 0.5f,
			0f,
			-(data.height - 1) * data.cellSize * 0.5f
		);

		for (int y = 0; y < data.height; y++)
		{
			for (int x = 0; x < data.width; x++)
			{
				GridCellType cell = cells[x, y];
				Vector3 pos = new Vector3(x * data.cellSize, 0f, y * data.cellSize) + originOffset;

				switch (cell)
				{
					case GridCellType.Empty:
						if (data.emptyPrefab != null)
							Object.Instantiate(data.emptyPrefab, pos, Quaternion.identity, root.transform);
						break;

					case GridCellType.Road:
						if (data.roadPrefab != null)
							Object.Instantiate(data.roadPrefab, pos, Quaternion.identity, roadsRoot.transform);
						break;

					case GridCellType.HouseSmall:
						if (data.houseSmallPrefab != null)
							Object.Instantiate(data.houseSmallPrefab, pos, Quaternion.identity, housesRoot.transform);
						break;

					case GridCellType.SpawnDay:
						SpawnMarker(data.daySpawnMarkerPrefab, pos, markersRoot.transform, GridCellType.SpawnDay);
						if (data.roadPrefab != null)
							Object.Instantiate(data.roadPrefab, pos, Quaternion.identity, roadsRoot.transform);
						break;

					case GridCellType.SpawnNight:
						SpawnMarker(data.nightSpawnMarkerPrefab, pos, markersRoot.transform, GridCellType.SpawnNight);
						if (data.roadPrefab != null)
							Object.Instantiate(data.roadPrefab, pos, Quaternion.identity, roadsRoot.transform);
						break;
				}
			}
		}

		return root;
	}

	private static void SpawnMarker(GameObject prefab, Vector3 pos, Transform parent, GridCellType type)
	{
		GameObject go;

		if (prefab != null)
			go = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
		else
			go = new GameObject(type.ToString());

		go.transform.SetParent(parent);

		GridLevelMarker marker = go.GetComponent<GridLevelMarker>();
		if (marker == null)
			marker = go.AddComponent<GridLevelMarker>();

		marker.markerType = type;

		if (type == GridCellType.SpawnDay)
			go.tag = "SpawnPoint";
		else if (type == GridCellType.SpawnNight)
			go.tag = "NightSpawn";
	}

	private static GridCellType[,] ParseGrid(GridLevelData data)
	{
		string[] rows = data.rawRows
			.Replace("\r", "")
			.Split('\n');

		List<string> validRows = new List<string>();
		for (int i = 0; i < rows.Length; i++)
		{
			if (!string.IsNullOrWhiteSpace(rows[i]))
				validRows.Add(rows[i].Trim());
		}

		if (validRows.Count != data.height)
		{
			Debug.LogError($"[GridLevelBuilder] Row count mismatch. Expected {data.height}, got {validRows.Count}");
			return null;
		}

		GridCellType[,] cells = new GridCellType[data.width, data.height];

		for (int y = 0; y < data.height; y++)
		{
			string[] parts = validRows[data.height - 1 - y].Split(',');

			if (parts.Length != data.width)
			{
				Debug.LogError($"[GridLevelBuilder] Column count mismatch in row {y}. Expected {data.width}, got {parts.Length}");
				return null;
			}

			for (int x = 0; x < data.width; x++)
			{
				if (!int.TryParse(parts[x].Trim(), out int value))
				{
					Debug.LogError($"[GridLevelBuilder] Failed to parse cell [{x},{y}] value: {parts[x]}");
					return null;
				}

				cells[x, y] = (GridCellType)value;
			}
		}

		return cells;
	}
}