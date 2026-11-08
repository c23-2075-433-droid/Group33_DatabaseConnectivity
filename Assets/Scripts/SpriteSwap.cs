using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Changes the art on any SpriteRenderer, on cue.
///
/// PoseSwitch already does this for the player, but only for the player - it
/// goes through PlayerMovement.SetAppearance so the walk frames change with
/// the standing pose. The gate and the guard are not the player: they have no
/// walk cycle and nothing to animate, they just become a different picture
/// when a word is said. "Bukas" opens the gate; "Kumusta" lifts the guard's
/// hand.
///
/// Wire Apply() to the word's objective in SceneObjectiveController.
///
/// Both sprites are optional on purpose. A scene that is built before its art
/// exists still runs: the renderer simply keeps whatever it already had, and
/// the builder says in its log line which picture is still missing. That is
/// better than a pink square or a NullReferenceException at the moment a
/// child says the word.
/// </summary>
public class SpriteSwap : MonoBehaviour
{
    [Tooltip("The renderer whose art changes. Defaults to this object's own.")]
    public SpriteRenderer target;

    [Tooltip("What it looks like before the word. Leave empty to keep what the renderer starts with.")]
    public Sprite beforeSprite;

    [Tooltip("What it changes to. Leave empty and Apply() does nothing, which is how an unbuilt art asset degrades.")]
    public Sprite afterSprite;

    // Instantiated rather than left null, so a builder can wire a listener
    // to it the moment the component is added. Same trap that PoseSequence
    // fell into; this one never bit only because no scene has wired it yet.
    [Tooltip("Runs after the swap, for a gleam or a sound.")]
    public UnityEvent onApplied = new UnityEvent();

    [Tooltip("Seconds to dip the art out and back in around the change, so one " +
             "pose melts into the next instead of popping. A quarter of this is " +
             "spent fading down, the rest coming back up.\n\n" +
             "Leave at 0 for the instant swap every scene built before this " +
             "field expects - a gate opening SHOULD snap. Pose changes on a " +
             "character are the case worth softening.")]
    public float fadeSeconds = 0f;

    private void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target != null && beforeSprite != null) target.sprite = beforeSprite;
    }

    /// <summary>
    /// Switches to the "after" art. Safe to call twice - the second call sets
    /// the same sprite again, which Unity treats as no change.
    /// </summary>
    public void Apply()
    {
        if (target == null || afterSprite == null) return;

        // A disabled object cannot run a coroutine, and a pose that is swapped
        // while switched off must still be correct when it comes back on.
        if (fadeSeconds > 0f && isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(FadeTo(afterSprite));
            return;
        }

        target.sprite = afterSprite;
        onApplied?.Invoke();
    }

    /// <summary>
    /// Dips the renderer's alpha down, changes the picture at the bottom of
    /// the dip, and brings it back. Quicker going out than coming in, which
    /// hides the change and lets the new pose arrive rather than flash.
    /// </summary>
    private System.Collections.IEnumerator FadeTo(Sprite sprite)
    {
        Color full = target.color;
        float outFor = fadeSeconds * 0.25f;
        float inFor = fadeSeconds - outFor;

        for (float t = 0f; t < outFor; t += Time.deltaTime)
        {
            target.color = new Color(full.r, full.g, full.b, full.a * (1f - t / outFor));
            yield return null;
        }

        target.sprite = sprite;
        onApplied?.Invoke();

        for (float t = 0f; t < inFor; t += Time.deltaTime)
        {
            target.color = new Color(full.r, full.g, full.b, full.a * (t / inFor));
            yield return null;
        }

        target.color = full;
    }

    /// <summary>Back to the "before" art, for a scene that is replayed without being rebuilt.</summary>
    public void Revert()
    {
        if (target == null || beforeSprite == null) return;
        target.sprite = beforeSprite;
    }
}
