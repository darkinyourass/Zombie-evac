using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public enum GameState { Planning, Playing, SuddenDeath, GameOver }
	public GameState State = GameState.Planning;

	[Header("Статистика уровня")]
	public int totalHumans;
	public int rescuedHumans;

	private float currentTimer;

	private void Awake() => Instance = this;

	public void SetTotalHumans(int count)
	{
		totalHumans = count;
		rescuedHumans = 0;
	}

	public void AddRescuedHumans(int count)
	{
		rescuedHumans += count;
		// ГОВОРИМ ИНТЕРФЕЙСУ ОБНОВИТЬ ЦИФРЫ
		UIManager.Instance.UpdateRescuedCount(rescuedHumans, totalHumans);
	}

	public void SetupTimer(float time)
	{
		currentTimer = time;
		UIManager.Instance.UpdateTimer(currentTimer, true);
	}

	public void StartGame()
	{
		if (State != GameState.Planning) return;
		State = GameState.Playing;
		LevelManager.Instance.StartInitialSpawns();
	}

	private void Update()
	{
		if (State == GameState.Playing)
		{
			currentTimer -= Time.deltaTime;
			UIManager.Instance.UpdateTimer(currentTimer, false);
			if (currentTimer <= 0)
			{
				State = GameState.SuddenDeath;
				// ЗАПУСКАЕМ ВСЕ СОЧНЫЕ ЭФФЕКТЫ
				LevelManager.Instance.StartSuddenDeath();
				UIManager.Instance.ShowNightPopup();
			}
		}

		if (State == GameState.Playing || State == GameState.SuddenDeath)
		{
			if (Human.AllHumans.Count == 0) EndLevel();
		}
	}

	public void EndLevel()
	{
		State = GameState.GameOver;

		// 1. Добавляем спасенных в банк валюты
		PlayerProfile.Instance.totalCurrency += rescuedHumans;

		// 2. Проверяем лутбокс за первое прохождение
		// (Используем имя уровня из LevelData как уникальный ID)
		string levelId = "LevelPassed_" + LevelManager.Instance.currentData.name;
		int hasPassedBefore = PlayerPrefs.GetInt(levelId, 0);

		if (hasPassedBefore == 0)
		{
			// УРОВЕНЬ ПРОЙДЕН ВПЕРВЫЕ!
			PlayerPrefs.SetInt(levelId, 1);

			// Выдаем валюту из конфига уровня
			PlayerProfile.Instance.totalCurrency += LevelManager.Instance.currentData.currencyReward;

			// Выдаем карту из конфига уровня
			if (LevelManager.Instance.currentData.hasCardReward)
			{
				PlayerProfile.Instance.AddCardReward(LevelManager.Instance.currentData.cardReward);

				// Здесь позже мы добавим красивый экран "ВЫ ОТКРЫЛИ КАРТУ!"
				Debug.Log("ВЫБИТ ЛУТБОКС! Получена карта: " + LevelManager.Instance.currentData.cardReward);
			}
		}

		// Сохраняем прогресс профиля
		PlayerProfile.Instance.SaveProfile();

		// Показываем обычный экран победы/поражения
		UIManager.Instance.ShowResultPopup(rescuedHumans, totalHumans);
	}
}