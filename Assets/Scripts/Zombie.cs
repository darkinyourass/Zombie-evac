using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour
{
	public static List<Zombie> AllZombies = new List<Zombie>();

	[Header("Настройки баланса")]
	public int hp = 30;
	public float moveSpeed = 2.0f;
	public int attackDamage = 1;
	public float attackRadius = 1.2f;

	[Header("Ссылки")]
	public GameObject zombiePrefab;

	[Header("Эффекты крови")]
	[Tooltip("Эффект крови, когда человек заражается")]
	public GameObject humanInfectionBloodEffect;

	[Tooltip("Эффект крови, когда зомби умирает")]
	public GameObject zombieDeathBloodEffect;

	[Tooltip("Смещение эффекта заражения по высоте")]
	public Vector3 humanInfectionEffectOffset = new Vector3(0f, 1f, 0f);

	[Tooltip("Смещение эффекта смерти зомби по высоте")]
	public Vector3 zombieDeathEffectOffset = new Vector3(0f, 1f, 0f);

	[Tooltip("Сколько секунд живёт visual effect")]
	public float effectLifetime = 3f;

	protected NavMeshAgent agent;

	private void Awake() => agent = GetComponent<NavMeshAgent>();
	private void OnEnable() => AllZombies.Add(this);
	private void OnDisable() => AllZombies.Remove(this);

	protected virtual void Start()
	{
		if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
			transform.position = hit.position;

		agent.speed = moveSpeed;
		StartCoroutine(Brain());
	}

	protected virtual IEnumerator Brain()
	{
		while (true)
		{
			if (agent.isOnNavMesh)
			{
				Transform target = FindTarget();
				if (target != null)
				{
					agent.SetDestination(target.position);

					if (Vector3.Distance(transform.position, target.position) <= attackRadius)
					{
						if (target.CompareTag("Human"))
						{
							Vector3 humanPos = target.position;

							SpawnHumanInfectionEffect(humanPos);

							if (zombiePrefab != null)
							{
								Instantiate(zombiePrefab, humanPos, Quaternion.identity);
							}

							Destroy(target.gameObject);
						}

						yield return new WaitForSeconds(1f);
					}
				}
			}

			yield return new WaitForSeconds(0.2f);
		}
	}

	private Transform FindTarget()
	{
		var baits = FindObjectsOfType<Bait>();
		foreach (var bait in baits)
		{
			if (bait != null && Vector3.Distance(transform.position, bait.transform.position) <= bait.attractRadius)
				return bait.transform;
		}

		Transform best = null;
		float minD = 100f;

		foreach (var h in Human.AllHumans)
		{
			if (h == null) continue;

			float d = Vector3.Distance(transform.position, h.transform.position);
			if (d < minD)
			{
				minD = d;
				best = h.transform;
			}
		}

		return best;
	}

	public virtual void TakeDamage(int damageTaken)
	{
		hp -= damageTaken;

		if (hp <= 0)
		{
			Vector3 deathPos = transform.position;
			SpawnZombieDeathEffect(deathPos);
			Destroy(gameObject);
		}
	}

	private void SpawnHumanInfectionEffect(Vector3 worldPosition)
	{
		if (humanInfectionBloodEffect == null) return;

		Vector3 spawnPos = worldPosition + humanInfectionEffectOffset;
		GameObject fx = Instantiate(humanInfectionBloodEffect, spawnPos, Quaternion.identity);
		Destroy(fx, effectLifetime);
	}

	private void SpawnZombieDeathEffect(Vector3 worldPosition)
	{
		if (zombieDeathBloodEffect == null) return;

		Vector3 spawnPos = worldPosition + zombieDeathEffectOffset;
		GameObject fx = Instantiate(zombieDeathBloodEffect, spawnPos, Quaternion.identity);
		Destroy(fx, effectLifetime);
	}
}