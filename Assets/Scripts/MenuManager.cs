using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
	[Header("UI Ёлементы")]
	[SerializeField] private TextMeshProUGUI globalCurrencyText;
	[SerializeField] private TextMeshProUGUI scientistsCurrencyText;

	private void Start()
	{
		if (PlayerProfile.Instance != null)
		{
			if (globalCurrencyText != null)
				globalCurrencyText.text = PlayerProfile.Instance.totalCurrency.ToString();

			if (scientistsCurrencyText != null)
				scientistsCurrencyText.text = PlayerProfile.Instance.totalScientistsCurrency.ToString();
		}
	}

	public void PlayGame()
	{
		SceneManager.LoadScene("Gameplay");
	}
}