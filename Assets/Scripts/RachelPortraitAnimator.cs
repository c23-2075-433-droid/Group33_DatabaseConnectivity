using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves Teacher Rachel's mouth while she is speaking, by swapping the sprite
/// in the narration panel's branch ring between a handful of drawn frames.
///
/// Why a script and not an Animator. The portrait does not exist until the
/// game is running: GameplayHUDLayout builds the whole HUD at run time,
/// because the fifteen chapter scenes are GENERATED and anything saved into
/// one is destroyed the next time its builder runs (Docs/VoicePromptingDesign.md
/// 6.3). An Animator Controller is an asset bound to a GameObject in a scene
/// or a prefab, and this project has neither - no .controller, no .anim and no
/// prefabs anywhere in it. Every other moving thing here is a coroutine or a
/// polled Update: GameplayHUD.Fade, VoiceInteractionUI.PulseRoutine, and
/// PoseSwitch swapping Kylo's walk frames. This is the same idea, one Image
/// instead of a SpriteRenderer.
///
/// What drives it is the narrator's own state, never a timer of our own:
/// VoiceNarrator.IsSpeaking is true from the moment a line is queued until
/// the queue empties, so her mouth stops the instant she does - including
/// when a line is cut off by the back button or a scene change, which a
/// duration-based animation would talk straight through. It also covers the
/// lines that have no recording yet: those are held on screen as captions
/// with IsSpeaking set, so she mouths them too rather than sitting frozen
/// under her own subtitle.
///
/// The mouth SHAPES are automated lip sync in the Adobe Animate sense:
/// Tools/bake_lipsync.py runs Rhubarb Lip Sync over every recording, which
/// finds the phonemes actually in the audio (guided by the line's script
/// text) and lays down a mouth keyframe for each - M/B/P, E, AI, O, U, F/V, L,
/// rest. Those land in VisemeTracks.json, and this just plays them against the
/// clip's clock. Because they come from the WAV itself, a re-cut take or a
/// spliced Filipino word is timed to what is really in the file - which the
/// old tracks, timed against the TTS text, were not.
///
/// A clip recorded since the last bake has no keyframes; for that one clip
/// the animator listens to the audio live (loudness, low-frequency share and
/// zero crossings) so she still talks, until the bake is re-run.
///
/// Missing frames are not an error. With no talking art loaded she is simply
/// the still portrait she was before - the same choice UITheme makes about
/// the portrait itself, where a missing sprite means a plainer panel rather
/// than a broken one.
/// </summary>
public class RachelPortraitAnimator : MonoBehaviour
{
    [Header("What is drawn")]

    [Tooltip("The Image in the branch ring. Set by GameplayHUDLayout when it builds the panel.")]
    public Image portrait;

    [Tooltip("Mouth closed, eyes open. What she wears between lines, and the frame every cycle starts from.")]
    public Sprite idle;

    [Tooltip("Mouth slightly open.")]
    public Sprite talk1;

    [Tooltip("Mouth moderately open.")]
    public Sprite talk2;

    [Tooltip("Mouth wide open.")]
    public Sprite talk3;

    [Tooltip("Eyes gently closed, same expression otherwise. Optional: with none, she simply never blinks.")]
    public Sprite blink;

    [Header("Lip-sync")]

    [Tooltip("Mouth for the vowel in 'say' and 'I'.")]
    public Sprite visemeAI;

    [Tooltip("Mouth for the vowel in 'bed'. Also stands in for consonants with no mouth of their own.")]
    public Sprite visemeE;

    [Tooltip("Mouth for the vowel in 'go'.")]
    public Sprite visemeO;

    [Tooltip("Mouth for the vowel in 'you'.")]
    public Sprite visemeU;

    [Tooltip("Lips pressed shut, for m, b and p.")]
    public Sprite visemeMBP;

    [Tooltip("Upper teeth on the lower lip, for f and v.")]
    public Sprite visemeFV;

    [Tooltip("Tongue raised, for l.")]
    public Sprite visemeL;

    [Header("Timing")]

