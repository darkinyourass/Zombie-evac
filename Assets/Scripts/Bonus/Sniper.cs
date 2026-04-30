using UnityEngine;
using System.Collections;

// [ГДЕ ВИСИТ]: На префабе Снайпера (который спавнится только на крышах).
// [НАСТРОЙКИ]: В инспекторе нужно назначить только myCardData.
public class Sniper : MonoBehaviour
{
	[Header("Связь с карточкой")]
	public CardData myCardData;

	[Header("Технические настройки")]
	public float aimDuration = 1.5f; // Сколько секунд сводится прицел

	// СТАТЫ ИЗ CARD DATA
	private float cooldownDelay;
	private float attackRange;
	private int damage;
	private float lifespan;

	private bool isExtracting = false;
	private LineRenderer laserLine;

	private void Awake()
	{
		// Инициализируем графику тут, чтобы избежать NullReference
		laserLine = gameObject.AddComponent<LineRenderer>();
		laserLine.material = new Material(Shader.Find("Sprites/Default"));
		laserLine.enabled = false;
	}

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
			cooldownDelay = 2.0f; attackRange = 25f; damage = 100; lifespan = 15f;
		}

		StartCoroutine(SniperRoutine());
	}

	private void Update()
	{
		if (isExtracting) return; // Убрали дерганый улет вверх

		lifespan -= Time.deltaTime;
		if (lifespan <= 0) StartCoroutine(ExtractRoutine());
	}

	// ФИКС №1: Красивый тактический уход (как у Солдата)
	private IEnumerator ExtractRoutine()
	{
		isExtracting = true;
		laserLine.enabled = false;

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

				// --- СОЧНЫЙ ПРИЦЕЛ (JUICE) ---
				while (aimTimer < aimDuration)
				{
					if (target == null || !HasLineOfSight(target.transform))
					{
						targetLost = true;
						break;
					}

					// Считаем прогресс от 0 до 1
					float progress = aimTimer / aimDuration;

					// Лазер становится толще (от 0.02 до 0.06)
					float currentWidth = Mathf.Lerp(0.02f, 0.06f, progress);
					laserLine.startWidth = currentWidth;
					laserLine.endWidth = currentWidth;

					// Лазер становится ярче (от прозрачного к кроваво-красному)
					Color currentColor = new Color(1, 0, 0, Mathf.Lerp(0.2f, 1f, progress));
					laserLine.startColor = currentColor;
					laserLine.endColor = currentColor;

					laserLine.SetPosition(0, transform.position + Vector3.up * 1.5f);
					laserLine.SetPosition(1, target.transform.position + Vector3.up);

					aimTimer += Time.deltaTime;
					yield return null;
				}

				if (!targetLost && target != null)
				{
					// --- ВЫСТРЕЛ (ВСПЫШКА) ---
					laserLine.startWidth = 0.15f;
					laserLine.endWidth = 0.05f;
					laserLine.startColor = Color.yellow; // Желтая вспышка от дула
					laserLine.endColor = new Color(1, 0.5f, 0); // Оранжевый на конце

					target.TakeDamage(damage);

					// Держим вспышку на экране долю секунды, чтобы глаз успел ее заметить
					yield return new WaitForSeconds(0.15f);

					laserLine.enabled = false;
					yield return new WaitForSeconds(cooldownDelay); // Полная перезарядка
				}
				else
				{
					// ФИКС №2: Цель спряталась - ищем новую быстро, без долгой перезарядки!
					laserLine.enabled = false;
					yield return new WaitForSeconds(0.2f);
				}
			}
			else
			{
				yield return new WaitForSeconds(0.5f); // Отдыхаем, пока нет целей
			}
		}
	}

	private Zombie FindTarget()
	{
		// Для снайпера в MVP "ближайший" подходит идеально, 
		// но если захотим балансить - можно будет искать зомби с MAX(хп)
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

	// ФИКС №3: Пробитие стен исправлено на Raycast (как у Солдата)
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
}