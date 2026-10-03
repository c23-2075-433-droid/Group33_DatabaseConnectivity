using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the persistent microphone + replay/speaker buttons shown during
/// voice-command levels. The GameObjects these buttons live on are created by
/// Assets/Editor/VoiceUIBuilder.cs (via "Tools > SALINLAHI > Add Voice UI To
/// Level 1", or automatically by BuildLevel1Scene.cs) - this script only
/// drives their behavior once they exist.
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

    private Coroutine pulseRoutine;

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
        if (isListening) StartPulse();
        else StopPulse();
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
        if (objectiveController != null) objectiveController.PlayCurrentInstruction();
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
