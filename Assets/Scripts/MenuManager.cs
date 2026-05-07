using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
	[Header("UI Элементы")]
	[SerializeField] private TextMeshProUGUI globalCurrencyText;   // Это твои Люди (Софта)
	[SerializeField] private TextMeshProUGUI scientistsCurrencyText; // Это Ученые (Харда)

	private void Start()
	{
		// Подписываемся в Start, когда PlayerProfile уже 100% загружен
		if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.OnProfileUpdated += RefreshUI;
			RefreshUI(); // Обновляем текст при старте
		}
	}

	private void OnDestroy()
	{
		// Обязательно отписываемся при удалении объекта, чтобы не было ошибок
		if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.OnProfileUpdated -= RefreshUI;
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
		}
	}

	public void PlayGame()
	{
		SceneManager.LoadScene("Gameplay");
	}
}