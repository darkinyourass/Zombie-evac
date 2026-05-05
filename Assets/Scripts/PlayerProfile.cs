using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerProfile : MonoBehaviour
{
	public static PlayerProfile Instance;

	[Header("Данные игрока")]
	public int totalCurrency = 0;
	public int totalScientistsCurrency = 0;

	[Header("Прогресс карты (Saga)")]
	public int currentRegionIndex = 0;
	public int currentLevelIndex = 0;
	public bool hasPendingMapAnimation = false;
	public bool hasPendingRegionAnimation = false;

	[Header("База Регионов")]
	public List<RegionConfig> allRegions = new List<RegionConfig>();

	[Header("Коллекция и Колода")]
	public List<CardData> allAvailableCards = new List<CardData>();
	public List<CardData> starterCards = new List<CardData>();
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
		else Destroy(gameObject);
	}

	public void LoadProfile()
	{
		totalCurrency = PlayerPrefs.GetInt("TotalCurrency", 0);
		totalScientistsCurrency = PlayerPrefs.GetInt("TotalScientistsCurrency", 0);

		currentRegionIndex = PlayerPrefs.GetInt("CurrentRegion", 0);
		currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 0);
		hasPendingMapAnimation = PlayerPrefs.GetInt("PendingMapAnim", 0) == 1;
		hasPendingRegionAnimation = PlayerPrefs.GetInt("PendingRegionAnim", 0) == 1;

		if (PlayerPrefs.HasKey("CardsProgress"))
		{
			string json = PlayerPrefs.GetString("CardsProgress");
			var wrapper = JsonUtility.FromJson<SerializationWrapper<CardProgress>>(json);
			if (wrapper != null && wrapper.target != null) ownedCardsProgress = wrapper.target;
		}

		string deckStr = PlayerPrefs.GetString("CurrentDeck", "");
		if (!string.IsNullOrEmpty(deckStr))
		{
			string[] deckIds = deckStr.Split(',');
			for (int i = 0; i < 5; i++)
			{
				currentDeck[i] = (i < deckIds.Length && deckIds[i] != "None") ? allAvailableCards.Find(c => c.name == deckIds[i]) : null;
			}
		}
		else GiveStarterCards();
	}

	public void SaveProfile()
	{
		PlayerPrefs.SetInt("TotalCurrency", totalCurrency);
		PlayerPrefs.SetInt("TotalScientistsCurrency", totalScientistsCurrency);

		PlayerPrefs.SetInt("CurrentRegion", currentRegionIndex);
		PlayerPrefs.SetInt("CurrentLevel", currentLevelIndex);
		PlayerPrefs.SetInt("PendingMapAnim", hasPendingMapAnimation ? 1 : 0);
		PlayerPrefs.SetInt("PendingRegionAnim", hasPendingRegionAnimation ? 1 : 0);

		string progressJson = JsonUtility.ToJson(new SerializationWrapper<CardProgress>(ownedCardsProgress));
		PlayerPrefs.SetString("CardsProgress", progressJson);

		string[] deckIds = new string[5];
		for (int i = 0; i < 5; i++) deckIds[i] = currentDeck[i] != null ? currentDeck[i].name : "None";
		PlayerPrefs.SetString("CurrentDeck", string.Join(",", deckIds));

		PlayerPrefs.Save();
	}

	public void CompleteCurrentLevel()
	{
		if (currentRegionIndex >= allRegions.Count) return;

		currentLevelIndex++;
		int levelsInCurrentRegion = allRegions[currentRegionIndex].levels.Count;

		if (currentLevelIndex >= levelsInCurrentRegion)
		{
			currentRegionIndex++;
			currentLevelIndex = 0;

			if (currentRegionIndex < allRegions.Count)
			{
				hasPendingRegionAnimation = true;
				hasPendingMapAnimation = false;
			}
			else
			{
				currentRegionIndex = allRegions.Count - 1;
				currentLevelIndex = levelsInCurrentRegion;
				hasPendingMapAnimation = true;
			}
		}
		else
		{
			hasPendingMapAnimation = true;
		}

		SaveProfile();
	}

	public void AddCardReward(CardData newCard)
	{
		if (newCard == null) return;
		CardProgress progress = ownedCardsProgress.Find(p => p.cardId == newCard.name);
		if (progress == null)
		{
			progress = new CardProgress(newCard.name);
			ownedCardsProgress.Add(progress);
			for (int i = 0; i < 5; i++)
			{
				if (currentDeck[i] == null) { currentDeck[i] = newCard; break; }
			}
		}
		else progress.collectedShards++;
		SaveProfile();
	}

	private void GiveStarterCards()
	{
		foreach (CardData card in starterCards) if (card != null) AddCardReward(card);
		SaveProfile();
	}

	[ContextMenu("Сбросить весь прогресс (Очистить сохранения)")]
	public void ResetProfile()
	{
		PlayerPrefs.DeleteAll();
		ownedCardsProgress.Clear();
		for (int i = 0; i < 5; i++) currentDeck[i] = null;
		totalCurrency = 0;
		totalScientistsCurrency = 0;
		currentRegionIndex = 0;
		currentLevelIndex = 0;
		hasPendingMapAnimation = false;
		hasPendingRegionAnimation = false;
	}
}

[System.Serializable]
public class SerializationWrapper<T>
{
	public List<T> target;
	public SerializationWrapper(List<T> target) => this.target = target;
}