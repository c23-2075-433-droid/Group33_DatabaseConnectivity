using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Plays a drawn action: a lead-in, a body that may repeat, a lead-out, and
/// then an idle pose. This is the component every animated action in SALINLAHI
/// is built from.
///
/// It is the other half of PoseSettle. That one eases the TRANSFORM so a pose
/// arrives rather than appears, which is the best that can be done with no
/// in-between art. This is for when the art exists.
///
/// THE FOUR PARTS, and why there are four. The project's animation rules say
/// that every action needs a starting pose, a preparation movement, the action
/// itself, and an ending pose - and that some actions repeat while others
/// happen once. Those two rules together are what this shape is for:
///
///   frames             the start and the preparation. Plays once.
///   loopFrames         the action itself. Repeats - see loopCycles.
///   tailFrames         the ending movement. Plays once.
///   destinationSprite  the idle pose it rests on.
///
/// Writing is the clearest case: he picks the pencil up (frames), writes and
/// writes and writes (loopFrames), puts it down (tailFrames), and sits there
/// (destinationSprite). Drinking is the other kind - lift, sip, lower, rest -
/// with nothing that should repeat, so loopFrames stays empty and it plays
/// through once.
///
/// WHAT LOOPS AND WHAT DOES NOT, from the project's rules: walking, running,
/// writing and reading loop. Sitting down, picking something up, drinking and
/// putting a hand up play once. Breathing loops, but that is IdleBreath, which
/// wobbles the transform rather than changing the drawing.
///
/// EVERY PART IS OPTIONAL. With no frames at all this does nothing, which is
/// how a beat with no drawn in-betweens keeps its old behaviour; with
/// loopFrames empty it behaves exactly as it did before looping existed, so
/// Paaralan Scene 2's four beats are untouched by this.
///
/// Two ways to start it, because the scenes need both:
///
///   playOnEnable  for a pose that begins the moment its object is switched
///                 on - "Upo" turns the seated Kylo on, and the sitting
///                 frames should run immediately.
///   Play()        for a beat on an object that is ALREADY on - "Sulat" does
///                 not switch anything on, it just changes what the seated
///                 Kylo is doing. Wire it to the word's objective, so the
///                 animation runs when the child says the word and the voice
///                 recogniser accepts it, and not before.
///
/// Deliberately not an Animator. These scenes are built from code by the
/// Build*Scene editor scripts, and a controller asset per pose would be a
/// second place to look when something is wrong. A list of sprites and a frame
/// rate is the whole of what this needs to be.
/// </summary>
public class PoseSequence : MonoBehaviour
{
    [Tooltip("The renderer to animate. Defaults to this object's own. " +
             "Ignored when player is set.")]
    public SpriteRenderer target;

    [Tooltip("Set this for a STANDING character and the frames are written " +
             "to PlayerMovement.standingSprite instead of straight to a " +
             "renderer.\n\nThis is not a convenience. PlayerMovement rewrites " +
             "its renderer from standingSprite on EVERY frame it is not " +
             "moving, so a sequence driving the renderer directly is " +
             "overwritten before anyone sees it - the character simply does " +
             "not animate. Driving standingSprite instead puts the frames " +
             "where PlayerMovement will read them, which is also how the " +
             "older PoseSwitch has always worked.\n\nLeave empty for a pose " +
             "with no PlayerMovement on it, such as the seated Kylo or the " +
             "school gate.")]
    public PlayerMovement player;

    [Tooltip("The lead-in: the starting pose and the preparation movement. " +
             "Do NOT repeat the destination here - it is set once the whole " +
             "run has played.\n\nLeave empty and this does nothing, which is " +
             "how a beat with no drawn in-betweens keeps its old behaviour.")]
    public Sprite[] frames;

    [Tooltip("The action itself, repeated. This is the part that makes " +
             "writing look like writing rather than like one stroke: three " +
             "or four frames of the hand moving, run round several times.\n\n" +
             "Leave EMPTY for an action that happens once - sitting down, " +
             "taking a drink, putting a hand up. The project's rules are " +
             "specific about which is which.")]
    public Sprite[] loopFrames;

    [Tooltip("How many times loopFrames repeats. Ignored when loopFrames is " +
             "empty.\n\nZero or less means repeat until something calls " +
             "Stop() - use that for walking, and for an action that should " +
             "carry on while the scene waits for the child to speak.")]
    public int loopCycles = 3;

