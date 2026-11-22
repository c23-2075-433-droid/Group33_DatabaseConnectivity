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
             "SceneObjectiveController word is currently active. Matched by PronunciationChecker, " +
             "the same way a lesson word is, so \"talong\" no longer makes the player jump.")]
    public string triggerWord = "talon";

    [Tooltip("If true, automatically starts listening again a moment after each result, so the player can keep speaking repeatedly without pressing anything.")]
    public bool listenContinuously = true;

    [Tooltip("Seconds to wait before restarting listening after a result, when Listen Continuously is on.")]
    public float restartDelay = 0.5f;

    [Tooltip("Dictation language model (on) vs the device's search-focused one (off). Dictation is " +
             "tuned for sentences and likes to 'correct' one spoken word into a phrase, while the search " +
             "model tends to return shorter, more literal text for a one-word answer. Worth trying both " +
             "on a real device and comparing the attempt log.")]
    public bool useFreeFormLanguageModel = true;

    [Tooltip("Write every attempt to a CSV - the target word, what the recognizer heard, and right/wrong - " +
             "so the WordVariants table can be tuned from real playtests instead of guesses. " +
             "See VoiceAttemptLog for where the file lands and how to pull it off a phone.")]
    public bool logAttempts = true;

    [Header("Stress Checking (tayô vs táyo)")]
    [Tooltip("For the words whose meaning depends on their stress (the ones WordStress marks: Tayô, " +
             "Upô, Bukás, Bása, Talón, Báti), record the player's voice while the word is asked and " +
             "check WHICH syllable they stressed - length, loudness and pitch - against Teacher " +
             "Rachel's own recording. The recognizer returns letters only and cannot tell tayô (stand " +
             "up) from táyo (we). See AnswerJudge and StressAudioCapture.")]
    public bool checkStressFromAudio = true;

    [Tooltip("Also record the player's voice with Unity's own microphone for the stress check. OFF by " +
             "default: on some phones the speech recognizer cannot get the microphone while this " +
             "recording holds it, and then nothing the child says is heard. With it off, stress is " +
             "judged from the recognizer's own loudness readings, which never touch the microphone.")]
    public bool recordOwnVoice = false;

    [Tooltip("ANDROID 13+, the six strictly judged words: the game records the child itself and hands that " +
             "same audio to Android's speech recognizer (Plugins/Android/SalinlahiAudioRecognizer.java), so the " +
             "recognizer checks WHICH word and the pronunciation model checks HOW it was said. Phones that do not " +
             "support it are detected and fall back to normal listening for the session.")]
    public bool listenWithAudioBridge = true;

    [Tooltip("TESTING ONLY: show the audio bridge's on-screen box and short messages (whether it works on this " +
             "phone, each listening round, error codes). Shown only in a Development Build - a normal (release) " +
             "build never shows them, even with this ticked.")]
    public bool showAudioBridgeMessages = false;

    /// <summary>The test box and messages: only when asked for AND in a development build (never in a release APK).</summary>
    private bool BridgeMessagesOn { get { return showAudioBridgeMessages && Debug.isDebugBuild; } }

    [Tooltip("LENIENT MODE ONLY (Accept Only Rachels Pronunciation off): where the old single stress " +
             "cut-off sits, 1 = halfway between Rachel's stress and the moved one. Strict mode does not use " +
             "it: its Correct and WrongStress lines are calibrated per word (PronunciationReferences).")]
    [Range(0.25f, 1.75f)]
    public float stressTolerance = 1f;

    [Tooltip("Only when the device gives NO audio and NO voice levels at all, so the stress can never " +
             "be measured: let the letters decide rather than leave the child stuck on the word. A " +
             "recording or level stream that exists but is unclear is never waved through.")]
    public bool acceptOnLettersWhenNoStressEvidence = true;

    /// <summary>How the answer's stress counts. See AnswerJudge.</summary>
    public enum StressCheck
    {
        /// <summary>The word first: stress only matters for same-spelling pairs, and only when clearly the other word.</summary>
        Balanced,
        /// <summary>Stress must be verified as Rachel's on the trusted words; other words reject only a clear wrong stress.</summary>
        StrictOnTrustedWords,
        /// <summary>Stress must be verified as Rachel's on every word with a known stress.</summary>
        StrictOnEveryWord,
        /// <summary>The rules before strict mode existed.</summary>
        Lenient,
    }

    [Tooltip("BALANCED (default): the right WORD is what counts. Different stress, speed, pitch, intonation or " +
             "accent never fail an ordinary word like Bangon. Only six words - Tayo, Upo, Bukas, Basa, Bati, " +
             "Pasok (AmbiguousWords) - get the strict hybrid check, because there a different pronunciation is a " +
             "different word. STRICT ON TRUSTED WORDS / STRICT ON EVERY WORD: the player must match " +
             "Rachel's stress (much stricter). LENIENT: the older rules. See AnswerJudge.")]
    public StressCheck stressCheck = StressCheck.Balanced;

    [Tooltip("For the six strictly judged words (Tayo, Upo, Bukas, Basa, Bati, Pasok): decide with the " +
             "classifier trained on the group's own recorded speakers (PronunciationModel, from " +
             "Tools/pronunciation_dataset), keeping the older Rachel-based judges only as a safety net. " +
             "Off: the older hybrid check alone. Needs the player's audio (Editor/desktop, or Record Own Voice).")]
    public bool useLearnedPronunciationModel = true;

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

    /// <summary>
    /// Raised when the player clearly SAID something and the recognizer could
    /// not make it out. Deliberately separate from OnAnswerChecked(.., false):
    /// saying the WRONG word and saying something unintelligible need
    /// different things from the child, so the narrator answers them
    /// differently. This raises no answer and is never scored.
    ///
    /// Pure silence does NOT raise this. The recognizer times out every few
    /// seconds while nobody is talking, and a game that said "I didn't hear
    /// you" every five seconds at a child who is still thinking would be
    /// unbearable - that case belongs to the narrator's idle nudge instead.
    /// </summary>
    public event Action OnNothingHeard;

    /// <summary>
    /// Raised when speech recognition cannot be used at all - permission was
    /// denied, or the device has no recognizer. The narrator points the player
    /// at the microphone button.
    /// </summary>
    public event Action OnMicrophoneUnavailable;

    /// <summary>
    /// Raised the moment the recognizer notices the player has started
    /// talking, before it knows what they said. The gap between this and
    /// OnAnswerChecked is the only window in which the game is genuinely
    /// working something out, which is what the status panel shows as
    /// "PROCESSING...". It is a notification only - nothing here decides
    /// anything or is scored.
    /// </summary>
    public event Action OnSpeechBegan;

    /// <summary>
    /// Raised after OnAnswerChecked(.., false) when the letters were right but
    /// the stress was on the wrong syllable - the child said táyo ("we") when
    /// tayô ("stand up") was asked. The narrator answers this one by saying
    /// the word again, so the child can hear where the stress goes.
    /// (word, stress pattern)
    /// </summary>
    public event Action<string, StressPattern> OnWrongStress;

    /// <summary>
    /// Raised INSTEAD of OnAnswerChecked when the right word was said but its
    /// stress could not be judged ("tayo po", or too faint to hear the
    /// syllables). Not scored and not a miss: the child is just asked to say
    /// the word again, on its own. (word, reason)
    /// </summary>
    public event Action<string, string> OnStressUnclear;

    /// <summary>The full verdict on the most recent answer, for listeners that need more than right/wrong.</summary>
    public AnswerJudge.Result LastJudgement { get; private set; }

    private StressAudioCapture stressCapture;

    // The recognizer's voice-level readings for the current listening
    // session, and when the first and last arrived (to know their rate).
    private readonly System.Collections.Generic.List<float> levelReadings = new System.Collections.Generic.List<float>();
    private double firstLevelAtMs = -1, lastLevelAtMs = -1;

    // Recognizer failures in a row while our own recording was running; see
    // StressAudioCapture for why this switches the recording off.
    private int failuresWhileCapturing;
    // True from the moment our own recording was started for this listening
    // round until its result arrives. Failures are counted on this, not on
    // whether the recording is STILL running when the failure arrives: a
    // phone that hands the microphone to the recognizer may stop our
    // recording first, and those failures must still count.
    private bool capturedThisRound;

    // ---- The audio bridge (Android 13+; see listenWithAudioBridge) ----------------
    private AndroidAudioRecognizer bridge;
    private bool bridgeRound;            // a bridge listening round is running
    private float bridgeRoundStarted;
    private bool bridgeDisabled;         // gave up for this session
    private bool bridgeWorked;           // at least one answer came through it
    private int bridgeMode;              // 0 = the standard recognizer, 1 = on-device
    private int bridgeFailures;          // failed rounds in a row in which the child DID talk
    private const int BridgeFailuresPerMode = 2;
    private const float BridgeRoundTimeout = 15f;

    private bool UseBridge
    {
        get
        {
            return listenWithAudioBridge && !bridgeDisabled && !string.IsNullOrEmpty(currentTargetWord) &&
                   AmbiguousWords.IsAmbiguous(currentTargetWord) && AndroidAudioRecognizer.IsSupported;
        }
    }

    private void BridgeMessage(string text)
    {
        if (BridgeMessagesOn) AndroidAudioRecognizer.Toast(text);
        else Debug.Log("VoiceCommand: audio bridge - " + text);
    }

    void OnEnable() { AndroidAudioRecognizer.ShowDiagnostics = BridgeMessagesOn; }

    private void StartBridgeRound()
    {
        if (bridge == null)
        {
            bridge = AndroidAudioRecognizer.Instance;
            bridge.Ready += HandleBridgeReady;
            bridge.SpeechBegan += HandleBridgeSpeechBegan;
            bridge.Result += HandleBridgeResult;
            bridge.Error += HandleBridgeError;
        }
        SetStatusText("Listening...");
        speechStartedThisSession = false;
        ResetLatency();
        ResetLevels();
        if (stressCapture != null) stressCapture.Stop();
        bridgeRound = true;
        bridgeRoundStarted = Time.realtimeSinceStartup;
        Debug.Log("VoiceCommand: audio bridge listening for " + currentTargetWord + " (" +
                  (bridgeMode == 0 ? "standard" : "on-device") + " recognizer)");
        AndroidAudioRecognizer.Note("listening for " + currentTargetWord + " (" + (bridgeMode == 0 ? "standard" : "on-device") + ")");
        bridge.StartListening(preferredLanguage, bridgeMode == 1);
    }

    private void StopBridgeRound()
    {
        if (!bridgeRound) return;
        bridgeRound = false;
        if (bridge != null) bridge.Cancel();
        SetListening(false);
    }

    private void HandleBridgeReady()
    {
        if (isShuttingDown || this == null || !bridgeRound) return;
        readyAtMs = NowMs;
        SetListening(true);
        AndroidAudioRecognizer.Note("recognizer ready");
    }

    private void HandleBridgeSpeechBegan()
    {
        if (isShuttingDown || this == null || !bridgeRound) return;
        speechStartedThisSession = true;
        if (speechBeganAtMs < 0) speechBeganAtMs = NowMs;
        OnSpeechBegan?.Invoke();
        AndroidAudioRecognizer.Note("speech began");
    }

    private void HandleBridgeResult(string text)
    {
        if (isShuttingDown || this == null || !bridgeRound) return;
        bridgeRound = false;
        SetListening(false);
        resultAtMs = NowMs;
        latencyIsReal = true;
        int rate;
        float[] audio = bridge.TakeAudio(out rate);

        AndroidAudioRecognizer.Note("heard \"" + text + "\", audio " + Describe(audio, rate));
        if (string.IsNullOrWhiteSpace(text)) { BridgeRoundFailed("empty result", audio, false); return; }

        if (!bridgeWorked)
            BridgeMessage("Audio listening works on this phone (" + (bridgeMode == 0 ? "standard" : "on-device") + " recognizer)");
        bridgeWorked = true;
        bridgeFailures = 0;
        AnswerJudge.Evidence e = new AnswerJudge.Evidence();
        if (StressWordLive && audio != null) { e.audio = audio; e.audioSampleRate = rate; }
        ProcessRecognizedText(text, e);
        RestartAfterRound();
    }

    private void HandleBridgeError(string code)
    {
        if (isShuttingDown || this == null || !bridgeRound) return;
        bridgeRound = false;
        SetListening(false);
        int rate = 16000;
        float[] audio = bridge != null ? bridge.TakeAudio(out rate) : null;
        int c;
        bool normalSilence = int.TryParse(code, out c) && (c == 6 || c == 7);   // speech timeout / no match
        AndroidAudioRecognizer.Note("error " + AndroidAudioRecognizer.ErrorName(code) + ", audio " + Describe(audio, rate));
        BridgeRoundFailed("error " + AndroidAudioRecognizer.ErrorName(code), audio, !normalSilence);
    }

    private static string Describe(float[] audio, int rate)
    {
        if (audio == null || audio.Length == 0) return "none";
        float peak = 0;
        foreach (float v in audio) peak = Math.Max(peak, Math.Abs(v));
        return (audio.Length / (float)rate).ToString("0.0") + " s, peak " + peak.ToString("0.00");
    }

    /// <summary>
    /// A round that ended without a word. Until the bridge has worked once on
    /// this phone, the round counts against it when the child talked (the
    /// game's own recording has a voice in it), or when the failure was not
    /// the recognizer's ordinary "heard nothing" (a refusal, a timeout, an
    /// audio error): a round of plain silence is just silence. Two counted
    /// failures switch to the on-device recognizer; two more, and the bridge
    /// is given up for this session and the game listens the normal way.
    /// </summary>
    private void BridgeRoundFailed(string why, float[] audio, bool hardFailure)
    {
        float peak = 0;
        if (audio != null) foreach (float v in audio) peak = Math.Max(peak, Math.Abs(v));
        bool talked = peak > 0.05f || speechStartedThisSession;
        Debug.Log("VoiceCommand: audio bridge round failed (" + why + "), peak " + peak.ToString("0.000") +
                  (talked ? " - the child talked" : " - silence") + (hardFailure ? ", hard failure" : ""));

        if ((talked || hardFailure) && !bridgeWorked)
        {
            if (++bridgeFailures >= BridgeFailuresPerMode)
            {
                bridgeFailures = 0;
                if (bridgeMode == 0)
                {
                    bridgeMode = 1;
                    BridgeMessage("Audio listening: trying the on-device recognizer (" + why + ")");
                    AndroidAudioRecognizer.Note("SWITCHED to the on-device recognizer");
                }
                else
                {
                    bridgeDisabled = true;
                    BridgeMessage("Audio listening is not supported on this phone (" + why + ") - using normal listening");
                    AndroidAudioRecognizer.Note("GAVE UP - normal listening from now on");
                }
            }
        }
        if (talked && speechStartedThisSession && OnNothingHeard != null) OnNothingHeard();
        RestartAfterRound();
    }

    private void RestartAfterRound()
    {
        if (listenContinuously && !isSuspended) Invoke(nameof(StartListening), restartDelay);
    }
    private const int FailuresBeforeCaptureOff = 3;

    private bool StressWordLive
    {
        get { return checkStressFromAudio && !string.IsNullOrEmpty(currentTargetWord) && StressPattern.For(currentTargetWord) != null; }
    }

    private StressAudioCapture Capture
    {
        get
        {
            if (stressCapture == null)
            {
                stressCapture = GetComponent<StressAudioCapture>();
                if (stressCapture == null) stressCapture = gameObject.AddComponent<StressAudioCapture>();
            }
            return stressCapture;
        }
    }

    private static StressAnalyzer.LevelScale DeviceLevelScale
    {
        get
        {
#if UNITY_IOS
            return StressAnalyzer.LevelScale.LinearDecibels;
#else
            return StressAnalyzer.LevelScale.AndroidPower;
#endif
        }
    }

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

    // Set while the narrator is talking. On a phone speaker the recognizer
    // hears Rachel saying "Sabihin mo: Lakad" perfectly well and scores it as
    // the player's own attempt, so listening stops for as long as she speaks.
    private bool isSuspended = false;

    /// <summary>
    /// Stops listening until ResumeListening. Called by VoiceNarrator around
    /// every spoken line.
    /// </summary>
    public void SuspendListening()
    {
        if (isShuttingDown) return;
        isSuspended = true;

        CancelInvoke(nameof(StartListening));   // drop any queued restart
        SetListening(false);
        if (stressCapture != null) stressCapture.Stop();
        StopBridgeRound();

        try
        {
            if (isInitialized && SpeechToText.IsBusy()) SpeechToText.Cancel();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("VoiceCommand: couldn't pause speech recognition: " + e.Message);
        }
    }

    /// <summary>Starts listening again after SuspendListening.</summary>
    public void ResumeListening()
    {
        if (isShuttingDown || !isSuspended) return;
        isSuspended = false;
        StartListening();
    }

    void OnDestroy()
    {
        isShuttingDown = true;
        if (stressCapture != null) stressCapture.Stop();
        StopBridgeRound();
        if (bridge != null)
        {
            bridge.Ready -= HandleBridgeReady;
            bridge.SpeechBegan -= HandleBridgeSpeechBegan;
            bridge.Result -= HandleBridgeResult;
            bridge.Error -= HandleBridgeError;
        }
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
        // No PlayerMovement needed. Only "Talon" is about the player, and
        // ProcessRecognizedText checks that reference itself, so a scene that
        // teaches words without a walking character can still be answered -
        // which matters now that the keyboard is the ONLY way to answer in
        // the Editor.
        if (!enableKeyboardFallback) return;
        if (!Input.GetKey(KeyCode.R)) StopCaptureIfNoStressWord();

        // R, held: say the current word into the computer's microphone, and
        // let go. The letters are taken as the word itself (there is no
        // recognizer here), and the recording goes through the real stress
        // check - so tayô and táyo can be tried out by ear in the Editor.
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (string.IsNullOrEmpty(currentTargetWord))
                Debug.Log("VoiceCommand (Editor): no objective is active right now.");
            else if (Capture.Begin())
            {
                Capture.MarkStart();
                Debug.Log("VoiceCommand (Editor): recording - say \"" + WordStress.Display(currentTargetWord) +
                          "\" and let go of R.");
            }
            else
                Debug.LogWarning("VoiceCommand (Editor): R pressed but the microphone could not start - " +
                                 (Capture.DisabledReason ?? "no reason given"));
        }
        else if (Input.GetKeyUp(KeyCode.R) && Capture.IsRecording)
        {
            int rate;
            float[] audio = Capture.TakeAnswer(out rate);
            Capture.Stop();
            if (audio == null) { Debug.Log("VoiceCommand (Editor): the recording was silent."); return; }

            // The R key's own click, on release, lands at the very end of the
            // recording on a desk microphone; drop it so it cannot read as a
            // (short, quiet) last syllable.
            int trim = Mathf.Min(audio.Length / 4, rate * 120 / 1000);
            if (trim > 0) System.Array.Resize(ref audio, audio.Length - trim);

            // The word asked for, taken BEFORE the answer is judged: a correct
            // answer moves the scene on to its next objective, and the label
            // (F5/F6/F7) belongs to the word that was actually said.
            string askedWord = currentTargetWord;
            SimulateSpeech(askedWord, audio, rate);
            SaveEditorRecording(audio, rate, LastJudgement);
            lastEditorAudio = audio;
            lastEditorRate = rate;
            lastEditorWord = askedWord;
            Debug.Log("VoiceCommand (Editor): to keep this recording for calibration, press F5 if you said it the way " +
                      "Rachel does, F6 if the stress was on the wrong syllable, F7 if a sound was wrong.");
            return;
        }

        // Label the last hold-R recording with what was REALLY said, for
        // Tools/test_stress_rules.sh labelled - the check against real
        // voices that the synthetic tests cannot replace.
        if (lastEditorAudio != null)
        {
            string label = Input.GetKeyDown(KeyCode.F5) ? "correct"
                         : Input.GetKeyDown(KeyCode.F6) ? "wrong_stress"
                         : Input.GetKeyDown(KeyCode.F7) ? "wrong_sound" : null;
            if (label != null)
            {
                SaveLabelledRecording(lastEditorAudio, lastEditorRate, lastEditorWord, label);
                lastEditorAudio = null;
            }
        }

        // These go through SimulateSpeech, NOT straight to PlayerMovement, so
        // the Editor exercises the real pipeline: objective matching,
        // OnAnswerChecked, and score tracking all behave as they do on device.

        // ENTER says whatever word the scene is currently asking for. This is
        // the one to use: it works in every scene, including ones built later,
        // without needing a new key per word.
        //
        // It deliberately does nothing while Rachel is still introducing a
        // word. No word is being asked for until she finishes - the badge is
        // faint and currentTargetWord is empty - so pressing ENTER over the
        // narration logs the line below instead of answering. That is the
        // same rule the child plays by, not a broken shortcut.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrEmpty(currentTargetWord)) SimulateSpeech(currentTargetWord);
            else Debug.Log("VoiceCommand (Editor fallback): no objective is active right now.");
        }
        // Individual words, for answering out of order or checking one action.
        else if (Input.GetKeyDown(KeyCode.B)) SimulateSpeech("bangon");
        else if (Input.GetKeyDown(KeyCode.T)) SimulateSpeech("tayo");
        else if (Input.GetKeyDown(KeyCode.L)) SimulateSpeech("lakad");
        else if (Input.GetKeyDown(KeyCode.K)) SimulateSpeech("kaliwa");
        else if (Input.GetKeyDown(KeyCode.N)) SimulateSpeech("kanan");
        else if (Input.GetKeyDown(KeyCode.O)) SimulateSpeech("bukas");
        else if (Input.GetKeyDown(KeyCode.I)) SimulateSpeech("ilaw");
        else if (Input.GetKeyDown(KeyCode.U)) SimulateSpeech("kunin");
        // Scene 3, the washing routine: towel and soap, the bath itself,
        // then teeth and hair. (No "tabo" - the dipper is not a word.)
        else if (Input.GetKeyDown(KeyCode.W)) SimulateSpeech("tuwalya");
        else if (Input.GetKeyDown(KeyCode.S)) SimulateSpeech("sabon");
        else if (Input.GetKeyDown(KeyCode.M)) SimulateSpeech("maligo");
        else if (Input.GetKeyDown(KeyCode.P)) SimulateSpeech("sipilyo");
        else if (Input.GetKeyDown(KeyCode.C)) SimulateSpeech("suklay");
        // Scene 4, getting dressed. ("Kunin" is U, above.)
        else if (Input.GetKeyDown(KeyCode.D)) SimulateSpeech("damit");
        else if (Input.GetKeyDown(KeyCode.Z)) SimulateSpeech("medyas");
        else if (Input.GetKeyDown(KeyCode.F)) SimulateSpeech("sapatos");
        else if (Input.GetKeyDown(KeyCode.V)) SimulateSpeech("bag");
        // Paaralan Scene 1, arriving at school.
        else if (Input.GetKeyDown(KeyCode.Q)) SimulateSpeech("kumusta");
        else if (Input.GetKeyDown(KeyCode.E)) SimulateSpeech("pasok");
        else if (Input.GetKeyDown(KeyCode.G)) SimulateSpeech("takbo");
        else if (Input.GetKeyDown(KeyCode.J)) SimulateSpeech(triggerWord);
        // A deliberately wrong answer, to test the "try again" path and see
        // attempts counted without the objective advancing.
        else if (Input.GetKeyDown(KeyCode.X)) SimulateSpeech("mali");
    }
