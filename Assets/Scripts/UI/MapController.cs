using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// [ГДЕ ВИСИТ]: На объекте MapPanel в меню.
public class MapController : MonoBehaviour
{
	[Header("Ссылки на UI Карты")]
	[SerializeField] private RectTransform mapSpawnContainer;
	[SerializeField] private RectTransform playerToken;
	[SerializeField] private ParticleSystem confettiFX;

	[Header("Дизайн (Цвета и Позиция)")]
	[SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
	[SerializeField] private Color currentColor = new Color(1f, 0.8f, 0f);
	[SerializeField] private Color completedColor = new Color(0.2f, 0.8f, 0.2f);
	[SerializeField] private Vector3 tokenOffset = new Vector3(0, 150f, 0);
	[SerializeField] private float animDuration = 0.5f;

	private CanvasGroup canvasGroup;
	private RegionMapVisual currentVisualMap;
	private GameObject currentSpawnedPrefab;
	private Dictionary<Image, Vector3> initialSectorScales = new Dictionary<Image, Vector3>();

	private void Start()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

		RefreshMap();
	}

	private void RefreshMap()
	{
		if (PlayerProfile.Instance == null || PlayerProfile.Instance.allRegions.Count == 0) return;

		bool animateRegion = PlayerProfile.Instance.hasPendingRegionAnimation;
		int regionIdx = PlayerProfile.Instance.currentRegionIndex;
		if (animateRegion) regionIdx = Mathf.Max(0, regionIdx - 1);

		regionIdx = Mathf.Clamp(regionIdx, 0, PlayerProfile.Instance.allRegions.Count - 1);
		RegionConfig regionToDisplay = PlayerProfile.Instance.allRegions[regionIdx];

		SpawnOrUpdateRegionVisual(regionToDisplay);

		if (currentVisualMap == null) return;

		int actualLevel = PlayerProfile.Instance.currentLevelIndex;
		bool animateStep = PlayerProfile.Instance.hasPendingMapAnimation;

		int visualLevel = animateStep ? Mathf.Max(0, actualLevel - 1) : actualLevel;
		if (animateRegion) visualLevel = 5;

		UpdateMapVisualState(visualLevel);

		if (animateRegion) StartCoroutine(RegionTransitionRoutine());
		else if (animateStep && actualLevel <= currentVisualMap.levelNodes.Length)
			StartCoroutine(AnimateProgressRoutine(visualLevel, actualLevel));
	}

	private void UpdateMapVisualState(int visualLevel)
	{
		for (int i = 0; i < currentVisualMap.levelNodes.Length; i++)
		{
			Color stateColor = GetColorForState(i, visualLevel);
			if (currentVisualMap.levelNodes[i] != null)
				currentVisualMap.levelNodes[i].GetComponent<Image>().color = stateColor;

			if (i < currentVisualMap.sectors.Length && currentVisualMap.sectors[i] != null)
			{
				Image s = currentVisualMap.sectors[i];
				s.color = stateColor;
				if (initialSectorScales.ContainsKey(s)) s.transform.localScale = initialSectorScales[s];
			}
		}

		for (int i = 0; i < currentVisualMap.lines.Length; i++)
		{
			if (currentVisualMap.lines[i] == null) continue;
			currentVisualMap.lines[i].color = (i < visualLevel) ? completedColor : lockedColor;
		}

		if (visualLevel < currentVisualMap.levelNodes.Length)
			playerToken.position = currentVisualMap.levelNodes[visualLevel].transform.position + tokenOffset;
	}

	private void SpawnOrUpdateRegionVisual(RegionConfig config)
	{
		if (currentSpawnedPrefab != null && currentSpawnedPrefab.name == config.regionUIPrefab.name + "(Clone)")
			return;

		if (currentSpawnedPrefab != null) Destroy(currentSpawnedPrefab);

		currentSpawnedPrefab = Instantiate(config.regionUIPrefab, mapSpawnContainer);
		currentVisualMap = currentSpawnedPrefab.GetComponent<RegionMapVisual>();

		initialSectorScales.Clear();
		foreach (var s in currentVisualMap.sectors)
		{
			if (s != null && !initialSectorScales.ContainsKey(s))
				initialSectorScales.Add(s, s.transform.localScale);
		}

		for (int i = 0; i < currentVisualMap.levelNodes.Length; i++)
		{
			if (currentVisualMap.levelNodes[i] == null) continue;
			int index = i;
			currentVisualMap.levelNodes[i].onClick.AddListener(() => OnNodeClicked(index));
		}
	}

