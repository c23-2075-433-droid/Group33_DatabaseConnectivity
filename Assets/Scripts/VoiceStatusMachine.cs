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
///     correct, try again, nothing heard, microphone blocked - ignores the
///     dwell on the way IN, because an answer must be acknowledged
///     immediately, and imposes its own longer hold on the way out.
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
        /// <summary>
        /// Ten seconds went by with nobody speaking. Appended rather than
        /// slotted in beside TryAgain on purpose: GameplayHUD.StateLabels is
        /// indexed by this enum, so inserting a member in the middle would
        /// silently relabel every state after it.
        /// </summary>
        NothingHeard,
    }

    [Tooltip("How long CORRECT / TRY AGAIN stay up, so an answer is readable before the next word.")]
    public float feedbackHold = 1.2f;

    [Tooltip("How long the silence message stays up. Longer than the others because it is paired " +
             "with a sound that runs for about five seconds, and the two are one event. It does " +
             "not hold the plank hostage: the child speaking clears it at once.")]
    public float nothingHeardHold = 1.2f;

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
    private float feedbackLeft;     // > 0 while a feedback state is latched
    private State feedbackState;    // which one, while feedbackLeft > 0
    private float graceLeft;        // > 0 while a drop in listening is being doubted
    private float processingLeft;   // > 0 while a result is outstanding
    private bool micBlocked;        // latched until the recognizer works again
    private bool wasListening;

    // ---- things that happen, told to the machine as they happen ----

    public void NoteCorrect()
    {
        Latch(State.Correct, feedbackHold);
    }

    public void NoteTryAgain()
    {
        Latch(State.TryAgain, feedbackHold);
    }

    /// <summary>
    /// The idle nudge went off: ten seconds passed with nobody speaking. This
    /// is NOT a miss and nothing about it is scored - it is the panel saying
    /// out loud what the crickets are saying, and it clears itself the same
    /// way CORRECT and TRY AGAIN do.
    ///
    /// Deliberately not raised by the recognizer timing out. VoiceCommand
    /// listens continuously and the recognizer gives up every few seconds
    /// while nobody is talking, so that event is the idle state rather than
    /// news. VoicePromptDirector's silence timer is the one thing here that
    /// means what a child would mean by silence.
    /// </summary>
    public void NoteNothingHeard()
    {
        if (micBlocked) return;
        Latch(State.NothingHeard, nothingHeardHold);
    }

    /// <summary>Puts up a feedback state and holds it for `hold` seconds.</summary>
    private void Latch(State state, float hold)
    {
        feedbackLeft = hold;
        feedbackState = state;
        processingLeft = 0f;
        Force(state);
    }

    /// <summary>The child started talking; a result is now outstanding.</summary>
    public void NoteSpeechBegan()
    {
        if (micBlocked) return;

        // The child talking ends the silence, so the message about it goes
        // immediately rather than sitting over PROCESSING for the rest of its
        // hold. Only that one: CORRECT and TRY AGAIN are about an answer and
        // have to stay readable for their full time, even if the child is
        // already saying the next thing.
        if (feedbackState == State.NothingHeard) feedbackLeft = 0f;

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
        if (feedbackLeft > 0f) return feedbackState;
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
