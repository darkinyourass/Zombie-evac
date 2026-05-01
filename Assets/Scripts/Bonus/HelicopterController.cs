using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

// [ГДЕ ВИСИТ]: На префабе Вертолета.
// [НАСТРОЙКИ]: В карточке Вертолета нужно добавить статы Duration (время ожидания) и Cooldown (скорость посадки).
public class HelicopterController : MonoBehaviour
{
	public enum HeliState { Landing, Loading, TakingOff }
	public HeliState currentState;

	[Header("Связь с карточкой")]
	public CardData myCardData;

	[Header("Технические настройки")]
	public float buffRadius = 15f;
	public float exitHeight = 40f;

	[Header("Визуал посадочной зоны")]
	public float landingRadius = 3f;
	public Color landingColor = new Color(0f, 1f, 0.2f, 0.5f);
	public GameObject customLandingPrefab;

	public GameObject sirenRingPrefab;
	public GameObject humanAlertPrefab;
	public float alertDuration = 2.0f;

	[Header("Ссылки")]
	public TextMeshProUGUI loadText;
	public GameObject hotWarning;

	private int maxCapacity;
	private float verticalSpeed;
	private float attractRadius;

	// НОВЫЕ ПАРАМЕТРЫ ДЛЯ СИНХРОНИЗАЦИИ С МАШИНОЙ
	private float loadTime;
	private float boardingCooldown;

	private int currentLoad = 0;
	private Vector3 targetPos;
	private GameObject landingMarker;
	private bool isTooHot = false;

	private List<GameObject> loadedHumans = new List<GameObject>();

	private void Start()
	{
		int currentLevel = 1;
		if (PlayerProfile.Instance != null && myCardData != null)
		{
			var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == myCardData.name);
			if (progress != null) currentLevel = progress.currentLevel;

			maxCapacity = (int)myCardData.GetCalculatedStat(StatType.Capacity, currentLevel);
			verticalSpeed = myCardData.GetCalculatedStat(StatType.Speed, currentLevel);
			attractRadius = myCardData.GetCalculatedStat(StatType.Radius, currentLevel);

			// Читаем новые статы
			loadTime = myCardData.GetCalculatedStat(StatType.Duration, currentLevel);
			boardingCooldown = myCardData.GetCalculatedStat(StatType.Cooldown, currentLevel);
		}

		// Дефолтные значения (Safety net)
		if (maxCapacity <= 0) maxCapacity = 6;
		if (verticalSpeed <= 0) verticalSpeed = 15f;
		if (attractRadius <= 0) attractRadius = 12f;
		if (loadTime <= 0) loadTime = 15f;
		if (boardingCooldown <= 0) boardingCooldown = 0.5f;
	}

	public void Launch(Vector3 pos)
	{
		if (maxCapacity == 0) Start();

		targetPos = pos;
		transform.position = new Vector3(pos.x, exitHeight, pos.z);
		currentState = HeliState.Landing;

		if (hotWarning != null) hotWarning.SetActive(false);
		if (loadText != null)
		{
			loadText.gameObject.SetActive(true);
			loadText.text = ""; 
		}

		if (customLandingPrefab != null)
		{
			landingMarker = Instantiate(customLandingPrefab, new Vector3(pos.x, 0.1f, pos.z), Quaternion.identity);
		}
		else
		{
			landingMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			landingMarker.transform.position = new Vector3(pos.x, 0.1f, pos.z);
			landingMarker.transform.localScale = new Vector3(landingRadius * 2, 0.01f, landingRadius * 2);
			Destroy(landingMarker.GetComponent<Collider>());

			Renderer r = landingMarker.GetComponent<Renderer>();
			r.material = new Material(Shader.Find("Sprites/Default"));
			r.material.color = landingColor;
		}
	}

	private void Update()
	{
		if (currentState == HeliState.Landing)
		{
			transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetPos.x, 1f, targetPos.z), verticalSpeed * Time.deltaTime);
			if (transform.position.y <= 1.1f) StartLoading();
		}
		else if (currentState == HeliState.TakingOff)
		{
			transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
			if (transform.position.y > exitHeight)
			{
				Destroy(gameObject);
			}
		}
	}

	private void StartLoading()
	{
		currentState = HeliState.Loading;
		if (landingMarker) Destroy(landingMarker);
		if (loadText != null) loadText.gameObject.SetActive(true);

		if (sirenRingPrefab != null)
		{
			GameObject ring = Instantiate(sirenRingPrefab, transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
			ring.GetComponent<SirenEffect>()?.Setup(attractRadius, 1.2f);
		}

		foreach (var h in Human.AllHumans)
		{
			Vector2 heliPos2D = new Vector2(transform.position.x, transform.position.z);
			Vector2 humanPos2D = new Vector2(h.transform.position.x, h.transform.position.z);

			if (Vector2.Distance(heliPos2D, humanPos2D) < attractRadius)
			{
				h.SetRescueTarget(transform);

				if (humanAlertPrefab != null)
				{
					GameObject alert = Instantiate(humanAlertPrefab, h.transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0), h.transform);
					Destroy(alert, alertDuration);
				}
			}
		}
		StartCoroutine(LoadRoutine());
	}

	private IEnumerator LoadRoutine()
	{
		float nextBoardTime = 0;
		float waitTimer = 0;

		// Логика 1 в 1 как у машины! Таймер против бесконечного ожидания.
		while (currentLoad < maxCapacity && !isTooHot && waitTimer < loadTime)
		{
			waitTimer += Time.deltaTime;

			// Проверка на зомби
			foreach (var z in Zombie.AllZombies)
			{
				if (z != null && Vector3.Distance(transform.position, z.transform.position) < 3f)
				{
					isTooHot = true;
					break;
				}
			}

			if (isTooHot) break;

			if (Time.time >= nextBoardTime)
			{
				int boarded = 0;
				foreach (var hum in Human.AllHumans)
				{
					Vector2 heliPos2D = new Vector2(transform.position.x, transform.position.z);
					Vector2 humanPos2D = new Vector2(hum.transform.position.x, hum.transform.position.z);

					if (Vector2.Distance(heliPos2D, humanPos2D) < 2.5f)
					{
						var nav = hum.GetComponent<NavMeshAgent>();
						if (nav != null) nav.enabled = false;
						hum.transform.position = new Vector3(0, -1000, 0);
						loadedHumans.Add(hum.gameObject);

						currentLoad++;
						boarded++;

						// Вертолет берет по одному за тик (можно вынести в конфиг, если надо)
						break;
					}
				}

				if (boarded > 0)
				{
				
					if (loadText) loadText.text = currentLoad.ToString();

					nextBoardTime = Time.time + boardingCooldown;
				}
			}
			yield return null;
		}

		TakeOff(isTooHot);
	}

	private void TakeOff(bool fromPanic = false)
	{
		if (fromPanic && hotWarning != null) hotWarning.SetActive(true);
		if (loadText != null) loadText.gameObject.SetActive(false);

		foreach (var h in Human.AllHumans)
		{
			if (h != null && h.rescueTarget == transform) h.CancelRescue();
		}

		currentState = HeliState.TakingOff;

		if (currentLoad > 0)
		{
			GameManager.Instance.AddRescuedHumans(currentLoad, transform.position);
			foreach (var lh in loadedHumans) { if (lh != null) Destroy(lh); }
			loadedHumans.Clear();
		}
	}
}