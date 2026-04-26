using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatHelicopter : MonoBehaviour
{
	public enum State { FlyingIn, Loading, FlyingOut }
	public State currentState;

	[Header("Настройки Эвакуации")]
	public float flySpeed = 25f;          // Летает быстрее обычного
	public float loadTime = 5f;           // Долго ждет на земле
	public int maxCapacity = 10;          // Забирает огромную толпу
	public float pickupRadius = 6f;       // Радиус посадочной зоны

	[Header("Настройки Снайпера на борту")]
	public float shootRadius = 20f;       // Как далеко стреляет
	public float fireRate = 0.4f;         // Как часто стреляет (раз в 0.4 сек)
	public int sniperDamage = 15;         // Убивает почти с одного выстрела

	private Vector3 targetPos;
	private int currentLoad = 0;
	private float shootTimer = 0f;

	public void Launch(Vector3 target)
	{
		targetPos = target;

		// Спавнимся далеко в небе (высота 40, отдаление 30)
		transform.position = targetPos + new Vector3(0, 40f, -30f);

		currentState = State.FlyingIn;
		StartCoroutine(HelicopterRoutine());
	}

	private IEnumerator HelicopterRoutine()
	{
		// 1. СТРЕМИТЕЛЬНО ЛЕТИМ ВНИЗ
		while (Vector3.Distance(transform.position, targetPos) > 0.5f)
		{
			transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * Time.deltaTime);
			SniperShoot(); // Начинаем стрелять еще на подлете!
			yield return null;
		}

		// 2. ПРИЗЕМЛИЛИСЬ, ГРУЗИМ ЛЮДЕЙ И ОТСТРЕЛИВАЕМСЯ
		currentState = State.Loading;
		float timer = 0;

		while (timer < loadTime && currentLoad < maxCapacity)
		{
			// Подбираем людей
			Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);
			foreach (var hit in hits)
			{
				if (hit.CompareTag("Human"))
				{
					Destroy(hit.gameObject);
					currentLoad++;
				}
			}

			SniperShoot(); // Снайпер прикрывает посадку

			timer += Time.deltaTime;
			yield return null;
		}

		// 3. РЕЗКО УЛЕТАЕМ
		currentState = State.FlyingOut;
		Vector3 exitPos = targetPos + new Vector3(0, 50f, 30f); // Улетаем вверх и вперед

		while (Vector3.Distance(transform.position, exitPos) > 0.5f)
		{
			transform.position = Vector3.MoveTowards(transform.position, exitPos, flySpeed * Time.deltaTime);
			SniperShoot(); // Отстреливаемся при отступлении!
			yield return null;
		}

		// Спаслись!
		GameManager.Instance.AddRescuedHumans(currentLoad);
		Destroy(gameObject);
	}

	// --- ЛОГИКА СНАЙПЕРА ---
	private void SniperShoot()
	{
		shootTimer += Time.deltaTime;
		if (shootTimer >= fireRate)
		{
			Zombie targetZombie = FindClosestZombie();
			if (targetZombie != null)
			{
				targetZombie.TakeDamage(sniperDamage);
				shootTimer = 0f;

				// Рисуем крутой желтый трассер от вертолета к зомби!
				StartCoroutine(DrawTracer(transform.position, targetZombie.transform.position + Vector3.up));
			}
		}
	}

	private Zombie FindClosestZombie()
	{
		Zombie closest = null;
		float minDist = shootRadius;

		// Используем наш глобальный список зомби (очень быстро работает!)
		foreach (var zombie in Zombie.AllZombies)
		{
			if (zombie == null) continue;
			float dist = Vector3.Distance(transform.position, zombie.transform.position);
			if (dist < minDist)
			{
				minDist = dist;
				closest = zombie;
			}
		}
		return closest;
	}

	// Автоматическая отрисовка пули (без всяких префабов)
	private IEnumerator DrawTracer(Vector3 start, Vector3 end)
	{
		GameObject tracerLine = new GameObject("Tracer");
		LineRenderer lr = tracerLine.AddComponent<LineRenderer>();

		lr.material = new Material(Shader.Find("Sprites/Default")); // Светящийся материал
		lr.startColor = Color.yellow;
		lr.endColor = new Color(1, 0.5f, 0, 0); // Растворяется к концу
		lr.startWidth = 0.1f;
		lr.endWidth = 0.02f;

		lr.SetPosition(0, start);
		lr.SetPosition(1, end);

		yield return new WaitForSeconds(0.05f); // Вспышка на долю секунды
		Destroy(tracerLine);
	}
}