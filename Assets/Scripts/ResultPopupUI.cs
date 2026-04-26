using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultPopupUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI resultText;
	[SerializeField] private Transform btnX2;
	[SerializeField] private GameObject btnNoThanks;

	public void Show(int rescued, int total)
	{
		gameObject.SetActive(true);
		resultText.text = $"СПАСЕНО:\n{rescued} / {total}";

		// Кнопка NoThanks скрыта в начале
		btnNoThanks.SetActive(false);
		// Появится через 1 секунду
		Invoke(nameof(ShowNoThanksButton), 1f);
	}

	private void ShowNoThanksButton()
	{
		btnNoThanks.SetActive(true);
	}

	private void Update()
	{
		// Пульсация кнопки x2 (увеличивается и уменьшается)
		if (btnX2 != null)
		{
			float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.05f;
			btnX2.localScale = new Vector3(scale, scale, 1f);
		}
	}

	// Вызываем при нажатии на любую кнопку в попапе
	public void FinishLevelAndGoToMenu()
	{
		// Увеличиваем прогресс уровня
		int currentLevel = PlayerPrefs.GetInt("CurrentLevelIndex", 0);
		PlayerPrefs.SetInt("CurrentLevelIndex", currentLevel + 1);
		PlayerPrefs.Save();

		// Загружаем меню
		SceneManager.LoadScene("MainMenu");
	}
}