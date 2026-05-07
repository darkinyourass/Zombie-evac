using UnityEngine;
using UnityEngine.UI;
using TMPro;

// [ГДЕ ВИСИТ]: На объекте CardInfoPopup (в корне Canvas).
// [ЧТО НАСТРОИТЬ]: Закинь ссылки на все элементы. 
// Ссылку на DeckMenuManager тоже закинь, чтобы обновлять фон после прокачки.
public class CardInfoPopup : MonoBehaviour
{
	public static CardInfoPopup Instance;

	[Header("UI Элементы")]
	[SerializeField] private GameObject windowPanel;
	[SerializeField] private Image cardIcon;
	[SerializeField] private TextMeshProUGUI cardNameText;
	[SerializeField] private TextMeshProUGUI levelText;
	[SerializeField] private TextMeshProUGUI statsText; // ОДИН текст для всех статов
	[SerializeField] private Button upgradeBtn;
	[SerializeField] private TextMeshProUGUI upgradeCostText;
	[SerializeField] private Button closeBtn;
	[SerializeField] private Button backgroundCloseBtn; // Темный фон

	[Header("Новые поля для меты")]
	[SerializeField] private TextMeshProUGUI rarityText;
	[SerializeField] private TextMeshProUGUI categoryText;

	[Header("Связь")]
	[SerializeField] private DeckMenuManager deckManager;

	private CardData currentData;
	private CardProgress currentProgress;

	private void Awake()
	{
		if (Instance == null) Instance = this;

		// Скрываем окно на старте
		windowPanel.transform.parent.gameObject.SetActive(false);

		// Кнопки закрытия
		closeBtn.onClick.AddListener(Close);
		if (backgroundCloseBtn != null) backgroundCloseBtn.onClick.AddListener(Close);
	}

	public void Show(CardData data, CardProgress progress)
	{
		currentData = data;
		currentProgress = progress;

		cardIcon.sprite = data.icon;
		cardNameText.text = data.cardName;

		// --- НОВОЕ: Категория и Редкость ---
		if (categoryText != null) categoryText.text = $"Тип: {data.category}";

		if (rarityText != null)
		{
			rarityText.text = data.rarity.ToString();
			switch (data.rarity)
			{
				case CardRarity.Common: rarityText.color = Color.white; break;
				case CardRarity.Rare: rarityText.color = new Color(0.2f, 0.6f, 1f); break; // Синий
				case CardRarity.Epic: rarityText.color = new Color(0.7f, 0.2f, 1f); break; // Фиолетовый
				case CardRarity.Legendary: rarityText.color = new Color(1f, 0.6f, 0f); break; // Оранжевый
			}
		}

		int lvl = progress != null ? progress.currentLevel : 1;
		int shards = progress != null ? progress.collectedShards : 0;
		levelText.text = $"Уровень {lvl}";

		bool isMaxLevel = lvl >= data.maxLevel || data.upgradeCosts.Count == 0;

		// Генерируем список статов с помощью Rich Text
		statsText.text = "";
		foreach (var stat in data.stats)
		{
			float currentVal = stat.GetFloatValue(lvl);
			// Форматируем строку (убираем лишние нули после запятой)
			string valStr = currentVal % 1 == 0 ? currentVal.ToString("F0") : currentVal.ToString("F1");
			string statLine = $"{stat.statName}: {valStr}{stat.unitSuffix}";

			// Если не макс уровень - считаем зеленую прибавку
			if (!isMaxLevel)
			{
				float nextVal = stat.GetFloatValue(lvl + 1);
				float diff = nextVal - currentVal;
				if (diff > 0)
				{
					string diffStr = diff % 1 == 0 ? diff.ToString("F0") : diff.ToString("F1");
					// Магия Unity UI: тег <color=green> покрасит только этот кусок текста
					statLine += $" <color=#32CD32>+{diffStr}{stat.unitSuffix}</color>";
				}
			}
			statsText.text += statLine + "\n\n"; // Двойной перенос для красоты
		}

		// Настраиваем кнопку апгрейда
		upgradeBtn.onClick.RemoveAllListeners();
		if (!isMaxLevel)
		{
			int costIndex = Mathf.Clamp(lvl - 1, 0, data.upgradeCosts.Count - 1);
			int requiredShards = data.upgradeCosts[costIndex].duplicateCardsNeeded;
			int upgradeCost = data.upgradeCosts[costIndex].currencyCost;

			if (shards >= requiredShards)
			{
				upgradeCostText.text = $"Улучшить ({upgradeCost}$)";
				// Делаем кнопку серой, если не хватает денег
				upgradeBtn.interactable = PlayerProfile.Instance.totalCurrency >= upgradeCost;
				upgradeBtn.onClick.AddListener(() => PerformUpgrade(upgradeCost, requiredShards));
			}
			else
			{
				upgradeCostText.text = $"Карточек: {shards}/{requiredShards}";
				upgradeBtn.interactable = false; // Кнопка неактивна
			}
			upgradeBtn.gameObject.SetActive(true);
		}
		else
		{
			upgradeBtn.gameObject.SetActive(false); // Прячем кнопку на макс левеле
		}

		windowPanel.transform.parent.gameObject.SetActive(true);
	}

	private void PerformUpgrade(int cost, int shardsToSpend)
	{
		// 1. Списываем ресы и качаем уровень
		PlayerProfile.Instance.totalCurrency -= cost;
		currentProgress.collectedShards -= shardsToSpend;
		currentProgress.currentLevel++;
		PlayerProfile.Instance.SaveProfile();

		// 2. Обновляем это же окно (чтобы показать новые статы)
		Show(currentData, currentProgress);

		// 3. Обновляем UI колоды на фоне
		if (deckManager != null) deckManager.RefreshUI();
	}

	public void Close()
	{
		windowPanel.transform.parent.gameObject.SetActive(false);
	}
}