using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// [ГДЕ ВИСИТ]: На объекте MenuManager в главной сцене
public class MenuManager : MonoBehaviour
{
	[Header("UI Элементы")]
	[SerializeField] private TextMeshProUGUI globalCurrencyText;   // Люди (Софта)
	[SerializeField] private TextMeshProUGUI scientistsCurrencyText; // Ученые (Харда)

	[Header("UI Энергии")]
	[SerializeField] private TextMeshProUGUI energyAmountText;
	[SerializeField] private TextMeshProUGUI energyTimerText;
	[SerializeField] private Slider energyProgressBar;

	// --- НОВОЕ: Ссылка на попап магазина для вызова по клику ---
	[SerializeField] private EnergyStorePopupUI energyStorePopup;

	private void Start()
	{
		if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.OnProfileUpdated += RefreshUI;
			PlayerProfile.Instance.OnEnergyUpdated += RefreshUI;
			RefreshUI();
		}
	}

	private void Update()
	{
		if (PlayerProfile.Instance != null && PlayerProfile.Instance.currentEnergy < PlayerProfile.Instance.maxEnergy)
		{
			if (energyTimerText != null)
			{
				float secondsLeft = PlayerProfile.Instance.GetSecondsToNextEnergy();
				int minutes = Mathf.FloorToInt(secondsLeft / 60);
				int seconds = Mathf.FloorToInt(secondsLeft % 60);
				energyTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
				if (!energyTimerText.gameObject.activeSelf) energyTimerText.gameObject.SetActive(true);
			}
		}
		else if (energyTimerText != null && energyTimerText.gameObject.activeSelf)
		{
			energyTimerText.gameObject.SetActive(false);
		}
	}

	private void OnDestroy()
	{
		if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.OnProfileUpdated -= RefreshUI;
			PlayerProfile.Instance.OnEnergyUpdated -= RefreshUI;
		}
	}

	private void RefreshUI()
	{
		if (PlayerProfile.Instance != null)
		{
			if (globalCurrencyText != null)
				globalCurrencyText.text = PlayerProfile.Instance.totalCurrency.ToString();

			if (scientistsCurrencyText != null)
				scientistsCurrencyText.text = PlayerProfile.Instance.totalScientistsCurrency.ToString();

			if (energyAmountText != null)
				energyAmountText.text = $"{PlayerProfile.Instance.currentEnergy}/{PlayerProfile.Instance.maxEnergy}";

			if (energyProgressBar != null)
			{
				energyProgressBar.maxValue = PlayerProfile.Instance.maxEnergy;
				energyProgressBar.value = PlayerProfile.Instance.currentEnergy;
			}
		}
	}

	public void PlayGame()
	{
		SceneManager.LoadScene("Gameplay");
	}

	// --- НОВОЕ: Метод для вызова попапа из UI кнопки ---
	public void OnEnergyUI_Clicked()
	{
		if (energyStorePopup != null)
		{
			energyStorePopup.Show();
		}
		else
		{
			Debug.LogWarning("Popup магазина энергии не привязан к MenuManager!");
		}
	}
}