using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour
{
	public static List<Zombie> AllZombies = new List<Zombie>();

	[Header("Настройки")]
	public int maxHealth = 100;
	public float detectRadius = 10f;
	public float attackDistance = 1.4f;
	public float attackCooldown = 1.0f;
	public float moveSpeed = 2.8f;

	[Header("Заражение")]
	public float infectDelay = 0.15f;
	public GameObject zombiePrefab;

	[Header("Хаос")]
	public ChaosSettings chaosSettings;

	[Header("Поведение у приманки")]
	[Tooltip("Радиус, в котором зомби слегка расходятся вокруг точки приманки")]
	public float crowdRadius = 0.8f;

	[Tooltip("Сила дрожания зомби вокруг приманки")]
	public float crowdShakeStrength = 0.12f;

	[Tooltip("Длительность одного цикла дрожания")]
	public float crowdShakeDuration = 0.35f;

	protected NavMeshAgent agent;
	protected int currentHealth;
	protected bool isDead = false;
	protected bool isAttacking = false;
	protected Coroutine brainRoutine;

	private bool isDistracted = false;
	private float nextChaosCheckTime = 0f;

	private Bait currentBait = null;
	private Vector3 currentBaitDestination;
	private Tween crowdShakeTween;

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
		KillCrowdTween();
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

			if (isDistracted)
			{
				yield return wait;
				continue;
			}

			Transform target;
			Vector3? overrideDestination;
			FindTargetWithBaitPriority(out target, out overrideDestination);

			if (target != null)
			{
				if (ShouldLoseTarget())
				{
					StartCoroutine(LoseTargetRoutine());
					yield return wait;
					continue;
				}

				Vector3 targetPos = overrideDestination ?? target.position;
				float dist = Vector3.Distance(transform.position, targetPos);

				if (dist > attackDistance)
				{
					if (agent != null && agent.enabled && agent.isOnNavMesh)
					{
						agent.SetDestination(targetPos);
					}

					StopCrowdBehaviorIfAny();
				}
				else
				{
					if (currentBait != null)
					{
						StartCrowdBehaviorAroundBait();
					}
					else
					{
						if (!isAttacking)
						{
							StartCoroutine(AttackRoutine(target));
						}
					}
				}
			}
			else
			{
				StopCrowdBehaviorIfAny();

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

		float duration = chaosSettings != null
			? chaosSettings.zombieLoseTargetDuration
			: 0.6f;

		yield return new WaitForSeconds(duration);
		isDistracted = false;
	}

	protected virtual void FindTargetWithBaitPriority(out Transform target, out Vector3? overrideDestination)
	{
		target = null;
		overrideDestination = null;

		currentBait = null;
		float bestDist = float.MaxValue;

		foreach (var bait in Bait.AllBaits)
		{
			if (bait == null) continue;

			Vector3 baitTargetPoint = bait.hasValidAttractPoint ? bait.attractPoint : bait.transform.position;
			float dist = Vector3.Distance(transform.position, baitTargetPoint);

			if (dist <= bait.attractRadius && dist < bestDist)
			{
				bestDist = dist;
				currentBait = bait;
			}
		}

		if (currentBait != null)
		{
			Vector3 baitTargetPoint = currentBait.hasValidAttractPoint ? currentBait.attractPoint : currentBait.transform.position;

			Vector2 offset2D = Random.insideUnitCircle * crowdRadius;
			Vector3 offset = new Vector3(offset2D.x, 0f, offset2D.y);
			currentBaitDestination = baitTargetPoint + offset;

			target = currentBait.transform;
			overrideDestination = currentBaitDestination;
			return;
		}

		currentBaitDestination = Vector3.zero;
		target = FindClosestVictim();
		overrideDestination = null;
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

	public virtual void SetBaitTarget(Bait bait)
	{
		if (bait == null) return;

		currentBait = bait;

		Vector3 baitTargetPoint = bait.hasValidAttractPoint ? bait.attractPoint : bait.transform.position;

		Vector2 offset2D = Random.insideUnitCircle * crowdRadius;
		Vector3 offset = new Vector3(offset2D.x, 0f, offset2D.y);
		currentBaitDestination = baitTargetPoint + offset;

		if (agent != null && agent.enabled && agent.isOnNavMesh)
		{
			agent.SetDestination(currentBaitDestination);
		}
	}

	private void StartCrowdBehaviorAroundBait()
	{
		if (currentBait == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
		{
			KillCrowdTween();
			return;
		}

		agent.SetDestination(currentBaitDestination);

		if (crowdShakeTween == null || !crowdShakeTween.IsActive())
		{
			crowdShakeTween = transform
				.DOShakePosition(
					crowdShakeDuration,
					strength: crowdShakeStrength,
					vibrato: 10,
					randomness: 100f,
					snapping: false,
					fadeOut: true)
				.SetLoops(-1, LoopType.Restart)
				.SetLink(gameObject);
		}
	}

	private void StopCrowdBehaviorIfAny()
	{
		currentBait = null;
		KillCrowdTween();
	}

	private void KillCrowdTween()
	{
		if (crowdShakeTween != null && crowdShakeTween.IsActive())
		{
			crowdShakeTween.Kill();
			crowdShakeTween = null;
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

		KillCrowdTween();

		if (agent != null && agent.enabled)
		{
			agent.isStopped = true;
		}

		Destroy(gameObject);
	}
}