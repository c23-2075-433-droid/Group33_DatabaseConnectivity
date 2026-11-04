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

    [Tooltip("Runs after the swap, for a gleam or a sound.")]
    public UnityEvent onApplied;

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
        target.sprite = afterSprite;
        onApplied?.Invoke();
    }

    /// <summary>Back to the "before" art, for a scene that is replayed without being rebuilt.</summary>
    public void Revert()
    {
        if (target == null || beforeSprite == null) return;
        target.sprite = beforeSprite;
    }
}
