using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; // Нужно для работы со списками

// [ГДЕ ВИСИТ]: На пустом объекте GameManager
public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public enum GameState { Planning, Playing, SuddenDeath, GameOver, Lose }
	public GameState State = GameState.Planning;

	[Header("Статистика уровня")]
	public int totalHumans;
	public int rescuedHumans;
	public int requiredHumans;

	[HideInInspector] public int pendingHumans = 0;

	private float currentTimer;
	private float startTimerAmount;

	private void Awake() => Instance = this;

	public void SetTotalHumans(int count)
	{
		totalHumans = count;
		rescuedHumans = 0;
		pendingHumans = 0;
		requiredHumans = LevelManager.Instance.currentData.requiredRescuedHumans;

		UIManager.Instance.UpdateRescuedCount(rescuedHumans, requiredHumans, false);
	}

	public void AddRescuedHumans(int count, Vector3 transportWorldPos)
	{
		// ФИКС 2: Если игра уже окончена, игнорируем любые попытки добавить очки
		if (State == GameState.GameOver || State == GameState.Lose) return;

		pendingHumans += count;
		UIManager.Instance.SpawnFlyingText(transportWorldPos, count);
	}

	public void OnFlyingTextReached(int count)
	{
		pendingHumans -= count;
		rescuedHumans += count;
		UIManager.Instance.UpdateRescuedCount(rescuedHumans, requiredHumans, true);
		CheckWinLoseCondition();
	}

	public void SetupTimer(float time)
	{
		currentTimer = time;
		startTimerAmount = time;
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

			CheckPerfectClear();

			if (currentTimer <= 0 && State == GameState.Playing)
			{
				State = GameState.SuddenDeath;
				LevelManager.Instance.StartSuddenDeath();
				UIManager.Instance.ShowNightPopup();
			}
		}

		if (State == GameState.Playing || State == GameState.SuddenDeath)
		{
			if (Human.AllHumans.Count == 0 && pendingHumans == 0) CheckWinLoseCondition(true);
			else CheckWinLoseCondition(false);
		}
	}

	private void CheckPerfectClear()
	{
		float spawnTime = LevelManager.Instance.currentData.initialZombies * LevelManager.Instance.currentData.initialSpawnDelay + 1f;
		if (startTimerAmount - currentTimer > spawnTime)
		{
			// Если зомби кончились, а люди еще есть - запускаем шоу
			if (Zombie.AllZombies.Count == 0 && Human.AllHumans.Count > 0)
			{
				StartCoroutine(PerfectClearRoutine());
			}
		}
	}

	// --- НОВАЯ САТИСФАЙНАЯ ЛОГИКА ---
	private IEnumerator PerfectClearRoutine()
	{
		// Переводим стейт, чтобы Update больше не пытался завершить игру
		State = GameState.GameOver;

		// Копируем список оставшихся людей, чтобы безопасно их удалять
		List<Human> survivors = new List<Human>(Human.AllHumans);
		Human.AllHumans.Clear(); // Очищаем основной список

		// Выпускаем цифры пулеметом!
		foreach (var h in survivors)
		{
			if (h != null)
			{
				pendingHumans += 1;
				UIManager.Instance.SpawnFlyingText(h.transform.position, 1);

				// Прячем человечка, чтобы он не мозолил глаза
				h.gameObject.SetActive(false);

				// Небольшая задержка для "сока" (80 миллисекунд)
				yield return new WaitForSeconds(0.08f);
			}
		}

		// Терпеливо ждем, пока все выпущенные цифры долетят и обновят счетчик
		while (pendingHumans > 0)
		{
			yield return null;
		}

		// Делаем драматичную паузу в полсекунды, чтобы игрок кайфанул от финальной цифры
		yield return new WaitForSeconds(0.5f);

		// Теперь можно смело вызывать экран победы с флагом "Идеально"
		EndLevel(true);
	}

	private void CheckWinLoseCondition(bool isLevelEmpty = false)
	{
		if (State == GameState.GameOver || State == GameState.Lose) return;

		int possibleToRescue = rescuedHumans + pendingHumans + Human.AllHumans.Count;

		if (possibleToRescue < requiredHumans)
		{
			State = GameState.Lose;
			UIManager.Instance.ShowLosePopup();
		}
		else if (isLevelEmpty && rescuedHumans >= requiredHumans)
		{
			EndLevel(false);
		}
	}

	public void EndLevel(bool isPerfect = false)
	{
		State = GameState.GameOver;
		PlayerProfile.Instance.totalCurrency += rescuedHumans;

		string levelId = "LevelPassed_" + LevelManager.Instance.currentData.name;
		int hasPassedBefore = PlayerPrefs.GetInt(levelId, 0);
		CardData droppedCard = null;

		if (hasPassedBefore == 0)
		{
			PlayerPrefs.SetInt(levelId, 1);
			PlayerProfile.Instance.totalCurrency += LevelManager.Instance.currentData.currencyReward;

			if (LevelManager.Instance.currentData.levelRewardLootbox != null)
			{
				droppedCard = LevelManager.Instance.currentData.levelRewardLootbox.OpenBox();
				if (droppedCard != null) PlayerProfile.Instance.AddCardReward(droppedCard);
			}
		}

		PlayerProfile.Instance.SaveProfile();
		UIManager.Instance.ShowResultPopup(rescuedHumans, requiredHumans, droppedCard, isPerfect);
	}

	public void RestartLevel()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	public void GoToMainMenu()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene("MainMenu");
	}
}