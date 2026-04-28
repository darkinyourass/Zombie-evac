using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultPopupUI : MonoBehaviour
{
	[Header("Тексты результатов")]
	[SerializeField] private TextMeshProUGUI resultText;

	[Header("Кнопки")]
	[SerializeField] private Transform btnX2;
	[SerializeField] private GameObject btnNoThanks;

	[Header("Настройки анимаций")]
	[SerializeField] private float noThanksDelay = 1.5f; // Задержка появления "Нет, спасибо"
	[SerializeField] private float pulseSpeed = 5f;      // Скорость пульсации х2
	[SerializeField] private float pulseMagnitude = 0.05f; // Сила пульсации х2 (0.05 = 5%)

	[Header("Окно Лутбокса (Награда)")]
	[SerializeField] private GameObject rewardPanel;
	[SerializeField] private Image rewardCardIcon;
	[SerializeField] private TextMeshProUGUI rewardCardName;

	private Vector3 initialX2Scale; // Запоминаем изначальный размер кнопки х2

	private void Awake()
	{
		// Запоминаем размер кнопки, который ты настроил в Инспекторе
		if (btnX2 != null)
		{
			initialX2Scale = btnX2.localScale;
		}
	}

	public void Show(int rescued, int total, CardData rewardCard)
	{
		gameObject.SetActive(true);
		resultText.text = $"СПАСЕНО:\n{rescued} / {total}";

		btnNoThanks.SetActive(false);
		// Используем новую переменную для задержки
		Invoke(nameof(ShowNoThanksButton), noThanksDelay);

		// --- ЛОГИКА ЛУТБОКСА ---
		if (rewardCard != null && rewardPanel != null)
		{
			rewardPanel.SetActive(true);
			rewardCardIcon.sprite = rewardCard.icon;

			bool isNew = true;
			if (PlayerProfile.Instance != null)
			{
				var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == rewardCard.name);
				if (progress != null && (progress.currentLevel > 1 || progress.collectedShards > 0))
				{
					isNew = false;
				}
			}

			if (isNew)
			{
				rewardCardName.text = $"НОВАЯ КАРТА!\n<color=yellow>{rewardCard.cardName}</color>";
			}
			else
			{
				rewardCardName.text = $"ОСКОЛОК!\n<color=orange>{rewardCard.cardName}</color>";
			}

			StartCoroutine(AnimateRewardPopup());
		}
		else if (rewardPanel != null)
		{
			rewardPanel.SetActive(false);
		}
	}

	private IEnumerator AnimateRewardPopup()
	{
		rewardPanel.transform.localScale = Vector3.zero;
		float time = 0f;
		float duration = 0.5f;

		while (time < duration)
		{
			time += Time.deltaTime;
			float t = time / duration;
			float scale = Mathf.LerpUnclamped(0f, 1.2f, t) - (t > 0.8f ? (t - 0.8f) * 1f : 0f);
			rewardPanel.transform.localScale = new Vector3(scale, scale, 1f);
			yield return null;
		}
		rewardPanel.transform.localScale = Vector3.one;
	}

	private void ShowNoThanksButton()
	{
		if (btnNoThanks != null) btnNoThanks.SetActive(true);
	}

	private void Update()
	{
		if (btnX2 != null)
		{
			// Пульсируем относительно ИЗНАЧАЛЬНОГО размера
			float multiplier = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
			btnX2.localScale = initialX2Scale * multiplier;
		}
	}

	public void FinishLevelAndGoToMenu()
	{
		int currentLevel = PlayerPrefs.GetInt("CurrentLevelIndex", 0);
		PlayerPrefs.SetInt("CurrentLevelIndex", currentLevel + 1);
		PlayerPrefs.Save();

		SceneManager.LoadScene("MainMenu");
	}
}