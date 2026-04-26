using UnityEngine;

public class InputManager : MonoBehaviour
{
	public static InputManager Instance;

	[Header("Оригинальные Префабы")]
	public GameObject helicopterPrefab;
	public GameObject soldierPrefab;
	public GameObject baitPrefab;
	public GameObject bombPrefab;
	public GameObject carPrefab;
	public GameObject sniperPrefab;
	public GameObject combatHelicopterPrefab;

	private Camera mainCam;
	private CardManager.CardType draggingCard = CardManager.CardType.None; // По умолчанию пусто
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

	public void StartDragging(CardManager.CardType type)
	{
		draggingCard = type;
		isDragging = true;
	}

	public void UpdateDragging(Vector2 screenPos)
	{
		if (!isDragging || draggingCard == CardManager.CardType.None) return;

		Ray ray = mainCam.ScreenPointToRay(screenPos);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			float radius = GetCardRadius(draggingCard);
			DrawRadiusCircle(hit.point, radius);

			bool isUI = screenPos.y < Screen.height * 0.25f;
			bool isBuilding = hit.collider.CompareTag("Building");
			bool canPlace = !isUI;

			if (draggingCard == CardManager.CardType.Sniper)
			{
				if (!isBuilding) canPlace = false;
			}
			else if (draggingCard != CardManager.CardType.Bomb)
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

		// Запоминаем текущую карту и СБРАСЫВАЕМ память (предохранитель!)
		CardManager.CardType cardToPlay = draggingCard;
		draggingCard = CardManager.CardType.None;

		if (Input.mousePosition.y < Screen.height * 0.25f) return false;

		Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			bool isBuilding = hit.collider.CompareTag("Building");

			if (cardToPlay == CardManager.CardType.Sniper && !isBuilding) return false;
			if (cardToPlay != CardManager.CardType.Bomb && cardToPlay != CardManager.CardType.Sniper && isBuilding) return false;

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

	private float GetCardRadius(CardManager.CardType type)
	{
		switch (type)
		{
			case CardManager.CardType.Helicopter:
				return helicopterPrefab ? helicopterPrefab.GetComponent<HelicopterController>().attractRadius : 12f;
			case CardManager.CardType.Soldier:
				return soldierPrefab ? soldierPrefab.GetComponent<Soldier>().attackRange : 12f;
			case CardManager.CardType.Bomb:
				return bombPrefab ? bombPrefab.GetComponent<Bomb>().damageRadius : 6f;
			case CardManager.CardType.Car:
				return 4f;
			case CardManager.CardType.Sniper:
				return sniperPrefab ? sniperPrefab.GetComponent<Sniper>().attackRange : 25f;
			case CardManager.CardType.CombatHelicopter:
				return 6f; // Радиус посадки тяжелого вертолета
			default: return 5f;
		}
	}

	private void ExecuteCardLogic(CardManager.CardType card, Vector3 pos)
	{
		GameObject spawnedObject = null;
		switch (card)
		{
			case CardManager.CardType.Helicopter:
				spawnedObject = Instantiate(helicopterPrefab);
				spawnedObject.GetComponent<HelicopterController>().Launch(pos);
				break;
			case CardManager.CardType.Soldier:
				Instantiate(soldierPrefab, pos, Quaternion.identity);
				break;
			case CardManager.CardType.Bait:
				Instantiate(baitPrefab, pos, Quaternion.identity);
				break;
			case CardManager.CardType.Bomb:
				spawnedObject = Instantiate(bombPrefab);
				spawnedObject.GetComponent<Bomb>().Launch(pos);
				break;
			case CardManager.CardType.Car:
				spawnedObject = Instantiate(carPrefab);
				spawnedObject.GetComponent<CarController>().Launch(pos);
				break;
			case CardManager.CardType.Sniper:
				Instantiate(sniperPrefab, pos, Quaternion.identity);
				break;
			// --- НОВЫЙ БЛОК ДЛЯ БОЕВОГО ВЕРТОЛЕТА ---
			case CardManager.CardType.CombatHelicopter:
				spawnedObject = Instantiate(combatHelicopterPrefab);
				spawnedObject.GetComponent<CombatHelicopter>().Launch(pos);
				break;
		}
	}
}