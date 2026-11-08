using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Plays a short run of drawn frames and then lands on a destination pose.
///
/// This is the other half of PoseSettle. That one eases the TRANSFORM so a
/// pose arrives rather than appears, which is the best that can be done with
/// no in-between art. This is for when the art exists: Paaralan Scene 2 now
/// has drawn frames of Kylo lowering onto his chair and of him reaching into
/// his bag, and those want playing rather than cutting to.
///
/// Two ways to start it, because the scene needs both:
///
///   playOnEnable  for a pose that begins the moment its object is switched
///                 on - "Upo" turns the seated Kylo on, and the sitting
///                 frames should run immediately.
///   Play()        for a beat on an object that is ALREADY on - "Kuha" does
///                 not switch anything on, it just changes what the seated
///                 Kylo is doing. Wire it to the word's objective.
///
/// Deliberately not an Animator. These scenes are built from code by the
/// Build*Scene editor scripts, and a controller asset per pose would be a
/// second place to look when something is wrong. A list of sprites and a
/// frame rate is the whole of what this needs to be.
///
/// Opt-in by construction: with no frames assigned it does nothing at all, so
/// adding it to a scene that has no drawn in-betweens changes nothing.
/// </summary>
public class PoseSequence : MonoBehaviour
{
    [Tooltip("The renderer to animate. Defaults to this object's own.")]
    public SpriteRenderer target;

    [Tooltip("The lead-in frames, in order. Do NOT repeat the destination " +
             "here - it is set once these have played.\n\nLeave empty and " +
             "this does nothing, which is how a beat with no drawn " +
             "in-betweens keeps its old behaviour.")]
    public Sprite[] frames;

    [Tooltip("What he ends on. Leave empty to return to whatever the " +
             "renderer was showing when the sequence started, which is what " +
             "a pose that was switched on already wants.")]
    public Sprite destinationSprite;

    [Tooltip("How fast the lead-in plays. Three frames at 11 is about a " +
             "quarter of a second, which is roughly how long a child takes " +
             "to drop onto a chair.")]
    public float framesPerSecond = 11f;

    [Tooltip("Start as soon as the object is switched on. Turn this OFF for " +
             "a beat on an object that is already visible, and call Play() " +
             "from the word's objective instead.")]
    public bool playOnEnable = true;

    // Instantiated here, not left to Unity. A builder that does
    // AddComponent<PoseSequence>() and then hands this straight to
    // UnityEventTools.AddPersistentListener gets a NullReferenceException
    // off a null field, which is how this scene came out half-built with no
    // UI and an "Untitled" title bar. The project's own MakeObjective news
    // up its UnityEvent for the same reason.
    [Tooltip("Runs once the destination pose is set, for a gleam or a sound.")]
    public UnityEvent onFinished = new UnityEvent();

    [Tooltip("Which frame fires onFrameReached, counting from 0. Leave at -1 " +
             "for never.\n\nThis exists because a prop a character puts down " +
             "has to appear AS the hands reach for the surface, not after the " +
             "whole run has finished. One frame late and the thing he is " +
             "holding vanishes from his hands and pops into place somewhere " +
             "else, which is the exact seam this was meant to hide.")]
    public int fireOnFrame = -1;

    [Tooltip("Runs as fireOnFrame is shown.")]
    public UnityEvent onFrameReached = new UnityEvent();

    private Sprite fallback;

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    /// <summary>Runs the frames and lands on the destination.</summary>
    public void Play()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target == null) return;

        // Read the fallback now rather than caching it in Awake: a previous
        // beat may have changed the sprite since, and this has to land on
        // what is current, not on what was first.
        fallback = destinationSprite != null ? destinationSprite : target.sprite;

        if (frames == null || frames.Length == 0 || !isActiveAndEnabled)
        {
            // No art, or switched off and unable to run a coroutine. Either
            // way the destination is still owed.
            target.sprite = fallback;
            onFrameReached?.Invoke();   // still owed, or the prop never appears
            onFinished?.Invoke();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(Run());
    }

    private System.Collections.IEnumerator Run()
    {
        float hold = framesPerSecond > 0f ? 1f / framesPerSecond : 0.1f;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) continue;   // a missing drawing skips, it does not stall
            target.sprite = frames[i];
            if (i == fireOnFrame) onFrameReached?.Invoke();
            yield return new WaitForSeconds(hold);
        }

        target.sprite = fallback;
        onFinished?.Invoke();
    }

    /// <summary>
    /// Back to the destination at once, for a scene replayed without a
    /// rebuild, or one switched off mid-sequence.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        if (target != null && fallback != null) target.sprite = fallback;
    }
}
