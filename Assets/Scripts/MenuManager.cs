using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
	[Header("UI Элементы")]
	[SerializeField] private TextMeshProUGUI globalCurrencyText;

	private void Start()
	{
		// Берем валюту напрямую из синглтона PlayerProfile!
		if (globalCurrencyText != null && PlayerProfile.Instance != null)
		{
			globalCurrencyText.text = PlayerProfile.Instance.totalCurrency.ToString();
		}
	}

	public void PlayGame()
	{
		SceneManager.LoadScene("Gameplay");
	}
}