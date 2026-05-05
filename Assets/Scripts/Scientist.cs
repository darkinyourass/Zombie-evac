using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// [ГДЕ ВИСИТ]: На префабе учёного (Scientist).
[RequireComponent(typeof(NavMeshAgent))]
public class Scientist : MonoBehaviour
{
	public static List<Scientist> AllScientists = new List<Scientist>();

	[Header("Настройки баланса")]
	public float walkSpeed = 1.5f;
	public float runSpeed = 4.0f;
	public float panicRadius = 8f;

	[Header("Хаос")]
	public ChaosSettings chaosSettings;

	private NavMeshAgent agent;

	[HideInInspector] public bool isRescuing = false;
	[HideInInspector] public Transform rescueTarget = null;

	private bool isPanicking = false;
	private float panicEndTime = 0f;
	private float nextPanicCheckTime = 0f;

	private void Awake() => agent = GetComponent<NavMeshAgent>();
	private void OnEnable() => AllScientists.Add(this);
	private void OnDisable() => AllScientists.Remove(this);

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
		if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

		if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Planning)
		{
			if (!agent.isStopped) agent.isStopped = true;
			return;
		}
		else if (agent.isStopped)
		{
			agent.isStopped = false;
		}

		// Если сейчас в панике — просто ждём окончания
		if (isPanicking)
		{
			agent.speed = runSpeed;

			if (Time.time >= panicEndTime)
			{
				isPanicking = false;
			}

			return;
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

			// Редкая паника
			if (ShouldPanic(minD))
			{
				StartPanicMove();
				return;
			}

			Vector3 runDir = (transform.position - nearest.transform.position).normalized;
			Vector3 targetFlee = transform.position + runDir * 6f;

			if (NavMesh.SamplePosition(targetFlee, out NavMeshHit hit, 4f, NavMesh.AllAreas))
			{
				if (Vector3.Distance(agent.destination, hit.position) > 1f)
				{
					agent.SetDestination(hit.position);
				}
			}
			else
			{
				SetRandomDest();
			}

			return;
		}

		// --- ПРИОРИТЕТ 3: МИРНАЯ ЖИЗНЬ ---
		agent.speed = walkSpeed;

		if (!agent.pathPending)
		{
			if (!agent.hasPath || agent.remainingDistance < 0.5f || agent.pathStatus != NavMeshPathStatus.PathComplete)
			{
				SetRandomDest();
			}
		}
	}

	private bool ShouldPanic(float nearestZombieDistance)
	{
		if (chaosSettings == null) return false;
		if (!chaosSettings.chaosEnabled) return false;
		if (Time.time < nextPanicCheckTime) return false;
		if (nearestZombieDistance > chaosSettings.humanPanicTriggerRadius) return false;

		nextPanicCheckTime = Time.time + chaosSettings.humanPanicCheckInterval;

		return Random.value < chaosSettings.humanPanicChance;
	}

	private void StartPanicMove()
	{
		isPanicking = true;

		float duration = 0.4f;
		float moveDistance = 3f;

		if (chaosSettings != null)
		{
			duration = chaosSettings.humanPanicDuration;
			moveDistance = chaosSettings.humanPanicMoveDistance;
		}

		panicEndTime = Time.time + duration;

		Vector2 random2D = Random.insideUnitCircle.normalized;
		Vector3 panicDir = new Vector3(random2D.x, 0f, random2D.y);
		Vector3 panicTarget = transform.position + panicDir * moveDistance;

		if (NavMesh.SamplePosition(panicTarget, out NavMeshHit hit, 4f, NavMesh.AllAreas))
		{
			agent.SetDestination(hit.position);
		}
		else
		{
			SetRandomDest();
		}
	}

	public void SetRescueTarget(Transform t)
	{
		isRescuing = true;
		rescueTarget = t;
		isPanicking = false;

		if (agent.isActiveAndEnabled && agent.isOnNavMesh)
		{
			Vector3 groundTarget = new Vector3(rescueTarget.position.x, transform.position.y, rescueTarget.position.z);
			agent.SetDestination(groundTarget);
		}
	}

	public void CancelRescue()
	{
		isRescuing = false;
		rescueTarget = null;

		if (agent.isActiveAndEnabled && agent.isOnNavMesh)
		{
			agent.ResetPath();
		}
	}

	private void SetRandomDest()
	{
		Vector3 rd = transform.position + Random.insideUnitSphere * 10f;
		rd.y = transform.position.y;

		if (NavMesh.SamplePosition(rd, out NavMeshHit h, 10f, NavMesh.AllAreas))
		{
			agent.SetDestination(h.position);
		}
	}
}