using UnityEngine;
using System.Collections.Generic;

public class Bait : MonoBehaviour
{
	public static List<Bait> AllBaits = new List<Bait>();

	[Header("Настройки")]
	[SerializeField] private float lifeTime = 6f;
	public float attractRadius = 15f;
	[SerializeField] private Transform rangeVisual;

	private void OnEnable() => AllBaits.Add(this);
	private void OnDisable() => AllBaits.Remove(this);

	private void Start()
	{
		if (rangeVisual != null)
		{
			// Умный расчет: делим нужный размер на масштаб родителя, 
			// чтобы визуальный круг всегда 100% совпадал с зоной притяжения.
			float trueScaleX = (attractRadius * 2) / transform.localScale.x;
			float trueScaleZ = (attractRadius * 2) / transform.localScale.z;

			rangeVisual.localScale = new Vector3(trueScaleX, 0.01f, trueScaleZ);
		}

		Destroy(gameObject, lifeTime);
	}
}