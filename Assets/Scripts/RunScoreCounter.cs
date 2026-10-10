using UnityEngine;

/// <summary>
/// Adds this scene's voice attempts to the level's running total (RunScore).
///
/// One of these goes in every scene of a level. The scene that ends the level
/// also has a SessionScoreTracker, which saves the total and shows it; this
/// only counts, so no scene counts twice.
/// </summary>
public class RunScoreCounter : MonoBehaviour
{
    [Tooltip("The scene's VoiceCommand. If empty, one is looked up in the scene.")]
    public VoiceCommand voiceCommand;

    [Tooltip("The scene's objective list, for recording WHICH words were said. " +
             "If empty, one is looked up in the scene.")]
    public SceneObjectiveController objectiveController;

    [Tooltip("Tick this in a level's FIRST scene, so starting the level clears " +
             "the previous run's total. Leave it off in later scenes or they " +
             "would wipe the score the player has already earned.")]
    public bool resetOnStart = false;

    void Awake()
    {
        if (voiceCommand == null) voiceCommand = FindFirstObjectByType<VoiceCommand>();
        if (objectiveController == null) objectiveController = FindFirstObjectByType<SceneObjectiveController>();
        // Awake, not Start: a scene's first spoken answer can arrive before
        // Start has run everywhere, and clearing after that would lose it.
        if (resetOnStart) RunScore.Reset();
    }

    void OnEnable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked += HandleAnswerChecked;
        if (objectiveController != null) objectiveController.OnObjectiveCompleted += HandleObjectiveCompleted;
    }

    void OnDisable()
    {
        if (voiceCommand != null) voiceCommand.OnAnswerChecked -= HandleAnswerChecked;
        if (objectiveController != null) objectiveController.OnObjectiveCompleted -= HandleObjectiveCompleted;
    }

    private void HandleObjectiveCompleted(string word)
    {
        RunScore.NotePractised(word);
    }

    private void HandleAnswerChecked(string spokenText, bool wasCorrect)
    {
        RunScore.Add(wasCorrect);
        Debug.Log("RunScoreCounter: run total " + RunScore.Correct + " correct / " +
                   RunScore.Attempts + " attempts.");
    }
}
