using UnityEngine;

/// <summary>
/// Simple 2D platformer movement for the SALINLAHI player character.
/// Move with A/D or Left/Right arrow keys. Jump with Space or Up/W.
/// Requires a Rigidbody2D and a Collider2D on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units per second.")]
    public float moveSpeed = 5f;

    [Header("Jumping")]
    [Tooltip("Upward velocity applied when jumping.")]
    public float jumpForce = 9f;

    [Tooltip("Layers considered 'ground' for jump/landing checks.")]
    public LayerMask groundLayer = ~0; // defaults to Everything

    [Tooltip("How far below the player's collider to check for ground.")]
    public float groundCheckDistance = 0.15f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private float moveInput;
    private SpriteRenderer spriteRenderer;

    // Set when a walk objective is answered correctly (Lakad / Kaliwa / Kanan).
    // While the timer is > 0, movement is forced in voiceWalkDirection instead of
    // reading the keyboard, so voice control works with no keyboard attached
    // (e.g. on a phone).
    private float voiceWalkTimer = 0f;
    private float voiceWalkDirection = 1f; // +1 = right (Kanan), -1 = left (Kaliwa)

    [Header("Wake-Up Poses (Level 1 - Umaga na!)")]
    [Tooltip("Sprite shown while the character is lying down / sleeping.")]
    public Sprite lyingDownSprite;

    [Tooltip("Sprite shown while the character is sitting up (Bangon).")]
    public Sprite sittingUpSprite;

    [Tooltip("Sprite shown while the character is standing (Tayo) / default idle.")]
    public Sprite standingSprite;

    [Header("Walk Cycle")]
    [Tooltip("Walk-cycle frames, in order, played on a loop while the character is moving. " +
             "Leave empty to just hold on standingSprite while walking (no animation).")]
    public Sprite[] walkFrames;

    [Tooltip("How many walk frames to show per second.")]
    public float walkFramesPerSecond = 8f;

    [Tooltip("How much faster Takbo (run) is than Lakad (walk), in both speed " +
             "and how quickly the walk frames cycle.")]
    public float runMultiplier = 1.9f;

    [Tooltip("How long one spoken walk word (Lakad / Kaliwa / Kanan) keeps the " +
             "character moving, in seconds. With no walkTarget set this is the " +
             "whole walk, covering this many seconds times moveSpeed. With a " +
             "walkTarget set the walk ends on arrival instead, and this is only " +
             "a safety cap. Set per scene in the Inspector.")]
    public float voiceWalkDuration = 1.2f;

    [Header("Walk Target (optional)")]
    [Tooltip("If set, a spoken walk word heads for this object and stops on " +
             "arrival rather than after a fixed time. Level 1 points this at the " +
             "ExitTrigger, so Lakad always reaches the door however far away it " +
             "is - the same walk-until-you-arrive rule RoomTransition uses for " +
             "the hallway door. Leave empty in scenes where a walk is just a walk.")]
    public Transform walkTarget;

    [Tooltip("How close to walkTarget counts as having arrived, in world units.")]
    public float walkTargetThreshold = 0.35f;

    [Tooltip("How far before walkTarget the character starts slowing down, in " +
             "world units. A spoken walk otherwise runs at full speed until the " +
             "instant it stops, which reads as a character being switched off " +
             "rather than one arriving somewhere. Over this last stretch both " +
             "the speed and the walk-cycle rate fall off together, so his legs " +
             "slow with him instead of scurrying on the spot.\n\n" +
             "Leave at 0 to keep the plain hard stop - that is what every scene " +
             "built before this field existed expects, so the default changes " +
             "nothing anywhere until a builder opts in.")]
    public float arrivalEaseDistance = 0f;

    [Tooltip("The slowest the character may creep while easing in, as a fraction " +
             "of normal speed. Without a floor the approach would halve forever " +
             "and never quite arrive.")]
    [Range(0.05f, 1f)]
    public float arrivalEaseFloor = 0.2f;

    // Counts down alongside voiceWalkTimer while a run is in effect, so a run
    // is an ordinary walk that simply moves and animates faster.
    private float runTimer = 0f;

    /// <summary>True while "Takbo" is still running.</summary>
    private bool IsRunning => runTimer > 0f;

    /// <summary>
    /// True while a spoken walk or run is still playing out. Read by
    /// SceneObjectiveController, which waits for the action a word started to
    /// finish before asking the next word or completing the scene.
    /// </summary>
    public bool IsVoiceWalking => voiceWalkTimer > 0f;

    /// <summary>
    /// True while a pose is being animated into place rather than simply
    /// switched - the sit-up WakeUpSequence plays for "Bangon". Set by whoever
    /// is doing the animating, through SetPosing.
    /// </summary>
    public bool IsPosing { get; private set; }

    /// <summary>
    /// True while ANY action a spoken word started is still playing out,
    /// whether that is movement or a pose being animated.
    ///
    /// This is the one SceneObjectiveController waits on. The distinction
    /// matters because the two kinds of action look nothing alike to this
    /// script - a walk is a timer this file owns, a sit-up is a coroutine in
    /// another component - and the sequencer should not have to know which
    /// kind the word it just heard started.
    /// </summary>
    public bool IsActionPlaying => IsVoiceWalking || IsPosing;

    /// <summary>
    /// Held true by WakeUpSequence for the length of the sit-up, so the next
    /// word is not asked over the top of it. Anything that animates a pose on
    /// this character should do the same.
    /// </summary>
    public void SetPosing(bool posing) => IsPosing = posing;

    /// <summary>
    /// Raised after the wake stage changes, with the stage just entered.
    ///
    /// Bangon() and TayoUp() are already the single place each of those words
    /// lands - they are what the objective's onCorrect event calls, and they
    /// return early unless the stage before was the right one. So this fires
    /// exactly once per real transition and never for a wrong answer, which is
    /// what WakeUpSequence needs in order to stop the snoring and start the
    /// sit-up without a second copy of the "was that correct?" question.
    /// </summary>
    public event System.Action<WakeStage> OnWakeStageChanged;

    private int currentWalkFrame = 0;
    private float walkFrameTimer = 0f;

    // Tracks which wake-up stage the player is in, so movement/jump input can be
    // ignored until the player has actually stood up (can't walk while lying down).
    public enum WakeStage { LyingDown, SittingUp, Standing }
    public WakeStage currentWakeStage = WakeStage.LyingDown;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Level 1 opens with the character lying down asleep.
        if (spriteRenderer != null && lyingDownSprite != null)
        {
            spriteRenderer.sprite = lyingDownSprite;

            // The current lyingDownSprite art already has the head on the LEFT,
            // which is the pillow/headboard end of the bed, so no mirroring is
            // needed. (The older art faced the other way and was flipped here -
            // if the sleeping art is ever replaced with a head-right version,
            // set this back to true.)
            spriteRenderer.flipX = false;
        }

        // While the character is still in bed, gravity would drag him off the
        // mattress down to the floor collider the moment Play starts (the bed
        // is painted into the background and has no collider of its own). Hold
        // him still until he actually stands up - TayoUp() turns physics back on.
        if (rb != null && currentWakeStage != WakeStage.Standing)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        // Before standing up, ignore keyboard/jump input entirely — the only
        // valid actions are the Bangon() and TayoUp() voice/UI triggers.
        if (currentWakeStage != WakeStage.Standing)
        {
            moveInput = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (voiceWalkTimer > 0f)
        {
            // A voice walk command is in effect: keep moving in the requested
            // direction and count down, ignoring the keyboard until it finishes.
            moveInput = voiceWalkDirection;
            voiceWalkTimer -= Time.deltaTime;
            if (runTimer > 0f) runTimer -= Time.deltaTime;

            // With a walkTarget set, arriving ends the walk instead of the
            // timer running out. A fixed duration only reaches the target when
            // the distance happens to match, which is what left Kylo standing
            // short of the Level 1 door.
            if (walkTarget != null)
            {
                float dx = walkTarget.position.x - transform.position.x;
                bool headingToward = (dx > 0f) == (voiceWalkDirection > 0f);
                if (headingToward && Mathf.Abs(dx) <= walkTargetThreshold) StopVoiceWalk();
            }
        }
        else
        {
            // Read input every frame for responsiveness.
            moveInput = Input.GetAxisRaw("Horizontal");

            // Fallback direct key checks in case Input axes aren't configured.
            if (moveInput == 0f)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;
                else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
            }
        }

        // Flip sprite to face movement direction (player art faces right by default).
        if (spriteRenderer != null && moveInput != 0f)
        {
            spriteRenderer.flipX = moveInput < 0f;
        }

        UpdateWalkAnimation(moveInput != 0f);

        CheckGrounded();

        bool jumpPressed = Input.GetKeyDown(KeyCode.Space)
                            || Input.GetKeyDown(KeyCode.UpArrow)
                            || Input.GetKeyDown(KeyCode.W);

        if (jumpPressed)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        float speed = moveSpeed * (IsRunning ? runMultiplier : 1f) * ArrivalEase();
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
    }

    /// <summary>
    /// How much of full speed to use right now: 1 everywhere except the last
    /// <see cref="arrivalEaseDistance"/> units before a walkTarget, where it
    /// falls off to <see cref="arrivalEaseFloor"/> so the character settles
    /// into place instead of stopping dead.
    ///
    /// Returns 1 unchanged when the feature is off, when there is no target,
    /// when no spoken walk is in progress, or when the character is walking
    /// away from the target rather than toward it - so it can only ever slow
    /// an approach, never interfere with ordinary movement.
    /// </summary>
    private float ArrivalEase()
    {
        if (arrivalEaseDistance <= 0f || walkTarget == null || voiceWalkTimer <= 0f) return 1f;

        float dx = walkTarget.position.x - transform.position.x;
        if ((dx > 0f) != (voiceWalkDirection > 0f)) return 1f;   // not heading for it

        // Measured to the point he actually stops at, not to the target's
        // centre, so the slow-down finishes exactly as the walk ends.
        float remaining = Mathf.Abs(dx) - walkTargetThreshold;
        if (remaining >= arrivalEaseDistance) return 1f;

        float k = Mathf.Clamp01(remaining / arrivalEaseDistance);
        return Mathf.Lerp(arrivalEaseFloor, 1f, k);
    }

    /// <summary>
    /// Makes the player jump, if currently grounded. Public so other systems
    /// (e.g. VoiceCommand) can trigger the SAME jump logic as the Space key,
    /// instead of a second/duplicate jump implementation.
    /// </summary>
    public void Jump()
    {
        if (!isGrounded) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    /// <summary>
    /// Makes the player walk forward (right) for the given duration, if currently
    /// not already mid-walk-command. Public so other systems (e.g. VoiceCommand)
    /// can trigger movement from voice instead of the keyboard.
    /// Only takes effect once the player has stood up (see currentWakeStage).
    /// </summary>
    public void Walk(float duration = 1.2f) => WalkInDirection(1f, duration);

    /// <summary>
    /// Walks left or right for the given duration. Pass -1 for left (Kaliwa)
    /// or +1 for right (Kanan / Lakad forward). Only takes effect once the
    /// player has stood up (see currentWakeStage).
    /// </summary>
    public void WalkInDirection(float direction, float duration = 1.2f)
    {
        if (currentWakeStage != WakeStage.Standing) return;

        voiceWalkDirection = direction < 0f ? -1f : 1f;
        voiceWalkTimer = duration;

        // Face the walk direction immediately (don't wait for the next
        // Update()), so a stale flip state can't make the character appear
        // to walk backward for a frame.
        if (spriteRenderer != null) spriteRenderer.flipX = voiceWalkDirection < 0f;
    }

    // Parameterless wrappers so an objective's onCorrect UnityEvent (see
    // SceneObjectiveController) can target them directly - Unity's persistent
    // UnityEvent listeners can only call methods with no parameters.
    /// <summary>"Lakad" - walk forward (right).</summary>
    public void WalkForward() => WalkInDirection(1f, voiceWalkDuration);

    /// <summary>"Kaliwa" - walk left.</summary>
    public void WalkLeft() => WalkInDirection(-1f, voiceWalkDuration);

    /// <summary>"Kanan" - walk right.</summary>
    public void WalkRight() => WalkInDirection(1f, voiceWalkDuration);

    /// <summary>
    /// Ends a voice walk early. RoomTransition calls this the moment the
    /// player reaches the doorway, so Kylo stops at the door instead of
    /// drifting past it for the rest of the duration.
    /// </summary>
    public void StopVoiceWalk()
    {
        voiceWalkTimer = 0f;
        runTimer = 0f;
        moveInput = 0f;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// "Takbo" - run forward. The same walk, moving and animating faster for
    /// its duration, so it needs no separate run art.
    /// </summary>
    public void Takbo() => RunInDirection(1f, 1.4f);

    /// <summary>Runs in a direction for a time. See Takbo.</summary>
    public void RunInDirection(float direction, float duration = 1.4f)
    {
        if (currentWakeStage != WakeStage.Standing) return;
        WalkInDirection(direction, duration);
        runTimer = duration;
    }

    /// <summary>
    /// "Bangon" (get up / sit up). Called when the player says "Bangon" while
    /// lying down. Swaps to the sitting-up sprite and advances the wake stage.
    /// Public so VoiceCommand (or a tap/UI fallback) can trigger it, the same
    /// way Jump() and Walk() are triggered.
    /// </summary>
    public void Bangon()
    {
        if (currentWakeStage != WakeStage.LyingDown) return; // already past this stage

        currentWakeStage = WakeStage.SittingUp;
        if (spriteRenderer != null)
        {
            if (sittingUpSprite != null) spriteRenderer.sprite = sittingUpSprite;
            spriteRenderer.flipX = false; // undo the lying-down mirror
        }
        Debug.Log("PlayerMovement: Bangon triggered - now sitting up.");

        // Last, so anything listening sees a character already on the sitting
        // pose. WakeUpSequence animates that pose up off the mattress from
        // here, and it cannot animate a sprite that has not been set yet.
        OnWakeStageChanged?.Invoke(currentWakeStage);
    }

    /// <summary>
    /// "Tayo" (stand up). Called when the player says "Tayo" while sitting up.
    /// Swaps to the standing sprite and unlocks movement/Lakad.
    /// </summary>
    public void TayoUp()
    {
        if (currentWakeStage != WakeStage.SittingUp) return; // must Bangon first

        currentWakeStage = WakeStage.Standing;
        if (spriteRenderer != null)
        {
            if (standingSprite != null) spriteRenderer.sprite = standingSprite;
            spriteRenderer.flipX = false; // face right by default once standing
        }

        // He's out of bed now, so hand him back to physics: gravity settles him
        // onto the floor and walking/jumping work from here on.
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        Debug.Log("PlayerMovement: Tayo triggered - now standing, movement unlocked.");

        OnWakeStageChanged?.Invoke(currentWakeStage);
    }

    /// <summary>
    /// Swaps the standing and walking art for a different set, and redraws the
    /// current pose straight away so the change is visible without waiting for
    /// the next step.
    ///
    /// This is how Kylo starts carrying his school bag: the character art is
    /// drawn with and without it, and saying "Kunin" swaps one set for the
    /// other (see PickUpItem). Passing null for either argument leaves that
    /// part of the art alone.
    /// </summary>
    public void SetAppearance(Sprite newStanding, Sprite[] newWalkFrames)
    {
        if (newStanding != null) standingSprite = newStanding;
        if (newWalkFrames != null && newWalkFrames.Length > 0) walkFrames = newWalkFrames;

        currentWalkFrame = 0;
        walkFrameTimer = 0f;

        // Only repaint when the player is actually on the standing pose;
        // mid-walk the animation picks the new frames up on its own, and
        // while lying down or sitting up those sprites still apply.
        if (spriteRenderer != null && currentWakeStage == WakeStage.Standing && standingSprite != null)
        {
            spriteRenderer.sprite = standingSprite;
        }
    }

    /// <summary>
    /// Cycles through walkFrames while isMoving is true, otherwise resets to the
    /// standing sprite. Called every Update() once the player has stood up.
    /// Safe to call with an empty/unassigned walkFrames array — it just no-ops
    /// and holds on standingSprite (or whatever sprite is currently set).
    /// </summary>
    private void UpdateWalkAnimation(bool isMoving)
    {
        if (spriteRenderer == null) return;

        if (!isMoving)
        {
            currentWalkFrame = 0;
            walkFrameTimer = 0f;
            if (standingSprite != null)
            {
                spriteRenderer.sprite = standingSprite;
            }
            return;
        }

        bool hasFullWalkCycle = walkFrames != null && walkFrames.Length > 1;
        bool hasSingleWalkFrame = walkFrames != null && walkFrames.Length == 1;

        if (!hasFullWalkCycle && !hasSingleWalkFrame)
        {
            // No walk art at all — just hold on the standing pose.
            if (standingSprite != null) spriteRenderer.sprite = standingSprite;
            return;
        }

        walkFrameTimer += Time.deltaTime;
        // The same easing the speed uses, so a slowing walk takes shorter,
        // slower strides rather than running on the spot as it creeps in.
        float fps = walkFramesPerSecond * (IsRunning ? runMultiplier : 1f) * ArrivalEase();
        float frameDuration = 1f / Mathf.Max(0.01f, fps);

        if (walkFrameTimer >= frameDuration)
        {
            walkFrameTimer -= frameDuration;
            currentWalkFrame++;

            // A footfall every OTHER frame. The art is a four-frame cycle with
            // two strides in it, so stepping on every frame gives a child who
            // walks for 1.2 seconds about ten footsteps instead of five, and
            // it reads as scurrying rather than walking.
            if (currentWalkFrame % 2 == 0) SoundEffects.Footstep();
        }

        if (hasFullWalkCycle)
        {
            currentWalkFrame %= walkFrames.Length;
            spriteRenderer.sprite = walkFrames[currentWalkFrame];
        }
        else
        {
            // Only one walk-cycle frame is available (a known asset gap — see
            // walkFrames tooltip). Alternate it with the standing pose so the
            // character visibly "steps" instead of sliding across the floor
            // holding one static pose. Swap out for a real 2-4 frame walk
            // cycle here once that art exists — no other code changes needed.
            bool showWalkFrame = (currentWalkFrame % 2) == 1;
            spriteRenderer.sprite = showWalkFrame ? walkFrames[0] : standingSprite;
        }
    }

    private void CheckGrounded()
    {
        if (col == null)
        {
            isGrounded = false;
            return;
        }

        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }
}