#endif

#if !UNITY_EDITOR
    void Update()
    {
        StopCaptureIfNoStressWord();
        if (isShuttingDown || isSuspended || !isInitialized) return;

        // The scene changed its word: move between the bridge (the six words)
        // and the normal recognizer (every other word) right away.
        if (bridgeRound && !UseBridge)
        {
            StopBridgeRound();
            StartListening();
        }
        else if (!bridgeRound && UseBridge && SpeechToText.IsBusy())
        {
            SpeechToText.Cancel();
            CancelInvoke(nameof(StartListening));
            Invoke(nameof(StartListening), 0.2f);
        }
        else if (bridgeRound && Time.realtimeSinceStartup - bridgeRoundStarted > BridgeRoundTimeout)
        {
            int rate = 16000;
            float[] audio = null;
            if (bridge != null) { bridge.KeepNow(); audio = bridge.TakeAudio(out rate); }
            StopBridgeRound();
            AndroidAudioRecognizer.Note("no answer in " + BridgeRoundTimeout + " s, audio " + Describe(audio, rate));
            BridgeRoundFailed("no answer from the recognizer in " + BridgeRoundTimeout + " s", audio, true);
        }
    }
#endif

#if UNITY_EDITOR
    private static int editorRecordingCount;
    private float[] lastEditorAudio;
    private int lastEditorRate;
    private string lastEditorWord;

    /// <summary>
    /// Editor only: keeps a hold-R recording under Recordings/Labelled/WORD/LABEL
    /// (ignored by git: voices never leave this computer), for
    /// Tools/test_stress_rules.sh labelled to judge.
    /// </summary>
    private static void SaveLabelledRecording(float[] audio, int rate, string word, string label)
    {
        try
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Recordings", "Labelled",
                                                                           word.Trim(), label));
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");
            WriteWav(path, audio, rate);
            Debug.Log("VoiceCommand (Editor): kept as " + label + " -> " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("VoiceCommand (Editor): could not keep the labelled recording: " + e.Message);
        }
    }

    private static void WriteWav(string path, float[] audio, int rate)
    {
        using (var w = new System.IO.BinaryWriter(System.IO.File.Create(path)))
        {
            int bytes = audio.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2);
            w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (float v in audio) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
        }
    }

    /// <summary>
    /// Editor only: keeps each hold-R recording as a WAV under
    /// Library/StressRecordings (never in a build, ignored by git), named with
    /// its verdict and score, so a disputed verdict can be listened to and
    /// re-measured instead of guessed at.
    /// </summary>
    private static void SaveEditorRecording(float[] audio, int rate, AnswerJudge.Result verdict)
    {
        try
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Library", "StressRecordings");
            System.IO.Directory.CreateDirectory(dir);
            string score = float.IsNaN(verdict.stress.measured) ? "na"
                : verdict.stress.measured.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string word = verdict.pattern != null ? verdict.pattern.Word : "word";
            string name = System.DateTime.Now.ToString("HHmmss") + "_" + (++editorRecordingCount) + "_" + word + "_" +
                          verdict.outcome + "_score" + score + ".wav";
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(dir, name));

            WriteWav(path, audio, rate);
            Debug.Log("VoiceCommand (Editor): recording saved to " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("VoiceCommand (Editor): could not save the recording: " + e.Message);
        }
    }
