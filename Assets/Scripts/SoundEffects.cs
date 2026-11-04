using UnityEngine;

/// <summary>
/// The game's short sounds: Kylo's footsteps, the cue that says a word landed,
/// and the click a button makes when it is pressed.
///
/// Why this exists at all, rather than a PlayOneShot wherever a sound is
/// wanted. Three reasons, in order of how much they matter:
///
///   1. The scenes are GENERATED. An AudioSource dropped into one by hand is
///      destroyed the next time somebody runs Build Bahay Scene 2, and so is
///      every clip assigned to it. Nothing here lives in a scene.
///   2. Effects need their own volume. Master, Narration and Music already
///      exist; a footstep answering to the music slider would be wrong.
///   3. A footstep that fires five times in 1.2 seconds has to not sound like
///      the same sample five times. Pitch is varied per shot, which is one
///      line here and would be copied everywhere otherwise.
///
/// Timing note worth keeping. This game listens to the child almost
/// continuously, and the microphone hears the device's own speaker -
/// VoiceCommand says so plainly. Effects get away with what music cannot
/// because of WHEN they play: Kylo only moves after a word has been
/// recognised, during the window SceneObjectiveController holds open while
/// the action plays out, when no objective is live and nothing is being
/// scored. A footstep heard by the recognizer lands where there is nothing
/// to match it against.
/// </summary>
public class SoundEffects : MonoBehaviour
{
    public static SoundEffects Instance { get; private set; }

    /// <summary>Where the clips live, under a Resources folder so a run-time object can load them.</summary>
    public const string Folder = "Sfx/";

    /// <summary>The sound a button makes. Played by UIClickSound, not by the buttons themselves.</summary>
    public const string ClickId = "ui_click";

    [Tooltip("How far the pitch of a repeated effect is allowed to wander, so five " +
             "footsteps in a row are not five copies of one sound.")]
    [Range(0f, 0.4f)]
    public float pitchVariation = 0.12f;

    // Two sources, not one, and the reason is pitchVariation. It is set on the
    // source per shot, and a source carries one pitch for everything already
    // playing through it - so a click fired while Kylo is mid-stride would
    // retune the footstep that is still sounding. The world's effects wander;
    // the UI's never do; they get a source each.
    private AudioSource source;
    private AudioSource uiSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = NewSource();
        uiSource = NewSource();
    }

    private AudioSource NewSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = 0f;
        return s;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Plays an effect by name, at the Effects volume and a slightly varied
    /// pitch. A name with no clip behind it is silent and is NOT an error:
    /// most of this game's sound has not been made yet, and a missing file
    /// should cost nothing.
    /// </summary>
    public static void Play(string id, float volumeScale = 1f)
    {
        if (Instance == null || string.IsNullOrEmpty(id)) return;
        Instance.PlayHere(id, volumeScale);
    }

    /// <summary>
    /// The press of a button. Flat pitch, unlike everything else here: a
    /// footstep that wanders sounds like a different foot, but a button that
    /// wanders sounds like a different button, and the child presses LARO on
    /// the menu and Continue on every results panel often enough to hear it.
    ///
    /// Quieter than full, too. This fires on every press in the game, Rachel
    /// is usually about to talk, and a click at the same level as the cue that
    /// says a word was right would be telling the child those matter equally.
    /// </summary>
    public static void Click()
    {
        if (Instance == null) return;

        AudioClip clip = Resources.Load<AudioClip>(Folder + ClickId);
        if (clip == null) return;

        Instance.uiSource.PlayOneShot(clip, Mathf.Clamp01(VolumeSettings.Effects * 0.85f));
    }

    private void PlayHere(string id, float volumeScale)
    {
        AudioClip clip = Resources.Load<AudioClip>(Folder + id);
        if (clip == null) return;

        // PlayOneShot rather than clip + Play, so two footsteps can overlap
        // at the end of a run without cutting each other off.
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, Mathf.Clamp01(VolumeSettings.Effects * volumeScale));
    }

    /// <summary>
    /// One of the footstep sounds for the surface being walked on. Only wood
    /// exists so far - the house. The set this came from also has stone,
    /// gravel, leaves and mud, so school and park are a drop-in away.
    /// </summary>
    public static void Footstep(string surface = "wood")
    {
        Play("step_" + surface + "_" + Random.Range(1, 4), 0.7f);
    }
}

/// <summary>
/// Puts the effects player in the game once. The same shape as
/// GameplayHUDBootstrap, and attaching everywhere like MusicPlayerBootstrap.
/// </summary>
public static class SoundEffectsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (SoundEffects.Instance != null) return;

        GameObject go = new GameObject("SoundEffects");
        go.AddComponent<SoundEffects>();
    }
}
