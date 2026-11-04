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
/// Four channels now: everything (the AudioListener), Teacher Rachel, the
/// background music, and the short effects.
///
/// This file used to say that music, when it arrived, would want an
/// AudioMixer with a group per category. It does not yet, and the reason is
/// worth recording rather than quietly ignoring. Narration is applied
/// straight to the narrator's own AudioSource, and music is applied the same
/// way to MusicPlayer's - two sources, two multiplies, no routing. A mixer
/// would mean re-routing VoiceNarrator for no gain today, and there is no
/// mixer anywhere in the project to be consistent with. When sound effects
/// arrive and there are four categories fighting over one output, that is the
/// change to make.
/// </summary>
public static class VolumeSettings
{
    private const string MasterKey    = "SALINLAHI_MasterVolume";
    private const string MutedKey     = "SALINLAHI_Muted";
    private const string NarrationKey = "SALINLAHI_NarrationVolume";
    private const string MusicKey     = "SALINLAHI_MusicVolume";
    private const string MusicOnKey   = "SALINLAHI_MusicOn";
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
    /// Whether the music plays at all, kept apart from how loud it is - the
    /// same split Muted and Master already use here. The settings board shows
    /// MUSIC as an on/off plate rather than a slider, so turning it off must
    /// not forget the level behind it.
    /// </summary>
    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicOnKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MusicOnKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>
    /// The background music, on top of Master. 0..1.
    ///
    /// Defaults to 0.6 rather than 1: music is a bed under Rachel and under a
    /// child who is about to be listened to, and a soundtrack that arrives at
    /// full volume on first run is one the player turns off rather than down.
    /// </summary>
    public static float Music
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.6f));
        set
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Apply();
        }
    }

    /// <summary>
    /// Footsteps and cues, on top of Master. 0..1.
    ///
    /// Read by SoundEffects when it fires a shot rather than pushed onto a
    /// source, because effects are one-shots: there is no sustained source to
    /// set the volume of.
    /// </summary>
    public static float Effects
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 0.8f));
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

        VoiceNarrator narrator = Object.FindFirstObjectByType<VoiceNarrator>();
        if (narrator != null) narrator.ApplyNarrationVolume();

        // Not FindFirstObjectByType: the music player outlives the scene, and
        // its own static handle is both cheaper and correct across a load.
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.ApplyMusicVolume();
    }
}
