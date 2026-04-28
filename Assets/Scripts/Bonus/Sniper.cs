using UnityEngine;
using System.Collections;

public class Sniper : MonoBehaviour
{
	[Header("Связь с карточкой")]
	public CardData myCardData;

	[Header("Технические настройки")]
	public float aimDuration = 1.5f; // Оставляем техническим (время прицеливания)

	// СТАТЫ ИЗ CARD DATA
	private float cooldownDelay;
	private float attackRange;
	private int damage;
	private float lifespan;

	private bool isExtracting = false;
	private LineRenderer laserLine;

	private void Start()
	{
		int currentLevel = 1;
		if (PlayerProfile.Instance != null && myCardData != null)
		{
			var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == myCardData.name);
			if (progress != null) currentLevel = progress.currentLevel;

			cooldownDelay = myCardData.GetCalculatedStat(StatType.Cooldown, currentLevel);
			attackRange = myCardData.GetCalculatedStat(StatType.Radius, currentLevel);
			damage = (int)myCardData.GetCalculatedStat(StatType.Damage, currentLevel);
			lifespan = myCardData.GetCalculatedStat(StatType.Duration, currentLevel);
		}
		else
		{
			Debug.LogWarning("У Снайпера нет CardData! Берем базу.");
			cooldownDelay = 2.0f; attackRange = 25f; damage = 100; lifespan = 15f;
		}

		laserLine = gameObject.AddComponent<LineRenderer>();
		laserLine.startWidth = 0.02f;
		laserLine.endWidth = 0.02f;
		laserLine.material = new Material(Shader.Find("Sprites/Default"));
		laserLine.startColor = new Color(1, 0, 0, 0.5f);
		laserLine.endColor = new Color(1, 0, 0, 0.5f);
		laserLine.enabled = false;

		StartCoroutine(SniperRoutine());
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
	}

	private IEnumerator ExtractRoutine()
	{
		isExtracting = true;
		laserLine.enabled = false;
		yield return new WaitForSeconds(1.5f);
		Destroy(gameObject);
	}

	private IEnumerator SniperRoutine()
	{
		while (!isExtracting)
		{
			Zombie target = FindTarget();
			if (target != null)
			{
				laserLine.enabled = true;
				float aimTimer = 0f;
				bool targetLost = false;

				while (aimTimer < aimDuration)
				{
					if (target == null || !HasLineOfSight(target.transform))
					{
						targetLost = true;
						break;
					}

					laserLine.SetPosition(0, transform.position + Vector3.up * 1.5f);
					laserLine.SetPosition(1, target.transform.position + Vector3.up);

					aimTimer += Time.deltaTime;
					yield return null;
				}

				if (!targetLost && target != null)
				{
					laserLine.startWidth = 0.1f;
					laserLine.startColor = Color.red;

					target.TakeDamage(damage);
					yield return new WaitForSeconds(0.1f);

					laserLine.startWidth = 0.02f;
					laserLine.startColor = new Color(1, 0, 0, 0.5f);
				}

				laserLine.enabled = false;
				yield return new WaitForSeconds(cooldownDelay);
			}
			else
			{
				yield return new WaitForSeconds(0.5f);
			}
		}
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
		Vector3 end = target.position + Vector3.up * 1.0f;
		if (Physics.Linecast(start, end, out RaycastHit hit))
		{
			if (hit.collider.CompareTag("Building")) return false;
		}
		return true;
	}
}