    [Tooltip("The ending movement: putting the pencil down, lowering the " +
             "hand. Plays once, after the loop. Leave empty and the action " +
             "goes straight from its last frame to the idle pose, which is " +
             "the snap this whole component exists to avoid - so fill it in " +
             "for anything where the hands are somewhere they cannot stay.")]
    public Sprite[] tailFrames;

    [Tooltip("The idle pose he rests on when it is all over. Leave empty to " +
             "return to whatever the renderer was showing when the sequence " +
             "started, which is what a pose that was switched on already " +
             "wants.")]
    public Sprite destinationSprite;

    [Tooltip("How fast it plays. Three frames at 11 is about a quarter of a " +
             "second, which is roughly how long a child takes to drop onto a " +
             "chair.\n\nScenes scale this by their own Pace constant rather " +
             "than setting it directly, so one number slows a whole scene.")]
    public float framesPerSecond = 11f;

    [Tooltip("Frames per second for the looping body only, when it should " +
             "run at a different speed from the lead-in. Zero means use " +
             "framesPerSecond. Writing is usually a little quicker than the " +
             "reach that sets it up.")]
    public float loopFramesPerSecond = 0f;

    [Tooltip("Start as soon as the object is switched on. Turn this OFF for " +
             "a beat on an object that is already visible, and call Play() " +
             "from the word's objective instead - which is also what keeps " +
             "the animation tied to the spoken command.")]
    public bool playOnEnable = true;

    // Instantiated here, not left to Unity. A builder that does
    // AddComponent<PoseSequence>() and then hands this straight to
    // UnityEventTools.AddPersistentListener gets a NullReferenceException off
    // a null field, which is how Paaralan Scene 3 once came out half-built
    // with no UI and an "Untitled" title bar. The project's own MakeObjective
    // news up its UnityEvent for the same reason.
    [Tooltip("Runs once the idle pose is set, for a gleam or a sound.")]
    public UnityEvent onFinished = new UnityEvent();

    [Tooltip("Which frame of the LEAD-IN fires onFrameReached, counting from " +
             "0. Leave at -1 for never.\n\nThis exists because a prop a " +
             "character puts down has to appear AS the hands reach the " +
             "surface, not after the whole run has finished. One frame late " +
             "and the thing he is holding vanishes from his hands and pops " +
             "into place somewhere else, which is the exact seam this was " +
             "meant to hide.")]
    public int fireOnFrame = -1;

    [Tooltip("Runs as fireOnFrame is shown.")]
    public UnityEvent onFrameReached = new UnityEvent();

    [Tooltip("A looping action this one TAKES OVER FROM. Set it and Play() " +
             "ends that action first - Stop(), so its own ending movement " +
             "plays - and starts this one only once it has landed.\n\n" +
             "This is what joins two drawn beats that share a character. " +
             "Paaralan Scene 3 reads with an endless loop and writes when " +
             "the next word is said: without this, \"Sulat\" would cut the " +
             "reading off wherever it happened to be, with the book still " +
             "in his hands. With it, he closes the book, puts it down, and " +
             "then picks the pencil up.\n\nLeave empty for an action that " +
             "starts from the idle pose.")]
    public PoseSequence interrupts;

    [Tooltip("Safety cap in seconds on the wait for `interrupts` to finish, " +
             "so an ending movement that never lands cannot stall this one " +
             "for good.")]
    public float interruptWait = 3f;

    [Tooltip("A SECOND renderer, driven in step with loopFrames.\n\nFor the " +
             "part of an action that has to be drawn in a different sorting " +
             "order from the character. Paaralan Scene 3's \"Sulat\" is the " +
             "case it exists for: Kylo is drawn BEHIND the desk, and the " +
             "notebook lies on top of it, so no pixel of him can ever be on " +
             "the page - his pencil stops at the desk's near edge with the " +
             "wood between it and the paper. The point of the pencil is " +
             "drawn on this instead, in front of both.\n\nIt is one " +
             "component driving two renderers rather than two components, " +
             "because two would be two coroutines and a frame of drift is a " +
             "pencil point that lags the hand holding it.\n\nLeave empty " +
             "and nothing here does anything.")]
    public SpriteRenderer companion;

    [Tooltip("What `companion` shows, frame for frame with loopFrames. Same " +
             "length as that array; a shorter one simply stops being read. " +
             "It is cleared before the lead-in and after the tail, so the " +
             "second renderer is blank whenever the action is not running.")]
    public Sprite[] companionLoopFrames;

