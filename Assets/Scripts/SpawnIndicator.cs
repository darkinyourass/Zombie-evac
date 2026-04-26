using UnityEngine;

public class SpawnIndicator : MonoBehaviour
{
	private void Start()
	{
		// Сразу подписываемся на событие старта игры
		// Если у нас в GameManager нет события, будем проверять в Update
	}

	private void Update()
	{
		// Если игра началась — удаляем индикатор
		if (GameManager.Instance.State != GameManager.GameState.Planning)
		{
			Destroy(gameObject);
		}
	}
}