#endif

    /// <summary>The microphone is only ours while a stress-pair word is live.</summary>
    private void StopCaptureIfNoStressWord()
    {
        if (stressCapture != null && stressCapture.IsRecording && !StressWordLive) stressCapture.Stop();
    }

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
#if !UNITY_EDITOR
        SetStatusText("Requesting microphone permission...");
#endif
        RequestPermissionAndListen();
    }

    // ---- Why the recognizer is switched off in the Editor ---------------
    //
    // The speech plugin has no microphone in the Editor, so SpeechToText.Start
    // runs an emulator instead: it waits about 1.6 seconds and then delivers
    // the fixed string "Hello world" as a SUCCESSFUL result, with no error
    // code. Because listenContinuously is on, that repeats for as long as the
    // scene is playing, and nothing downstream can tell those results from a
    // child speaking. Every playtest therefore:
    //
    //   - counted an attempt every ~1.6 seconds while any word was live, so
    //     accuracy collapsed and every saved score came out at one star;
    //   - made VoicePromptDirector see a miss each cycle, so Rachel fired her
    //     slow hint at ~3 s and her replay hint at ~6 s at a child who had not
    //     said anything;
    //   - filled salinlahi_voice_attempts.csv with mali / "Hello world" rows,
    //     which is the log the WordVariants table is supposed to be tuned from.
    //
    // ("Hello world" no longer scores tama for Ilaw - a variant has to be the
    // whole recognizer result now - but it is still a wrong answer every cycle,
    // and "hello" on its own is still a listed variant of Ilaw.)
    //
    // So the recognizer is never started under UNITY_EDITOR. Speech comes from
    // the keyboard fallback in Update() instead - ENTER says whatever word the
    // scene is currently asking for - which runs through the same
    // ProcessRecognizedText as a real result, so the Editor still exercises
    // objective matching, feedback and scoring. Nothing here changes on a
    // device: the gate is compiled out of real builds.
    //
    // The emulator lives in the plugin (Plugins/SpeechToText/SpeechToText.cs),
    // which arrives as a git package, so it cannot be edited from this project
    // - the two start calls below are the only place to stop it.

    /// <summary>
    /// Requests microphone permission (if not already granted) and starts
    /// listening once granted. Safe to call again any time you want to
    /// (re)start listening, e.g. from a "Listen" button.
    /// </summary>
    public void RequestPermissionAndListen()
    {
        // Still checked in the Editor: this one is about the plugin failing to
        // initialize at all, which is worth hearing about wherever it happens.
        if (!isInitialized)
        {
            SetStatusText("Speech recognition unavailable on this device.");
            if (OnMicrophoneUnavailable != null) OnMicrophoneUnavailable();
            return;
        }

#if UNITY_EDITOR
        // Deliberately does NOT raise OnMicrophoneUnavailable: nothing is
        // broken, there is simply no microphone here, and that event has the
        // narrator point the child at the mic button. The panel sits on
        // SPEAK NOW instead, which is the truth - the game is waiting for you.
        SetListening(false);
        SetStatusText("Editor: the microphone is off here. Press ENTER to say the current word.");
        return;
#else
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
                if (OnMicrophoneUnavailable != null) OnMicrophoneUnavailable();
            }
        });
