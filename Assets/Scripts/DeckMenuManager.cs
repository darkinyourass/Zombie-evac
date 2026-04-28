using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckMenuManager : MonoBehaviour
{
	[Header("Слоты текущей колоды (5 шт)")]
	public Image[] deckSlots;

	[Header("Инвентарь (Открытые карты)")]
	public Transform inventoryPanel;
	public GameObject inventoryCardPrefab;

	private void Start()
	{
		RefreshUI();
	}

	public void RefreshUI()
	{
		if (PlayerProfile.Instance == null) return;

		// 1. Обновляем 5 слотов колоды
		for (int i = 0; i < 5; i++)
		{
			CardData cardInSlot = PlayerProfile.Instance.currentDeck[i];
			Button btn = deckSlots[i].GetComponent<Button>();
			btn.onClick.RemoveAllListeners();

			if (cardInSlot != null)
			{
				deckSlots[i].sprite = cardInSlot.icon;
				deckSlots[i].color = Color.white;

				int slotIndex = i;
				btn.onClick.AddListener(() => RemoveFromDeck(slotIndex));
			}
			else
			{
				deckSlots[i].sprite = null;
				deckSlots[i].color = new Color(0, 0, 0, 0.5f);
			}
		}

		// 2. Очищаем инвентарь
		foreach (Transform child in inventoryPanel) Destroy(child.gameObject);

		// 3. Спавним открытые карты, которых ЕЩЕ НЕТ в колоде
		foreach (CardProgress progress in PlayerProfile.Instance.ownedCardsProgress)
		{
			CardData unlockedCard = PlayerProfile.Instance.allAvailableCards.Find(c => c.name == progress.cardId);

			if (unlockedCard != null)
			{
				bool inDeck = false;
				foreach (var d in PlayerProfile.Instance.currentDeck)
				{
					if (d != null && d.name == unlockedCard.name) inDeck = true;
				}

				if (!inDeck)
				{
					GameObject btnObj = Instantiate(inventoryCardPrefab, inventoryPanel);
					btnObj.GetComponent<Image>().sprite = unlockedCard.icon;

					Button btn = btnObj.GetComponent<Button>();
					btn.onClick.AddListener(() => AddToDeck(unlockedCard));
				}
			}
		}
	}

	private void AddToDeck(CardData card)
	{
		for (int i = 0; i < 5; i++)
		{
			if (PlayerProfile.Instance.currentDeck[i] == null)
			{
				PlayerProfile.Instance.currentDeck[i] = card;
				PlayerProfile.Instance.SaveProfile();
				RefreshUI();
				return;
			}
		}
		Debug.LogWarning("Колода полная! Сначала уберите карту.");
	}

	private void RemoveFromDeck(int slotIndex)
	{
		CardData cardToRemove = PlayerProfile.Instance.currentDeck[slotIndex];
		if (cardToRemove == null) return;

		// --- ПРОВЕРКА НА ЭВАКУАЦИЮ ---
		if (cardToRemove.category == CardCategory.Evacuation)
		{
			int evacuationCardsCount = 0;
			foreach (var c in PlayerProfile.Instance.currentDeck)
			{
				if (c != null && c.category == CardCategory.Evacuation) evacuationCardsCount++;
			}

			// Если это единственная эвакуация в колоде - удалять нельзя!
			if (evacuationCardsCount <= 1)
			{
				Debug.LogWarning("Действие отменено: В колоде должна быть хотя бы одна карта эвакуации!");
				// Здесь позже можно вызывать всплывающее окно "Нельзя убрать транспорт!"
				return;
			}
		}

		PlayerProfile.Instance.currentDeck[slotIndex] = null;
		PlayerProfile.Instance.SaveProfile();
		RefreshUI();
	}
}