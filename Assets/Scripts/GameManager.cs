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

		PlayerProfile.Instance.totalCurrency += rescuedHumans;

		string levelId = "LevelPassed_" + LevelManager.Instance.currentData.name;
		int hasPassedBefore = PlayerPrefs.GetInt(levelId, 0);

		CardData droppedCard = null; // Запоминаем выпавшую карту

		if (hasPassedBefore == 0)
		{
			PlayerPrefs.SetInt(levelId, 1);
			PlayerProfile.Instance.totalCurrency += LevelManager.Instance.currentData.currencyReward;

			if (LevelManager.Instance.currentData.levelRewardLootbox != null)
			{
				// Открываем лутбокс и сохраняем результат
				droppedCard = LevelManager.Instance.currentData.levelRewardLootbox.OpenBox();
				if (droppedCard != null)
				{
					PlayerProfile.Instance.AddCardReward(droppedCard);
				}
			}
		}

		PlayerProfile.Instance.SaveProfile();

		// Передаем карту в интерфейс!
		UIManager.Instance.ShowResultPopup(rescuedHumans, totalHumans, droppedCard);
	}
}