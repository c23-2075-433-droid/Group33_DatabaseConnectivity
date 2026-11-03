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

    // The line being lip-synced, and where we are in its track.
    private AudioClip trackedClip;
    private VisemeTracks.Change[] track;
    private int trackAt;

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

        // Real lip-sync first: if the clip she is playing has a mouth track,
        // follow it. This is the difference between a mouth that moves while
        // she talks and a mouth that moves WITH what she is saying.
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
    /// Drives the mouth from the playing clip's own timings.
    ///
    /// Returns false when there is nothing to follow - no clip, or a clip with
    /// no track - and the caller falls back to the cycle. That happens for
    /// every line that has not been generated yet, and for the Filipino word
    /// clips, which are recorded by a person and so have no alignment.
    /// </summary>
    private bool Sync()
    {
        AudioClip clip = narrator.CurrentClip;
        if (clip == null)
        {
            // Silence inside a line that HAS a recording: the 0.12s between
            // two parts, or the 0.2s tail. The pause is part of how the line
            // is spoken, so she holds her mouth shut through it - the cycle
            // would flutter three shapes into a third of a second and read as
            // exactly the randomness the tracks are here to remove.
            //
            // sawClip is what tells the two silences apart. A line with no
            // recording at all never sets it, so it still gets the cycle and
            // mouths its caption, which is the whole point of the fallback.
            if (!sawClip) return false;
            Show(idle);
            return true;
        }
        sawClip = true;

        if (!ReferenceEquals(clip, trackedClip))
        {
            trackedClip = clip;
            track = VisemeTracks.For(clip.name);
            trackAt = 0;
        }
        if (track == null || track.Length == 0) return false;

        float t = narrator.PlaybackTime;


        // Scrub backwards if the clip restarted; otherwise walk forward. The
        // track is in order, so this is a step per frame at most, not a search.
        if (trackAt > 0 && track[trackAt - 1].time > t) trackAt = 0;
        while (trackAt < track.Length && track[trackAt].time <= t) trackAt++;

        Show(MouthNamed(trackAt > 0 ? track[trackAt - 1].mouth : "Idle"));
        return true;
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
