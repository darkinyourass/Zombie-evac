using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerProfile : MonoBehaviour
{
	public static PlayerProfile Instance;

	[Header("Данные игрока")]
	public int totalCurrency = 0;

	[Header("Коллекция и Колода")]
	public List<CardData> allAvailableCards = new List<CardData>();

	[Tooltip("Перетащи сюда карты, которые игрок получит при первом запуске игры")]
	public List<CardData> starterCards = new List<CardData>(); // <-- НОВОЕ ПОЛЕ

	public List<CardProgress> ownedCardsProgress = new List<CardProgress>();
	public CardData[] currentDeck = new CardData[5];

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
			LoadProfile();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void LoadProfile()
	{
		totalCurrency = PlayerPrefs.GetInt("TotalCurrency", 0);

		// 1. Загружаем прогресс карт
		if (PlayerPrefs.HasKey("CardsProgress"))
		{
			string json = PlayerPrefs.GetString("CardsProgress");
			var wrapper = JsonUtility.FromJson<SerializationWrapper<CardProgress>>(json);
			if (wrapper != null && wrapper.target != null)
			{
				ownedCardsProgress = wrapper.target;
			}
		}

		// 2. Загружаем колоду
		string deckStr = PlayerPrefs.GetString("CurrentDeck", "");
		if (!string.IsNullOrEmpty(deckStr))
		{
			string[] deckIds = deckStr.Split(',');
			for (int i = 0; i < 5; i++)
			{
				if (i < deckIds.Length && deckIds[i] != "None")
				{
					currentDeck[i] = allAvailableCards.Find(c => c.name == deckIds[i]);
				}
				else
				{
					currentDeck[i] = null;
				}
			}
		}
		else
		{
			GiveStarterCards();
		}
	}

	public void SaveProfile()
	{
		PlayerPrefs.SetInt("TotalCurrency", totalCurrency);

		string progressJson = JsonUtility.ToJson(new SerializationWrapper<CardProgress>(ownedCardsProgress));
		PlayerPrefs.SetString("CardsProgress", progressJson);

		string[] deckIds = new string[5];
		for (int i = 0; i < 5; i++)
		{
			deckIds[i] = currentDeck[i] != null ? currentDeck[i].name : "None";
		}
		PlayerPrefs.SetString("CurrentDeck", string.Join(",", deckIds));

		PlayerPrefs.Save();
	}

	public void AddCardReward(CardData newCard)
	{
		if (newCard == null) return;

		CardProgress progress = ownedCardsProgress.Find(p => p.cardId == newCard.name);

		if (progress == null)
		{
			progress = new CardProgress(newCard.name);
			ownedCardsProgress.Add(progress);
			Debug.Log("Открыта новая карта: " + newCard.cardName);

			for (int i = 0; i < 5; i++)
			{
				if (currentDeck[i] == null)
				{
					currentDeck[i] = newCard;
					Debug.Log("Карта добавлена в слот " + i);
					break;
				}
			}
		}
		else
		{
			progress.collectedShards++;
			Debug.Log($"Получен осколок для {newCard.cardName}! Всего: {progress.collectedShards}");
		}

		SaveProfile();
	}

	// ТЕПЕРЬ МЫ БЕРЕМ КАРТЫ ИЗ НАСТРОЕК ИНСПЕКТОРА
	private void GiveStarterCards()
	{
		foreach (CardData card in starterCards)
		{
			if (card != null) AddCardReward(card);
		}
		SaveProfile(); // Жестко сохраняем после выдачи
	}

	// МЕТОД ДЛЯ ОЧИСТКИ СОХРАНЕНИЙ (чтобы ты мог легко тестить игру с нуля)
	[ContextMenu("Сбросить весь прогресс (Очистить сохранения)")]
	public void ResetProfile()
	{
		PlayerPrefs.DeleteAll();
		ownedCardsProgress.Clear();
		for (int i = 0; i < 5; i++) currentDeck[i] = null;
		totalCurrency = 0;
		Debug.LogWarning("ПРОГРЕСС СБРОШЕН! Перезапустите игру.");
	}
}

[System.Serializable]
public class SerializationWrapper<T>
{
	public List<T> target;
	public SerializationWrapper(List<T> target) => this.target = target;
}