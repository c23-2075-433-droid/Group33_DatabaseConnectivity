using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kylo asleep, and Kylo waking up: the snoring that runs under Level 1's
/// opening, and the sit-up that answers "Bangon".
///
/// Level 1 starts on a child asleep in bed being asked to wake him. Until this
/// existed, that was a still picture and a silence - and then the picture
/// changed, in one frame, to a different still picture. Nothing told the child
/// that Kylo was asleep, and nothing told them their word had done anything
/// except that the badge went away.
///
/// Two halves, and they share a file because they are two halves of one beat:
///
///   SLEEPING. A soft snore breathes under the scene - inhale, pause, whistle,
///   rest, round again - for as long as Kylo is lying down. It keeps going
///   through a wrong answer and through one the recognizer could not make out,
///   because he is still asleep after both. See Rhythm, below, for why the
///   loop is built out of two one-shots instead of a looping clip.
///
///   WAKING UP. The moment Bangon lands, the snore stops, Kylo yawns, and the
///   sitting pose rises up off the mattress and settles upright with a stretch
///   on the end of it. Nothing here decides whether the answer was right:
///   PlayerMovement.Bangon() is what the objective's onCorrect event calls,
///   and it raises OnWakeStageChanged only on the real transition, so a wrong
///   word cannot reach any of this.
///
/// Why a coroutine and a transform rather than an Animator. There is not one
/// Animator, AnimationClip or AnimatorController anywhere in this project, and
/// that is not an oversight - every pose in the game is a SpriteRenderer swap,
/// and every bit of motion over the top of it is a coroutine on a transform
/// (see PoseSettle, IdleBreath, WalkIntoScene). An Animator here would mean a
/// second system animating the one character the sprite-swap system already
/// owns, with two things writing one transform, which is how a character
/// starts to jitter. What it would buy is in-between DRAWINGS, and those need
/// an artist, not a state machine. So this does what PoseSettle does, one step
/// further: the sitting pose starts tipped back near-horizontal where the
/// sleeping one lay, and swings up onto its hip, overshooting a little and
/// rocking back the way a child actually sits up.
///
/// Rhythm, and why the snore is two files. A loop baked into one clip either
/// breathes mechanically or carries its own trailing silence - and
/// Tools/import_sfx.py trims silence off every effect that arrives, for the
/// good reason that a footstep with 80 ms of padding fires late. So the
/// pauses live here as fields, and the pitch wanders a touch each breath, the
/// same trick SoundEffects plays on footsteps so five in a row are not five
/// copies of one sound.
///
/// The microphone. This game listens to the child almost continuously and the
/// recognizer hears the device's own speaker - VoiceCommand says so plainly,
/// and MusicPlayer ducks the soundtrack for exactly that reason. A snore is a
/// harder case than music, because it plays WHILE the child is being asked for
/// the word: silence it and the sleeping half of the scene is gone. So it is
/// quiet, it sits around 100-130 Hz where a six-year-old's vowels do not, it
/// drops right back under Rachel, and it pulls back a little while the
/// microphone is open. That last number is the one to measure on a real phone
/// rather than trust - see Docs/VoicePromptingDesign.md.
///
/// Nothing in a saved scene refers to this. WakeUpSequenceBootstrap attaches
/// it at run time to whichever character starts a scene lying down, for the
/// reason the HUD, the music and the prompt director are all built that way:
/// the chapter scenes are generated, and anything added to one by hand is
/// destroyed the next time its builder runs.
/// </summary>
public class WakeUpSequence : MonoBehaviour
{
    // The ids are the file names under Assets/Resources/Sfx. Made by
    // Tools/make_wakeup_sfx.py; a missing one is silent and is not an error.
    public const string SnoreInId = "snore_in";
    public const string SnoreOutId = "snore_out";
    public const string YawnId = "yawn";
    public const string StretchId = "stretch";

    [Header("Snoring")]
    [Tooltip("How loud the snore is, before the Effects setting. Low on purpose: " +
             "it is atmosphere playing under a child who is being listened to, not " +
             "a punchline.")]
    [Range(0f, 1f)]
    public float snoreVolume = 0.4f;

    [Tooltip("Gap between the inhale and the whistle that answers it.")]
    public float breathGap = 0.26f;

    [Tooltip("Rest after the whistle, before the next breath starts. The slowest " +
             "part of the cycle - about three seconds a breath altogether, which is " +
             "a sleeping child rather than one running a fever.")]
    public float breathRest = 0.85f;

