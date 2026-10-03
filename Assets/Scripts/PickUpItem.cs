using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Picks an object up off the scene and onto the character.
///
/// Built for "Kunin" (take / get) in Bahay Scene 4, where Kylo collects his
/// school bag before leaving: the bag prop in the room is switched off and the
/// character's art is swapped for the set drawn with the bag on his back. It
/// is written generally, so the same component handles any later "pick this
/// up" word without new code.
///
/// Why the art is swapped rather than a bag sprite being parented to the
/// player: the bag is painted into the character art, not a separate layer, so
/// there are two complete sets of frames - plain, and with the bag. See
/// Tools/strip_backpack.py, which produced the plain set from the original
/// drawings.
///
/// Wire PickUp() to the "Kunin" objective's onCorrect event in
/// SceneObjectiveController, the same way the other words are wired.
/// </summary>
public class PickUpItem : MonoBehaviour
{
    [Header("What is being picked up")]
    [Tooltip("The prop sitting in the room, switched off once it has been taken. " +
             "For the bag this is the chair-and-backpack prop.")]
    public GameObject itemInScene;

    [Tooltip("Optional. Shown in place of the one above - use it when the prop is a " +
             "chair WITH a bag on it and an empty chair should stay behind.")]
    public GameObject itemAfterTaking;

    [Header("How the character changes")]
    [Tooltip("The player, whose standing and walking art is swapped.")]
    public PlayerMovement player;

    [Tooltip("Standing art with the item carried. Leave empty to keep the current art.")]
    public Sprite carryingStandingSprite;

    [Tooltip("Walk frames with the item carried. Leave empty to keep the current frames.")]
    public Sprite[] carryingWalkFrames;

    [Header("Events")]
    [Tooltip("Runs after the item has been taken, for a sound, a line from Teacher Rachel, or anything else.")]
    public UnityEvent onPickedUp;

    // Saying the word twice must not double-fire the event or switch an
    // already-hidden prop back on.
    private bool taken = false;

    /// <summary>True once the item has been collected.</summary>
    public bool HasBeenTaken => taken;

    /// <summary>
    /// Takes the item. Safe to call more than once; only the first call does
    /// anything.
    /// </summary>
    public void PickUp()
    {
        if (taken) return;
        taken = true;

        if (itemInScene != null) itemInScene.SetActive(false);
        if (itemAfterTaking != null) itemAfterTaking.SetActive(true);

        if (player != null)
        {
            player.SetAppearance(carryingStandingSprite, carryingWalkFrames);
        }

        Debug.Log("PickUpItem: " +
                  (itemInScene != null ? itemInScene.name : "item") + " taken.");

        onPickedUp?.Invoke();
    }
}