    private Sprite fallback;
    private bool running;
    private bool holding;

    /// <summary>
    /// Puts a frame on screen, through whichever route this pose uses - the
    /// character's idle sprite when one is set, the renderer otherwise.
    /// </summary>
    private void Show(Sprite s)
    {
        if (player != null) player.standingSprite = s;
        else if (target != null) target.sprite = s;
    }

    /// <summary>
    /// Puts the matching frame on the second renderer, or clears it when
    /// `i` is negative or past the end of the list. Safe to call with no
    /// companion set, which is every action in the game but one.
    /// </summary>
    private void ShowCompanion(int i)
    {
        if (companion == null) return;
        companion.sprite = (i >= 0 && companionLoopFrames != null
                            && i < companionLoopFrames.Length)
                           ? companionLoopFrames[i] : null;
    }

    /// <summary>What is showing now, for a sequence with no destination.</summary>
    private Sprite Current()
    {
        if (player != null) return player.standingSprite;
        return target != null ? target.sprite : null;
    }

    /// <summary>True while an action is playing, for a caller that must wait.</summary>
    public bool IsPlaying { get { return running; } }

    /// <summary>
    /// True while this is sitting in an ENDLESS loop waiting for Stop() -
    /// reading, walking, anything whose end comes from outside.
    ///
    /// It is still IsPlaying, and it still has an ending movement owed to it,
    /// but nothing should WAIT for it: the thing it is waiting for is the
    /// child speaking, and the child is not asked until the scene stops
    /// waiting. SceneObjectiveController would hold the next word for its
    /// whole safety cap and then ask it anyway, which is four seconds of
    /// silence in the middle of a lesson. See IsPosePlaying there.
    /// </summary>
    public bool IsHoldingLoop { get { return running && holding; } }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    /// <summary>Runs the whole action and lands on the idle pose.</summary>
    public void Play()
    {
        if (player == null && target == null) target = GetComponent<SpriteRenderer>();
        if (player == null && target == null) return;

        // HAND OVER FROM A LOOPING ACTION, rather than cutting it off.
        //
        // An endless loop is waiting for the next word, so the next word is
        // exactly what ends it - and the two beats meet at a seam the child
        // watches. Halting the loop here would start this action from
        // whatever frame the other one had up, which for "Sulat" taking over
        // from "Basa" means the pencil coming out while the book is still in
        // his hands and the book then vanishing.
        //
        // Stop() instead, which lets the other action play its own ending
        // movement - he closes the book and puts it back on the desk - and
        // begin only once it has landed on its idle pose.
        if (interrupts != null && interrupts != this && interrupts.IsPlaying
            && isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(RunAfter(interrupts));
            return;
        }

        Begin();
    }

    /// <summary>
    /// Ends the looping action this one takes over from, waits for its ending
    /// movement, and then runs. Capped, so an ending that never lands cannot
    /// swallow the word that was just said.
    /// </summary>
    private System.Collections.IEnumerator RunAfter(PoseSequence other)
    {
        // Claimed from this moment on. The hand-over IS part of this action
        // as far as the scene is concerned, so the next word is not asked
        // while the book is still being put down.
        running = true;
        other.Stop();

        float waited = 0f;
        while (other != null && other.IsPlaying && waited < interruptWait)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // It had its chance. Taking the renderer now is better than leaving
        // the spoken word unanswered.
        if (other != null && other.IsPlaying) other.Halt();

        running = false;
        Begin();
    }