    [Tooltip("How far the pitch of each breath may wander, so the snore does not " +
             "tick over like a machine. The same dial SoundEffects turns on footsteps.")]
    [Range(0f, 0.3f)]
    public float pitchWander = 0.07f;

    [Tooltip("How quickly the snore is gone once Bangon lands. Fast, but not a cut: " +
             "a sound that stops dead in one frame clicks.")]
    public float snoreFadeOut = 0.12f;

    [Header("How far the snore gets out of the way")]
    [Tooltip("Volume while Teacher Rachel is speaking, as a fraction of the above. " +
             "She is the game; a snore must never compete with the thing the child " +
             "is meant to understand.")]
    [Range(0f, 1f)]
    public float duckUnderNarration = 0.3f;

    [Tooltip("Volume while the microphone is open. Unlike the music, this does not " +
             "go to nearly nothing: the snore is what tells the child Kylo is " +
             "asleep, and it is wanted during the very moment they are answering. " +
             "Measure it on a real phone against the attempt log rather than " +
             "trusting the default - if answers start getting lost, this is the " +
             "first number to pull down.")]
    [Range(0f, 1f)]
    public float duckWhileListening = 0.7f;

    [Tooltip("How quickly the snore ducks and comes back.")]
    public float duckSeconds = 0.3f;

    [Header("Drawn in-between frames")]
    [Tooltip("Where the sit-up's drawn frames live, under a Resources folder so a " +
             "run-time object can load them. Made by Tools/import_wakeup_frames.py " +
             "out of one Canva strip - the same way the Upo frames are made.\n\n" +
             "With the frames present they ARE the sit-up: his arms, back and knees " +
             "genuinely change between drawings, which no transform can imitate. " +
             "Without them the swing below plays instead, which is the cheap " +
             "nine-tenths PoseSettle's comment describes. Nothing breaks either way.")]
    public string framesFolder = "Wake/";

    [Tooltip("What fraction of the sit-up is the drawn frames, the rest being the " +
             "dissolve from the last frame into the real sitting pose. Only used " +
             "when the frames are there.")]
    [Range(0.2f, 0.9f)]
    public float framesShare = 0.6f;

    [Header("Waking up")]
    [Tooltip("Beat between the snore stopping and the sit-up starting. The yawn " +
             "begins in this gap, so the sound leads the movement by a hair - which " +
             "is the right way round, since a yawn is what wakes you.")]
    public float yawnLead = 0.2f;

    [Tooltip("How long the sleeping pose takes to dissolve into the sitting one.\n\n" +
             "Without this the two drawings change over in a single frame, and no " +
             "amount of easing the swing hides that: they are different pictures - " +
             "6.1 units wide lying down against 3.9 sitting - so the silhouette jumps " +
             "however well the geometry lines up. Fading one into the other while the " +
             "swing is already under way is what turns a cut into a movement. Keep it " +
             "shorter than the swing; a long dissolve reads as a ghost rather than a " +
             "boy.")]
    public float poseCrossFade = 0.3f;

    [Tooltip("How long the sit-up takes, drawn frames or swing. Long enough to read " +
             "as a movement, short enough that the child is not kept waiting for the " +
             "next word.\n\n" +
             "With the frames, Frames Share of this is divided between them - at 1.6 " +
             "that is about a third of a second a pose, where 0.85 gave a sixth and " +
             "went past too quickly to see what he was doing. The whole beat, yawn to " +
             "stretch, then runs about 2.3 seconds and \"Tayo\" is asked at around 2.6.")]
    public float riseSeconds = 1.6f;

    [Tooltip("A breath held sitting upright before he stretches. Worth having for " +
             "two reasons: it is what a person actually does - up first, stretch " +
             "second - and it keeps the stretch's slide-whistle out of the loud " +
             "middle of the yawn, which is a little over a second long.")]
    public float stretchDelay = 0.3f;

    [Tooltip("How far back the sitting pose is tipped when it starts, in degrees - " +
             "roughly where the sleeping pose lay. Positive tips the head toward the " +
             "pillow end of the bed, which in this art is the left.\n\n" +
             "A rigid rotation cannot match both ends of the sleeping pose at once: " +
             "the sitting art measures about 3.7 units from seat to crown, and swung " +
             "flat enough for the head to touch the mattress it reaches further left " +
             "than the sleeping pose does. Flatter is the better error - a child who " +
             "starts half upright has not been woken by anything - so this is set " +
             "flat and Rise Drift X pulls the overreach back in.")]
    public float riseFromAngle = 68f;

