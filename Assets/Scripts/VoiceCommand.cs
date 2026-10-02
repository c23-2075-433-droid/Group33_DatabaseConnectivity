using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SALINLAHI voice-command prototype: listens for the spoken word "Talon"
/// and calls the player's existing Jump() method when it's heard.
///
/// Uses the free "UnitySpeechToText" plugin (yasirkula), which wraps the
/// device's built-in speech recognizer (the same one Google Assistant /
/// Gboard voice typing use). See the setup instructions for how to import it
/// — this script assumes the plugin is already in the project.
///
/// This is intentionally a SEPARATE script from PlayerMovement, per the
/// project's rule: voice recognition should call the existing Jump()
/// function rather than creating a second jump system.
/// </summary>
public class VoiceCommand : MonoBehaviour, ISpeechToTextListener
{
    [Header("References")]
    [Tooltip("The player's existing PlayerMovement script. Its Jump() method is called when 'Talon' is recognized.")]
    public PlayerMovement player;

    [Tooltip("Optional UI Text that shows the recognized speech, for debugging. Leave empty if you don't have one yet.")]
    public Text recognizedTextDisplay;

    [Header("Recognition Settings")]
    [Tooltip("BCP-47 locale to request from the device's speech recognizer. 'fil-PH' is Filipino (Philippines).")]
    public string preferredLanguage = "fil-PH";

    [Tooltip("The word that triggers a jump. This is a general movement ability (not tied to a " +
             "specific lesson objective), so it's always checked - regardless of whatever " +
             "SceneObjectiveController word is currently active. Matching is case-insensitive " +
             "and checks if the recognized phrase CONTAINS this word (more forgiving than an exact match).")]
    public string triggerWord = "talon";

    [Tooltip("If true, automatically starts listening again a moment after each result, so the player can keep speaking repeatedly without pressing anything.")]
    public bool listenContinuously = true;

    [Tooltip("Seconds to wait before restarting listening after a result, when Listen Continuously is on.")]
    public float restartDelay = 0.5f;

    [Header("Vocabulary Answer Checking")]
    [Tooltip("When set, incoming speech is checked against this Tagalog word - this is how EVERY " +
             "taught word (Bangon, Tayo, Lakad, and every future vocabulary word) is recognized. " +
             "Normally driven by SceneObjectiveController, not set by hand. " +
             "Uses PronunciationChecker, which tolerates small recognizer mistakes.")]
    public string currentTargetWord = "";

    [Tooltip("Extra accepted spellings/mishearings for the current target word (optional).")]
    public string[] currentTargetWordVariants;

    /// <summary>
    /// Raised after every recognition attempt while currentTargetWord is set:
    /// (spokenText, wasCorrect). Hook this up to your feedback system (correct
    /// animation/sound, or the "mali mali mali" wrong-answer cue).
    /// </summary>
    public event Action<string, bool> OnAnswerChecked;

    // --- Microphone button listening indicator (see VoiceInteractionUI) ---
    // True while the device's speech recognizer is actively listening for the
    // player to speak (between OnReadyForSpeech and the next result). The mic
    // button pulses while this is true and sits still otherwise.
    public bool IsListening { get; private set; }
    public event Action<bool> OnListeningStateChanged;

    private void SetListening(bool listening)
    {
        if (IsListening == listening) return;
        IsListening = listening;
        OnListeningStateChanged?.Invoke(listening);
    }

    private bool isInitialized = false;

    // The speech plugin holds on to this script as its listener, and results can
    // still arrive after the object is gone (stopping Play, or loading another
    // scene, mid-recognition). Touching anything on a destroyed MonoBehaviour
    // then throws MissingReferenceException, so every callback checks this first.
    private bool isShuttingDown = false;

    void OnDestroy()
    {
        isShuttingDown = true;
        CancelInvoke();                      // drop any queued StartListening()
        SetListening(false);

        // Tell the recognizer to stop, so it doesn't keep delivering results
        // to a listener that no longer exists.
        try
        {
            if (isInitialized && SpeechToText.IsBusy()) SpeechToText.Cancel();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("VoiceCommand: couldn't cancel speech recognition on destroy: " + e.Message);
        }
    }

#if UNITY_EDITOR
    [Header("Editor Testing Only")]
    [Tooltip("Editor-only keyboard shortcuts that simulate voice commands, since the speech plugin doesn't run in the Editor (Android/iOS only). Stripped out of real builds.")]
    public bool enableKeyboardFallback = true;

    void Update()
    {
        if (!enableKeyboardFallback || player == null) return;

        // These go through SimulateSpeech, NOT straight to PlayerMovement, so
        // the Editor exercises the real pipeline: objective matching,
        // OnAnswerChecked, and score tracking all behave as they do on device.
        if (Input.GetKeyDown(KeyCode.B)) SimulateSpeech("bangon");
        else if (Input.GetKeyDown(KeyCode.T)) SimulateSpeech("tayo");
        else if (Input.GetKeyDown(KeyCode.L)) SimulateSpeech("lakad");
        else if (Input.GetKeyDown(KeyCode.J)) SimulateSpeech(triggerWord);
        // A deliberately wrong answer, to test the "try again" path and see
        // attempts counted without the objective advancing.
        else if (Input.GetKeyDown(KeyCode.X)) SimulateSpeech("mali");
        // Raw movement check, bypassing the word system on purpose.
        else if (Input.GetKeyDown(KeyCode.K)) player.WalkLeft();
    }
#endif

