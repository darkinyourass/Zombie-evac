using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public CardManager.CardType cardType;
	public float cost;
	public float cooldownTime = 3f;

	[Header("Ссылки")]
	public Image cardImage;
	public Image cooldownFill;
	public TextMeshProUGUI timerText;

	private CanvasGroup canvasGroup;
	private Transform originalParent;
	private int originalSiblingIndex;
	private Vector2 originalAnchoredPos;
	private RectTransform rectTransform;

	private bool isOnCooldown = false;
	private float currentCooldown = 0f;

	// ПРЕДОХРАНИТЕЛЬ
	private bool isCurrentlyDragging = false;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		rectTransform = GetComponent<RectTransform>();
	}

	private void Start()
	{
		if (cooldownFill != null) cooldownFill.fillAmount = 0;
		if (timerText != null) timerText.text = "";
	}

	private void Update()
	{
		if (isOnCooldown)
		{
			currentCooldown -= Time.deltaTime;

			if (cooldownFill != null)
			{
				cooldownFill.gameObject.SetActive(true);
				cooldownFill.fillAmount = currentCooldown / cooldownTime;
			}

			if (timerText != null)
			{
				timerText.gameObject.SetActive(true);
				timerText.text = currentCooldown.ToString("F1");
			}

			if (currentCooldown <= 0)
			{
				isOnCooldown = false;
				if (cooldownFill != null) cooldownFill.fillAmount = 0;
				if (timerText != null) timerText.text = "";
				if (cardImage != null) cardImage.color = Color.white;
			}
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (isOnCooldown || EnergyManager.Instance.CurrentEnergy < cost) return;

		// Запоминаем всё ДО отрыва
		originalParent = transform.parent;
		originalSiblingIndex = transform.GetSiblingIndex();
		originalAnchoredPos = rectTransform.anchoredPosition;

		transform.SetParent(transform.root, false); // false сохраняет масштаб
		canvasGroup.blocksRaycasts = false;
		if (cardImage != null) cardImage.color = new Color(1, 1, 1, 0.5f);

		isCurrentlyDragging = true; // СТАРТ УСПЕШЕН
		InputManager.Instance.StartDragging(cardType);
	}

	public void OnDrag(PointerEventData eventData)
	{
		// Если старт не удался - игнорируем любые движения мыши!
		if (!isCurrentlyDragging) return;

		rectTransform.position = eventData.position;
		InputManager.Instance.UpdateDragging(eventData.position);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		// Если старт не удался - ничего не делаем!
		if (!isCurrentlyDragging) return;
		isCurrentlyDragging = false; // Сбрасываем флаг

		// ЖЕЛЕЗОБЕТОННЫЙ ВОЗВРАТ
		transform.SetParent(originalParent, false);
		transform.SetSiblingIndex(originalSiblingIndex);
		rectTransform.anchoredPosition = originalAnchoredPos;

		canvasGroup.blocksRaycasts = true;
		if (cardImage != null) cardImage.color = Color.white;

		bool success = InputManager.Instance.EndDragging();

		if (success)
		{
			EnergyManager.Instance.TrySpendEnergy(cost);
			StartCooldown();
		}
	}

	public void StartCooldown()
	{
		if (!isOnCooldown)
		{
			isOnCooldown = true;
			currentCooldown = cooldownTime;
			if (cardImage != null) cardImage.color = Color.gray;
		}
	}
}