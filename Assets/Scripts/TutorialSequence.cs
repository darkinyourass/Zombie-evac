using UnityEngine;
using System.Collections.Generic;

public enum TutorialStepType
{
	DialogOnly,       // Только текст (клик в любое место для продолжения)
	ClickOnly,        // Только палец и подсветка кнопки (без диалога)
	DialogAndClick    // И диалог, и требование кликнуть по конкретной кнопке
}

public enum DialogPosition
{
	Top,
	Center,
	Bottom
}

[System.Serializable]
public class TutorialStep
{
	[Header("Тип шага")]
	public TutorialStepType stepType;

	[Tooltip("ID цели (TutorialTarget), на которую нужно кликнуть. Оставь пустым для DialogOnly.")]
	public string targetId;

	[Tooltip("Затемнять ли остальной экран вокруг цели?")]
	public bool useDarkMask = true;

	[Header("Настройки Диалога")]
	[TextArea(2, 4)]
	public string dialogText;
	public Sprite characterIcon;
	public DialogPosition dialogPosition = DialogPosition.Bottom;
}

[CreateAssetMenu(fileName = "NewTutorial", menuName = "ZombieGame/TutorialSequence")]
public class TutorialSequence : ScriptableObject
{
	[Tooltip("Уникальный ID туториала для сохранения прогресса (чтобы не показывать дважды)")]
	public string tutorialId = "Tutorial_1";

	public List<TutorialStep> steps = new List<TutorialStep>();
}