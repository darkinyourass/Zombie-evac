using UnityEngine;
using TMPro;
using System.Collections;

public class HelicopterController : MonoBehaviour
{
	public enum HeliState { Landing, Loading, TakingOff }
	public HeliState currentState;

	[Header("Настройки")]
	public float verticalSpeed = 15f;
	public int maxCapacity = 6;
	public float attractRadius = 12f;
	public float buffRadius = 15f;
	public float exitHeight = 40f;

	[Header("Визуал посадочной зоны")]
	public float landingRadius = 3f; // Настраиваемый размер круга (независимо от сбора людей)
	public Color landingColor = new Color(0f, 1f, 0.2f, 0.5f); // Цвет (с прозрачностью)
	[Tooltip("Закинь сюда свой красивый префаб (например, светящееся кольцо). Если пусто - скрипт нарисует круг сам.")]
	public GameObject customLandingPrefab;

	[Header("Ссылки")]
	public TextMeshProUGUI loadText;
	public GameObject hotWarning;

	private int currentLoad = 0;
	private Vector3 targetPos;
	private GameObject landingMarker;

	public void Launch(Vector3 pos)
	{
		targetPos = pos;
		transform.position = new Vector3(pos.x, exitHeight, pos.z);
		currentState = HeliState.Landing;

		if (hotWarning != null) hotWarning.SetActive(false);
		if (loadText != null) loadText.gameObject.SetActive(false);

		// СОЗДАЕМ МАРКЕР ПОСАДКИ
		if (customLandingPrefab != null)
		{
			// Если ты назначил свой крутой префаб маркера
			landingMarker = Instantiate(customLandingPrefab, new Vector3(pos.x, 0.1f, pos.z), Quaternion.identity);
		}
		else
		{
			// Если префаба нет - рисуем базовый цилиндр, но с правильным прозрачным материалом
			landingMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			landingMarker.transform.position = new Vector3(pos.x, 0.1f, pos.z);
			landingMarker.transform.localScale = new Vector3(landingRadius * 2, 0.01f, landingRadius * 2);
			Destroy(landingMarker.GetComponent<Collider>());

			// Используем шейдер Sprites/Default, он идеально работает с прозрачностью (Alpha)
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
		if (landingMarker) Destroy(landingMarker); // Убираем маркер
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
		if (landingMarker) Destroy(landingMarker); // На всякий случай удаляем маркер при панике

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