using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
	public enum CardType { None, Helicopter, Soldier, Bait, Bomb, Car, Sniper, CombatHelicopter };
	public static CardManager Instance;

	[Header("Куда спавнить карты?")]
	public Transform cardsPanel; // Контейнер внизу экрана (Horizontal Layout Group)

	[Header("Префабы UI-карточек")]
	[Tooltip("Настрой, какая UI-кнопка соответствует какой карте")]
	public List<CardDataMapping> cardPrefabs;

	[System.Serializable]
	public struct CardDataMapping
	{
		public CardType type;
		public GameObject uiPrefab; // Префаб самой кнопки с картинкой и скриптом CardUI
	}

	private void Awake() => Instance = this;

	private void Start()
	{
		SpawnDeck();
	}

	private void SpawnDeck()
	{
		// 1. Очищаем панель (если там лежали тестовые карты)
		foreach (Transform child in cardsPanel)
		{
			Destroy(child.gameObject);
		}

		// 2. Проверяем, есть ли профиль (на случай запуска сразу с игровой сцены)
		if (PlayerProfile.Instance == null)
		{
			Debug.LogWarning("PlayerProfile не найден! Играем тестовой колодой (Машина, Солдат).");
			// Если мы тестируем уровень, создадим временный профиль
			GameObject tempProfile = new GameObject("TempProfile");
			tempProfile.AddComponent<PlayerProfile>();
		}

		// 3. Спавним карты из колоды!
		foreach (CardType type in PlayerProfile.Instance.currentDeck)
		{
			if (type != CardType.None)
			{
				GameObject prefabToSpawn = GetCardPrefab(type);
				if (prefabToSpawn != null)
				{
					Instantiate(prefabToSpawn, cardsPanel);
				}
			}
		}
	}

	private GameObject GetCardPrefab(CardType type)
	{
		foreach (var mapping in cardPrefabs)
		{
			if (mapping.type == type) return mapping.uiPrefab;
		}
		return null;
	}
}