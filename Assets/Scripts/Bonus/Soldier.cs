using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// [ГДЕ ВИСИТ]: На префабе Солдата.
// [НАСТРОЙКИ]: Назначить только myCardData.
public class Soldier : MonoBehaviour
{
	[Header("Связь с карточкой")]
	public CardData myCardData;

	private float fireRate;
	private float attackRange;
	private int damage;
	private float lifespan;

	private float currentFireRate;
	// private float buffTimer = 0f; // <-- ОТКЛЮЧЕНО
	private bool isExtracting = false;

	private Renderer[] allRenderers;
	// private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>(); // <-- ОТКЛЮЧЕНО
	private LineRenderer tracerLine;

	private void Awake()
	{
		allRenderers = GetComponentsInChildren<Renderer>();

		/* --- ОТКЛЮЧЕНО ДЛЯ MVP: Логика кеширования цветов для баффа ---
		foreach (var r in allRenderers)
		{
			if (r == null) continue;
			if (r.material.HasProperty("_BaseColor")) originalColors[r] = r.material.GetColor("_BaseColor");
			else if (r.material.HasProperty("_Color")) originalColors[r] = r.material.color;
		}
		*/

		tracerLine = gameObject.AddComponent<LineRenderer>();
		tracerLine.startWidth = 0.2f;
		tracerLine.endWidth = 0.05f;
		tracerLine.material = new Material(Shader.Find("Sprites/Default"));
		tracerLine.startColor = Color.yellow;
		tracerLine.endColor = new Color(1, 0.5f, 0);
		tracerLine.enabled = false;
	}

	private void Start()
	{
		int currentLevel = 1;
		if (PlayerProfile.Instance != null && myCardData != null)
		{
			var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == myCardData.name);
			if (progress != null) currentLevel = progress.currentLevel;

			fireRate = myCardData.GetCalculatedStat(StatType.FireRate, currentLevel);
			attackRange = myCardData.GetCalculatedStat(StatType.Radius, currentLevel);
			damage = (int)myCardData.GetCalculatedStat(StatType.Damage, currentLevel);
			lifespan = myCardData.GetCalculatedStat(StatType.Duration, currentLevel);
		}
		else
		{
			fireRate = 1.5f; attackRange = 12f; damage = 20; lifespan = 8f;
		}

		currentFireRate = fireRate;
		StartCoroutine(ShootRoutine());
	}

	private void Update()
	{
		if (isExtracting) return;

		lifespan -= Time.deltaTime;
		if (lifespan <= 0) StartCoroutine(ExtractRoutine());

		/* --- ОТКЛЮЧЕНО ДЛЯ MVP ---
		if (buffTimer > 0)
		{
			buffTimer -= Time.deltaTime;
			if (buffTimer <= 0)
			{
				currentFireRate = fireRate;
				RestoreOriginalColors();
			}
		}
		*/
	}

	private IEnumerator ExtractRoutine()
	{
		isExtracting = true;
		tracerLine.enabled = false;

		var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
		if (agent != null) agent.enabled = false;

		var col = GetComponent<Collider>();
		if (col != null) col.enabled = false;

		float t = 0;
		Vector3 startScale = transform.localScale;
		while (t < 1f)
		{
			t += Time.deltaTime * 3f;
			transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
			yield return null;
		}

		Destroy(gameObject);
	}

	private IEnumerator ShootRoutine()
	{
		while (!isExtracting)
		{
			Zombie target = FindTarget();
			if (target != null)
			{
				target.TakeDamage(damage);

				tracerLine.SetPosition(0, transform.position + Vector3.up * 1.5f);
				tracerLine.SetPosition(1, target.transform.position + Vector3.up);
				tracerLine.enabled = true;

				StartCoroutine(HideTracer());
			}
			yield return new WaitForSeconds(currentFireRate);
		}
	}

	private IEnumerator HideTracer()
	{
		yield return new WaitForSeconds(0.2f);
		tracerLine.enabled = false;
	}

	private Zombie FindTarget()
	{
		Zombie best = null; float minD = attackRange;
		foreach (var z in Zombie.AllZombies)
		{
			if (z == null) continue;
			float d = Vector3.Distance(transform.position, z.transform.position);

			if (d < minD && HasLineOfSight(z.transform))
			{
				minD = d; best = z;
			}
		}
		return best;
	}

	private bool HasLineOfSight(Transform target)
	{
		Vector3 start = transform.position + Vector3.up * 1.5f;
		Vector3 dir = (target.position + Vector3.up * 1.0f) - start;

		if (Physics.Raycast(start, dir.normalized, out RaycastHit hit, dir.magnitude))
		{
			if (hit.collider.CompareTag("Building")) return false;
		}
		return true;
	}

	/* --- ОТКЛЮЧЕНО ДЛЯ MVP: Методы перекраски ---
	public void ApplyHeliBuff(float speedMultiplier)
	{
		currentFireRate = fireRate / speedMultiplier;
		buffTimer = 2f;
		ChangeColor(Color.green);
	}

	private void ChangeColor(Color targetColor)
	{
		if (allRenderers == null) return;
		foreach (var r in allRenderers)
		{
			if (r == null) continue;
			if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", targetColor);
			else if (r.material.HasProperty("_Color")) r.material.color = targetColor;
		}
	}

	private void RestoreOriginalColors()
	{
		if (allRenderers == null) return;
		foreach (var r in allRenderers)
		{
			if (r == null || !originalColors.ContainsKey(r)) continue;
			if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", originalColors[r]);
			else if (r.material.HasProperty("_Color")) r.material.color = originalColors[r];
		}
	}
	*/
}