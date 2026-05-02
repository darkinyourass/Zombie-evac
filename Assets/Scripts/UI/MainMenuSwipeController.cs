using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

// [ГДЕ ВИСИТ]: На объекте MenuSwipeController.
public class MainMenuSwipeController : MonoBehaviour, IDragHandler, IEndDragHandler
{
	// Глобальный флаг. Если true - меню не реагирует на пальцы.
	public static bool IsSwipeLocked = false;

	[Header("Ссылки на UI")]
	[SerializeField] private RectTransform tabsContainer;
	[SerializeField] private RectTransform[] navButtons;

	[Header("Дизайн кнопок")]
	[SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 0.2f);
	[SerializeField] private Color inactiveColor = new Color(0.8f, 0.8f, 0.8f);
	[SerializeField] private float activeWidth = 350f;
	[SerializeField] private float inactiveWidth = 200f;

	[Header("Настройки свайпа")]
	[SerializeField] private float snapDuration = 0.3f;
	[SerializeField] private float swipeThreshold = 100f;

	private int currentTab = 1;
	private float panelWidth;

	private void Start()
	{
		panelWidth = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;

		foreach (var btn in navButtons)
		{
			if (btn.GetComponent<LayoutElement>() == null) btn.gameObject.AddComponent<LayoutElement>();
		}

		GoToTab(1, true);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (IsSwipeLocked) return; // <-- БЛОКИРОВКА СВАЙПА

		float dragDelta = eventData.position.x - eventData.pressPosition.x;
		float basePosition = (1 - currentTab) * panelWidth;
		float targetX = basePosition + dragDelta;

		targetX = Mathf.Clamp(targetX, -panelWidth, panelWidth);
		tabsContainer.anchoredPosition = new Vector2(targetX, tabsContainer.anchoredPosition.y);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (IsSwipeLocked) return; // <-- БЛОКИРОВКА СВАЙПА

		float dragDelta = eventData.position.x - eventData.pressPosition.x;

		if (Mathf.Abs(dragDelta) > swipeThreshold)
		{
			if (dragDelta > 0 && currentTab > 0) currentTab--;
			else if (dragDelta < 0 && currentTab < 2) currentTab++;
		}

		GoToTab(currentTab, false);
	}

	public void GoToTabFromButton(int tabIndex)
	{
		if (IsSwipeLocked) return; // На всякий случай блочим и кнопки
		GoToTab(tabIndex, false);
	}

	private void GoToTab(int tabIndex, bool instant)
	{
		currentTab = tabIndex;
		float targetX = (1 - currentTab) * panelWidth;

		if (instant)
		{
			tabsContainer.anchoredPosition = new Vector2(targetX, tabsContainer.anchoredPosition.y);
		}
		else
		{
			tabsContainer.DOAnchorPosX(targetX, snapDuration).SetEase(Ease.OutCubic);
		}

		for (int i = 0; i < navButtons.Length; i++)
		{
			bool isActive = (i == currentTab);

			LayoutElement le = navButtons[i].GetComponent<LayoutElement>();
			float targetW = isActive ? activeWidth : inactiveWidth;

			Image btnImg = navButtons[i].GetComponent<Image>();
			Color targetColor = isActive ? activeColor : inactiveColor;

			if (instant)
			{
				le.preferredWidth = targetW;
				if (btnImg != null) btnImg.color = targetColor;
			}
			else
			{
				DOTween.To(() => le.preferredWidth, x => le.preferredWidth = x, targetW, snapDuration);
				if (btnImg != null) btnImg.DOColor(targetColor, snapDuration);
			}
		}
	}
}