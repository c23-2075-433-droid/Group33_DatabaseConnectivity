using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Decides WHAT Rachel says and WHEN. VoiceNarrator only says it.
///
/// Everything here hangs off events the scene already raises - an objective
/// going live, an answer being checked, the sequence being paused - so the
/// rule from the design holds by construction: Rachel speaks at a state
/// change and is silent the rest of the time. Nothing narrates while Kylo is
/// walking, because SceneObjectiveController does not raise an objective
/// while she is.
///
/// Nothing needs wiring: VoicePromptBootstrap adds one of these to every
/// scene that has a SceneObjectiveController, so the fifteen scene builders
/// are untouched and a rebuilt scene still prompts.
/// </summary>
public class VoicePromptDirector : MonoBehaviour
{
    [Header("Help after repeated misses")]
    [Tooltip("Misses on the same word before the word is automatically said again, slowly. The most important accessibility behaviour here: a child who cannot reach the replay button still gets help.")]
    public int missesBeforeHint = 2;

    [Tooltip("Misses on the same word before Rachel points at the replay button.")]
    public int missesBeforeReplayHint = 4;

    [Header("Silence")]
    [Tooltip("Seconds of silence before Rachel encourages the player. Reset by any speech, right or wrong.")]
    public float idleNudgeSeconds = 10f;

    [Tooltip("Seconds between repeats of the whole prompt after that first nudge.")]
    public float idleRepromptSeconds = 20f;

    [Header("Scene lines")]
    [Tooltip("Play the scene's intro and outro lines (rachel_lX_sY_intro/_outro) when they exist in the voice bank.")]
    public bool speakSceneLines = true;

    [Tooltip("Say \"Nakikinig ako\" before the first word of a scene only. After that the pulsing mic says it without words.")]
    public bool cueListeningOnFirstWord = false;

    private SceneObjectiveController controller;
    private VoiceCommand voiceCommand;
    private VoiceNarrator narrator;

    private SceneObjectiveController.SceneObjective currentObjective;
    private int missCount;
    private int notHeardCount;
    private int promptsThisScene;
    private float silenceTimer;
    private bool nudged;

    void Awake()
    {
        controller = FindFirstObjectByType<SceneObjectiveController>();
        if (controller == null)
        {
            enabled = false;
            return;
        }

        voiceCommand = controller.voiceCommand != null
            ? controller.voiceCommand
            : FindFirstObjectByType<VoiceCommand>();

        narrator = VoiceNarrator.Instance;

        controller.OnObjectiveShown += HandleObjectiveShown;
        controller.OnObjectiveCompleted += HandleObjectiveCompleted;
        controller.onAllObjectivesComplete.AddListener(HandleSceneComplete);

        if (voiceCommand != null)
        {
            voiceCommand.OnAnswerChecked += HandleAnswerChecked;
            voiceCommand.OnNothingHeard += HandleNothingHeard;
            voiceCommand.OnMicrophoneUnavailable += HandleMicrophoneUnavailable;
        }

        // Queued in Awake, which runs before any Start, so the intro is
        // already in front of the first prompt by the time
        // SceneObjectiveController asks for it.
        if (speakSceneLines) SpeakSceneLine(true);
    }

    void OnDestroy()
    {
        if (controller != null)
        {
            controller.OnObjectiveShown -= HandleObjectiveShown;
            controller.OnObjectiveCompleted -= HandleObjectiveCompleted;
            controller.onAllObjectivesComplete.RemoveListener(HandleSceneComplete);
        }
        if (voiceCommand != null)
        {
            voiceCommand.OnAnswerChecked -= HandleAnswerChecked;
            voiceCommand.OnNothingHeard -= HandleNothingHeard;
            voiceCommand.OnMicrophoneUnavailable -= HandleMicrophoneUnavailable;
        }
        if (narrator != null) narrator.StopSpeaking();
    }

    // ---- A word goes live -------------------------------------------------

    private void HandleObjectiveShown(SceneObjectiveController.SceneObjective objective)
    {
        currentObjective = objective;
        missCount = 0;
        notHeardCount = 0;
        ResetSilence();

        if (narrator == null) return;

        if (promptsThisScene == 0 && cueListeningOnFirstWord)
            narrator.SpeakLine(NarratorScript.CueListening);

        narrator.SpeakPrompt(objective.word, objective.instructionClip, promptsThisScene > 0);
        promptsThisScene++;
    }