    [Tooltip("Seconds each mouth frame is held. About a ninth of a second reads as speech; much faster and it flutters.")]
    public float frameSeconds = 0.11f;

    [Tooltip("How long one blink lasts.")]
    public float blinkSeconds = 0.09f;

    [Tooltip("Shortest and longest wait between blinks, while she is listening rather than talking. " +
             "Her panel is only on screen around her lines, and the quiet between two of them runs " +
             "about two seconds - so a window much wider than this means she never blinks at all.")]
    public Vector2 blinkEvery = new Vector2(1.5f, 4f);

    [Header("Lip-sync keyframes")]

    [Tooltip("Seconds added to the clip's clock when reading the baked keyframes. Positive shows each " +
             "mouth earlier, negative later. Leave at 0 unless a device's audio lags the picture.")]
    public float keyframeOffsetSeconds = 0f;

    [Header("Live fallback, for clips not baked yet")]

    [Tooltip("Seconds her mouth runs ahead of the sound. Lips take a shape just before it is heard; " +
             "a few hundredths of a second reads as in sync, zero reads as late.")]
    public float leadSeconds = 0.05f;

    [Tooltip("Shortest time one mouth shape is held, so fast speech does not flutter.")]
    public float minHoldSeconds = 0.07f;

    [Tooltip("How quickly the mouth opens to follow the loudness, in seconds.")]
    public float attackSeconds = 0.02f;

    [Tooltip("How quickly the mouth closes to follow the loudness, in seconds.")]
    public float releaseSeconds = 0.06f;

    [Tooltip("Loudness, as a share of the line's own speaking level, under which her mouth is shut.")]
    [Range(0f, 1f)] public float closedBelow = 0.16f;

    [Tooltip("Loudness, as a share of the line's own speaking level, above which her mouth is wide open.")]
    [Range(0f, 1.5f)] public float wideAbove = 0.62f;

    [Tooltip("Share of the energy under 700 Hz above which a sound counts as rounded (O, U).")]
    [Range(0f, 1f)] public float roundedAbove = 0.8f;

    [Tooltip("Zero crossings per second above which a sound counts as hiss (s, f, t) rather than voice.")]
    public float hissCrossings = 3500f;

    /// <summary>
    /// The voice she is lip-syncing to. Handed over by GameplayHUD, which
    /// already holds one - NOT read from VoiceNarrator.Instance, whose getter
    /// CREATES a narrator if there is none and would resurrect it during a
    /// scene change. See GameplayHUD.OnDestroy for the same warning.
    /// </summary>
    [System.NonSerialized]
    public VoiceNarrator narrator;

    /// <summary>
    /// The narration panel's fade. While the panel is invisible there is
    /// nothing to animate, and assigning a sprite to a UI Image dirties the
    /// canvas batch - so an off-screen Rachel costs a float comparison a
    /// frame and nothing else.
    /// </summary>
    [System.NonSerialized]
    public CanvasGroup visibility;

    // The mouth cycle, built once from whichever frames were supplied:
    // idle, talk1, talk2, talk3, talk2, talk1, and round again.
    private Sprite[] cycle;
    private int frame;
    private float timer;

    private float blinkAt;
    private float blinkUntil;

    // What the Image is actually showing. The sprite is only assigned when
    // this changes, so a held frame does not rebuild the canvas every tick.
    private Sprite shown;

    // Whether this run of speech has played any audio yet. See Sync().
    private bool sawClip;

    // The line being lip-synced: its baked keyframes and where we are in them,
    // or - for a clip with none - what has been measured of it live.
    private AudioClip trackedClip;
    private VisemeTracks.Change[] track;
    private int trackAt;
    private float clipLevel;      // its usual speaking loudness: the 1.0 of `level`
    private float[] window;       // samples read each frame, reused
    private float[] output;       // the fallback read of what the speaker is playing
    private float level;          // smoothed loudness, 0 = silent, 1 = speaking normally
    private string mouth = "Idle";
    private float heldFor;
    private float sinceOpen = 99f;