    private void Begin()
    {
        // TAKE THE RENDERER OFF ANY OTHER ACTION FIRST.
        //
        // StopAllCoroutines below stops THIS component's coroutines, not the
        // object's. That was harmless while a character had one drawn action;
        // Paaralan Scene 3 is the first scene to put two on one figure -
        // "Sulat" and "Tanong" - and they share a SpriteRenderer. Started
        // over the top of each other they would both write a sprite every
        // frame and the figure would flicker between writing and putting its
        // hand up, with the loser's onFinished firing partway through the
        // winner and, in Sulat's case, dropping the pencil back on the desk
        // mid-gesture.
        //
        // SceneObjectiveController waits for a drawn action to finish before
        // asking the next word, so this should not arise in ordinary play. It
        // is cheap to make impossible rather than unlikely: that wait gives up
        // after maxActionWait, and a scene replayed without a rebuild has no
        // such protection at all.
        foreach (PoseSequence other in GetComponents<PoseSequence>())
            if (other != this && other.running && other.target == target)
                other.Halt();

        // Read the fallback now rather than caching it in Awake: a previous
        // beat may have changed the sprite since, and this has to land on what
        // is current, not on what was first.
        fallback = destinationSprite != null ? destinationSprite : Current();

        if (!HasAnyFrames() || !isActiveAndEnabled)
        {
            // No art, or switched off and unable to run a coroutine. Either
            // way the destination is still owed, and so is anything waiting on
            // fireOnFrame - without that, a prop never appears at all.
            Show(fallback);
            onFrameReached?.Invoke();
            onFinished?.Invoke();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(Run());
    }

    /// <summary>
    /// Drops this action immediately, without touching the sprite.
    ///
    /// For handing the renderer to another action that is starting now: it
    /// is about to set its own first frame, so putting the idle pose up here
    /// would only insert one frame of him sitting still between the two.
    /// Nor does onFinished fire - this action did not finish, it was
    /// interrupted, and anything listening would be acting on a lie.
    /// </summary>
    private void Halt()
    {
        StopAllCoroutines();
        running = false;
        holding = false;
        ShowCompanion(-1);
    }

    /// <summary>
    /// Ends an endless loop and plays the ending movement. For walking, which
    /// repeats until the character arrives, and for any action left running
    /// while the scene waits.
    /// </summary>
    public void Stop()
    {
        // A flag, NOT loopCycles. Writing to loopCycles would stop this run
        // and also leave the component permanently endless for the next one,
        // because it is a serialized field the builder set on purpose.
        stopRequested = true;

        // No longer merely holding: the ending movement is owed now, and
        // anything waiting for this action to land should wait for it.
        holding = false;
    }

    private bool stopRequested;

    private bool HasAnyFrames()
    {
        return Count(frames) > 0 || Count(loopFrames) > 0 || Count(tailFrames) > 0;
    }

    private static int Count(Sprite[] a)
    {
        return a == null ? 0 : a.Length;
    }

    private System.Collections.IEnumerator Run()
    {
        running = true;
        stopRequested = false;
        ShowCompanion(-1);        // blank through the lead-in

        float hold = framesPerSecond > 0f ? 1f / framesPerSecond : 0.1f;
        float loopRate = loopFramesPerSecond > 0f ? loopFramesPerSecond : framesPerSecond;
        float loopHold = loopRate > 0f ? 1f / loopRate : 0.1f;

        // --- the starting pose and the preparation ---
        for (int i = 0; i < Count(frames); i++)
        {
            if (frames[i] == null) continue;   // a missing drawing skips, it does not stall
            Show(frames[i]);
            if (i == fireOnFrame) onFrameReached?.Invoke();
            yield return new WaitForSeconds(hold);
        }

        // --- the action, repeated ---
        // A cycles count of zero or less means carry on until Stop(), which is
        // what walking wants. Guarded so that an endless loop with no frames
        // in it cannot spin forever doing nothing.
        if (Count(loopFrames) > 0)
        {
            bool endless = loopCycles <= 0;
            // Only an endless loop HOLDS: a counted one ends by itself and is
            // worth waiting for. See IsHoldingLoop.
            holding = endless && !stopRequested;
            for (int c = 0; endless || c < loopCycles; c++)
            {
                for (int i = 0; i < loopFrames.Length; i++)
                {
                    if (loopFrames[i] == null) continue;
                    Show(loopFrames[i]);
                    ShowCompanion(i);
                    yield return new WaitForSeconds(loopHold);
                }
                if (stopRequested) break;
            }
            holding = false;
        }
        ShowCompanion(-1);

        // --- the ending movement ---
        for (int i = 0; i < Count(tailFrames); i++)
        {
            if (tailFrames[i] == null) continue;
            Show(tailFrames[i]);
            yield return new WaitForSeconds(hold);
        }

        // --- the idle pose ---
        Show(fallback);
        running = false;
        onFinished?.Invoke();
    }

    /// <summary>
    /// Back to the idle pose at once, for a scene replayed without a rebuild,
    /// or one switched off mid-sequence.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        running = false;
        holding = false;
        ShowCompanion(-1);
        if (fallback != null && (player != null || target != null)) Show(fallback);
    }
}
