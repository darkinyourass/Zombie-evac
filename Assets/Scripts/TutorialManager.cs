using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
	public static TutorialManager Instance;

	[Header("UI Ссылки (Оверлей)")]
	public Button fullScreenBlocker;
	public GameObject dialogPanel;
	public TextMeshProUGUI dialogText;
	public Image dialogIcon;
	public RectTransform fingerPointer;

	[Header("Настройки Маски (Дырка)")]
	[Tooltip("Объект MaskContainer, внутри которого лежат 4 маски")]
	public RectTransform maskContainer;
	public RectTransform topMask;
	public RectTransform bottomMask;
	public RectTransform leftMask;
	public RectTransform rightMask;

	[Tooltip("Отступ (воздух) вокруг выделяемой кнопки в пикселях")]
	public float maskPadding = 25f;

	private Dictionary<string, RectTransform> activeTargets = new Dictionary<string, RectTransform>();
	private TutorialSequence currentSequence;
	private int currentStepIndex = 0;
	private bool isTutorialActive = false;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		fullScreenBlocker.onClick.AddListener(OnScreenClicked);
		SceneManager.sceneUnloaded += OnSceneUnloaded;

		// Автоматически настраиваем правильные якоря для масок
		SetupMaskRect(topMask);
		SetupMaskRect(bottomMask);
		SetupMaskRect(leftMask);
		SetupMaskRect(rightMask);

		CloseTutorialUI();
	}

	private void OnDestroy()
	{
		SceneManager.sceneUnloaded -= OnSceneUnloaded;
	}

	private void OnSceneUnloaded(Scene scene)
	{
		activeTargets.Clear();
	}

	public void RegisterTarget(string id, RectTransform rect)
	{
		activeTargets[id] = rect;

		if (isTutorialActive && GetCurrentStep() != null && GetCurrentStep().targetId == id)
		{
			ShowStep(GetCurrentStep());
		}
	}

	public void UnregisterTarget(string id)
	{
		if (activeTargets.ContainsKey(id)) activeTargets.Remove(id);
	}

	public void StartTutorial(TutorialSequence sequence)
	{
		if (PlayerPrefs.GetInt("TUTORIAL_DONE_" + sequence.tutorialId, 0) == 1) return;

		currentSequence = sequence;
		currentStepIndex = 0;
		isTutorialActive = true;

		fullScreenBlocker.gameObject.SetActive(true);
		ShowStep(GetCurrentStep());
	}

	private TutorialStep GetCurrentStep()
	{
		if (currentSequence == null || currentStepIndex >= currentSequence.steps.Count) return null;
		return currentSequence.steps[currentStepIndex];
	}

	private void ShowStep(TutorialStep step)
	{
		if (step == null)
		{
			FinishTutorial();
			return;
		}

		// 1. Диалог
		if (step.stepType == TutorialStepType.DialogOnly || step.stepType == TutorialStepType.DialogAndClick)
		{
			dialogPanel.SetActive(true);
			dialogText.text = step.dialogText;

			if (step.characterIcon != null)
			{
				dialogIcon.gameObject.SetActive(true);
				dialogIcon.sprite = step.characterIcon;
			}
			else dialogIcon.gameObject.SetActive(false);
		}
		else dialogPanel.SetActive(false);

		// 2. Сброс
		fingerPointer.gameObject.SetActive(false);
		maskContainer.gameObject.SetActive(false);

		// 3. Указатель и Маска
		if (step.stepType == TutorialStepType.ClickOnly || step.stepType == TutorialStepType.DialogAndClick)
		{
			if (activeTargets.TryGetValue(step.targetId, out RectTransform targetRect) && targetRect != null)
			{
				fingerPointer.gameObject.SetActive(true);
				fingerPointer.position = targetRect.position;

				if (step.useDarkMask)
				{
					maskContainer.gameObject.SetActive(true);
					FocusMaskOnTarget(targetRect);
				}
			}
			else Debug.Log($"[Tutorial] Ждем появления цели: {step.targetId}...");
		}
	}

	// --- ЛОГИКА ВЫРЕЗАНИЯ ДЫРКИ В МАСКЕ ---
	private void SetupMaskRect(RectTransform rect)
	{
		if (rect == null) return;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
	}

	private void FocusMaskOnTarget(RectTransform target)
	{
		// 1. Получаем 4 угла кнопки в мировых координатах
		Vector3[] corners = new Vector3[4];
		target.GetWorldCorners(corners);

		// 2. Переводим углы кнопки в локальные координаты нашего контейнера масок
		RectTransformUtility.ScreenPointToLocalPointInRectangle(maskContainer, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out Vector2 bottomLeft);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(maskContainer, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out Vector2 topRight);

		float targetWidth = topRight.x - bottomLeft.x;
		float targetHeight = topRight.y - bottomLeft.y;

		// Используем отступ из Инспектора
		float padding = maskPadding;
		targetWidth += padding * 2;
		targetHeight += padding * 2;
		bottomLeft.x -= padding;
		bottomLeft.y -= padding;
		topRight.x += padding;
		topRight.y += padding;

		Vector2 targetCenter = new Vector2(bottomLeft.x + targetWidth / 2f, bottomLeft.y + targetHeight / 2f);

		// Размер экрана с запасом
		float maxScreenW = 4000f;
		float maxScreenH = 4000f;

		// 3. Выстраиваем 4 стены
		topMask.sizeDelta = new Vector2(maxScreenW, maxScreenH);
		topMask.anchoredPosition = new Vector2(targetCenter.x, topRight.y + (maxScreenH / 2f));

		bottomMask.sizeDelta = new Vector2(maxScreenW, maxScreenH);
		bottomMask.anchoredPosition = new Vector2(targetCenter.x, bottomLeft.y - (maxScreenH / 2f));

		leftMask.sizeDelta = new Vector2(maxScreenW, targetHeight);
		leftMask.anchoredPosition = new Vector2(bottomLeft.x - (maxScreenW / 2f), targetCenter.y);

		rightMask.sizeDelta = new Vector2(maxScreenW, targetHeight);
		rightMask.anchoredPosition = new Vector2(topRight.x + (maxScreenW / 2f), targetCenter.y);
	}

	private void OnScreenClicked()
	{
		if (!isTutorialActive) return;

		TutorialStep step = GetCurrentStep();
		if (step == null) return;

		if (step.stepType == TutorialStepType.DialogOnly)
		{
			NextStep();
		}
		else
		{
			if (activeTargets.TryGetValue(step.targetId, out RectTransform targetRect) && targetRect != null)
			{
				Vector2 clickPos = Input.mousePosition;
				if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, clickPos, null))
				{
					Button targetBtn = targetRect.GetComponent<Button>();
					if (targetBtn != null && targetBtn.interactable) targetBtn.onClick.Invoke();
					NextStep();
				}
			}
		}
	}

	private void NextStep()
	{
		currentStepIndex++;
		if (currentStepIndex >= currentSequence.steps.Count) FinishTutorial();
		else ShowStep(GetCurrentStep());
	}

	public void FinishTutorial()
	{
		if (currentSequence != null)
		{
			PlayerPrefs.SetInt("TUTORIAL_DONE_" + currentSequence.tutorialId, 1);
			PlayerPrefs.Save();
		}
		CloseTutorialUI();
	}

	private void CloseTutorialUI()
	{
		isTutorialActive = false;
		fullScreenBlocker.gameObject.SetActive(false);
		dialogPanel.SetActive(false);
		fingerPointer.gameObject.SetActive(false);
		if (maskContainer) maskContainer.gameObject.SetActive(false);
	}
}