    // A clip's speaking level is measured once, ever - a scan of a few
    // hundred windows, not something to repeat each time she says the line.
    private static readonly Dictionary<AudioClip, float> levels = new Dictionary<AudioClip, float>();

    private const float WindowSeconds = 0.03f;
    private const float LowPassHz = 700f;

    void OnEnable()
    {
        Reset();
    }

    void OnDisable()
    {
        // Whatever interrupted her, the next thing anyone sees is her resting
        // face - not the half-open mouth she happened to be on.
        Show(idle);
    }

    void Update()
    {
        if (portrait == null) return;
        if (cycle == null) Build();

        // Faded out: nothing on screen to move. Come back on the idle frame.
        if (visibility != null && visibility.alpha <= 0.01f)
        {
            if (shown != idle) Reset();
            return;
        }

        bool speaking = narrator != null && narrator.IsSpeaking;
        if (!speaking) sawClip = false;

        // Real lip-sync first: if there is audio, follow the sound. This is
        // the difference between a mouth that moves while she talks and a
        // mouth that moves WITH what she is saying.
        if (speaking && Sync()) return;

        if (speaking && cycle.Length > 1)
        {
            // Never zero: a frame length of nothing would divide by it below.
            float hold = Mathf.Max(0.02f, frameSeconds);

            timer += Time.deltaTime;
            if (timer >= hold)
            {
                // A modulo rather than a subtraction: one long frame (a hitch,
                // a scene still loading) should not leave her owing six mouth
                // shapes and spitting them all out at once.
                timer %= hold;
                frame = (frame + 1) % cycle.Length;
            }
            Show(cycle[frame]);
            return;
        }

        if (!speaking && shown != idle && blinkUntil <= 0f) Reset();

        Blink();
    }

