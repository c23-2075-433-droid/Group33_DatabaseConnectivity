using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Changes which art the player is drawn with, on cue.
///
/// This is how "Kumusta" makes Kylo turn to face the child and wave: the
/// greeting has nothing to pick up and nothing to light up, so the character
/// himself has to be the feedback.
///
/// PickUpItem can already swap the art, but only as a side effect of taking
/// something away. This does only the swap, so a word that changes how the
/// character stands reads as exactly that in the Inspector.
///
/// Wire Apply() to the word's objective in SceneObjectiveController.
/// </summary>
public class PoseSwitch : MonoBehaviour
{
    [Tooltip("The player whose art changes.")]
    public PlayerMovement player;

    [Tooltip("Standing art to switch to. Leave empty to keep the current one.")]
    public Sprite standingSprite;

    [Tooltip("Walk frames to switch to. Leave empty to keep the current ones.")]
    public Sprite[] walkFrames;

    [Tooltip("Runs after the pose changes, for a sound or a follow-on action.")]
    public UnityEvent onApplied;

    /// <summary>
    /// Switches the player to this pose. Safe to call repeatedly - setting the
    /// same art twice does nothing.
    /// </summary>
    public void Apply()
    {
        if (player == null) return;
        player.SetAppearance(standingSprite, walkFrames);
        onApplied?.Invoke();
    }
}
