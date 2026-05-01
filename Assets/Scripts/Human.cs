using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// [ГДЕ ВИСИТ]: На префабе человека (Human).
// [НАСТРОЙКИ]: Ничего настраивать не нужно.
[RequireComponent(typeof(NavMeshAgent))]
public class Human : MonoBehaviour
{
	public static List<Human> AllHumans = new List<Human>();

	[Header("Настройки баланса")]
	public float walkSpeed = 1.5f;
	public float runSpeed = 4.0f;
	public float panicRadius = 8f;

	private NavMeshAgent agent;

	[HideInInspector] public bool isRescuing = false;
	[HideInInspector] public Transform rescueTarget = null;

	private void Awake() => agent = GetComponent<NavMeshAgent>();
	private void OnEnable() => AllHumans.Add(this);
	private void OnDisable() => AllHumans.Remove(this);

	private void Start()
	{
		agent.speed = walkSpeed;
		if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Planning)
		{
			SetRandomDest();
		}
	}

	private void Update()
	{
		// Если агент спрятан в транспорте — скрипт спит
		if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

		// Заморозка на этапе планирования
		if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Planning)
		{
			if (!agent.isStopped) agent.isStopped = true;
			return;
		}
		else if (agent.isStopped)
		{
			agent.isStopped = false;
		}

		// --- ПРИОРИТЕТ 1: ЭВАКУАЦИЯ ---
		if (isRescuing && rescueTarget != null)
		{
			agent.speed = runSpeed;
			Vector3 groundTarget = new Vector3(rescueTarget.position.x, transform.position.y, rescueTarget.position.z);

			if (Vector3.Distance(agent.destination, groundTarget) > 0.5f)
			{
				agent.SetDestination(groundTarget);
			}
			return;
		}

		// --- ПРИОРИТЕТ 2: ПОБЕГ ОТ ЗОМБИ ---
		Zombie nearest = null;
		float minD = panicRadius;

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
			agent.speed = runSpeed;
			Vector3 runDir = (transform.position - nearest.transform.position).normalized;
			Vector3 targetFlee = transform.position + runDir * 6f;

			// Проверяем, не ведет ли путь в стену
			if (NavMesh.SamplePosition(targetFlee, out NavMeshHit hit, 4f, NavMesh.AllAreas))
			{
				if (Vector3.Distance(agent.destination, hit.position) > 1f)
				{
					agent.SetDestination(hit.position);
				}
			}
			else
			{
				// Сзади тупик — ломимся в случайную сторону
				SetRandomDest();
			}
			return;
		}

		// --- ПРИОРИТЕТ 3: МИРНАЯ ЖИЗНЬ ---
		agent.speed = walkSpeed;

		// Гуляем. Если застряли или дошли до конца — выбираем новую точку
		if (!agent.pathPending)
		{
			if (!agent.hasPath || agent.remainingDistance < 0.5f || agent.pathStatus != NavMeshPathStatus.PathComplete)
			{
				SetRandomDest();
			}
		}
	}

	public void SetRescueTarget(Transform t)
	{
		isRescuing = true;
		rescueTarget = t;

		if (agent.isActiveAndEnabled && agent.isOnNavMesh)
		{
			Vector3 groundTarget = new Vector3(rescueTarget.position.x, transform.position.y, rescueTarget.position.z);
			agent.SetDestination(groundTarget);
		}
	}

	// Вызывается транспортом, когда он улетает/уезжает
	public void CancelRescue()
	{
		isRescuing = false;
		rescueTarget = null;

		// Сбрасываем путь. В следующем кадре Update сам поймет, что делать: гулять или бежать от зомби
		if (agent.isActiveAndEnabled && agent.isOnNavMesh)
		{
			agent.ResetPath();
		}
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