using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public enum GameState { Planning, Playing, SuddenDeath, GameOver, Lose }
	public GameState State = GameState.Planning;

	[Header("Статистика уровня")]
	public int totalHumans;
	public int rescuedHumans;
	public int requiredHumans;

	[Header("Статистика учёных")]
	public int totalScientists;
	public int rescuedScientists;

	[HideInInspector] public int pendingHumans = 0;
	[HideInInspector] public int pendingScientists = 0;

	private float currentTimer;
	private float startTimerAmount;
	private bool perfectClearStarted = false;

	private void Awake() => Instance = this;

	public void SetTotalHumans(int count)
	{
		totalHumans = count;
		rescuedHumans = 0;
		pendingHumans = 0;

		totalScientists = Scientist.AllScientists.Count;
		rescuedScientists = 0;
		pendingScientists = 0;

		requiredHumans = LevelManager.Instance.currentData.requiredRescuedHumans;

		UIManager.Instance.UpdateRescuedCount(GetTotalRescued(), requiredHumans, false);
	}

	// ===== ВЫЗЫВАЕТСЯ ТОЛЬКО ТРАНСПОРТОМ =====
	public void AddRescuedFromTransport(int humans, int scientists, Vector3 transportWorldPos)
	{
		if (State == GameState.GameOver || State == GameState.Lose) return;

		int total = humans + scientists;
		if (total <= 0) return;

		pendingHumans += humans;
		pendingScientists += scientists;

		UIManager.Instance.SpawnFlyingText(transportWorldPos, total);
	}

	// ===== СТАРЫЕ ОБЁРТКИ ДЛЯ СОВМЕСТИМОСТИ =====
	public void AddRescuedHumans(int count, Vector3 transportWorldPos)
	{
		// Старый код считает только людей — для совместимости учёных не добавляем
		AddRescuedFromTransport(count, 0, transportWorldPos);
	}

	public void AddRescuedScientists(int count, Vector3 transportWorldPos)
	{
		AddRescuedFromTransport(0, count, transportWorldPos);
	}

	// ===== FLYING TEXT ДОЛЕТЕЛ =====
	public void OnFlyingTextReached(int totalAmount)
	{
		int totalPending = pendingHumans + pendingScientists;

		if (totalPending <= 0)
		{
			// На всякий случай всё кидаем в людей
			rescuedHumans += totalAmount;
		}
		else
		{
			float humansRatio = (float)pendingHumans / totalPending;
			int humansFromThis = Mathf.RoundToInt(totalAmount * humansRatio);
			int scientistsFromThis = totalAmount - humansFromThis;

			pendingHumans -= humansFromThis;
			pendingScientists -= scientistsFromThis;

			rescuedHumans += humansFromThis;
			rescuedScientists += scientistsFromThis;
		}

		UIManager.Instance.UpdateRescuedCount(GetTotalRescued(), requiredHumans, true);
		CheckWinLoseCondition();
	}

	// Этот метод больше не используется, но оставим, чтобы не ловить null-референсы, если где-то старый вызов
	public void OnFlyingScientistTextReached(int amount) { }

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
			bool levelEmpty = GetAliveResidentsCount() == 0 && (pendingHumans + pendingScientists) == 0;

			if (levelEmpty) CheckWinLoseCondition(true);
			else CheckWinLoseCondition(false);
		}
	}

	private int GetAliveResidentsCount()
	{
		return Human.AllHumans.Count + Scientist.AllScientists.Count;
	}

	private int GetTotalRescued()
	{
		return rescuedHumans + rescuedScientists;
	}

	private int GetPendingTotal()
	{
		return pendingHumans + pendingScientists;
	}

	private void CheckPerfectClear()
	{
		if (perfectClearStarted) return;

		float spawnTime = LevelManager.Instance.currentData.initialZombies * LevelManager.Instance.currentData.initialSpawnDelay + 1f;
		if (startTimerAmount - currentTimer > spawnTime)
		{
			if (Zombie.AllZombies.Count == 0 && GetAliveResidentsCount() > 0)
			{
				perfectClearStarted = true;
				StartCoroutine(PerfectClearRoutine());
			}
		}
	}

	private IEnumerator PerfectClearRoutine()
	{
		State = GameState.GameOver;

		List<Human> humanSurvivors = new List<Human>(Human.AllHumans);
		List<Scientist> scientistSurvivors = new List<Scientist>(Scientist.AllScientists);

		Human.AllHumans.Clear();
		Scientist.AllScientists.Clear();

		foreach (var h in humanSurvivors)
		{
			if (h != null)
			{
				pendingHumans += 1;
				UIManager.Instance.SpawnFlyingText(h.transform.position, 1);
				h.gameObject.SetActive(false);
				yield return new WaitForSeconds(0.08f);
			}
		}

		foreach (var s in scientistSurvivors)
		{
			if (s != null)
			{
				pendingScientists += 1;
				UIManager.Instance.SpawnFlyingText(s.transform.position, 1);
				s.gameObject.SetActive(false);
				yield return new WaitForSeconds(0.08f);
			}
		}

		while (pendingHumans > 0 || pendingScientists > 0)
		{
			yield return null;
		}

		yield return new WaitForSeconds(0.5f);

		EndLevel(true);
	}

	private void CheckWinLoseCondition(bool isLevelEmpty = false)
	{
		if (State == GameState.GameOver || State == GameState.Lose) return;

		int possibleToRescue = GetTotalRescued() + GetPendingTotal() + GetAliveResidentsCount();

		if (possibleToRescue < requiredHumans)
		{
			State = GameState.Lose;
			UIManager.Instance.ShowLosePopup();
		}
		else if (isLevelEmpty && GetTotalRescued() >= requiredHumans)
		{
			EndLevel(false);
		}
	}

	public void EndLevel(bool isPerfect = false)
	{
		State = GameState.GameOver;

		PlayerProfile.Instance.totalCurrency += rescuedHumans;
		PlayerProfile.Instance.totalScientistsCurrency += rescuedScientists;

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
		UIManager.Instance.ShowResultPopup(rescuedHumans, GetTotalRescued(), rescuedScientists, droppedCard, isPerfect);
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