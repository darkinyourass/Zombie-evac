using UnityEngine;
using TMPro;
using DG.Tweening;

// [ГДЕ ВИСИТ]: На префабе летящего текста (UI Canvas -> TextMeshPro).
public class FlyingText : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI textComponent;
	[SerializeField] private float flyDuration = 0.6f;

	public void Launch(string text, Vector2 startScreenPos, RectTransform target, int amount)
	{
		if (textComponent != null) textComponent.text = text;
		transform.position = startScreenPos;

		// Обязательно выводим текст поверх всех остальных элементов UI
		transform.SetAsLastSibling();

		// Летим к счетчику плавно
		transform.DOMove(target.position, flyDuration).SetEase(Ease.InOutQuad);

		// Уменьшаемся в полете и в самом конце передаем очки гейм-менеджеру
		transform.DOScale(Vector3.zero, flyDuration).SetEase(Ease.InBack).OnComplete(() =>
		{
			GameManager.Instance.OnFlyingTextReached(amount);
			Destroy(gameObject);
		});
	}
}