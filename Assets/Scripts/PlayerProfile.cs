using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerProfile : MonoBehaviour
{
	public static PlayerProfile Instance;

	[Header("Данные игрока")]
	public int totalCurrency = 0;
	public List<CardManager.CardType> unlockedCards = new List<CardManager.CardType>();

	// Наша колода на 5 слотов (None означает пустой слот)
	public CardManager.CardType[] currentDeck = new CardManager.CardType[5];

	private void Awake()
	{
		// Делаем профиль бессмертным при загрузке новых сцен
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

		// Загружаем открытые карты (по умолчанию открыты только Машина и Солдат)
		string unlockedStr = PlayerPrefs.GetString("UnlockedCards", "Car,Soldier");
		unlockedCards = unlockedStr.Split(',')
			.Select(s => (CardManager.CardType)System.Enum.Parse(typeof(CardManager.CardType), s))
			.ToList();

		// Загружаем колоду (по умолчанию 2 карты и 3 пустых слота)
		string deckStr = PlayerPrefs.GetString("CurrentDeck", "Car,Soldier,CombatHelicopter,Bait,Sniper");
		var deckList = deckStr.Split(',')
			.Select(s => (CardManager.CardType)System.Enum.Parse(typeof(CardManager.CardType), s))
			.ToArray();

		for (int i = 0; i < 5; i++)
		{
			if (i < deckList.Length) currentDeck[i] = deckList[i];
			else currentDeck[i] = CardManager.CardType.None;
		}
	}

	public void SaveProfile()
	{
		PlayerPrefs.SetInt("TotalCurrency", totalCurrency);
		PlayerPrefs.SetString("UnlockedCards", string.Join(",", unlockedCards));
		PlayerPrefs.SetString("CurrentDeck", string.Join(",", currentDeck));
		PlayerPrefs.Save();
	}

	// Метод для получения карты из лутбокса
	public void AddCardReward(CardManager.CardType newCard)
	{
		if (!unlockedCards.Contains(newCard))
		{
			unlockedCards.Add(newCard);
			Debug.Log("Открыта новая карта: " + newCard);

			// Автоматически кладем в первый пустой слот
			for (int i = 0; i < 5; i++)
			{
				if (currentDeck[i] == CardManager.CardType.None)
				{
					currentDeck[i] = newCard;
					Debug.Log("Карта добавлена в слот " + i);
					break;
				}
			}
			SaveProfile();
		}
	}
}