    /// <summary>
    /// Drives the mouth from the sound of the playing clip.
    ///
    /// Returns false when there is nothing to listen to - a line with no
    /// recording yet - and the caller falls back to the cycle, so she still
    /// mouths her caption.
    /// </summary>
    private bool Sync()
    {
        AudioClip clip = narrator.CurrentClip;
        if (clip == null)
        {
            // Silence inside a line that HAS a recording: the 0.12s between
            // two parts, or the 0.2s tail. The pause is part of how the line
            // is spoken, so she holds her mouth shut through it - the cycle
            // would flutter three shapes into a third of a second.
            //
            // sawClip is what tells the two silences apart. A line with no
            // recording at all never sets it, so it still gets the cycle and
            // mouths its caption, which is the whole point of the fallback.
            if (!sawClip) return false;
            level = 0f;
            mouth = "Idle";
            Show(idle);
            return true;
        }
        sawClip = true;

        if (!ReferenceEquals(clip, trackedClip))
        {
            trackedClip = clip;
            track = VisemeTracks.For(clip.name);
            trackAt = 0;
            clipLevel = track == null ? SpeakingLevelOf(clip) : 0f;
            level = 0f;
            heldFor = 99f;
        }

        // The baked keyframes, when the clip has them - which is every clip
        // Tools/bake_lipsync.py has been run over. Exactly Animate's Auto
        // Lip-Sync: the phonemes were found in the recording ahead of time,
        // and here they are simply played back against the clip's clock.
        if (track != null && track.Length > 0)
        {
            float t = narrator.PlaybackTime + keyframeOffsetSeconds;

            // Scrub back if the clip restarted; otherwise walk forward. The
            // track is in order, so this is a step per frame at most.
            if (trackAt > 0 && track[trackAt - 1].time > t) trackAt = 0;
            while (trackAt < track.Length && track[trackAt].time <= t) trackAt++;

            Show(MouthNamed(trackAt > 0 ? track[trackAt - 1].mouth : "Idle"));
            return true;
        }

        // No keyframes yet - a recording added since the last bake. Listen
        // to it live instead, so she is never frozen on a new line.
        float rms, crossings, lowShare;
        if (!Measure(clip, out rms, out crossings, out lowShare)) return false;

        // Loudness against the line's own speaking level, so a soft line and
        // a loud one open her mouth the same amount. Opens fast, closes a
        // little slower - the way a jaw actually moves.
        float target = clipLevel > 0f ? Mathf.Clamp(rms / clipLevel, 0f, 1.5f) : 0f;
        float ease = target > level ? attackSeconds : releaseSeconds;
        level = Mathf.Lerp(level, target, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, ease)));

        heldFor += Time.deltaTime;
        string next = Classify(level, crossings, lowShare);
        if (next != "Idle" && next != "MBP") sinceOpen = 0f;
        else sinceOpen += Time.deltaTime;

        if (next != mouth && heldFor >= minHoldSeconds)
        {
            mouth = next;
            heldFor = 0f;
        }
        Show(MouthNamed(mouth));
        return true;
    }

    /// <summary>Which mouth a moment of sound calls for.</summary>
    private string Classify(float loudness, float crossings, float lowShare)
    {
        if (loudness < closedBelow)
        {
            // A short closure in the middle of speech is a b, m or p: lips
            // pressed. A longer one is her simply resting between words.
            return sinceOpen < 0.18f ? "MBP" : "Idle";
        }

        // Hiss: lots of zero crossings and little low-pitched energy under it.
        if (crossings > hissCrossings && lowShare < 0.35f)
            return loudness < 0.4f ? "FV" : "E";

        // Rounded vowels put nearly all their energy low, and cross zero
        // slowly. An "ee" is low-heavy too but crosses faster, from its high
        // second formant, so it stays spread.
        bool rounded = lowShare > roundedAbove && crossings < 1100f;

        if (loudness >= wideAbove) return rounded ? "O" : "AI";
        if (rounded) return "U";
        return lowShare > 0.6f && crossings < 1600f ? "L" : "E";
    }

    /// <summary>
    /// Reads the stretch of the clip about to be heard and measures it:
    /// loudness (RMS), zero crossings per second, and the share of its energy
    /// below LowPassHz. Reads the clip's own samples, so it is unaffected by
    /// the volume slider; falls back to the speaker's output where a clip
    /// cannot be read (one not set to Decompress On Load).
    /// </summary>
    private bool Measure(AudioClip clip, out float rms, out float crossings, out float lowShare)
    {
        rms = crossings = lowShare = 0f;
        int channels = Mathf.Max(1, clip.channels);
        int rate = clip.frequency;
        int frames = Mathf.Min(clip.samples, Mathf.Max(64, Mathf.RoundToInt(rate * WindowSeconds)));
        if (frames <= 0) return false;

        int start = narrator.PlaybackSamples + Mathf.RoundToInt(leadSeconds * rate) - frames / 2;
        start = Mathf.Clamp(start, 0, clip.samples - frames);

        if (window == null || window.Length != frames * channels) window = new float[frames * channels];
        if (clip.GetData(window, start))
        {
            Analyse(window, channels, rate, out rms, out crossings, out lowShare);
            return true;
        }

        // Fallback: what the speaker is actually playing, un-scaled by the
        // volume. With the volume at nothing there is nothing to measure.
        float volume = narrator.Volume;
        if (volume < 0.01f) return false;
        if (output == null) output = new float[1024];
        if (!narrator.ReadOutput(output)) return false;
        Analyse(output, 1, AudioSettings.outputSampleRate, out rms, out crossings, out lowShare);
        rms /= volume;
        return true;
    }

    private static void Analyse(float[] data, int channels, int rate,
                                out float rms, out float crossings, out float lowShare)
    {
        int frames = data.Length / channels;
        float a = 1f - Mathf.Exp(-2f * Mathf.PI * LowPassHz / rate);
        float lp = 0f, energy = 0f, lowEnergy = 0f, previous = 0f;
        int crossed = 0;

        for (int i = 0; i < frames; i++)
        {
            float x = 0f;
            for (int c = 0; c < channels; c++) x += data[i * channels + c];
            x /= channels;

            energy += x * x;
            lp += a * (x - lp);
            lowEnergy += lp * lp;
            if (i > 0 && (x >= 0f) != (previous >= 0f)) crossed++;
            previous = x;
        }

        rms = Mathf.Sqrt(energy / Mathf.Max(1, frames));
        crossings = crossed * (float)rate / Mathf.Max(1, frames);
        lowShare = energy > 1e-9f ? Mathf.Clamp01(lowEnergy / energy) : 0f;
    }

    /// <summary>
    /// The loudness this clip is usually spoken at: the 80th percentile of its
    /// windows that are not silence. Not the peak - one plosive would set the
    /// bar so high the rest of the line barely opened her mouth.
    /// </summary>
    private static float SpeakingLevelOf(AudioClip clip)
    {
        float known;
        if (levels.TryGetValue(clip, out known)) return known;

        int channels = Mathf.Max(1, clip.channels);
        int frames = Mathf.Min(clip.samples, Mathf.Max(64, Mathf.RoundToInt(clip.frequency * WindowSeconds)));
        var found = new List<float>();
        if (frames > 0)
        {
            float[] chunk = new float[frames * channels];
            for (int start = 0; start + frames <= clip.samples; start += frames)
            {
                if (!clip.GetData(chunk, start)) break;
                float sum = 0f;
                for (int i = 0; i < chunk.Length; i++) sum += chunk[i] * chunk[i];
                float r = Mathf.Sqrt(sum / chunk.Length);
                if (r > 0.005f) found.Add(r);
            }
        }

        // An unreadable clip goes through the output fallback, which is in
        // real units, so it is judged against an ordinary speaking level.
        float result = 0.1f;
        if (found.Count > 0)
        {
            found.Sort();
            result = found[Mathf.Min(found.Count - 1, Mathf.FloorToInt(found.Count * 0.8f))];
        }
        levels[clip] = result;
        return result;
    }

    private Sprite MouthNamed(string mouth)
    {
        switch (mouth)
        {
            case "AI":  return visemeAI  != null ? visemeAI  : talk3;
            case "E":   return visemeE   != null ? visemeE   : talk1;
            case "O":   return visemeO   != null ? visemeO   : talk2;
            case "U":   return visemeU   != null ? visemeU   : talk1;
            case "MBP": return visemeMBP != null ? visemeMBP : idle;
            case "FV":  return visemeFV  != null ? visemeFV  : talk1;
            case "L":   return visemeL   != null ? visemeL   : talk2;
            default:    return idle;
        }
    }

    /// <summary>
    /// Shuts the mouth and starts the next cycle from the beginning, so she
    /// always opens on the same frame rather than resuming mid-vowel.
    /// </summary>
    private void Reset()
    {
        frame = 0;
        timer = 0f;
        trackedClip = null;
        track = null;
        trackAt = 0;
        level = 0f;
        mouth = "Idle";
        heldFor = 99f;
        sinceOpen = 99f;
        blinkUntil = 0f;
        blinkAt = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
        Show(idle);
    }

    private void Blink()
    {
        if (blink == null || idle == null) return;

        if (blinkUntil > 0f)
        {
            if (Time.time >= blinkUntil)
            {
                blinkUntil = 0f;
                blinkAt = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
                Show(idle);
            }
            return;
        }

        if (Time.time >= blinkAt)
        {
            blinkUntil = Time.time + blinkSeconds;
            Show(blink);
        }
    }

    private void Show(Sprite sprite)
    {
        if (portrait == null || sprite == null || ReferenceEquals(shown, sprite)) return;
        shown = sprite;
        portrait.sprite = sprite;
    }

    /// <summary>
    /// Builds the mouth cycle out of whatever art arrived. Each missing frame
    /// just drops out, so three frames animate, two animate, and one leaves a
    /// still portrait - there is no count at which this breaks.
    /// </summary>
    private void Build()
    {
        if (idle == null) idle = portrait.sprite;

        Sprite[] order = { idle, talk1, talk2, talk3, talk2, talk1 };
        int count = 0;
        for (int i = 0; i < order.Length; i++) if (order[i] != null) count++;

        cycle = new Sprite[count];
        int at = 0;
        for (int i = 0; i < order.Length; i++) if (order[i] != null) cycle[at++] = order[i];

        Reset();
    }
}
