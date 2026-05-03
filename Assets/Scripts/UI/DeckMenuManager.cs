using UnityEngine;
using UnityEngine.UI;

// [ГДЕ ВИСИТ]: На объекте DeckPanel.
// [ЧТО НАСТРОИТЬ]:
// 1. Deck Slots: выдели 5 твоих Image (DeckSlot_1 ... 5) из ActiveDeckBlock и перетащи сюда.
// 2. Inventory Panel: перетащи сюда объект InventoryBlock.
// 3. Inventory Card Prefab: перетащи сюда твой MetaCardPrefab из папки Prefabs.
public class DeckMenuManager : MonoBehaviour
{
	[Header("Колода")]
	[SerializeField] private Image[] deckSlots; // 5 слотов

	[Header("Инвентарь")]
	[SerializeField] private Transform inventoryPanel;
	[SerializeField] private GameObject inventoryCardPrefab;

	private void Start()
	{
		// Start вызывается гарантированно ПОСЛЕ того, как все Awake отработали.
		// Значит PlayerProfile уже точно существует.
		RefreshUI();
	}

	private void OnEnable()
	{
		// Это нужно, чтобы интерфейс обновлялся, когда ты свайпаешь вкладки туда-сюда
		if (PlayerProfile.Instance != null)
		{
			RefreshUI();
		}
	}

	public void RefreshUI()
	{
		if (PlayerProfile.Instance == null) return;

		// 1. Рисуем активную колоду
		for (int i = 0; i < deckSlots.Length; i++)
		{
			CardData cardInSlot = PlayerProfile.Instance.currentDeck[i];
			Button slotBtn = deckSlots[i].GetComponent<Button>();

			// Если на слоте еще нет кнопки - добавляем (фикс для новичков)
			if (slotBtn == null) slotBtn = deckSlots[i].gameObject.AddComponent<Button>();

			slotBtn.onClick.RemoveAllListeners();

			if (cardInSlot != null)
			{
				deckSlots[i].sprite = cardInSlot.icon;
				deckSlots[i].color = Color.white;

				int indexToClear = i; // Локальная копия для замыкания (важно в C#!)
				Transform slotTransform = deckSlots[i].transform;
				CardProgress prog = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == cardInSlot.name);
				slotBtn.onClick.AddListener(() => CardPopupManager.Instance.OpenContextMenu(cardInSlot, prog, true, slotTransform));
			}
			else
			{
				deckSlots[i].sprite = null;
				deckSlots[i].color = new Color(0, 0, 0, 0.3f); // Полупрозрачный пустой слот
			}
		}

		// 2. Очищаем старые карты в инвентаре
		foreach (Transform child in inventoryPanel) Destroy(child.gameObject);

		// 3. Рисуем карты в инвентаре
		foreach (CardProgress progress in PlayerProfile.Instance.ownedCardsProgress)
		{
			CardData cardData = PlayerProfile.Instance.allAvailableCards.Find(c => c.name == progress.cardId);
			if (cardData == null) continue;

			// Проверяем, есть ли уже эта карта в колоде
			bool isEquipped = false;
			foreach (var deckCard in PlayerProfile.Instance.currentDeck)
			{
				if (deckCard != null && deckCard.name == cardData.name) isEquipped = true;
			}

			// Если карты нет в колоде - спавним её в инвентарь
			if (!isEquipped)
			{
				GameObject cardObj = Instantiate(inventoryCardPrefab, inventoryPanel);
				MetaCardUI cardUI = cardObj.GetComponent<MetaCardUI>();

				Transform cardTransform = cardObj.transform;
				cardUI.Setup(cardData, progress, () => CardPopupManager.Instance.OpenContextMenu(cardData, progress, false, cardTransform));
			}
		}
	}

	private void AddToDeck(CardData card)
	{
		for (int i = 0; i < PlayerProfile.Instance.currentDeck.Length; i++)
		{
			if (PlayerProfile.Instance.currentDeck[i] == null)
			{
				PlayerProfile.Instance.currentDeck[i] = card;
				PlayerProfile.Instance.SaveProfile();
				RefreshUI();
				return;
			}
		}
		Debug.Log("Колода заполнена! Сначала убери карту.");
	}

	private void RemoveFromDeck(int slotIndex)
	{
		PlayerProfile.Instance.currentDeck[slotIndex] = null;
		PlayerProfile.Instance.SaveProfile();
		RefreshUI();
	}
}