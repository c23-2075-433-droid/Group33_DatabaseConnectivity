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
/// Two channels carry sound today: everything (the AudioListener) and Teacher
/// Rachel. MusicOn and Effects are the other two the settings screen shows.
/// They are remembered but route nothing, because the project still has no
/// music track, no sound effects and no AudioMixer - when it does, they want a
/// mixer group each, and this is where the wiring goes.
///
/// A setting that is saved and does nothing is honest; one that silently does
/// the wrong thing is not. Effects deliberately does NOT drive Master: Master
/// is the whole bus including Rachel, so binding the two would make the SOUND
/// EFFECTS slider quietly fight the MS. RACHEL slider below it.
/// </summary>
public static class VolumeSettings
{
    private const string MasterKey    = "SALINLAHI_MasterVolume";
    private const string MutedKey     = "SALINLAHI_Muted";
    private const string NarrationKey = "SALINLAHI_NarrationVolume";
    private const string MusicKey     = "SALINLAHI_MusicOn";
    private const string EffectsKey   = "SALINLAHI_EffectsVolume";

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

    /// <summary>
    /// Background music on or off. Remembered, but nothing plays music yet.
    /// Defaults to on, so the setting does not start out looking broken.
    /// </summary>
    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>
    /// Sound effects, on top of Master. 0..1. Remembered, but the game has no
    /// sound effects to apply it to yet.
    /// </summary>
    public static float Effects
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 1f));
        set
        {
            PlayerPrefs.SetFloat(EffectsKey, Mathf.Clamp01(value));
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

        // MusicOn and Effects have nothing to push onto yet. They are read back
        // by the settings screens and will route through an AudioMixer when the
        // music and effects exist.

        VoiceNarrator narrator = Object.FindFirstObjectByType<VoiceNarrator>();
        if (narrator != null) narrator.ApplyNarrationVolume();
    }
}
