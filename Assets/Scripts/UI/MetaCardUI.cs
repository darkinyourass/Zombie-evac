using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// [ГДЕ ВИСИТ]: На твоем префабе MetaCardPrefab (в папке Prefabs).
// [ЧТО НАСТРОИТЬ]: Закинь ссылки на Image и TextMeshPro внутри префаба.
public class MetaCardUI : MonoBehaviour
{
	[Header("Ссылки на UI")]
	[SerializeField] private Image cardIcon;
	[SerializeField] private TextMeshProUGUI levelText;
	[SerializeField] private TextMeshProUGUI progressText;
	[SerializeField] private Image progressBarFill;

	[Header("Цвета бара")]
	[SerializeField] private Color colorNotEnough = new Color(0.2f, 0.6f, 1f); // Синий
	[SerializeField] private Color colorEnough = Color.green; // Зеленый

	private Button btn;

	// Метод инициализации. Его будет вызывать менеджер колоды при спавне.
	public void Setup(CardData data, CardProgress progress, UnityAction onClickAction)
	{
		if (btn == null) btn = GetComponent<Button>();

		cardIcon.sprite = data.icon;

		int lvl = progress != null ? progress.currentLevel : 1;
		int shards = progress != null ? progress.collectedShards : 0;
		levelText.text = $"Lvl {lvl}";

		// Логика прогресс-бара
		bool isMaxLevel = lvl >= data.maxLevel || data.upgradeCosts.Count == 0;
		if (!isMaxLevel)
		{
			// Берем цену апгрейда из конфига (индекс на 1 меньше уровня)
			int costIndex = Mathf.Clamp(lvl - 1, 0, data.upgradeCosts.Count - 1);
			int required = data.upgradeCosts[costIndex].duplicateCardsNeeded;

			progressText.text = $"{shards}/{required}";
			progressBarFill.fillAmount = Mathf.Clamp01((float)shards / required);
			progressBarFill.color = (shards >= required) ? colorEnough : colorNotEnough;
		}
		else
		{
			progressText.text = "MAX";
			progressBarFill.fillAmount = 1f;
			progressBarFill.color = Color.yellow;
		}

		// Привязываем клик
		btn.onClick.RemoveAllListeners();
		btn.onClick.AddListener(onClickAction);
	}
}