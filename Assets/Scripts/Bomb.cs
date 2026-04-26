using UnityEngine;

public class Bomb : MonoBehaviour
{
	[Header("Настройки")]
	public float fallSpeed = 30f;
	public float damageRadius = 6f;
	public int damage = 1000;

	[Header("Визуал")]
	public GameObject explosionPrefab;

	private Vector3 targetPos;
	private bool isFalling = false;
	private float startHeight = 40f;
	private GameObject warningCircle;
	private LineRenderer circleRenderer;

	public void Launch(Vector3 pos)
	{
		targetPos = pos;
		transform.position = new Vector3(pos.x, startHeight, pos.z);
		isFalling = true;
		CreateWarningCircle(pos);
	}

	private void CreateWarningCircle(Vector3 pos)
	{
		warningCircle = new GameObject("BombWarning");
		warningCircle.transform.position = new Vector3(pos.x, 0.1f, pos.z);

		circleRenderer = warningCircle.AddComponent<LineRenderer>();
		circleRenderer.startWidth = 0.2f;
		circleRenderer.endWidth = 0.2f;
		circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
		circleRenderer.startColor = Color.red;
		circleRenderer.endColor = Color.red;
		circleRenderer.loop = true;
		circleRenderer.positionCount = 32;
	}

	private void Update()
	{
		if (!isFalling) return;

		transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);
		float progress = 1f - (transform.position.y / startHeight);
		DrawCircle(damageRadius * progress);

		if (Vector3.Distance(transform.position, targetPos) < 0.2f)
		{
			Explode();
		}
	}

	private void DrawCircle(float radius)
	{
		if (circleRenderer == null) return;
		float angle = 0f;
		for (int i = 0; i < 32; i++)
		{
			float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
			float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
			circleRenderer.SetPosition(i, warningCircle.transform.position + new Vector3(x, 0, z));
			angle += (360f / 32f);
		}
	}

	private void Explode()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
		foreach (var hit in hits)
		{
			if (hit.CompareTag("Zombie")) hit.GetComponent<Zombie>()?.TakeDamage(damage);
			else if (hit.CompareTag("Building")) Destroy(hit.gameObject);

			// ФРЕНДЛИ ФАЙР: Убиваем своих
			else if (hit.CompareTag("Soldier") || hit.CompareTag("Sniper")) Destroy(hit.gameObject);
		}

		if (explosionPrefab != null)
		{
			GameObject boom = Instantiate(explosionPrefab, targetPos, Quaternion.identity);
			Destroy(boom, 3f);
		}
		else CreateProceduralExplosion(targetPos);

		Destroy(warningCircle);
		Destroy(gameObject);
	}

	private void CreateProceduralExplosion(Vector3 pos)
	{
		GameObject fx = new GameObject("ProceduralExplosion");
		fx.transform.position = pos;
		ParticleSystem ps = fx.AddComponent<ParticleSystem>();

		var main = ps.main;
		main.duration = 1f;
		main.startLifetime = 0.5f;
		main.startSpeed = 15f;
		main.startSize = 1.5f;
		main.startColor = new Color(1f, 0.3f, 0f);

		var emission = ps.emission;
		emission.rateOverTime = 0;
		emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 100) });

		var shape = ps.shape;
		shape.shapeType = ParticleSystemShapeType.Sphere;
		shape.radius = damageRadius / 2f;

		Destroy(fx, 2f);
	}
}