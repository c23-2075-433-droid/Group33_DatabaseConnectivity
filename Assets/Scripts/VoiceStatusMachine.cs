using UnityEngine;

/// <summary>
/// What the listening panel says, and the rules that stop it flickering.
///
/// Kept as a plain class with no UI in it, because the flicker rules are the
/// only genuinely fiddly part of the HUD and they are much easier to reason
/// about - and to change after testing on a phone - away from RectTransforms.
/// GameplayHUD feeds it four booleans every frame and draws whatever Current
/// comes back as.
///
/// The flicker is real and has two separate causes:
///
///   1. VoiceCommand listens CONTINUOUSLY. Every result stops the recognizer,
///      waits restartDelay, and starts it again, and the Android recognizer
///      also times out by itself every few seconds when nobody is talking. So
///      IsListening drops to false and back roughly every five seconds while
///      the child is simply thinking. Shown literally, the panel strobes
///      between "LISTENING..." and "SPEAK NOW" forever.
///
///   2. The narrator SUSPENDS the microphone for every line it speaks, so
///      IsListening goes false there too - but that one must be visible,
///      because the child genuinely should not be talking over Rachel.
///
/// Three rules, in this order of authority:
///
///   - Rachel speaking wins over the microphone states, so cause 2 is shown
///     and never swallowed by the grace period below.
///   - A drop in listening is held for listeningGrace before it is believed.
///     If listening comes back first, as in cause 1, nothing on screen moved
///     at all.
///   - Any state is held for minDwell before another may replace it, so a
///     burst of recognizer activity cannot strobe the panel. Feedback -
///     correct, try again, microphone blocked - ignores the dwell on the way
///     IN, because an answer must be acknowledged immediately, and imposes
///     its own longer hold on the way out.
/// </summary>
public class VoiceStatusMachine
{
    public enum State
    {
        /// <summary>Nothing is being asked: the scene is paused, finished, or between words.</summary>
        Idle,
        /// <summary>A word is live and the recognizer is between listening sessions.</summary>
        SpeakNow,
        Listening,
        /// <summary>The child is talking and the result has not come back yet.</summary>
        Processing,
        /// <summary>Teacher Rachel is speaking; the microphone is deliberately deaf.</summary>
        TeacherSpeaking,
        Correct,
        TryAgain,
        /// <summary>Permission denied, or the device has no recognizer at all.</summary>
        MicBlocked,
    }

    [Tooltip("How long CORRECT / TRY AGAIN stay up, so an answer is readable before the next word.")]
    public float feedbackHold = 1.2f;

    [Tooltip("How long a drop in listening is ignored for, to cover the gap between recognizer sessions.")]
    public float listeningGrace = 1.2f;

    [Tooltip("Shortest time any state stays on screen before another may replace it.")]
    public float minDwell = 0.5f;

    [Tooltip("Longest PROCESSING may stay up if a result never arrives.")]
    public float processingTimeout = 6f;

    public State Current { get; private set; } = State.Idle;

    /// <summary>Raised only when the state actually changes, so nothing redraws per frame.</summary>
    public event System.Action<State> OnChanged;

    private float timeInState;
    private float feedbackLeft;     // > 0 while CORRECT / TRY AGAIN is latched
    private float graceLeft;        // > 0 while a drop in listening is being doubted
    private float processingLeft;   // > 0 while a result is outstanding
    private bool micBlocked;        // latched until the recognizer works again
    private bool wasListening;

    // ---- things that happen, told to the machine as they happen ----

    public void NoteCorrect()
    {
        feedbackLeft = feedbackHold;
        processingLeft = 0f;
        Force(State.Correct);
    }

    public void NoteTryAgain()
    {
        feedbackLeft = feedbackHold;
        processingLeft = 0f;
        Force(State.TryAgain);
    }

    /// <summary>The child started talking; a result is now outstanding.</summary>
    public void NoteSpeechBegan()
    {
        if (micBlocked) return;
        processingLeft = processingTimeout;
        graceLeft = 0f;
    }

    public void NoteMicBlocked()
    {
        micBlocked = true;
        processingLeft = 0f;
        Force(State.MicBlocked);
    }

    /// <summary>A new word has gone live: clear everything held over from the last one.</summary>
    public void NoteObjectiveChanged()
    {
        feedbackLeft = 0f;
        processingLeft = 0f;
        graceLeft = 0f;
    }

    // ---- the per-frame decision ----

    public void Tick(float dt, bool objectiveLive, bool narratorSpeaking, bool listening)
    {
        timeInState += dt;
        if (feedbackLeft > 0f) feedbackLeft -= dt;
        if (processingLeft > 0f) processingLeft -= dt;

        // The recognizer working again is the only thing that clears a block.
        if (micBlocked && listening) micBlocked = false;

        // Doubt a drop in listening, unless Rachel is the reason for it - that
        // one is real and should be shown at once.
        if (wasListening && !listening && !narratorSpeaking) graceLeft = listeningGrace;
        if (listening) graceLeft = 0f;
        else if (graceLeft > 0f) graceLeft -= dt;
        wasListening = listening;

        Set(Decide(objectiveLive, narratorSpeaking, listening));
    }

    private State Decide(bool objectiveLive, bool narratorSpeaking, bool listening)
    {
        if (micBlocked) return State.MicBlocked;
        if (feedbackLeft > 0f) return Current == State.Correct ? State.Correct : State.TryAgain;
        if (narratorSpeaking) return State.TeacherSpeaking;
        if (!objectiveLive) return State.Idle;
        if (processingLeft > 0f) return State.Processing;
        if (listening || graceLeft > 0f) return State.Listening;
        return State.SpeakNow;
    }

    /// <summary>Takes the state if it has earned it: see minDwell.</summary>
    private void Set(State next)
    {
        if (next == Current) return;
        if (timeInState < minDwell) return;
        Current = next;
        timeInState = 0f;
        OnChanged?.Invoke(next);
    }

    /// <summary>Takes the state now, dwell or no dwell. Only an answer does this.</summary>
    private void Force(State next)
    {
        if (next == Current) return;
        Current = next;
        timeInState = 0f;
        OnChanged?.Invoke(next);
    }
}