    void Awake()
    {
        // Initialize with our preferred language. If the device doesn't support
        // it, the plugin falls back to the device's default recognition language
        // rather than crashing.
        try
        {
            SpeechToText.Initialize(preferredLanguage);
            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("VoiceCommand: Speech-to-text failed to initialize: " + e.Message);
            isInitialized = false;
        }
    }

    void Start()
    {
        SetStatusText("Requesting microphone permission...");
        RequestPermissionAndListen();
    }

    /// <summary>
    /// Requests microphone permission (if not already granted) and starts
    /// listening once granted. Safe to call again any time you want to
    /// (re)start listening, e.g. from a "Listen" button.
    /// </summary>
    public void RequestPermissionAndListen()
    {
        if (!isInitialized)
        {
            SetStatusText("Speech recognition unavailable on this device.");
            return;
        }

        SpeechToText.RequestPermissionAsync((permission) =>
        {
            // The permission dialog is async too, so the same "destroyed while
            // we were waiting" case applies here.
            if (isShuttingDown || this == null) return;

            if (permission == SpeechToText.Permission.Granted)
            {
                StartListening();
            }
            else
            {
                SetStatusText("Microphone permission denied. Voice commands won't work.");
                SetListening(false);
            }
        });
    }

    private void StartListening()
    {
        if (isShuttingDown || this == null) return;
        if (!isInitialized) return;

        // Don't try to start a new session if one's already running.
        if (SpeechToText.IsBusy()) return;

        SetStatusText("Listening...");
        SpeechToText.Start(this, preferOfflineRecognition: false);
    }

    // ---- ISpeechToTextListener callbacks ----

    // The recognizer is now actively listening for speech - this is the
    // moment the microphone button should start its "listening" pulse.
    void ISpeechToTextListener.OnReadyForSpeech()
    {
        if (isShuttingDown || this == null) return;
        SetListening(true);
    }

    void ISpeechToTextListener.OnBeginningOfSpeech() { }

    void ISpeechToTextListener.OnVoiceLevelChanged(float normalizedVoiceLevel) { }

    void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
    {
        if (isShuttingDown || this == null) return;

        // Live partial results, shown as they come in (before the final result).
        SetStatusText("Recognized (partial):\n\"" + spokenText + "\"");
    }

    void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
    {
        // A result can land after this object was destroyed (Play stopped or
        // scene changed while the recognizer was still working) - bail out
        // rather than touching a dead object.
        if (isShuttingDown || this == null) return;

        // A result just came in, so the recognizer has stopped actively
        // listening for this cycle - stop the mic button's pulse. If
        // listenContinuously is on, OnReadyForSpeech() will turn it back on
        // shortly once StartListening() runs again below.
        SetListening(false);

        if (errorCode.HasValue)
        {
            // Recognition failed for this attempt (e.g. no speech detected, no
            // internet, etc.) — handle gracefully, never crash.
            SetStatusText("Recognition error (code " + errorCode.Value + "). Try again.");
        }
        else
        {
            ProcessRecognizedText(spokenText);
        }

        if (listenContinuously)
        {
            Invoke(nameof(StartListening), restartDelay);
        }
    }

    /// <summary>
    /// Feeds a phrase through exactly the same handling as real recognized
    /// speech. Public so the Editor keyboard fallback (and any future
    /// tap-to-answer fallback) exercises the whole pipeline - objective
    /// checking, OnAnswerChecked listeners, score tracking - rather than
    /// short-cutting straight to PlayerMovement.
    /// </summary>
    public void SimulateSpeech(string spokenText)
    {
        Debug.Log("VoiceCommand: simulating recognized speech \"" + spokenText + "\"");
        ProcessRecognizedText(spokenText);
    }

    private void ProcessRecognizedText(string spokenText)
    {
        SetStatusText("Recognized:\n\"" + spokenText + "\"");

        string spokenLower = string.IsNullOrEmpty(spokenText) ? "" : spokenText.ToLowerInvariant();

        // Talon -> Jump is a general movement ability, not tied to any
        // specific lesson objective, so it's always checked - independent
        // of whatever word SceneObjectiveController currently has set below.
        if (spokenLower.Contains(triggerWord.ToLowerInvariant()) && player != null)
        {
            player.Jump();
        }

        // Vocabulary / lesson-objective mode: while a target word is set
        // (driven by SceneObjectiveController), every result is a
        // right/wrong check against it. This is how ALL taught words -
        // Bangon, Tayo, Lakad, and every future vocabulary word - are
        // recognized. This is also what "mali mali mali" and
        // correct-answer feedback hook into.
        if (!string.IsNullOrEmpty(currentTargetWord))
        {
            bool isCorrect = PronunciationChecker.IsCorrect(
                spokenText, currentTargetWord, currentTargetWordVariants);

            SetStatusText((isCorrect ? "Correct! " : "Try again. ") +
                           "Heard: \"" + spokenText + "\" (target: \"" + currentTargetWord + "\")");

            OnAnswerChecked?.Invoke(spokenText, isCorrect);
        }
    }

    private void SetStatusText(string text)
    {
        Debug.Log("VoiceCommand: " + text);
        if (recognizedTextDisplay != null)
        {
            recognizedTextDisplay.text = text;
        }
    }
}
