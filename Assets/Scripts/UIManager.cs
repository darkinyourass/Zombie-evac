using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
	public static UIManager Instance;

	[Header("UI ›ÎÂÏÂÌÚ˚")]
	[SerializeField] private TextMeshProUGUI timerText;
	[SerializeField] private TextMeshProUGUI manaText;
	[SerializeField] private Image manaFillBar;
	[SerializeField] private ResultPopupUI resultPopup;

	// ÕŒ¬Œ≈ œŒÀ≈: —˛‰‡ ÔÂÂÚ‡˘Ë ÚÂÍÒÚ ËÁ ËÌÚÂÙÂÈÒ‡, ÍÓÚÓ˚È ÔÓÍ‡Á˚‚‡ÂÚ "—Ô‡ÒÂÌÌ˚ı: 0/30"
	[SerializeField] private TextMeshProUGUI ingameRescuedText;

	[Header("ÕÓ˜ÌÓÈ ›ÙÙÂÍÚ")]
	public TextMeshProUGUI centerNightText;
	public AudioSource nightSound;

	private void Awake() => Instance = this;

	private void Start()
	{
		if (EnergyManager.Instance != null)
			EnergyManager.Instance.OnEnergyChanged += UpdateManaUI;

		if (centerNightText != null)
		{
			centerNightText.gameObject.SetActive(false);
			centerNightText.color = new Color(centerNightText.color.r, centerNightText.color.g, centerNightText.color.b, 0);
		}
	}

	private void OnDestroy()
	{
		if (EnergyManager.Instance != null)
			EnergyManager.Instance.OnEnergyChanged -= UpdateManaUI;
	}

	private void UpdateManaUI(float currentEnergy, float maxEnergy)
	{
		if (manaText != null) manaText.text = Mathf.FloorToInt(currentEnergy).ToString();
		if (manaFillBar != null && maxEnergy > 0) manaFillBar.fillAmount = currentEnergy / maxEnergy;
	}

	public void UpdateTimer(float time, bool isPlanning)
	{
		if (isPlanning)
		{
			timerText.text = "œÀ¿Õ»–Œ¬¿Õ»≈";
			timerText.color = Color.white;
			return;
		}

		int seconds = Mathf.CeilToInt(time);
		timerText.text = seconds.ToString("00");
		if (seconds <= 5) timerText.color = Color.red;
		else timerText.color = Color.white;

		if (seconds <= 0) timerText.text = "ÕŒ◊‹!";
	}

	// ÕŒ¬€… Ã≈“Œƒ ƒÀﬂ Œ¡ÕŒ¬À≈Õ»ﬂ —◊≈“◊» ¿
	public void UpdateRescuedCount(int rescued, int total)
	{
		if (ingameRescuedText != null)
		{
			ingameRescuedText.text = $"{rescued} / {total}";
		}
	}

	public void ShowNightPopup()
	{
		if (nightSound != null) nightSound.Play();
		if (centerNightText != null) StartCoroutine(NightTextRoutine());
	}

	private IEnumerator NightTextRoutine()
	{
		centerNightText.gameObject.SetActive(true);
		Color c = centerNightText.color;

		for (float t = 0; t < 1; t += Time.deltaTime * 2)
		{
			centerNightText.color = new Color(c.r, c.g, c.b, t);
			centerNightText.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, t);
			yield return null;
		}

		yield return new WaitForSeconds(1.5f);

		for (float t = 1; t > 0; t -= Time.deltaTime)
		{
			centerNightText.color = new Color(c.r, c.g, c.b, t);
			yield return null;
		}
		centerNightText.gameObject.SetActive(false);
	}

	public void ShowResultPopup(int rescued, int total, CardData reward = null)
	{
		if (resultPopup != null) resultPopup.Show(rescued, total, reward);
	}
}