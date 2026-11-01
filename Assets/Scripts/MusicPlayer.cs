using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The background music: one track per place, carried across scene loads, and
/// pulled down whenever anything more important than music is happening.
///
/// Why this is the one object in the project that uses DontDestroyOnLoad.
/// RunScore and VocabularyMemory both say in as many words that they are
/// static rather than DontDestroyOnLoad objects, and they are right to be -
/// they are pure data, and a static class survives a scene load for free. An
/// AudioSource cannot. Walking from the bedroom into the bathroom must not
/// restart the house music, and the only way a sound keeps sounding across
/// SceneManager.LoadScene is for the object making it to outlive the scene.
///
/// Ducking, and why it matters more here than in most games. This game listens
/// to the child through the device microphone, and VoiceCommand already
/// records what a phone speaker does to that: "the recognizer hears Rachel
/// saying 'Sabihin mo: Lakad' perfectly well and scores it as the player's own
/// attempt". The fix the project chose for narration was to deafen the
/// microphone for the length of every line. Music cannot do that - you cannot
/// stop the soundtrack for the whole scene - so it ducks instead, and
/// duckWhileListening is deliberately a field rather than a constant because
/// the right value is a measurement, not a guess. See Docs/VoicePromptingDesign.md.
///
/// Nothing in a saved scene refers to this: MusicPlayerBootstrap creates it at
/// run time, for the same reason the HUD and the prompt director are built
/// that way - the fifteen chapter scenes are generated, and anything added to
/// one by hand is destroyed the next time its builder runs.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    /// <summary>The live player, or null before the bootstrap has run.</summary>
    public static MusicPlayer Instance { get; private set; }

    [Header("How far music gets out of the way")]

    [Tooltip("Volume while Teacher Rachel is speaking, as a fraction of the music setting. " +
             "She is the game; music must never compete with the thing the child is meant to understand.")]
    [Range(0f, 1f)]
    public float duckUnderNarration = 0.35f;

    [Tooltip("Volume while the microphone is open. The recognizer hears this room's speaker, " +
             "so this is the number that decides whether music costs the game its answers. " +
             "Measure it on a real phone rather than trusting the default.")]
    [Range(0f, 1f)]
    public float duckWhileListening = 0.18f;

    [Header("Fades")]

    [Tooltip("Getting out of the way is quick - the point is to be gone before the child speaks.")]
    public float duckDownSeconds = 0.25f;

    [Tooltip("Coming back is slower, so the music swells rather than pops.")]
    public float duckUpSeconds = 0.5f;

    [Tooltip("Crossfade when the place changes. Within a chapter the track does not change at all.")]
    public float crossfadeSeconds = 1.2f;

    // Two sources so a change of place can crossfade; `live` is the one that
    // currently owns the track, `fading` is the one on its way out.
    private AudioSource live;
    private AudioSource fading;

    private string trackName;
    private Coroutine crossfade;

    // The duck multiplier, eased towards its target every frame rather than
    // snapped, so a line of Rachel's does not punch a hole in the music.
    private float duck = 1f;
    private float duckTarget = 1f;

    // Found per scene. NEVER VoiceNarrator.Instance: that getter CREATES a
    // narrator if there is none, which in a menu scene would conjure one out
    // of nothing just because the music wanted to know if it should duck.
    private VoiceNarrator narrator;
    private VoiceCommand voice;
    private float lookAgainAt;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        live = NewSource("Live");
        fading = NewSource("Fading");

        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private AudioSource NewSource(string label)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(transform, false);
        AudioSource a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = true;
        a.volume = 0f;
        // 2D: music has no position in the world, and a scene's AudioListener
        // moves with the camera.
        a.spatialBlend = 0f;
        return a;
    }

    // ------------------------------------------------------------------
    // which track belongs to where
    // ------------------------------------------------------------------

    /// <summary>
    /// The track a scene asks for, by name. One per place rather than one per
    /// scene: the five rooms of the house are one continuous morning, and
    /// restarting the music at every door would say otherwise.
    /// </summary>
    public static string TrackFor(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;

        if (sceneName.StartsWith("MainMenu")) return "music_menu";
        if (sceneName.StartsWith("LevelSelect")) return "music_journey";
        if (sceneName.StartsWith("Chapter1")) return "music_bahay";
        if (sceneName.StartsWith("Chapter2")) return "music_paaralan";
        if (sceneName.StartsWith("Chapter3")) return "music_parke";

        return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The scene's own AudioListener has just replaced the old one; the
        // references we cached belong to a scene that no longer exists.
        narrator = null;
        voice = null;
        Apply(scene.name);
    }

    private void Apply(string sceneName)
    {
        string wanted = TrackFor(sceneName);

        // Same place: leave the track exactly where it is, mid-bar and all.
        if (wanted == trackName) return;

        trackName = wanted;

        AudioClip clip = string.IsNullOrEmpty(wanted)
            ? null
            : Resources.Load<AudioClip>("Music/" + wanted);

        // A missing track is the normal state until the music is generated, and
        // it is not an error - the game is silent exactly as it is today.
        if (crossfade != null) StopCoroutine(crossfade);
        crossfade = StartCoroutine(Crossfade(clip));
    }

    private IEnumerator Crossfade(AudioClip next)
    {
        AudioSource outgoing = live;
        live = fading;
        fading = outgoing;

        if (next != null)
        {
            live.clip = next;
            live.volume = 0f;
            live.Play();
        }

        float from = fading.volume;
        float to = Target();

        for (float t = 0f; t < crossfadeSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / crossfadeSeconds;
            fading.volume = Mathf.Lerp(from, 0f, k);
            if (next != null) live.volume = Mathf.Lerp(0f, to, k);
            yield return null;
        }

        fading.Stop();
        fading.clip = null;
        fading.volume = 0f;
        if (next != null) live.volume = to;

        crossfade = null;
    }

    // ------------------------------------------------------------------
    // ducking
    // ------------------------------------------------------------------

    void Update()
    {
        // Re-find the narrator and the recognizer a few times a second rather
        // than every frame. Both are created at run time and may not exist yet
        // when a scene has only just loaded.
        if ((narrator == null || voice == null) && Time.unscaledTime >= lookAgainAt)
        {
            lookAgainAt = Time.unscaledTime + 0.5f;
            if (narrator == null) narrator = FindFirstObjectByType<VoiceNarrator>();
            if (voice == null) voice = FindFirstObjectByType<VoiceCommand>();
        }

        bool speaking = narrator != null && narrator.IsSpeaking;
        bool listening = voice != null && voice.IsListening;

        // Whichever reason is the stronger wins. Both at once is possible for a
        // moment while the recognizer is being cancelled.
        duckTarget = 1f;
        if (speaking) duckTarget = Mathf.Min(duckTarget, duckUnderNarration);
        if (listening) duckTarget = Mathf.Min(duckTarget, duckWhileListening);

        float seconds = duckTarget < duck ? duckDownSeconds : duckUpSeconds;
        duck = seconds <= 0f
            ? duckTarget
            : Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime / seconds);

        // The crossfade owns the volume while it runs, so it is not fought over.
        if (crossfade == null && live != null && live.clip != null) live.volume = Target();
    }

    private float Target()
    {
        return Mathf.Clamp01(VolumeSettings.Music) * Mathf.Clamp01(duck);
    }

    /// <summary>
    /// Pushes the saved music volume onto the live track. Called by
    /// VolumeSettings.Apply, the same way the narrator's volume is pushed.
    /// </summary>
    public void ApplyMusicVolume()
    {
        if (crossfade == null && live != null && live.clip != null) live.volume = Target();
    }
}

/// <summary>
/// Puts a MusicPlayer in the game once, and only once.
///
/// The same shape as GameplayHUDBootstrap and VoicePromptBootstrap, for the
/// same reason they give: the chapter scenes are generated, so a music player
/// added to a saved scene would be destroyed the next time someone ran its
/// builder. This one differs in attaching EVERYWHERE - the menu and the
/// journey map have music too - and in surviving the load rather than being
/// rebuilt, which MusicPlayer.Awake handles.
/// </summary>
public static class MusicPlayerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (MusicPlayer.Instance != null) return;

        GameObject go = new GameObject("MusicPlayer");
        go.AddComponent<MusicPlayer>();
    }
}
