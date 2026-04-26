using UnityEngine;
using System.Collections;

public class Sniper : MonoBehaviour
{
	[Header("Настройки Снайпера")]
	public float aimDuration = 1.5f; // Сколько секунд целится (светит лазером)
	public float cooldownDelay = 2.0f; // Пауза после выстрела
	public float attackRange = 25f;    // Огромный радиус!
	public int damage = 100;           // Убивает любого зомби сразу
	public float lifespan = 15f;       // Сколько живет на крыше

	private bool isExtracting = false;
	private LineRenderer laserLine;

	private void Start()
	{
		// Создаем лазерный прицел
		laserLine = gameObject.AddComponent<LineRenderer>();
		laserLine.startWidth = 0.02f; // Тонкий красный луч
		laserLine.endWidth = 0.02f;
		laserLine.material = new Material(Shader.Find("Sprites/Default"));
		laserLine.startColor = new Color(1, 0, 0, 0.5f); // Прозрачный красный
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
				// ФАЗА 1: ПРИЦЕЛИВАНИЕ
				laserLine.enabled = true;
				float aimTimer = 0f;
				bool targetLost = false;

				while (aimTimer < aimDuration)
				{
					// Если зомби умер или зашел за стену во время прицеливания - сбрасываем прицел
					if (target == null || !HasLineOfSight(target.transform))
					{
						targetLost = true;
						break;
					}

					// Ведем лазер за зомби
					laserLine.SetPosition(0, transform.position + Vector3.up * 1.5f);
					laserLine.SetPosition(1, target.transform.position + Vector3.up);

					aimTimer += Time.deltaTime;
					yield return null;
				}

				// ФАЗА 2: ВЫСТРЕЛ
				if (!targetLost && target != null)
				{
					// Делаем лазер жирным и ярким на долю секунды
					laserLine.startWidth = 0.1f;
					laserLine.startColor = Color.red;

					target.TakeDamage(damage);
					yield return new WaitForSeconds(0.1f);

					// Возвращаем тонкий лазер
					laserLine.startWidth = 0.02f;
					laserLine.startColor = new Color(1, 0, 0, 0.5f);
				}

				laserLine.enabled = false;

				// ФАЗА 3: ПЕРЕЗАРЯДКА
				yield return new WaitForSeconds(cooldownDelay);
			}
			else
			{
				yield return new WaitForSeconds(0.5f); // Ищем цель
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