    // ---- An answer comes back --------------------------------------------

    /// <summary>
    /// A word was said correctly. Taken from the controller rather than from
    /// VoiceCommand.OnAnswerChecked, because the controller raises this
    /// BEFORE it moves on to the next word - so "Tama!" is always queued
    /// ahead of the next prompt, even in a scene with waitForActionToFinish
    /// turned off.
    /// </summary>
    private void HandleObjectiveCompleted(string word)
    {
        currentObjective = null;
        ResetSilence();

        // Short on purpose: this plays over Kylo starting to move.
        if (narrator != null) narrator.SpeakCorrect();
    }

    private void HandleAnswerChecked(string spokenText, bool wasCorrect)
    {
        ResetSilence();

        // The correct case is handled by HandleObjectiveCompleted above.
        if (wasCorrect) return;
        if (narrator == null || currentObjective == null) return;

        missCount++;
        notHeardCount = 0;

        if (missCount == missesBeforeHint)
        {
            // Say it again, slowly, without being asked.
            narrator.SpeakHint(currentObjective.word);
        }
        else if (missCount >= missesBeforeReplayHint)
        {
            narrator.SpeakLine(NarratorScript.Retry2);
            narrator.SpeakLine(NarratorScript.HintReplay);
        }
        else
        {
            narrator.SpeakLine(missCount > missesBeforeHint
                ? NarratorScript.Retry2
                : NarratorScript.Retry1);
        }
    }

    /// <summary>
    /// Nothing was recognised at all - a different problem from saying the
    /// wrong word, and it needs a different thing from the child, so it gets
    /// its own line. This never counts as a miss.
    /// </summary>
    private void HandleNothingHeard()
    {
        ResetSilence();
        if (narrator == null || currentObjective == null) return;

        notHeardCount++;
        narrator.SpeakLine(notHeardCount >= 2
            ? NarratorScript.NotHeard2
            : NarratorScript.NotHeard1);
    }

    private void HandleMicrophoneUnavailable()
    {
        if (narrator == null) return;
        narrator.SpeakLine(NarratorScript.MicBlocked);
    }

    // ---- Silence ----------------------------------------------------------

    void Update()
    {
        if (narrator == null || currentObjective == null) return;
        if (controller == null || controller.IsPaused) return;
        if (narrator.IsSpeaking) return;

        silenceTimer += Time.deltaTime;

        if (!nudged)
        {
            if (silenceTimer < idleNudgeSeconds) return;
            nudged = true;
            silenceTimer = 0f;
            narrator.SpeakLine(NarratorScript.IdleNudge);
            return;
        }

        if (silenceTimer < idleRepromptSeconds) return;
        silenceTimer = 0f;
        narrator.SpeakReplay(currentObjective.word, currentObjective.instructionClip);
    }

    private void ResetSilence()
    {
        silenceTimer = 0f;
        nudged = false;
    }

    // ---- Scene lines ------------------------------------------------------

    private void HandleSceneComplete()
    {
        currentObjective = null;
        if (speakSceneLines) SpeakSceneLine(false);
    }

    private void SpeakSceneLine(bool intro)
    {
        if (narrator == null) return;

        string id = NarratorScript.SceneLineId(SceneManager.GetActiveScene().name, intro);
        // Silent rather than captioned when the line is neither recorded nor
        // in the bank: the scene intros are long English story lines, and a
        // caption bar holding the screen for one is worse than nothing.
        if (!narrator.CanSay(id)) return;

        narrator.SpeakLine(id);
    }
}

/// <summary>
/// Puts a VoicePromptDirector in every scene that has voice objectives,
/// without any scene storing a reference to it.
///
/// This is why no scene builder had to change: the scenes are generated, and
/// anything added to them by hand is wiped the next time someone runs
/// Tools > SALINLAHI > Build Bahay Scene 2. Nothing that is created at
/// runtime can be wiped.
/// </summary>
public static class VoicePromptBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach();
    }

    private static void Attach()
    {
        // Runs after every Awake and before every Start, so the director is
        // subscribed in time for SceneObjectiveController.Start() raising the
        // scene's first objective.
        if (Object.FindFirstObjectByType<SceneObjectiveController>() == null) return;
        if (Object.FindFirstObjectByType<VoicePromptDirector>() != null) return;

        GameObject go = new GameObject("VoicePromptDirector");
        go.AddComponent<VoicePromptDirector>();
    }
}