    [Tooltip("How far to the right the pose starts, in world units, easing back to " +
             "nothing as he comes up. This is what keeps his head on the pillow at " +
             "the first frame rather than through the headboard - see Rise From " +
             "Angle. Measured for Level 1's bed: at 68 degrees the crown overreaches " +
             "the sleeping pose by a little under a unit. The same trick, and the " +
             "same reasoning, as PoseSettle's Drift From.")]
    public float riseDriftX = 0.9f;

    [Tooltip("How far past upright the sit-up leans before settling, in degrees, and " +
             "it is exactly that many - the lean is applied to what is left of the " +
             "swing rather than added on top of it, so the two cannot fight. This is " +
             "the whole difference between a child sitting up and a plank being " +
             "raised; set it to 0 for a plain rise.")]
    public float riseOvershoot = 9f;

    [Tooltip("How much the figure is squashed into the mattress at the start, as a " +
             "fraction of its height.")]
    [Range(0f, 0.25f)]
    public float startSquash = 0.07f;

    [Tooltip("How long the stretch on the end of the sit-up takes.")]
    public float stretchSeconds = 0.6f;

    [Tooltip("How far the figure reaches up in the stretch, as a fraction of its " +
             "height.")]
    [Range(0f, 0.3f)]
    public float stretchReach = 0.08f;

    [Tooltip("How far the stretch leans, in degrees, so he reaches rather than " +
             "merely inflates.")]
    public float stretchLean = 5f;

    [Tooltip("Loudness of the yawn and the stretch, before the Effects setting. " +
             "Louder than the snore: these are the game answering a word the child " +
             "got right.")]
    [Range(0f, 1f)]
    public float wakeVolume = 0.85f;

    private PlayerMovement movement;
    private SpriteRenderer sprite;
    private AudioSource snoreSource;

    private Coroutine snoring;
    private Coroutine waking;
    private Coroutine fading;

    // The sleeping pose as the scene authored it, read while he is still in it.
    // The dissolve needs a copy of the picture he was lying in, and by the time
    // "Bangon" has been called PlayerMovement has already replaced it.
    private Sprite sleepingSprite;
    private Vector3 sleepingPosition;
    private Quaternion sleepingRotation;
    private Vector3 sleepingScale;

    // The second renderer the dissolve fades out. A plain GameObject rather
    // than a child of this one: parented, it would inherit the swing and tip
    // over with him, and the whole point is that the sleeping pose stays on the
    // mattress while the sitting one rises off it.
    private SpriteRenderer sleepingGhost;

    // His own tint, so the dissolve can be undone exactly rather than by
    // assuming white.
    private Color spriteTint = Color.white;

    // The drawn sit-up, frame 0 first, or null when it has not been made yet.
    private Sprite[] frames;

    // Where the sit-up has to end up: read once, at the moment the word lands,
    // and kept as fields so a sit-up cut short by a fast "Tayo" can be put back
    // exactly rather than left on a half-rotated frame.
    private Vector3 restPosition;
    private Vector3 restScale;
    private Quaternion restRotation;

    // Eased towards its target every frame rather than snapped, the way
    // MusicPlayer does it, so a line of Rachel's does not punch a hole in the
    // snore.
    private float duck = 1f;

