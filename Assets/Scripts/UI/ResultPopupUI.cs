using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPopupUI : MonoBehaviour
{
	[Header("Тексты результатов")]
	[SerializeField] private TextMeshProUGUI totalResultText;
	[SerializeField] private TextMeshProUGUI humansResultText;
	[SerializeField] private TextMeshProUGUI scientistsResultText;

	[Header("Идеальное прохождение")]
	[Tooltip("Перетащи сюда UI-объект плашки ИДЕАЛЬНО! (Сделай его выключенным по умолчанию)")]
	public GameObject perfectBadge;

	[Header("Кнопки")]
	[SerializeField] private Transform btnX2;
	[SerializeField] private GameObject btnNoThanks;

	[Header("Настройки анимаций")]
	[SerializeField] private float noThanksDelay = 1.5f;
	[SerializeField] private float pulseSpeed = 5f;
	[SerializeField] private float pulseMagnitude = 0.05f;

	[Header("Окно Лутбокса (Награда)")]
	[SerializeField] private GameObject rewardPanel;
	[SerializeField] private Image rewardCardIcon;
	[SerializeField] private TextMeshProUGUI rewardCardName;

	private Vector3 initialX2Scale;

	private void Awake()
	{
		if (btnX2 != null)
		{
			initialX2Scale = btnX2.localScale;
		}
	}

	// rescuedHumans – только обычные люди
	// rescuedTotal – люди + учёные
	// rescuedScientists – только учёные
	public void Show(int rescuedHumans, int rescuedTotal, int rescuedScientists, CardData rewardCard, bool isPerfect = false)
	{
		gameObject.SetActive(true);

		if (totalResultText != null)
			totalResultText.text = $"ВСЕГО СПАСЕНО:\n{rescuedTotal}";

		if (humansResultText != null)
			humansResultText.text = $"ЛЮДИ:\n{rescuedHumans}";

		bool levelHasScientists = GameManager.Instance != null && GameManager.Instance.totalScientists > 0;

		if (scientistsResultText != null)
		{
			scientistsResultText.gameObject.SetActive(levelHasScientists);

			if (levelHasScientists)
			{
				scientistsResultText.text = $"УЧЁНЫЕ:\n{rescuedScientists}";
			}
		}

		if (perfectBadge != null)
		{
			perfectBadge.SetActive(false);
			perfectBadge.transform.localScale = Vector3.zero;
		}

		if (btnNoThanks != null)
		{
			btnNoThanks.SetActive(false);
			CancelInvoke(nameof(ShowNoThanksButton));
			Invoke(nameof(ShowNoThanksButton), noThanksDelay);
		}

		if (isPerfect && perfectBadge != null)
		{
			perfectBadge.SetActive(true);
			perfectBadge.transform.DOScale(Vector3.one, 0.5f)
				.SetEase(Ease.OutBack)
				.SetDelay(0.3f);
		}

		if (rewardCard != null && rewardPanel != null)
		{
			rewardPanel.SetActive(true);

			if (rewardCardIcon != null)
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

			if (rewardCardName != null)
			{
				if (isNew)
				{
					rewardCardName.text = $"НОВАЯ КАРТА!\n<color=yellow>{rewardCard.cardName}</color>";
				}
				else
				{
					rewardCardName.text = $"ОСКОЛОК!\n<color=orange>{rewardCard.cardName}</color>";
				}
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
		if (rewardPanel == null) yield break;

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
		if (btnNoThanks != null)
			btnNoThanks.SetActive(true);
	}

	private void Update()
	{
		if (btnX2 != null)
		{
			float multiplier = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
			btnX2.localScale = initialX2Scale * multiplier;
		}
	}

	public void FinishLevelAndGoToMenu()
	{
		if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.CompleteCurrentLevel();
		}

		SceneManager.LoadScene("MainMenu");
	}
}