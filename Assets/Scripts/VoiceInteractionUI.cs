using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the persistent microphone + replay/speaker buttons shown during
/// voice-command levels. The GameObjects these buttons live on are created by
/// Assets/Editor/VoiceUIBuilder.cs (via "Tools > SALINLAHI > Add Voice UI To
/// Level 1", and by the scene builders) - this script only drives their
/// behavior once they exist.
///
///   - MICROPHONE BUTTON: a status indicator for VoiceCommand's listening
///     state (see "Mic listening indicator" below). It does not start/stop
///     recognition itself - VoiceCommand already listens continuously on its
///     own - but tapping it nudges VoiceCommand to listen again, in case
///     recognition ever stalls.
///   - REPLAY/SPEAKER BUTTON: replays the current word's instruction audio
///     via SceneObjectiveController (see "Replay button" below). It never
///     touches the microphone or speech recognition.
/// </summary>
public class VoiceInteractionUI : MonoBehaviour
{
    /// <summary>What the microphone is saying about itself right now.</summary>
    public enum MicVisual
    {
        /// <summary>Not listening, and that is fine: between words, or paused.</summary>
        Idle,
        /// <summary>Waiting for the player to speak - the pulse.</summary>
        Listening,
        /// <summary>The player spoke and the answer is being worked out.</summary>
        Processing,
        /// <summary>Permission denied, or the device has no recognizer.</summary>
        Blocked,
    }

    [Header("References")]
    [Tooltip("The scene's VoiceCommand - used to know when it's actively listening.")]
    public VoiceCommand voiceCommand;

    [Tooltip("The scene's SceneObjectiveController - used to replay the current word's instruction audio.")]
    public SceneObjectiveController objectiveController;

    [Header("Microphone Button")]
    public Image micIcon;
    public Button micButton;
    [Tooltip("How much the mic icon grows at the peak of its pulse (1 = no growth). Kept small so the animation stays subtle.")]
    public float pulseScale = 1.08f;
    [Tooltip("Pulse cycles per second while listening.")]
    public float pulseSpeed = 1.5f;

    [Header("Replay Button")]
    public Button replayButton;
    [Tooltip("How far the replay icon shrinks on press (1 = no shrink).")]
    public float replayPunchScale = 0.85f;
    public float replayPunchDuration = 0.15f;

    [Tooltip("Press replay again within this many seconds and the word is said slowly, for a child who heard it but could not catch it.")]
    public float slowReplayWindow = 3f;

    [Header("Microphone Tints")]
    [Tooltip("While waiting for the player, and between words.")]
    public Color idleTint = Color.white;
    [Tooltip("While the answer is being worked out - held still and slightly large, so it reads as thinking rather than waiting.")]
    public Color processingTint = new Color(0.86f, 0.92f, 1f);
    [Tooltip("When the microphone cannot be used at all.")]
    public Color blockedTint = new Color(1f, 0.62f, 0.55f);
    [Tooltip("How large the icon sits while processing.")]
    public float processingScale = 1.05f;

    private float lastReplayTime = -99f;

    private Coroutine pulseRoutine;

    // Once GameplayHUD has told this what to show, it owns the microphone's
    // look and the listening event is ignored. Two things deciding the same
    // icon from the same events, with their own timers, is how you end up with
    // a mic that pulses while the panel under it says TRY AGAIN.
    private bool hudDriven;

    void OnEnable()
    {
        if (voiceCommand != null) voiceCommand.OnListeningStateChanged += HandleListeningStateChanged;
        if (micButton != null) micButton.onClick.AddListener(OnMicPressed);
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayPressed);

        // Pick up whatever state VoiceCommand is already in (e.g. it may have
        // started listening before this UI's OnEnable ran).
        if (voiceCommand != null) HandleListeningStateChanged(voiceCommand.IsListening);
    }

    void OnDisable()
    {
        if (voiceCommand != null) voiceCommand.OnListeningStateChanged -= HandleListeningStateChanged;
        if (micButton != null) micButton.onClick.RemoveListener(OnMicPressed);
        if (replayButton != null) replayButton.onClick.RemoveListener(OnReplayPressed);
        StopPulse();
    }

    // ---------------------------------------------------------------------
    // MIC LISTENING INDICATOR - gentle pulse while VoiceCommand is actively
    // listening, still otherwise. Purely visual; recognition itself is
    // entirely owned by VoiceCommand.
    // ---------------------------------------------------------------------

    private void HandleListeningStateChanged(bool isListening)
    {
        // The fallback for a scene with no HUD. With one, GameplayHUD decides,
        // because it is the only thing that knows the difference between the
        // recognizer restarting itself and the narrator having silenced it.
        if (hudDriven) return;
        if (isListening) StartPulse();
        else StopPulse();
    }

    /// <summary>
    /// Says what the microphone should look like. Called by GameplayHUD, which
    /// works the state out once and drives the icon and the listening plank
    /// from the same answer so the two cannot disagree.
    /// </summary>
    public void SetVisual(MicVisual visual)
    {
        hudDriven = true;

        if (visual == MicVisual.Listening)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
            if (micIcon != null && visual == MicVisual.Processing)
            {
                micIcon.rectTransform.localScale =
                    new Vector3(processingScale, processingScale, 1f);
            }
        }

        if (micIcon == null) return;
        switch (visual)
        {
            case MicVisual.Processing: micIcon.color = processingTint; break;
            case MicVisual.Blocked:    micIcon.color = blockedTint;    break;
            default:                   micIcon.color = idleTint;       break;
        }
    }

    private void StartPulse()
    {
        if (pulseRoutine != null || micIcon == null) return;
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
        if (micIcon != null) micIcon.rectTransform.localScale = Vector3.one;
    }

    /// <summary>Slow "breathing" scale, always at or above resting size so it never looks like it shrank/glitched.</summary>
    private IEnumerator PulseRoutine()
    {
        RectTransform rt = micIcon.rectTransform;
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float wave = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1
            float scale = Mathf.Lerp(1f, pulseScale, wave);
            rt.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    /// <summary>
    /// Manual nudge only - VoiceCommand already listens continuously by
    /// itself, so this mostly no-ops (RequestPermissionAndListen skips
    /// starting a new session while one is already running). Lets the player
    /// tap the mic to try again if recognition ever stalls.
    /// </summary>
    private void OnMicPressed()
    {
        if (voiceCommand != null) voiceCommand.RequestPermissionAndListen();
    }

    // ---------------------------------------------------------------------
    // REPLAY BUTTON - replays the current instruction, with a small press
    // animation. Deliberately never calls into VoiceCommand.
    // ---------------------------------------------------------------------

    private void OnReplayPressed()
    {
        StartCoroutine(ReplayPunchRoutine());
        if (objectiveController == null) return;

        bool secondPress = Time.time - lastReplayTime <= slowReplayWindow;
        lastReplayTime = Time.time;

        if (secondPress) objectiveController.PlayCurrentInstructionSlowly();
        else objectiveController.PlayCurrentInstruction();
    }

    /// <summary>Subtle press feedback: shrink briefly then spring back to full size.</summary>
    private IEnumerator ReplayPunchRoutine()
    {
        if (replayButton == null) yield break;
        RectTransform rt = replayButton.GetComponent<RectTransform>();
        float half = replayPunchDuration * 0.5f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1f, replayPunchScale, t / half);
            rt.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(replayPunchScale, 1f, t / half);
            rt.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }
}