	private Color GetColorForState(int index, int visualLevel)
	{
		if (index < visualLevel) return completedColor;
		if (index == visualLevel) return currentColor;
		return lockedColor;
	}

	private IEnumerator AnimateProgressRoutine(int fromIndex, int toIndex)
	{
		canvasGroup.blocksRaycasts = false;
		MainMenuSwipeController.IsSwipeLocked = true;
		yield return new WaitForSeconds(0.3f);

		if (fromIndex < currentVisualMap.levelNodes.Length)
			currentVisualMap.levelNodes[fromIndex].GetComponent<Image>().DOColor(completedColor, animDuration);

		if (fromIndex < currentVisualMap.sectors.Length)
			currentVisualMap.sectors[fromIndex].DOColor(completedColor, animDuration);

		if (fromIndex < currentVisualMap.lines.Length && currentVisualMap.lines[fromIndex] != null)
		{
			currentVisualMap.lines[fromIndex].DOColor(completedColor, animDuration);
			currentVisualMap.lines[fromIndex].transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), animDuration, 1);
		}

		yield return new WaitForSeconds(animDuration);

		if (toIndex < currentVisualMap.levelNodes.Length)
		{
			playerToken.DOJump(currentVisualMap.levelNodes[toIndex].transform.position + tokenOffset, 70f, 1, 0.6f).SetEase(Ease.OutQuad);
			yield return new WaitForSeconds(0.2f);
			currentVisualMap.levelNodes[toIndex].GetComponent<Image>().DOColor(currentColor, animDuration);
			if (toIndex < currentVisualMap.sectors.Length)
			{
				Image s = currentVisualMap.sectors[toIndex];
				s.DOColor(currentColor, animDuration);
				Vector3 orig = initialSectorScales.ContainsKey(s) ? initialSectorScales[s] : Vector3.one;
				s.transform.localScale = orig * 0.8f;
				s.transform.DOScale(orig, animDuration).SetEase(Ease.OutBack);
			}
		}

		yield return new WaitForSeconds(0.6f);
		PlayerProfile.Instance.hasPendingMapAnimation = false;
		PlayerProfile.Instance.SaveProfile();
		MainMenuSwipeController.IsSwipeLocked = false;
		canvasGroup.blocksRaycasts = true;
	}

	private IEnumerator RegionTransitionRoutine()
	{
		canvasGroup.blocksRaycasts = false;
		MainMenuSwipeController.IsSwipeLocked = true;
		yield return new WaitForSeconds(0.5f);

		if (currentVisualMap != null)
		{
			if (confettiFX != null) confettiFX.Play();
			foreach (var sector in currentVisualMap.sectors)
			{
				if (sector != null)
					sector.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5, 1f);
			}
		}

		yield return new WaitForSeconds(2.0f);

		if (confettiFX != null) confettiFX.Stop();
		PlayerProfile.Instance.hasPendingRegionAnimation = false;
		PlayerProfile.Instance.SaveProfile();
		RefreshMap();

		MainMenuSwipeController.IsSwipeLocked = false;
		canvasGroup.blocksRaycasts = true;
	}

	private void OnNodeClicked(int nodeIndex)
	{
		if (PlayerProfile.Instance == null) return;

		// ЛОГИКА ОТКАЗА (ОШИБКИ)
		if (nodeIndex > PlayerProfile.Instance.currentLevelIndex)
		{
			if (currentVisualMap != null && currentVisualMap.levelNodes[nodeIndex] != null)
			{
				Transform nodeTransform = currentVisualMap.levelNodes[nodeIndex].transform;
				nodeTransform.DOComplete();
				nodeTransform.DOPunchPosition(new Vector3(15f, 0, 0), 0.4f, 10, 1f);
				nodeTransform.DOPunchRotation(new Vector3(0, 0, 5f), 0.4f, 10, 1f);
			}
			return;
		}

		PlayerPrefs.SetInt("SelectedLevelToPlay", nodeIndex);
		DOTween.KillAll();
		SceneManager.LoadScene("Gameplay");
	}

	private void OnDestroy() => transform.DOKill();
}