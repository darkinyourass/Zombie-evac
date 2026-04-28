using UnityEngine;

public class InputManager : MonoBehaviour
{
	public static InputManager Instance;

	private Camera mainCam;
	// ТЕПЕРЬ МЫ ТЯНЕМ НЕ ПРОСТО ENUM, А САМУ КАРТОЧКУ (CARD DATA)
	private CardData draggingCard = null;
	private bool isDragging;
	private LineRenderer radiusCircle;

	private void Awake() => Instance = this;

	private void Start()
	{
		mainCam = Camera.main;
		GameObject circleObj = new GameObject("DragRadiusVisual");
		radiusCircle = circleObj.AddComponent<LineRenderer>();
		radiusCircle.startWidth = 0.2f;
		radiusCircle.endWidth = 0.2f;
		radiusCircle.material = new Material(Shader.Find("Sprites/Default"));
		radiusCircle.loop = true;
		radiusCircle.enabled = false;
	}

	// Изменили входящий параметр с CardType на CardData
	public void StartDragging(CardData card)
	{
		draggingCard = card;
		isDragging = true;
	}

	public void UpdateDragging(Vector2 screenPos)
	{
		if (!isDragging || draggingCard == null) return;

		Ray ray = mainCam.ScreenPointToRay(screenPos);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			float radius = GetCardRadius(draggingCard);
			DrawRadiusCircle(hit.point, radius);

			bool isUI = screenPos.y < Screen.height * 0.25f;
			bool isBuilding = hit.collider.CompareTag("Building");
			bool canPlace = !isUI;

			if (draggingCard.cardType == CardManager.CardType.Sniper)
			{
				if (!isBuilding) canPlace = false;
			}
			else if (draggingCard.cardType != CardManager.CardType.Bomb)
			{
				if (isBuilding) canPlace = false;
			}

			if (!canPlace)
			{
				radiusCircle.startColor = new Color(1, 0, 0, 0.5f);
				radiusCircle.endColor = new Color(1, 0, 0, 0.5f);
			}
			else
			{
				radiusCircle.startColor = new Color(0, 1, 0, 0.5f);
				radiusCircle.endColor = new Color(0, 1, 0, 0.5f);
			}
		}
	}

	public bool EndDragging()
	{
		if (!isDragging) return false;

		isDragging = false;
		radiusCircle.enabled = false;

		CardData cardToPlay = draggingCard;
		draggingCard = null;

		if (Input.mousePosition.y < Screen.height * 0.25f) return false;

		Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			bool isBuilding = hit.collider.CompareTag("Building");

			if (cardToPlay.cardType == CardManager.CardType.Sniper && !isBuilding) return false;
			if (cardToPlay.cardType != CardManager.CardType.Bomb && cardToPlay.cardType != CardManager.CardType.Sniper && isBuilding) return false;

			ExecuteCardLogic(cardToPlay, hit.point);

			if (GameManager.Instance.State == GameManager.GameState.Planning)
			{
				GameManager.Instance.StartGame();
			}
			return true;
		}
		return false;
	}

	private void DrawRadiusCircle(Vector3 center, float radius)
	{
		radiusCircle.enabled = true;
		radiusCircle.positionCount = 32;
		float angle = 0f;
		for (int i = 0; i < 32; i++)
		{
			float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
			float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
			radiusCircle.SetPosition(i, center + new Vector3(x, 0.2f, z));
			angle += (360f / 32f);
		}
	}

	// НОВАЯ ЛОГИКА: Берем радиус напрямую из CardData с учетом уровня игрока!
	private float GetCardRadius(CardData card)
	{
		int currentLevel = 1;
		if (PlayerProfile.Instance != null)
		{
			var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == card.name);
			if (progress != null) currentLevel = progress.currentLevel;
		}

		// Просим CardData саму посчитать радиус
		float radius = card.GetCalculatedStat(StatType.Radius, currentLevel);

		// Если радиус не назначен в Инспекторе (например, для Солдата мы еще не настроили),
		// даем базовые значения, чтобы игра не сломалась.
		if (radius <= 0)
		{
			switch (card.cardType)
			{
				case CardManager.CardType.Car: return 4f;
				case CardManager.CardType.Soldier: return 12f;
				case CardManager.CardType.Sniper: return 25f;
				case CardManager.CardType.Helicopter: return 12f;
				default: return 5f;
			}
		}
		return radius;
	}

	// НОВАЯ ЛОГИКА: Берем префаб прямо из карточки (card.cardPrefab)
	private void ExecuteCardLogic(CardData card, Vector3 pos)
	{
		if (card.cardPrefab == null)
		{
			Debug.LogError($"Префаб не назначен в CardData для {card.cardName}!");
			return;
		}

		GameObject spawnedObject = null;
		switch (card.cardType)
		{
			case CardManager.CardType.Helicopter:
				spawnedObject = Instantiate(card.cardPrefab);
				spawnedObject.GetComponent<HelicopterController>().Launch(pos);
				break;
			case CardManager.CardType.Bomb:
				spawnedObject = Instantiate(card.cardPrefab);
				spawnedObject.GetComponent<Bomb>().Launch(pos);
				break;
			case CardManager.CardType.Car:
				spawnedObject = Instantiate(card.cardPrefab);
				spawnedObject.GetComponent<CarController>().Launch(pos);
				break;
			case CardManager.CardType.CombatHelicopter:
				spawnedObject = Instantiate(card.cardPrefab);
				// Предполагаю, что у него тоже есть Launch
				spawnedObject.SendMessage("Launch", pos, SendMessageOptions.DontRequireReceiver);
				break;
			default:
				// Soldier, Bait, Sniper просто спавнятся в точке клика
				Instantiate(card.cardPrefab, pos, Quaternion.identity);
				break;
		}
	}
}