using UnityEngine;

/// <summary>
/// The player's sound settings, in one place, because two screens now change
/// them: the main menu's Settings popup and the in-game settings panel
/// (GameplayHUD).
///
/// The keys used to be private constants inside MainMenuController, which was
/// fine while the menu was the only thing that touched them. It also meant the
/// saved volume was applied in MainMenuController.Start() and nowhere else, so
/// starting Unity straight into a gameplay scene - which is how the game is
/// actually tested - came up at full volume however quiet the player had set
/// it. Apply() fixes that: anything that can show a volume control calls it on
/// start.
///
/// Named VolumeSettings rather than AudioSettings because UnityEngine already
/// has an AudioSettings, and a second one in the global namespace would be
/// ambiguous in every file that says "using UnityEngine".
///
/// Two channels, because the game has two things that make sound: everything
/// (the AudioListener) and Teacher Rachel. There is no music and no sound
/// effects in the project yet; when there are, they want an AudioMixer with a
/// group per category, and the sliders for them belong here beside these.
/// </summary>
public static class VolumeSettings
{
    private const string MasterKey    = "SALINLAHI_MasterVolume";
    private const string MutedKey     = "SALINLAHI_Muted";
    private const string NarrationKey = "SALINLAHI_NarrationVolume";

    /// <summary>Everything the game plays. 0..1.</summary>
    public static float Master
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
        set
        {
            PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>Teacher Rachel's voice, on top of Master. 0..1.</summary>
    public static float Narration
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(NarrationKey, 1f));
        set
        {
            PlayerPrefs.SetFloat(NarrationKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>Mute silences everything without forgetting the volume behind it.</summary>
    public static bool Muted
    {
        get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>
    /// Pushes the saved settings onto the live audio. Call on start from
    /// anything that owns a volume control, and after any change.
    ///
    /// Rachel's own AudioSource is left alone if she has not been created yet -
    /// VoiceNarrator applies Narration itself in Awake, so a narrator that
    /// appears later still comes up at the right level.
    /// </summary>
    public static void Apply()
    {
        AudioListener.volume = Muted ? 0f : Master;

        VoiceNarrator narrator = Object.FindFirstObjectByType<VoiceNarrator>();
        if (narrator != null) narrator.ApplyNarrationVolume();
    }
}
