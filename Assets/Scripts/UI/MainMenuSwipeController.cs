using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

// [ГДЕ ВИСИТ]: На объекте TabsContainer (внутри Container/SafeArea).
public class MainMenuSwipeController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public static bool IsSwipeLocked = false;

	[Header("Ссылки")]
	[SerializeField] private RectTransform[] navButtons;

	[Header("Настройки пружины")]
	[SerializeField] private float snapDuration = 0.4f;
	[SerializeField] private float swipeThreshold = 0.15f;
	[Range(0.05f, 0.4f)]
	[SerializeField] private float elasticity = 0.25f; // Чем меньше, тем "туже" резина

	private int currentTab = 1;
	private RectTransform rectTransform;
	private RectTransform parentRect;
	private RectTransform[] tabs;
	private float startPosition;

	private void Start()
	{
		rectTransform = GetComponent<RectTransform>();
		parentRect = transform.parent.GetComponent<RectTransform>();

		// Собираем все вкладки
		tabs = new RectTransform[transform.childCount];
		for (int i = 0; i < transform.childCount; i++)
		{
			tabs[i] = transform.GetChild(i).GetComponent<RectTransform>();
			tabs[i].anchorMin = Vector2.zero;
			tabs[i].anchorMax = Vector2.one;
			tabs[i].sizeDelta = Vector2.zero;
			tabs[i].localScale = Vector3.one;
		}

		AlignTabs();
		GoToTab(1, true);
	}

	private void AlignTabs()
	{
		float width = parentRect.rect.width;
		float overlapBuffer = 2f;

		for (int i = 0; i < tabs.Length; i++)
		{
			// Устраняем щели наложением в 2px
			tabs[i].anchoredPosition = new Vector2((i - 1) * (width - overlapBuffer / 2f), 0);
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (IsSwipeLocked) return;
		rectTransform.DOKill();
		startPosition = rectTransform.anchoredPosition.x;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (IsSwipeLocked) return;

		float width = parentRect.rect.width;
		// Считаем дельту от точки первого нажатия
		float dragDelta = eventData.position.x - eventData.pressPosition.x;

		// Лимиты: Deck (0) центрирован при X = width, Meta (2) при X = -width
		float maxX = width;
		float minX = -width;

		float targetX = startPosition + dragDelta;

		// Логика "Резины" для обоих краев
		if (targetX > maxX)
		{
			// Тянем вправо (Deck -> пустота)
			float overshot = targetX - maxX;
			targetX = maxX + (overshot * elasticity);

			// Жесткий лимит, чтобы панель не скрылась (не более 30% ширины)
			targetX = Mathf.Min(targetX, maxX + (width * 0.3f));
		}
		else if (targetX < minX)
		{
			// Тянем влево (Meta -> пустота)
			float overshot = targetX - minX;
			targetX = minX + (overshot * elasticity);

			// Жесткий лимит (не более 30% ширины)
			targetX = Mathf.Max(targetX, minX - (width * 0.3f));
		}

		rectTransform.anchoredPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (IsSwipeLocked) return;

		float width = parentRect.rect.width;
		float dragDelta = eventData.position.x - eventData.pressPosition.x;

		if (Mathf.Abs(dragDelta) > width * swipeThreshold)
		{
			if (dragDelta > 0 && currentTab > 0) currentTab--;
			else if (dragDelta < 0 && currentTab < tabs.Length - 1) currentTab++;
		}

		GoToTab(currentTab, false);
	}

	public void GoToTabFromButton(int tabIndex)
	{
		if (IsSwipeLocked) return;
		GoToTab(tabIndex, false);
	}

	private void GoToTab(int tabIndex, bool instant)
	{
		if (parentRect == null) return;

		currentTab = tabIndex;
		float width = parentRect.rect.width;
		float targetX = (1 - currentTab) * width;

		if (instant)
			rectTransform.anchoredPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);
		else
			// OutBack дает приятный пружинистый довод
			rectTransform.DOAnchorPosX(targetX, snapDuration).SetEase(Ease.OutBack, 0.6f);

		UpdateButtonsUI();
	}

	private void UpdateButtonsUI()
	{
		for (int i = 0; i < navButtons.Length; i++)
		{
			Image btnImg = navButtons[i].GetComponent<Image>();
			if (btnImg != null)
				btnImg.color = (i == currentTab) ? Color.green : Color.gray;
		}
	}
}