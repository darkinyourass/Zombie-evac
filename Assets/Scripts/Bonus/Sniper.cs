using UnityEngine;
using System.Collections;

// [ГДЕ ВИСИТ]: На префабе Снайпера (спавнится только на зданиях).
// [НАСТРОЙКИ]: В инспекторе нужно назначить myCardData.
public class Sniper : MonoBehaviour
{
	[Header("Связь с карточкой")]
	public CardData myCardData;

	[Header("Технические настройки")]
	public float aimDuration = 1.5f;
	public float muzzleHeight = 1.5f;
	public float targetHeight = 1.0f;

	private float cooldownDelay;
	private float attackRange;
	private int damage;
	private float lifespan;

	private bool isExtracting = false;
	private LineRenderer laserLine;
	private Transform myBuilding;

	public void Init(Transform buildingTransform)
	{
		myBuilding = buildingTransform;
	}

	private void Awake()
	{
		laserLine = gameObject.AddComponent<LineRenderer>();
		laserLine.material = new Material(Shader.Find("Sprites/Default"));
		laserLine.enabled = false;
		laserLine.positionCount = 2;
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
			cooldownDelay = 2.0f;
			attackRange = 25f;
			damage = 100;
			lifespan = 15f;
		}

		if (cooldownDelay <= 0f) cooldownDelay = 2f;
		if (attackRange <= 0f) attackRange = 25f;
		if (damage <= 0) damage = 100;
		if (lifespan <= 0f) lifespan = 15f;

		StartCoroutine(SniperRoutine());
	}

	private void Update()
	{
		if (isExtracting) return;

		lifespan -= Time.deltaTime;
		if (lifespan <= 0f)
			StartCoroutine(ExtractRoutine());
	}

	private IEnumerator ExtractRoutine()
	{
		isExtracting = true;
		laserLine.enabled = false;

		float t = 0f;
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

				while (aimTimer < aimDuration)
				{
					if (target == null || !HasLineOfSight(target.transform))
					{
						targetLost = true;
						break;
					}

					float progress = aimTimer / aimDuration;

					float currentWidth = Mathf.Lerp(0.02f, 0.06f, progress);
					laserLine.startWidth = currentWidth;
					laserLine.endWidth = currentWidth;

					Color currentColor = new Color(1f, 0f, 0f, Mathf.Lerp(0.2f, 1f, 1f));
					laserLine.startColor = currentColor;
					laserLine.endColor = currentColor;

					laserLine.SetPosition(0, transform.position + Vector3.up * muzzleHeight);
					laserLine.SetPosition(1, target.transform.position + Vector3.up * targetHeight);

					aimTimer += Time.deltaTime;
					yield return null;
				}

				if (!targetLost && target != null)
				{
					laserLine.startWidth = 0.15f;
					laserLine.endWidth = 0.05f;
					laserLine.startColor = Color.yellow;
					laserLine.endColor = new Color(1f, 0.5f, 0f);

					target.TakeDamage(damage);

					yield return new WaitForSeconds(0.15f);

					laserLine.enabled = false;
					yield return new WaitForSeconds(cooldownDelay);
				}
				else
				{
					laserLine.enabled = false;
					yield return new WaitForSeconds(0.2f);
				}
			}
			else
			{
				laserLine.enabled = false;
				yield return new WaitForSeconds(0.35f);
			}
		}
	}

	private Zombie FindTarget()
	{
		Zombie best = null;
		float bestProgress = float.NegativeInfinity;

		foreach (var z in Zombie.AllZombies)
		{
			if (z == null) continue;

			float d = Vector3.Distance(transform.position, z.transform.position);
			if (d > attackRange) continue;
			if (!HasLineOfSight(z.transform)) continue;

			float progressScore = z.transform.position.z;
			if (progressScore > bestProgress)
			{
				bestProgress = progressScore;
				best = z;
			}
		}

		return best;
	}

	private bool HasLineOfSight(Transform target)
	{
		Vector3 start = transform.position + Vector3.up * muzzleHeight;
		Vector3 end = target.position + Vector3.up * targetHeight;
		Vector3 dir = end - start;
		float dist = dir.magnitude;

		if (dist <= 0.01f) return true;

		if (Physics.Raycast(start, dir.normalized, out RaycastHit hit, dist))
		{
			Zombie hitZombie = hit.collider.GetComponent<Zombie>();
			if (hitZombie != null && hitZombie.transform == target)
				return true;

			if (hit.collider.CompareTag("Building"))
			{
				if (myBuilding != null && hit.collider.transform == myBuilding)
					return true;

				return false;
			}

			if (hit.collider.transform != target)
				return false;
		}

		return true;
	}
}