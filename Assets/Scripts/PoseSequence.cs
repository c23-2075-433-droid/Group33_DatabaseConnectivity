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
    [Tooltip("The renderer to animate. Defaults to this object's own.")]
    public SpriteRenderer target;

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

    private Sprite fallback;
    private bool running;

    /// <summary>True while an action is playing, for a caller that must wait.</summary>
    public bool IsPlaying { get { return running; } }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    /// <summary>Runs the whole action and lands on the idle pose.</summary>
    public void Play()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target == null) return;

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
        fallback = destinationSprite != null ? destinationSprite : target.sprite;

        if (!HasAnyFrames() || !isActiveAndEnabled)
        {
            // No art, or switched off and unable to run a coroutine. Either
            // way the destination is still owed, and so is anything waiting on
            // fireOnFrame - without that, a prop never appears at all.
            target.sprite = fallback;
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

        float hold = framesPerSecond > 0f ? 1f / framesPerSecond : 0.1f;
        float loopRate = loopFramesPerSecond > 0f ? loopFramesPerSecond : framesPerSecond;
        float loopHold = loopRate > 0f ? 1f / loopRate : 0.1f;

        // --- the starting pose and the preparation ---
        for (int i = 0; i < Count(frames); i++)
        {
            if (frames[i] == null) continue;   // a missing drawing skips, it does not stall
            target.sprite = frames[i];
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
            for (int c = 0; endless || c < loopCycles; c++)
            {
                for (int i = 0; i < loopFrames.Length; i++)
                {
                    if (loopFrames[i] == null) continue;
                    target.sprite = loopFrames[i];
                    yield return new WaitForSeconds(loopHold);
                }
                if (stopRequested) break;
            }
        }

        // --- the ending movement ---
        for (int i = 0; i < Count(tailFrames); i++)
        {
            if (tailFrames[i] == null) continue;
            target.sprite = tailFrames[i];
            yield return new WaitForSeconds(hold);
        }

        // --- the idle pose ---
        target.sprite = fallback;
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
        if (target != null && fallback != null) target.sprite = fallback;
    }
}