    // Found per scene, and never VoiceNarrator.Instance - that getter CREATES
    // a narrator if there is none, and conjuring one up just to ask whether to
    // duck is the mistake MusicPlayer records having avoided.
    private VoiceNarrator narrator;
    private VoiceCommand voice;
    private float lookAgainAt;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) spriteTint = sprite.color;

        snoreSource = gameObject.AddComponent<AudioSource>();
        snoreSource.playOnAwake = false;
        snoreSource.loop = false;
        // 2D. The bed is a few units from the camera and a snore panned to one
        // ear would be a bug, not a feature.
        snoreSource.spatialBlend = 0f;
    }

    void OnEnable()
    {
        if (movement != null) movement.OnWakeStageChanged += HandleWakeStageChanged;
    }

    void OnDisable()
    {
        if (movement != null) movement.OnWakeStageChanged -= HandleWakeStageChanged;
    }

    void Start()
    {
        if (movement == null) return;
        if (movement.currentWakeStage != PlayerMovement.WakeStage.LyingDown) return;

        RememberSleepingPose();
        LoadFrames();
        snoring = StartCoroutine(Snore());
    }

    /// <summary>
    /// Picks up the drawn sit-up, if it has been made: frame 0, then as many
    /// numbered frames as are there.
    ///
    /// Frame 0 is the sleeping pose re-drawn on the frames' own canvas, and it
    /// is what makes the rest of this simple - every frame shares one canvas
    /// whose centre is the sitting pose's centre, so the whole sit-up plays
    /// from one renderer at one transform with no geometry in the C# at all.
    /// One frame on its own is not a sit-up, so fewer than two is the same as
    /// none.
    /// </summary>
    private void LoadFrames()
    {
        List<Sprite> found = new List<Sprite>();
        for (int i = 0; ; i++)
        {
            Sprite s = Resources.Load<Sprite>(framesFolder + "wake_frame_" + i);
            if (s == null) break;
            found.Add(s);
        }

        frames = found.Count >= 2 ? found.ToArray() : null;

        if (frames == null && found.Count == 1)
            Debug.Log("WakeUpSequence: only frame 0 of the drawn sit-up is there, so " +
                      "the swing plays instead. See Tools/import_wakeup_frames.py.");
    }

    /// <summary>
    /// Keeps the sleeping pose - picture, place and turn - while he is still
    /// in it, for the dissolve to fade out of later.
    ///
    /// The sprite comes from PlayerMovement's own authored field rather than
    /// off the renderer, so it is the pose the scene MEANT him to sleep in
    /// even if something else has painted over it by now.
    /// </summary>
    private void RememberSleepingPose()
    {
        sleepingSprite = movement.lyingDownSprite;
        if (sleepingSprite == null && sprite != null) sleepingSprite = sprite.sprite;

        sleepingPosition = transform.localPosition;
        sleepingRotation = transform.localRotation;
        sleepingScale = transform.localScale;
    }

    void OnDestroy()
    {
        // The stage this held open belongs to the sequencer, and a scene left
        // mid-sit-up must not hand the next one a character that is forever
        // mid-action.
        if (movement != null) movement.SetPosing(false);

        // The dissolve's copy is a GameObject of its own, so it does not go
        // when this component does. A scene left mid-dissolve would otherwise
        // keep a sleeping boy on the bed with nobody left to fade him out.
        if (sleepingGhost != null) Destroy(sleepingGhost.gameObject);
    }

    // ------------------------------------------------------------------
    // sleeping
    // ------------------------------------------------------------------

    private IEnumerator Snore()
    {
        // A beat before the first breath, so the scene does not open on a
        // snore already in progress.
        yield return new WaitForSeconds(0.4f);

        while (true)
        {
            // One wander for the whole breath, not one per clip: the inhale
            // and the whistle that answers it are the same chest.
            float pitch = 1f + Random.Range(-pitchWander, pitchWander);

            yield return PlayBreath(SnoreInId, pitch);
            yield return new WaitForSeconds(breathGap);
            yield return PlayBreath(SnoreOutId, pitch);
            yield return new WaitForSeconds(breathRest);
        }
    }

    private IEnumerator PlayBreath(string id, float pitch)
    {
        AudioClip clip = Resources.Load<AudioClip>(SoundEffects.Folder + id);

        // A missing clip is silent and is NOT an error - SoundEffects says the
        // same, and most of this game's sound has still to be made. The wait
        // keeps the rhythm honest so the sleeping half of the scene is merely
        // quiet rather than spinning on a tight loop.
        if (clip == null)
        {
            yield return new WaitForSeconds(0.9f);
            yield break;
        }

        snoreSource.clip = clip;
        snoreSource.pitch = pitch;
        snoreSource.volume = Level();
        snoreSource.Play();

        // Held here rather than with one WaitForSeconds, so the duck follows
        // Rachel and the microphone WHILE a breath is sounding - a sustained
        // sound cannot have its volume set once at the start and be done with.
        float length = clip.length / Mathf.Max(pitch, 0.01f);
        for (float t = 0f; t < length && snoreSource.isPlaying; t += Time.deltaTime)
        {
            snoreSource.volume = Level();
            yield return null;
        }
    }

    void Update()
    {
        // Only the snore is ducked, and only while it is running. Once Kylo is
        // awake this component has nothing left to listen for.
        if (snoring == null) return;

        // Both are created at run time and may not exist yet when a scene has
        // only just loaded, so look again a couple of times a second rather
        // than every frame.
        if ((narrator == null || voice == null) && Time.unscaledTime >= lookAgainAt)
        {
            lookAgainAt = Time.unscaledTime + 0.5f;
            if (narrator == null) narrator = FindAnyObjectByType<VoiceNarrator>();
            if (voice == null) voice = FindAnyObjectByType<VoiceCommand>();
        }

        float target = 1f;
        if (narrator != null && narrator.IsSpeaking) target = Mathf.Min(target, duckUnderNarration);
        if (voice != null && voice.IsListening) target = Mathf.Min(target, duckWhileListening);

        duck = duckSeconds <= 0f
            ? target
            : Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime / duckSeconds);
    }

    private float Level()
    {
        return Mathf.Clamp01(VolumeSettings.Effects * snoreVolume * Mathf.Clamp01(duck));
    }

    /// <summary>
    /// Stops the snoring, quickly but not in one frame. Safe to call when
    /// nothing is snoring.
    /// </summary>
    private IEnumerator StopSnoring()
    {
        if (snoring != null)
        {
            StopCoroutine(snoring);
            snoring = null;
        }

        if (snoreSource == null || !snoreSource.isPlaying) yield break;

        float from = snoreSource.volume;
        for (float t = 0f; t < snoreFadeOut; t += Time.deltaTime)
        {
            snoreSource.volume = Mathf.Lerp(from, 0f, t / snoreFadeOut);
            yield return null;
        }

        snoreSource.Stop();
        snoreSource.clip = null;
        snoreSource.volume = 0f;
    }

    // ------------------------------------------------------------------
    // waking up
    // ------------------------------------------------------------------

    /// <summary>
    /// Puts the sleeping picture up as a copy of its own and takes him down to
    /// nothing, in one frame, so the dissolve can start from there. Returns
    /// false if there is nothing to dissolve - no renderer, no sleeping pose
    /// remembered, or the dissolve turned off - in which case he is left
    /// visible and the sit-up simply plays as a straight swap.
    /// </summary>
    private bool BeginDissolve(Sprite from, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        if (sprite == null || from == null || poseCrossFade <= 0f) return false;

        sleepingGhost = NewGhost(from, pos, rot, scale);
        sprite.color = WithAlpha(spriteTint, 0f);
        return true;
    }

    /// <summary>
    /// Fades the sleeping copy out and him in, while he is already swinging up.
    ///
    /// The two halves are deliberately not symmetrical: he comes in on the
    /// SQUARE of the curve, so he is most of the way solid by the time the
    /// sleeping copy has gone faint. Matched fades put two equally
    /// half-transparent boys on screen through the middle of it, which is what
    /// reads as a ghost rather than as one pose becoming another.
    /// </summary>
    private IEnumerator FinishDissolve()
    {
        for (float t = 0f; t < poseCrossFade; t += Time.deltaTime)
        {
            float k = Smoothstep(Mathf.Clamp01(t / poseCrossFade));

            if (sleepingGhost != null) sleepingGhost.color = WithAlpha(spriteTint, 1f - k);
            if (sprite != null) sprite.color = WithAlpha(spriteTint, k * k);

            yield return null;
        }

        fading = null;
        ClearSleepingGhost();
    }

    private SpriteRenderer NewGhost(Sprite from, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject go = new GameObject("Kylo Waking (dissolving)");

        // Beside him, never under him: parented, it would inherit the swing
        // and tip over with him.
        go.transform.SetParent(transform.parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;

        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = from;
        r.flipX = sprite.flipX;
        r.sharedMaterial = sprite.sharedMaterial;
        r.sortingLayerID = sprite.sortingLayerID;

        // One behind him, so as he solidifies he is the one in front.
        r.sortingOrder = sprite.sortingOrder - 1;
        r.color = spriteTint;
        return r;
    }

    /// <summary>
    /// Takes the dissolve down: the copy goes, and he is left at his own full
    /// tint. Safe at any point, including when no dissolve ever started.
    /// </summary>
    private void ClearSleepingGhost()
    {
        if (sleepingGhost != null)
        {
            Destroy(sleepingGhost.gameObject);
            sleepingGhost = null;
        }

        if (sprite != null) sprite.color = spriteTint;
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = Mathf.Clamp01(a);
        return c;
    }

    private void HandleWakeStageChanged(PlayerMovement.WakeStage stage)
    {
        if (stage == PlayerMovement.WakeStage.SittingUp)
        {
            if (waking != null) StopCoroutine(waking);
            waking = StartCoroutine(WakeUp());
            return;
        }

        // Standing, or anything else: whatever we were animating is over. A
        // child who says "Tayo" the instant he is upright gets to stand up, and
        // the sit-up lets go of the transform rather than fighting for it.
        CancelWakeUp();
    }

    private void CancelWakeUp()
    {
        if (waking == null) return;

        StopCoroutine(waking);
        waking = null;

        if (fading != null)
        {
            StopCoroutine(fading);
            fading = null;
        }
        ClearSleepingGhost();

        // Off an in-between and back onto a real pose. A sit-up cut short
        // would otherwise leave him on whichever drawn frame was showing -
        // halfway out of bed, for the rest of the scene.
        if (sprite != null && frames != null && movement != null &&
            movement.sittingUpSprite != null &&
            movement.currentWakeStage == PlayerMovement.WakeStage.SittingUp)
        {
            sprite.sprite = movement.sittingUpSprite;
        }

        // Rotation and scale only, deliberately - NOT position. Whatever stage
        // just took over has already decided where the character stands, and
        // "Tayo" in particular puts him back down to his bed height before
        // handing him to physics. Restoring the seated position over the top of
        // that would undo the decision of the thing that interrupted us. The
        // rotation and the squash are ours alone, and leaving either behind
        // would strand a standing boy tipped over at nine degrees.
        transform.localRotation = restRotation;
        transform.localScale = restScale;

        if (movement != null) movement.SetPosing(false);
    }

    /// <summary>
    /// Puts the transform back exactly where the sit-up has to end: the end of
    /// the rise and the end of the stretch, so neither ever lands on a rounding
    /// error. A sit-up cut short does NOT come through here - see CancelWakeUp.
    /// </summary>
    private void Rest()
    {
        transform.localRotation = restRotation;
        transform.localPosition = restPosition;
        transform.localScale = restScale;
    }

    private IEnumerator WakeUp()
    {
        // Rest is read here, at the moment the word lands, so the pose settles
        // back to wherever the scene actually put the character. Nothing in
        // this routine changes where he ends up - only how he gets there.
        restPosition = transform.localPosition;
        restScale = transform.localScale;
        restRotation = transform.localRotation;

        // The seat he pivots on. A figure rotated about his CENTRE swings his
        // feet through the mattress, which reads as a plank being tipped rather
        // than a child sitting up - so the turn happens where his weight
        // actually rests.
        //
        // Not the bottom edge of the art either, which is his feet: they hang
        // lower than his backside in the sitting pose, which is the whole
        // reason PlayerMovement sinks him by sittingPoseSeatDrop on "Bangon".
        // That number IS the gap between the two, so his seat is exactly that
        // far up from the bottom edge - and reading it from there rather than
        // keeping a second copy means the bed stays tuned in one place.
        float pivotDrop = HalfHeight() * Mathf.Abs(restScale.y) - SeatAboveFeet();
        Vector3 pivot = restPosition + new Vector3(0f, -pivotDrop, 0f);

        // The sequencer waits on this, so "Tayo" is not asked over the top of
        // the sit-up and the results of the last word are not shown mid-move.
        if (movement != null) movement.SetPosing(true);

        // First, and immediately: he is awake, so he is not snoring. Started
        // rather than yielded on - the fade runs alongside the yawn, which is
        // the point of it.
        StartCoroutine(StopSnoring());

        // Either way, this frame, before anything is drawn: "Bangon" has
        // already put the sitting picture on him at its final place, so
        // leaving it alone would hold on a boy sitting bolt upright in bed
        // through the yawn - the very cut all of this exists to remove.
        bool drawn = frames != null;
        bool dissolving = false;

        if (drawn)
        {
            // Frame 0 IS the sleeping pose, drawn on the frames' own canvas,
            // which is centred where he now sits. So he simply goes on looking
            // asleep, in the right place, with no copy and no dissolve needed
            // at this end at all.
            Rest();
            sprite.sprite = frames[0];
        }
        else
        {
            dissolving = BeginDissolve(sleepingSprite, sleepingPosition,
                                       sleepingRotation, sleepingScale);
            SetPose(pivot, riseDriftX, riseFromAngle, startSquash);
        }

        // The yawn leads the movement by a hair. A yawn is what wakes you up,
        // not something you do once you are already upright - so it sounds
        // while he is still lying there, and the picture does not change yet.
        SoundEffects.Play(YawnId, wakeVolume);
        if (yawnLead > 0f) yield return new WaitForSeconds(yawnLead);

        if (drawn)
        {
            yield return PlayDrawnRise();
        }
        else
        {
            yield return SwingUp(pivot, dissolving);
        }

        // Land exactly where the scene meant, never on a rounding error. This
        // is the sitting pose as the scene built it, untouched.
        Rest();

        // --- the stretch, a breath after the landing ---
        if (stretchDelay > 0f) yield return new WaitForSeconds(stretchDelay);

        SoundEffects.Play(StretchId, wakeVolume);

        yield return StretchBeat(pivot, pivotDrop);

        Rest();

        if (movement != null) movement.SetPosing(false);
        waking = null;
    }

    /// <summary>
    /// The drawn sit-up: his arms, back and knees change between pictures,
    /// which is the thing no transform can imitate. The last frame then
    /// dissolves into the real sitting pose, which covers its change of view -
    /// the frames are side-on, the sitting pose is three-quarter, and he puts
    /// his glasses on somewhere in there.
    ///
    /// Nothing here touches the transform. The frames are drawn on one canvas
    /// centred on the sitting pose's centre, so every one of them is correct
    /// at the transform he is already sitting at.
    /// </summary>
    private IEnumerator PlayDrawnRise()
    {
        int steps = frames.Length - 1;                       // frame 0 is already up
        float each = (riseSeconds * framesShare) / Mathf.Max(1, steps);

        for (int i = 1; i < frames.Length; i++)
        {
            // The first step is dissolved and the rest are cut, and the
            // difference is which drawings meet.
            //
            // Frames 1 upward are one set, drawn in one pass, so stepping
            // between them is animation and wants a clean cut - fading one
            // pose into the next would only blur the movement. But frame 0 is
            // the game's own sleeping art, and that is a different hand: he is
            // drawn smaller there, with a smaller head, than in either the
            // generated poses or the sitting sprite. The sequence cannot
            // reconcile that, and the import's own note says why it should not
            // try. So it is faded, exactly as the far end is.
            if (i == 1 &&
                BeginDissolve(frames[0], restPosition, restRotation, restScale))
            {
                sprite.sprite = frames[1];
                yield return FinishDissolve();
                continue;
            }

            sprite.sprite = frames[i];
            yield return new WaitForSeconds(each);
        }

        // Hand the last frame to a copy, put the real pose back on him, and
        // dissolve between the two over what is left of the sit-up.
        bool dissolving = BeginDissolve(frames[frames.Length - 1],
                                        restPosition, restRotation, restScale);
        if (movement != null && movement.sittingUpSprite != null)
            sprite.sprite = movement.sittingUpSprite;

        if (dissolving) yield return FinishDissolve();
        else if (sprite != null) sprite.color = spriteTint;
    }

    /// <summary>
    /// The sit-up done with a transform, for before the frames are drawn: the
    /// sitting pose swings up about his seat out of a dissolve from the
    /// sleeping one. PoseSettle's comment calls this shape of thing the cheap
    /// nine-tenths, and that is exactly what it is.
    /// </summary>
    private IEnumerator SwingUp(Vector3 pivot, bool dissolving)
    {
        // The dissolve has to happen DURING the movement. Held so a sit-up cut
        // short takes it down rather than leaving a second boy on the bed.
        if (dissolving) fading = StartCoroutine(FinishDissolve());

        for (float t = 0f; t < riseSeconds; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / riseSeconds);

            // The swing eases across the WHOLE duration, and the forward lean
            // takes over what is left of it rather than being added on top.
            //
            // Both of those are corrections. The swing used to finish at four
            // fifths and the lean used to be a separate term, so around that
            // point the two went slack together: the angle crept to within two
            // degrees a second and sat there before reversing, which is a hitch
            // and not an overshoot. Handing the lean the remainder keeps
            // something always moving, puts the turnaround inside about two
            // frames, and - because the lean owns the angle outright when it
            // peaks - makes riseOvershoot mean the degrees it says.
            float swing = Smoothstep(k);
            float lean = Mathf.Sin(Mathf.PI * k * k * k * k * k); // 1 at k=0.87, 0 at both ends

            float angle = riseFromAngle * (1f - swing) * (1f - lean) - riseOvershoot * lean;
            SetPose(pivot, riseDriftX * (1f - swing), angle, startSquash * (1f - swing));
            yield return null;
        }

    }

    /// <summary>
    /// The stretch on the end of the sit-up: he reaches up and over and lets
    /// go. Played whichever way he got upright, because it works on the real
    /// sitting pose either way.
    /// </summary>
    private IEnumerator StretchBeat(Vector3 pivot, float pivotDrop)
    {
        for (float t = 0f; t < stretchSeconds; t += Time.deltaTime)
        {
            // A single arc up and back down: reaching, holding for an instant,
            // letting go.
            float bell = Mathf.Sin(Mathf.Clamp01(t / stretchSeconds) * Mathf.PI);
            float reach = stretchReach * bell;

            transform.localScale = new Vector3(restScale.x * (1f - reach * 0.3f),
                                               restScale.y * (1f + reach),
                                               restScale.z);

            // Grown and leaned about the same seat the sit-up turned on. These
            // sprites are positioned by their CENTRE, so a figure that simply
            // got taller would push half the gain down through the mattress,
            // and one that simply rotated would swing his seat off the bed.
            Quaternion lean = Quaternion.Euler(0f, 0f, -stretchLean * bell);
            Vector3 lifted = restPosition + new Vector3(0f, pivotDrop * reach, 0f);

            transform.localRotation = lean * restRotation;
            transform.localPosition = pivot + lean * (lifted - pivot);
            yield return null;
        }
    }

    /// <summary>
    /// Rotates the figure about a seat below it, offset sideways, with a little
    /// squash - and leaves the transform somewhere Rest() can return from
    /// exactly, since every term here falls to zero together.
    ///
    /// The rotation is composed onto the pose's own, never assigned over it:
    /// Level 1's Kylo is turned 23 degrees about Y so he faces into the room,
    /// and overwriting that would snap him flat to the camera for the length of
    /// the sit-up.
    /// </summary>
    private void SetPose(Vector3 pivot, float driftX, float angle, float squash)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // The seat drifts with him, so the turn stays under his weight rather
        // than under the place his weight is going to end up.
        Vector3 from = restPosition + new Vector3(driftX, 0f, 0f);
        Vector3 seat = pivot + new Vector3(driftX, 0f, 0f);

        transform.localRotation = rotation * restRotation;
        transform.localPosition = seat + rotation * (from - seat);
        transform.localScale = new Vector3(restScale.x * (1f + squash * 0.5f),
                                           restScale.y * (1f - squash),
                                           restScale.z);
    }

    /// <summary>
    /// Eases in and out between 0 and 1 - slow to start, quickest in the
    /// middle, slow to arrive. A sit-up has to start gently: a child pushing
    /// himself up off a mattress has to get moving before he can move.
    /// </summary>
    private static float Smoothstep(float k)
    {
        k = Mathf.Clamp01(k);
        return k * k * (3f - 2f * k);
    }

    /// <summary>
    /// Half the height of the sprite currently showing, before scale. These
    /// sprites are positioned by their CENTRE - IdleBreath says the same - so
    /// this is the distance down from the transform to the bottom edge of the
    /// art, which in the sitting pose is the soles of his feet.
    /// </summary>
    private float HalfHeight()
    {
        if (sprite == null || sprite.sprite == null) return 0.5f;
        return sprite.sprite.bounds.size.y * 0.5f;
    }

    /// <summary>
    /// How far his backside sits above the bottom edge of the sitting art, in
    /// world units.
    ///
    /// This is PlayerMovement's sittingPoseSeatDrop, which is the same
    /// measurement seen from the other side: "Bangon" matches the bottom edges
    /// of the two poses and then sinks him by this much, precisely so that the
    /// thing resting on the mattress is his seat rather than his toes. Read
    /// rather than copied, so there is one number to tune and not two that can
    /// drift apart.
    /// </summary>
    private float SeatAboveFeet()
    {
        return movement != null ? movement.sittingPoseSeatDrop : 0f;
    }
}

/// <summary>
/// Puts a WakeUpSequence on whichever character starts a scene lying down.
///
/// The same shape as VoicePromptBootstrap, for the same reason: the chapter
/// scenes are generated, so a component added to a saved scene by hand is
/// destroyed the next time somebody runs its builder. Nothing created at run
/// time can be wiped.
///
/// Only Level 1 starts in bed - every other scene builder sets the stage to
/// Standing - so in practice this attaches once, in Chapter1_Level1_UmagaNa,
/// and quietly does nothing everywhere else. A second bedroom scene would get
/// it for free.
/// </summary>
public static class WakeUpSequenceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach();
    }

    private static void Attach()
    {
        // Runs after every Awake and before every Start, so the snore is in
        // place for the opening of the scene rather than a breath late.
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;
        if (player.currentWakeStage != PlayerMovement.WakeStage.LyingDown) return;
        if (player.GetComponent<WakeUpSequence>() != null) return;

        player.gameObject.AddComponent<WakeUpSequence>();
    }
}
