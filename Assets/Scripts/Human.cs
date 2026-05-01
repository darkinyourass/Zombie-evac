using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// [ГДЕ ВИСИТ]: На префабе человека (Human).
// [НАСТРОЙКИ]: Никакие новые ссылки в Инспекторе не нужны, все работает на базе старых.
[RequireComponent(typeof(NavMeshAgent))]
public class Human : MonoBehaviour
{
	public static List<Human> AllHumans = new List<Human>();

	[Header("Настройки баланса")]
	public float walkSpeed = 1.5f;    // Скорость спокойного шага
	public float runSpeed = 4.0f;     // Скорость паники (бег от зомби или к машине)
	public float panicRadius = 8f;    // На каком расстоянии замечает зомби

	private NavMeshAgent agent;

	// Скрываем эти переменные из Инспектора, так как они нужны только для кода
	[HideInInspector] public bool isRescuing = false;
	[HideInInspector] public Transform rescueTarget = null;

	private void Awake() => agent = GetComponent<NavMeshAgent>();
	private void OnEnable() => AllHumans.Add(this);
	private void OnDisable() => AllHumans.Remove(this);

	private void Start()
	{
		agent.speed = walkSpeed;

		// На старте не даем им случайную точку, если у нас фаза планирования
		if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Planning)
		{
			SetRandomDest();
		}
	}

	private void Update()
	{
		// --- ПУНКТ 1: Заморозка на этапе планирования ---
		if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Planning)
		{
			// Останавливаем агента, если он почему-то решил пойти
			if (agent.isOnNavMesh && !agent.isStopped) agent.isStopped = true;
			return; // Прерываем Update, чтобы они не сканировали зомби и не дергались
		}
		else
		{
			// Игра началась — "спускаем с поводка"
			if (agent.isOnNavMesh && agent.isStopped)
			{
				agent.isStopped = false;
				// Даем первую цель для прогулки, если они просто стояли
				if (!agent.hasPath) SetRandomDest();
			}
		}

		// 1. Если приехала машина/вертолет - бежим к ним!
		if (isRescuing && rescueTarget != null)
		{
			agent.speed = runSpeed;
			agent.SetDestination(rescueTarget.position);
			return;
		}

		// 2. Логика побега от зомби
		Zombie nearest = null;
		float minD = panicRadius; // Ищем зомби только внутри радиуса паники

		foreach (var z in Zombie.AllZombies)
		{
			if (z == null) continue;
			float d = Vector3.Distance(transform.position, z.transform.position);
			if (d < minD)
			{
				minD = d;
				nearest = z;
			}
		}

		if (nearest != null)
		{
			// Зомби близко! Включаем скорость бега и убегаем в противоположную сторону
			agent.speed = runSpeed;
			agent.SetDestination(transform.position + (transform.position - nearest.transform.position).normalized * 5f);
		}
		else
		{
			// Зомби нет, просто гуляем
			agent.speed = walkSpeed;
			if (!agent.pathPending && agent.remainingDistance < 0.5f)
			{
				SetRandomDest();
			}
		}
	}

	public void SetRescueTarget(Transform t)
	{
		isRescuing = true;
		rescueTarget = t;
	}

	public void CancelRescue()
	{
		isRescuing = false;
		rescueTarget = null;
	}

	private void SetRandomDest()
	{
		Vector3 rd = transform.position + Random.insideUnitSphere * 10f;
		if (NavMesh.SamplePosition(rd, out NavMeshHit h, 10f, NavMesh.AllAreas))
		{
			agent.SetDestination(h.position);
		}
	}
}