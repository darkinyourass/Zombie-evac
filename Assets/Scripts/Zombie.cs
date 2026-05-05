using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour
{
	public static List<Zombie> AllZombies = new List<Zombie>();

	[Header("Настройки")]
	public int maxHealth = 100;
	public float detectRadius = 10f; // теперь не ограничивает поиск жёстко, оставлен для совместимости
	public float attackDistance = 1.4f;
	public float attackCooldown = 1.0f;
	public float moveSpeed = 2.8f;

	[Header("Заражение")]
	public float infectDelay = 0.15f;
	public GameObject zombiePrefab;

	[Header("Хаос")]
	public ChaosSettings chaosSettings;

	protected NavMeshAgent agent;
	protected int currentHealth;
	protected bool isDead = false;
	protected bool isAttacking = false;
	protected Coroutine brainRoutine;

	private bool isDistracted = false;
	private float nextChaosCheckTime = 0f;

	protected virtual void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		currentHealth = maxHealth;
	}

	protected virtual void OnEnable()
	{
		AllZombies.Add(this);
	}

	protected virtual void OnDisable()
	{
		AllZombies.Remove(this);
	}

	protected virtual void Start()
	{
		if (agent != null)
		{
			agent.speed = moveSpeed;
		}

		brainRoutine = StartCoroutine(Brain());
	}

	protected virtual IEnumerator Brain()
	{
		WaitForSeconds wait = new WaitForSeconds(0.15f);

		while (!isDead)
		{
			if (GameManager.Instance != null &&
				(GameManager.Instance.State == GameManager.GameState.Planning ||
				 GameManager.Instance.State == GameManager.GameState.GameOver ||
				 GameManager.Instance.State == GameManager.GameState.Lose))
			{
				if (agent != null && agent.enabled && agent.isOnNavMesh)
				{
					agent.isStopped = true;
				}

				yield return wait;
				continue;
			}

			if (agent != null && agent.enabled && agent.isOnNavMesh)
			{
				agent.isStopped = false;
			}

			// Если зомби сейчас "затупил", просто пропускаем итерацию
			if (isDistracted)
			{
				yield return wait;
				continue;
			}

			Transform target = FindClosestVictim();

			if (target != null)
			{
				// Редкая потеря цели
				if (ShouldLoseTarget())
				{
					StartCoroutine(LoseTargetRoutine());
					yield return wait;
					continue;
				}

				float dist = Vector3.Distance(transform.position, target.position);

				if (dist > attackDistance)
				{
					if (agent != null && agent.enabled && agent.isOnNavMesh)
					{
						agent.SetDestination(target.position);
					}
				}
				else
				{
					if (!isAttacking)
					{
						StartCoroutine(AttackRoutine(target));
					}
				}
			}
			else
			{
				// Если никого не нашли, просто сбрасываем путь
				if (agent != null && agent.enabled && agent.isOnNavMesh)
				{
					agent.ResetPath();
				}
			}

			yield return wait;
		}
	}

	private bool ShouldLoseTarget()
	{
		if (chaosSettings == null) return false;
		if (!chaosSettings.chaosEnabled) return false;
		if (Time.time < nextChaosCheckTime) return false;

		nextChaosCheckTime = Time.time + chaosSettings.zombieLoseTargetCheckInterval;

		return Random.value < chaosSettings.zombieLoseTargetChance;
	}

	private IEnumerator LoseTargetRoutine()
	{
		isDistracted = true;

		if (agent != null && agent.enabled && agent.isOnNavMesh)
		{
			agent.ResetPath();
		}

		float duration = 0.6f;

		if (chaosSettings != null)
		{
			duration = chaosSettings.zombieLoseTargetDuration;
		}

		yield return new WaitForSeconds(duration);

		isDistracted = false;
	}

	protected virtual Transform FindClosestVictim()
	{
		Transform closest = null;
		float minDist = float.MaxValue;

		foreach (var h in Human.AllHumans)
		{
			if (h == null) continue;

			float d = Vector3.Distance(transform.position, h.transform.position);
			if (d < minDist)
			{
				minDist = d;
				closest = h.transform;
			}
		}

		foreach (var s in Scientist.AllScientists)
		{
			if (s == null) continue;

			float d = Vector3.Distance(transform.position, s.transform.position);
			if (d < minDist)
			{
				minDist = d;
				closest = s.transform;
			}
		}

		return closest;
	}

	protected virtual IEnumerator AttackRoutine(Transform target)
	{
		isAttacking = true;

		if (agent != null && agent.enabled && agent.isOnNavMesh)
		{
			agent.ResetPath();
		}

		yield return new WaitForSeconds(infectDelay);

		if (target != null && !isDead)
		{
			float dist = Vector3.Distance(transform.position, target.position);
			if (dist <= attackDistance + 0.25f)
			{
				InfectTarget(target.gameObject);
			}
		}

		yield return new WaitForSeconds(attackCooldown);
		isAttacking = false;
	}

	protected virtual void InfectTarget(GameObject targetObj)
	{
		if (targetObj == null) return;

		Human human = targetObj.GetComponent<Human>();
		if (human != null)
		{
			InfectHuman(human);
			return;
		}

		Scientist scientist = targetObj.GetComponent<Scientist>();
		if (scientist != null)
		{
			InfectScientist(scientist);
			return;
		}
	}

	protected virtual void InfectHuman(Human human)
	{
		if (human == null) return;

		Vector3 pos = human.transform.position;
		Quaternion rot = human.transform.rotation;

		Human.AllHumans.Remove(human);
		Destroy(human.gameObject);

		if (zombiePrefab != null)
		{
			Instantiate(zombiePrefab, pos, rot);
		}
	}

	protected virtual void InfectScientist(Scientist scientist)
	{
		if (scientist == null) return;

		Vector3 pos = scientist.transform.position;
		Quaternion rot = scientist.transform.rotation;

		Scientist.AllScientists.Remove(scientist);
		Destroy(scientist.gameObject);

		if (zombiePrefab != null)
		{
			Instantiate(zombiePrefab, pos, rot);
		}
	}

	public virtual void TakeDamage(int damageTaken)
	{
		if (isDead) return;

		currentHealth -= damageTaken;
		if (currentHealth <= 0)
		{
			Die();
		}
	}

	protected virtual void Die()
	{
		if (isDead) return;
		isDead = true;

		if (brainRoutine != null)
		{
			StopCoroutine(brainRoutine);
		}

		if (agent != null && agent.enabled)
		{
			agent.isStopped = true;
		}

		Destroy(gameObject);
	}
}