#endif
    }

    private void StartListening()
    {
#if UNITY_EDITOR
        // The other door into the emulator: ResumeListening after the narrator
        // finishes a line, and the queued restart after each result.
        SetListening(false);
        return;
#else
        if (isShuttingDown || this == null) return;
        if (!isInitialized) return;
        if (isSuspended) return;

        if (bridgeRound) return;
        if (UseBridge)
        {
            if (SpeechToText.IsBusy()) { SpeechToText.Cancel(); Invoke(nameof(StartListening), 0.2f); return; }
            StartBridgeRound();
            return;
        }

        // Don't try to start a new session if one's already running.
        if (SpeechToText.IsBusy()) return;

        SetStatusText("Listening...");
        speechStartedThisSession = false;
        ResetLatency();
        ResetLevels();
        // The recognizer must open the microphone first every time: a
        // recording still running from the last round can leave it with none.
        if (stressCapture != null) stressCapture.Stop();
        SpeechToText.Start(this, useFreeFormLanguageModel, preferOfflineRecognition: false);
#endif
    }

    // ---- ISpeechToTextListener callbacks ----

    // The recognizer is now actively listening for speech - this is the
    // moment the microphone button should start its "listening" pulse.
    void ISpeechToTextListener.OnReadyForSpeech()
    {
        if (isShuttingDown || this == null) return;
        readyAtMs = NowMs;
        SetListening(true);

        // Only now, with the recognizer holding the microphone: see
        // StressAudioCapture for why the order matters.
        bool record = recordOwnVoice;
        capturedThisRound = false;
        if (record && StressWordLive && !Capture.IsDisabled && Capture.Begin())
        {
            Capture.MarkStart();
            capturedThisRound = true;
        }
    }

    // Someone started talking. Remembered so a failed recognition can tell
    // "said something I couldn't make out" apart from "said nothing at all".
    private bool speechStartedThisSession = false;

    // ---- Latency measurement -------------------------------------------
    //
    // Activity 10 lists the voice round trip as unmeasured, and its monitoring
    // plan says to read it "from the timestamps in
    // salinlahi_voice_attempts.csv" - which was not possible, because that
    // column is a wall clock stamp per row. These four marks are the actual
    // measurement, taken from the recognizer's own callbacks and written to the
    // CSV's latency columns. VoiceAttemptLog documents what each column means
    // and which one is honest to quote.
    //
    // A Stopwatch rather than Time.realtimeSinceStartup: it is monotonic, it
    // does not care which thread the plugin delivers a callback on, and it does
    // not stop when the game is paused. One static clock for the whole run, so
    // the marks below are all on the same timebase.
    private static readonly System.Diagnostics.Stopwatch latencyClock =
        System.Diagnostics.Stopwatch.StartNew();

    private static double NowMs
    {
        get { return latencyClock.Elapsed.TotalMilliseconds; }
    }

    private double readyAtMs = -1;        // OnReadyForSpeech
    private double speechBeganAtMs = -1;  // OnBeginningOfSpeech
    private double lastPartialAtMs = -1;  // the most recent partial transcript
    private double resultAtMs = -1;       // OnResultReceived
    private int partialsThisSession;

    // False for simulated speech (the Editor keyboard fallback), where there is
    // no recognizer to time and every latency column must stay empty.
    private bool latencyIsReal;

    private void ResetLatency()
    {
        readyAtMs = -1;
        speechBeganAtMs = -1;
        lastPartialAtMs = -1;
        resultAtMs = -1;
        partialsThisSession = 0;
        latencyIsReal = false;
    }

    /// <summary>
    /// The timings for the attempt being logged right now. verdictMs is closed
    /// here, at the moment of logging, because logging happens immediately
    /// after the verdict is decided - so it measures this project's own
    /// canonicalisation and variant match and nothing else.
    /// </summary>
    private VoiceAttemptLog.Latency CurrentLatency()
    {
        if (!latencyIsReal) return VoiceAttemptLog.Latency.None;

        VoiceAttemptLog.Latency l = VoiceAttemptLog.Latency.None;
        l.partialCount = partialsThisSession;

        if (resultAtMs >= 0)
        {
            if (readyAtMs >= 0) l.listenToResultMs = resultAtMs - readyAtMs;
            if (speechBeganAtMs >= 0) l.speechToResultMs = resultAtMs - speechBeganAtMs;
            if (lastPartialAtMs >= 0) l.lastPartialToResultMs = resultAtMs - lastPartialAtMs;
            l.verdictMs = NowMs - resultAtMs;
        }

        return l;
    }

    void ISpeechToTextListener.OnBeginningOfSpeech()
    {
        if (isShuttingDown || this == null) return;
        speechStartedThisSession = true;
        // Only the first one: the recognizer can report speech beginning more
        // than once in a cycle, and the first is when the child started.
        if (speechBeganAtMs < 0) speechBeganAtMs = NowMs;
        OnSpeechBegan?.Invoke();
    }

    void ISpeechToTextListener.OnVoiceLevelChanged(float normalizedVoiceLevel)
    {
        if (isShuttingDown || this == null || isSuspended) return;
        double now = NowMs;
        if (firstLevelAtMs < 0) firstLevelAtMs = now;
        lastLevelAtMs = now;
        // A listening session is a few seconds; the cap only guards against a
        // recognizer that never stops.
        if (levelReadings.Count < 2000) levelReadings.Add(normalizedVoiceLevel);
    }

    private void ResetLevels()
    {
        levelReadings.Clear();
        firstLevelAtMs = -1;
        lastLevelAtMs = -1;
    }

    /// <summary>Average time between level readings this session; they arrive at a steady rate at source.</summary>
    private float LevelIntervalMs()
    {
        if (levelReadings.Count < 2 || firstLevelAtMs < 0) return 50f;
        double ms = (lastLevelAtMs - firstLevelAtMs) / (levelReadings.Count - 1);
        return ms > 1 ? (float)ms : 50f;
    }

    void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
    {
        if (isShuttingDown || this == null) return;

        // Partials stop arriving when the talking stops, so the last one is the
        // closest this plugin gets to an end-of-speech mark.
        lastPartialAtMs = NowMs;
        partialsThisSession++;

        // Live partial results, shown as they come in (before the final result).
        SetStatusText("Recognized (partial):\n\"" + spokenText + "\"");
    }

    void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
    {
        // Stamped first, before any handling at all, so verdictMs measures
        // this project's work rather than this method's own bookkeeping.
        double arrivedAtMs = NowMs;

        // A result can land after this object was destroyed (Play stopped or
        // scene changed while the recognizer was still working) - bail out
        // rather than touching a dead object.
        if (isShuttingDown || this == null) return;

        // The normal recognizer was cancelled to hand over to the audio
        // bridge; its farewell result is not an answer.
        if (bridgeRound) return;

        // Cancel() delivers a result of its own, so a result arriving while
        // the narrator is talking is our own interruption, not the player.
        if (isSuspended)
        {
            SetListening(false);
            return;
        }

        // A result just came in, so the recognizer has stopped actively
        // listening for this cycle - stop the mic button's pulse. If
        // listenContinuously is on, OnReadyForSpeech() will turn it back on
        // shortly once StartListening() runs again below.
        SetListening(false);

        resultAtMs = arrivedAtMs;
        latencyIsReal = true;

        if (errorCode.HasValue)
        {
            // Recognition failed for this attempt (e.g. no speech detected, no
            // internet, etc.) — handle gracefully, never crash.
            SetStatusText("Recognition error (code " + errorCode.Value + "). Try again.");

            // Counted whether or not speech began: a recognizer that could not
            // get the microphone never hears speech begin at all.
            if (capturedThisRound && stressCapture != null)
            {
                Debug.Log("VoiceCommand: recognizer failed (code " + errorCode.Value + ") while our own recording was on - " +
                          (failuresWhileCapturing + 1) + " of " + FailuresBeforeCaptureOff + " before recording is switched off");
                if (++failuresWhileCapturing >= FailuresBeforeCaptureOff)
                    stressCapture.DisableForSession(failuresWhileCapturing +
                        " recognizer failures in a row while recording - it may not be sharing the microphone");
            }

            // Only when the player actually spoke - see OnNothingHeard.
            if (speechStartedThisSession)
            {

                // Worth recording: a word the recognizer never manages to
                // transcribe needs a clearer prompt, not a new variant.
                if (logAttempts && !string.IsNullOrEmpty(currentTargetWord))
                    VoiceAttemptLog.RecordNothingHeard(currentTargetWord, CurrentLatency());

                if (OnNothingHeard != null) OnNothingHeard();
            }
        }
        else
        {
            // An empty result is the recognizer hearing nothing, the same as
            // an error for the purpose of "is our recording starving it".
            if (string.IsNullOrWhiteSpace(spokenText) && capturedThisRound && stressCapture != null)
            {
                Debug.Log("VoiceCommand: recognizer heard nothing while our own recording was on - " +
                          (failuresWhileCapturing + 1) + " of " + FailuresBeforeCaptureOff + " before recording is switched off");
                if (++failuresWhileCapturing >= FailuresBeforeCaptureOff)
                    stressCapture.DisableForSession(failuresWhileCapturing +
                        " empty recognizer results in a row while recording - it may not be sharing the microphone");
            }
            else failuresWhileCapturing = 0;
            ProcessRecognizedText(spokenText, CollectEvidence());
        }
        capturedThisRound = false;

        if (listenContinuously && !isSuspended)
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
        // No recognizer was involved, so there is nothing to time and the
        // latency columns stay empty rather than reporting a flattering zero.
        ResetLatency();
        ProcessRecognizedText(spokenText, new AnswerJudge.Evidence());
    }

    /// <summary>
    /// Like SimulateSpeech, with a recording of the answer for the stress
    /// check - what the Editor's hold-R shortcut uses, and what a test can
    /// feed a known tayô or táyo through.
    /// </summary>
    public void SimulateSpeech(string spokenText, float[] audio, int sampleRate)
    {
        Debug.Log("VoiceCommand: simulating recognized speech \"" + spokenText + "\" with " +
                  (audio != null ? (audio.Length / (float)Mathf.Max(1, sampleRate)).ToString("0.0") + " s of audio" : "no audio"));
        ResetLatency();
        ProcessRecognizedText(spokenText, new AnswerJudge.Evidence { audio = audio, audioSampleRate = sampleRate });
    }

    /// <summary>The recording and level readings of the session that just ended.</summary>
    private AnswerJudge.Evidence CollectEvidence()
    {
        AnswerJudge.Evidence e = new AnswerJudge.Evidence();
        if (!StressWordLive) return e;

        if (stressCapture != null && stressCapture.IsRecording)
        {
            int rate;
            e.audio = stressCapture.TakeAnswer(out rate);
            e.audioSampleRate = rate;
        }
        if (levelReadings.Count > 0)
        {
            e.levels = levelReadings.ToArray();
            e.levelIntervalMs = LevelIntervalMs();
            e.levelScale = DeviceLevelScale;
        }
        return e;
    }

    private void ProcessRecognizedText(string spokenText, AnswerJudge.Evidence evidence)
    {
        SetStatusText("Recognized:\n\"" + spokenText + "\"");

        // Talon -> Jump is a general movement ability, not tied to any
        // specific lesson objective, so it's always checked - independent
        // of whatever word SceneObjectiveController currently has set below.
        //
        // Goes through PronunciationChecker rather than string.Contains: a
        // substring check also fired on every word that merely CONTAINS the
        // trigger, so "talong" jumped too.
        if (player != null && PronunciationChecker.IsCorrect(spokenText, triggerWord))
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
            // The letters, and - for the words whose meaning depends on it -
            // where the stress fell. See AnswerJudge for the rules.
            AnswerJudge.Settings settings = AnswerJudge.Settings.Default;
            settings.stressTolerance = stressTolerance;
            settings.acceptOnLettersWhenNoEvidence = acceptOnLettersWhenNoStressEvidence;
            settings.balanced = stressCheck == StressCheck.Balanced;
            settings.requireRachelsStress = stressCheck == StressCheck.StrictOnTrustedWords || stressCheck == StressCheck.StrictOnEveryWord;
            settings.verifyOnlyTrustedWords = stressCheck == StressCheck.StrictOnTrustedWords;
            AnswerJudge.useLearnedModel = useLearnedPronunciationModel;

            AnswerJudge.Result verdict = AnswerJudge.Judge(
                spokenText, currentTargetWord, currentTargetWordVariants, evidence, settings);
            LastJudgement = verdict;
            bool isCorrect = verdict.IsCorrect;

            string heard = "Heard: \"" + spokenText + "\" (target: \"" + WordStress.Display(currentTargetWord) + "\")";
            switch (verdict.outcome)
            {
                case AnswerJudge.Outcome.Correct: SetStatusText("Correct! " + heard); break;
                case AnswerJudge.Outcome.WrongStress:
                    SetStatusText("Almost - say " + verdict.pattern.Spoken + ". " + heard + " - " + verdict.reason); break;
                case AnswerJudge.Outcome.Unclear:
                    SetStatusText("Say just " + verdict.pattern.Spoken + ", clearly. " + heard + " - " + verdict.reason); break;
                default: SetStatusText("Try again. " + heard); break;
            }
            if (verdict.pattern != null) Debug.Log("VoiceCommand: stress check - " + verdict.outcome + ", " + verdict.reason);

            // Only while an objective is live. Listening restarts every few
            // seconds whether or not anyone is talking, so logging outside a
            // lesson would bury the real attempts in idle noise.
            if (logAttempts)
                VoiceAttemptLog.Record(currentTargetWord, spokenText, verdict, CurrentLatency());

            // An unclear stress is neither right nor wrong: the child is asked
            // again and nothing is scored.
            if (verdict.outcome == AnswerJudge.Outcome.Unclear)
            {
                OnStressUnclear?.Invoke(currentTargetWord, verdict.reason);
                return;
            }

            OnAnswerChecked?.Invoke(spokenText, isCorrect);

            if (verdict.outcome == AnswerJudge.Outcome.WrongStress)
                OnWrongStress?.Invoke(currentTargetWord, verdict.pattern);
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
