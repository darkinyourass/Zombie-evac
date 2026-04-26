using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Soldier : MonoBehaviour
{
	[Header("Настройки Боя")]
	public float fireRate = 1.5f;
	public float attackRange = 12f;
	public int damage = 20;
	public float lifespan = 8f;

	private float currentFireRate;
	private float buffTimer = 0f;
	private bool isExtracting = false;

	private Renderer[] allRenderers;
	private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
	private LineRenderer tracerLine;

	private void Start()
	{
		currentFireRate = fireRate;
		allRenderers = GetComponentsInChildren<Renderer>();

		foreach (var r in allRenderers)
		{
			if (r == null) continue;
			if (r.material.HasProperty("_BaseColor")) originalColors[r] = r.material.GetColor("_BaseColor");
			else if (r.material.HasProperty("_Color")) originalColors[r] = r.material.color;
		}

		tracerLine = gameObject.AddComponent<LineRenderer>();
		tracerLine.startWidth = 0.2f;
		tracerLine.endWidth = 0.05f;
		tracerLine.material = new Material(Shader.Find("Sprites/Default"));
		tracerLine.startColor = Color.yellow;
		tracerLine.endColor = new Color(1, 0.5f, 0);
		tracerLine.enabled = false;

		StartCoroutine(ShootRoutine());
	}

	private void Update()
	{
		if (isExtracting)
		{
			transform.Translate(Vector3.up * 20f * Time.deltaTime);
			return;
		}

		lifespan -= Time.deltaTime;
		if (lifespan <= 0) StartCoroutine(ExtractRoutine());

		if (buffTimer > 0)
		{
			buffTimer -= Time.deltaTime;
			if (buffTimer <= 0)
			{
				currentFireRate = fireRate;
				RestoreOriginalColors();
			}
		}
	}

	private IEnumerator ExtractRoutine()
	{
		isExtracting = true;
		tracerLine.enabled = false;
		ChangeColor(Color.white);
		yield return new WaitForSeconds(1.5f);
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

			// ПРОВЕРКА СТЕН: Пускаем луч от груди солдата до груди зомби
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
		Vector3 end = target.position + Vector3.up * 1.0f;

		// Если луч столкнулся со зданием - зомби за стеной!
		if (Physics.Linecast(start, end, out RaycastHit hit))
		{
			if (hit.collider.CompareTag("Building")) return false;
		}
		return true;
	}

	public void ApplyHeliBuff(float speedMultiplier)
	{
		currentFireRate = fireRate / speedMultiplier;
		buffTimer = 2f;
		ChangeColor(Color.green);
	}

	private void ChangeColor(Color targetColor)
	{
		foreach (var r in allRenderers)
		{
			if (r == null) continue;
			if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", targetColor);
			else if (r.material.HasProperty("_Color")) r.material.color = targetColor;
		}
	}

	private void RestoreOriginalColors()
	{
		foreach (var r in allRenderers)
		{
			if (r == null || !originalColors.ContainsKey(r)) continue;
			if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", originalColors[r]);
			else if (r.material.HasProperty("_Color")) r.material.color = originalColors[r];
		}
	}
}