using UnityEngine;
using TMPro;
using System.Collections;

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

	[Header("Ссылки")]
	public TextMeshProUGUI loadText;
	public GameObject hotWarning;

	// СТАТЫ ИЗ CARD DATA
	private int maxCapacity;
	private float verticalSpeed;
	private float attractRadius;

	private int currentLoad = 0;
	private Vector3 targetPos;
	private GameObject landingMarker;

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
		}
		else
		{
			Debug.LogWarning("У Вертолета нет CardData! Берем базу.");
			maxCapacity = 6; verticalSpeed = 15f; attractRadius = 12f;
		}

		if (maxCapacity <= 0) maxCapacity = 6;
		if (verticalSpeed <= 0) verticalSpeed = 15f;
		if (attractRadius <= 0) attractRadius = 12f;
	}

	public void Launch(Vector3 pos)
	{
		if (maxCapacity == 0) Start();

		targetPos = pos;
		transform.position = new Vector3(pos.x, exitHeight, pos.z);
		currentState = HeliState.Landing;

		if (hotWarning != null) hotWarning.SetActive(false);
		if (loadText != null) loadText.gameObject.SetActive(false);

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
		else if (currentState == HeliState.Loading)
		{
			ApplyBuff();
			CheckDanger();
		}
		else if (currentState == HeliState.TakingOff)
		{
			transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
			if (transform.position.y > exitHeight)
			{
				GameManager.Instance.AddRescuedHumans(currentLoad);
				Destroy(gameObject);
			}
		}
	}

	private void StartLoading()
	{
		currentState = HeliState.Loading;
		if (landingMarker) Destroy(landingMarker);
		if (loadText != null) loadText.gameObject.SetActive(true);

		foreach (var h in Human.AllHumans)
		{
			if (Vector3.Distance(transform.position, h.transform.position) < attractRadius)
			{
				h.SetRescueTarget(transform);
			}
		}
		StartCoroutine(LoadRoutine());
	}

	private IEnumerator LoadRoutine()
	{
		while (currentLoad < maxCapacity)
		{
			Human h = null;
			foreach (var hum in Human.AllHumans)
			{
				if (Vector3.Distance(transform.position, hum.transform.position) < 2.5f) { h = hum; break; }
			}

			if (h != null)
			{
				Destroy(h.gameObject);
				currentLoad++;
				if (loadText) loadText.text = $"{currentLoad}/{maxCapacity}";
				yield return new WaitForSeconds(0.4f);
			}
			else
			{
				bool anyone = false;
				foreach (var hum in Human.AllHumans) if (hum.rescueTarget == transform) anyone = true;
				if (!anyone) break;
				yield return null;
			}
		}
		TakeOff();
	}

	private void CheckDanger()
	{
		foreach (var z in Zombie.AllZombies)
		{
			if (z != null && Vector3.Distance(transform.position, z.transform.position) < 3f)
			{
				TakeOff(true);
				break;
			}
		}
	}

	private void TakeOff(bool fromPanic = false)
	{
		if (fromPanic && hotWarning != null) hotWarning.SetActive(true);
		if (loadText != null) loadText.gameObject.SetActive(false);
		if (landingMarker) Destroy(landingMarker);

		foreach (var h in Human.AllHumans)
		{
			if (h != null && h.rescueTarget == transform) h.CancelRescue();
		}

		currentState = HeliState.TakingOff;
	}

	private void ApplyBuff()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, buffRadius);
		foreach (var hit in hits)
		{
			if (hit.CompareTag("Soldier")) hit.GetComponent<Soldier>()?.ApplyHeliBuff(2f);
		}
	}
}