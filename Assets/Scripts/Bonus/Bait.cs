using UnityEngine;
using System.Collections.Generic;

public class Bait : MonoBehaviour
{
	public static List<Bait> AllBaits = new List<Bait>();

	[Header("Связь с карточкой")]
	public CardData myCardData; // Перетащи сюда файл CardData Приманки

	[Header("Ссылки (Внутренние)")]
	[SerializeField] private Transform rangeVisual;

	// Эти переменные теперь скрыты, они берут значения из CardData
	private float lifeTime;
	public float attractRadius;

	private void OnEnable() => AllBaits.Add(this);
	private void OnDisable() => AllBaits.Remove(this);

	private void Start()
	{
		// 1. Узнаем текущий уровень карты из профиля
		int currentLevel = 1;
		if (PlayerProfile.Instance != null && myCardData != null)
		{
			var progress = PlayerProfile.Instance.ownedCardsProgress.Find(p => p.cardId == myCardData.name);
			if (progress != null) currentLevel = progress.currentLevel;
		}

		// 2. Берем точные цифры из CardData
		if (myCardData != null)
		{
			lifeTime = myCardData.GetCalculatedStat(StatType.Duration, currentLevel);
			attractRadius = myCardData.GetCalculatedStat(StatType.Radius, currentLevel);
		}
		else
		{
			Debug.LogWarning("У приманки не назначен CardData! Используем базовые значения (6 сек, 15 м).");
			lifeTime = 6f;
			attractRadius = 15f;
		}

		// 3. Настраиваем визуал
		if (rangeVisual != null)
		{
			float trueScaleX = (attractRadius * 2) / transform.localScale.x;
			float trueScaleZ = (attractRadius * 2) / transform.localScale.z;
			rangeVisual.localScale = new Vector3(trueScaleX, 0.01f, trueScaleZ);
		}

		// 4. Запускаем таймер уничтожения
		Destroy(gameObject, lifeTime);
	}
}