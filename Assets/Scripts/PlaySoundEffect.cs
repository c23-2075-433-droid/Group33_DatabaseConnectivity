using UnityEngine;

/// <summary>
/// Plays one of the effects in Resources/Sfx when something tells it to.
///
/// SoundEffects.Play is static, and a UnityEvent cannot hold a persistent
/// listener on a static method - so a generated scene has no way to ask for
/// a sound. This is the one-line component that gives it one: drop it on an
/// object, set the id, and wire Play() to a word's onCorrect the same way
/// PickUpItem.PickUp and PoseSequence.Play are wired.
///
/// The id is the file name in Assets/Resources/Sfx without its extension.
/// A missing clip is not an error here; SoundEffects already logs it once
/// and carries on, which is the right behaviour for a scene that is being
/// built before its audio exists.
/// </summary>
public class PlaySoundEffect : MonoBehaviour
{
    [Tooltip("File name in Assets/Resources/Sfx, without the extension - " +
             "for example 'brush_teeth'.")]
    public string id;

    [Tooltip("Scales this effect against the others. 1 is the clip as recorded.")]
    [Range(0f, 1f)]
    public float volume = 1f;

    /// <summary>Wire this to a word's onCorrect.</summary>
    public void Play()
    {
        if (string.IsNullOrEmpty(id)) return;
        SoundEffects.Play(id, volume);
    }
}
