using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckMenuManager : MonoBehaviour
{
	[Header("Слоты текущей колоды (5 шт)")]
	public Image[] deckSlots; // 5 картинок слотов

	[Header("Инвентарь (Открытые карты)")]
	public Transform inventoryPanel; // Куда спавнятся доступные карты
	public GameObject inventoryCardPrefab; // Простая кнопка с Image и Button

	[Header("База картинок")]
	public List<CardSpriteMapping> cardSprites;

	[System.Serializable]
	public struct CardSpriteMapping
	{
		public CardManager.CardType type;
		public Sprite icon;
	}

	private void Start()
	{
		RefreshUI();
	}

	public void RefreshUI()
	{
		// 1. Обновляем 5 слотов колоды
		for (int i = 0; i < 5; i++)
		{
			CardManager.CardType typeInSlot = PlayerProfile.Instance.currentDeck[i];
			Button btn = deckSlots[i].GetComponent<Button>();
			btn.onClick.RemoveAllListeners();

			if (typeInSlot != CardManager.CardType.None)
			{
				deckSlots[i].sprite = GetSprite(typeInSlot);
				deckSlots[i].color = Color.white;

				int slotIndex = i; // Обязательно для замыкания
				btn.onClick.AddListener(() => RemoveFromDeck(slotIndex));
			}
			else
			{
				deckSlots[i].sprite = null;
				deckSlots[i].color = new Color(0, 0, 0, 0.5f); // Пустой слот (полупрозрачный)
			}
		}

		// 2. Очищаем инвентарь перед обновлением
		foreach (Transform child in inventoryPanel) Destroy(child.gameObject);

		// 3. Спавним открытые карты, которых ЕЩЕ НЕТ в колоде
		foreach (CardManager.CardType unlocked in PlayerProfile.Instance.unlockedCards)
		{
			bool inDeck = false;
			foreach (var d in PlayerProfile.Instance.currentDeck) if (d == unlocked) inDeck = true;

			if (!inDeck)
			{
				GameObject btnObj = Instantiate(inventoryCardPrefab, inventoryPanel);
				btnObj.GetComponent<Image>().sprite = GetSprite(unlocked);

				Button btn = btnObj.GetComponent<Button>();
				btn.onClick.AddListener(() => AddToDeck(unlocked));
			}
		}
	}

	private void AddToDeck(CardManager.CardType card)
	{
		// Ищем первый пустой слот
		for (int i = 0; i < 5; i++)
		{
			if (PlayerProfile.Instance.currentDeck[i] == CardManager.CardType.None)
			{
				PlayerProfile.Instance.currentDeck[i] = card;
				PlayerProfile.Instance.SaveProfile();
				RefreshUI();
				return;
			}
		}
		Debug.Log("Колода полная! Сначала уберите карту.");
	}

	private void RemoveFromDeck(int slotIndex)
	{
		PlayerProfile.Instance.currentDeck[slotIndex] = CardManager.CardType.None;
		PlayerProfile.Instance.SaveProfile();
		RefreshUI();
	}

	private Sprite GetSprite(CardManager.CardType type)
	{
		foreach (var mapping in cardSprites)
		{
			if (mapping.type == type) return mapping.icon;
		}
		return null;
	}
}