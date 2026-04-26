using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CheatManager : MonoBehaviour
{
	[Header("Префабы для спавна (Только для боевой сцены)")]
	public GameObject humanPrefab;
	public GameObject zombiePrefab;

	private Camera mainCam;

	// Мобильный UI
	private bool showMenu = false;
	private enum SpawnMode { None, Human, Zombie }
	private SpawnMode currentMode = SpawnMode.None;

	private void Start()
	{
		mainCam = Camera.main;
	}

	private void Update()
	{
		// ПК-КНОПКИ
		if (Input.GetKeyDown(KeyCode.F1)) GiveMaxResources();
		if (Input.GetKeyDown(KeyCode.F2)) currentMode = SpawnMode.Human;
		if (Input.GetKeyDown(KeyCode.F3)) currentMode = SpawnMode.Zombie;
		if (Input.GetKeyDown(KeyCode.F4)) FillMana();
		if (Input.GetKeyDown(KeyCode.F5)) CheatWin();
		if (Input.GetKeyDown(KeyCode.F6)) AddCurrency();

		// ОБРАБОТКА ТАПА ПО ЭКРАНУ
		if (currentMode != SpawnMode.None && Input.GetMouseButtonDown(0))
		{
			if (Input.mousePosition.x < Screen.width * 0.45f && Input.mousePosition.y > Screen.height * 0.3f && showMenu)
				return;

			SpawnAtTap(Input.mousePosition);
		}
	}

	// --- МОБИЛЬНОЕ МЕНЮ ---
	private void OnGUI()
	{
		int fontSize = Mathf.Clamp(Screen.width / 40, 16, 40);
		GUI.skin.button.fontSize = fontSize;
		GUI.skin.label.fontSize = fontSize;
		GUI.skin.label.normal.textColor = Color.red;

		float btnW = Screen.width * 0.4f;
		float btnH = Screen.height * 0.08f;
		float padding = 10f;

		if (GUI.Button(new Rect(padding, padding, btnW, btnH), "🛠 DEV MENU"))
		{
			showMenu = !showMenu;
			currentMode = SpawnMode.None;
		}

		if (showMenu)
		{
			float yPos = padding + btnH + padding;

			if (GUI.Button(new Rect(padding, yPos, btnW, btnH), "Unlock All (F1)")) GiveMaxResources();
			yPos += btnH + padding;

			string humanText = currentMode == SpawnMode.Human ? ">> ТАПАЙ ДОРОГУ <<" : "Spawn Human (F2)";
			if (GUI.Button(new Rect(padding, yPos, btnW, btnH), humanText)) currentMode = SpawnMode.Human;
			yPos += btnH + padding;

			string zombieText = currentMode == SpawnMode.Zombie ? ">> ТАПАЙ ДОРОГУ <<" : "Spawn Zombie (F3)";
			if (GUI.Button(new Rect(padding, yPos, btnW, btnH), zombieText)) currentMode = SpawnMode.Zombie;
			yPos += btnH + padding;

			if (GUI.Button(new Rect(padding, yPos, btnW, btnH), "Max Mana (F4)")) FillMana();
			yPos += btnH + padding;

			if (GUI.Button(new Rect(padding, yPos, btnW, btnH), "Win Level (F5)")) CheatWin();
			yPos += btnH + padding;

			if (GUI.Button(new Rect(padding, yPos, btnW, btnH), "+10 Humans/Coins (F6)")) AddCurrency();
		}

		if (currentMode != SpawnMode.None && !showMenu)
		{
			GUI.Label(new Rect(Screen.width * 0.3f, Screen.height * 0.1f, Screen.width * 0.5f, btnH), "РЕЖИМ СПАВНА АКТИВЕН! ТАПАЙТЕ ПО ЗЕМЛЕ.");
		}
	}

	// --- ЛОГИКА ЧИТОВ ---

	private void GiveMaxResources()
	{
		if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.totalCurrency += 10000;
			foreach (CardManager.CardType card in System.Enum.GetValues(typeof(CardManager.CardType)))
			{
				if (card != CardManager.CardType.None && !PlayerProfile.Instance.unlockedCards.Contains(card))
				{
					PlayerProfile.Instance.unlockedCards.Add(card);
				}
			}
			PlayerProfile.Instance.SaveProfile();

			// ИСПРАВЛЕНИЕ: Мгновенное обновление меню!
			// Ищем скрипт меню на сцене и заставляем его перерисовать карточки
			DeckMenuManager menu = FindAnyObjectByType<DeckMenuManager>();
			if (menu != null) menu.RefreshUI();

			Debug.Log("<color=green>ЧИТ: Получено 10000 монет и открыты ВСЕ карты!</color>");
		}
	}

	private void FillMana()
	{
		if (EnergyManager.Instance != null)
		{
			EnergyManager.Instance.CheatFillEnergy();
		}
	}

	private void CheatWin()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.EndLevel();
			Debug.Log("<color=yellow>ЧИТ: Уровень мгновенно завершен!</color>");
		}
		else
		{
			Debug.LogWarning("Вы не в бою! Этот чит работает только на уровне.");
		}
	}

	private void AddCurrency()
	{
		// Если мы в бою - добавляем спасенных прямо в счетчик текущего уровня
		if (GameManager.Instance != null)
		{
			GameManager.Instance.AddRescuedHumans(10);
			Debug.Log("<color=cyan>ЧИТ: Добавлено 10 спасенных в текущем бою!</color>");
		}
		// Если мы в главном меню - просто накидываем монеты в профиль
		else if (PlayerProfile.Instance != null)
		{
			PlayerProfile.Instance.totalCurrency += 10;
			PlayerProfile.Instance.SaveProfile();
			Debug.Log("<color=cyan>ЧИТ: Добавлено 10 монет в профиль!</color>");
		}
	}

	private void SpawnAtTap(Vector2 screenPos)
	{
		GameObject prefab = currentMode == SpawnMode.Human ? humanPrefab : zombiePrefab;
		if (prefab == null || mainCam == null) return;

		Ray ray = mainCam.ScreenPointToRay(screenPos);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
			{
				Instantiate(prefab, navHit.position, Quaternion.identity);
			}
		}
	}
}