using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Обязательно добавляем для работы с UI текстом

public class MenuManager : MonoBehaviour
{
	[Header("UI Элементы")]
	[SerializeField] private TextMeshProUGUI globalCurrencyText; // Ссылка на текст счетчика

	private void Start()
	{
		// Как только загружается меню, достаем нашу валюту из памяти
		int totalSavedGlobal = PlayerPrefs.GetInt("TotalRescuedCurrency", 0);

		// Если мы привязали текст в инспекторе, выводим цифру
		if (globalCurrencyText != null)
		{
			globalCurrencyText.text = totalSavedGlobal.ToString();
		}
	}

	public void PlayGame()
	{
		SceneManager.LoadScene("Gameplay");
	}
}