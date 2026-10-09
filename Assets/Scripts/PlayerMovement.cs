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

    // Counts down alongside voiceWalkTimer while a run is in effect, so a run
    // is an ordinary walk that simply moves and animates faster.
    private float runTimer = 0f;

    /// <summary>True while "Takbo" is still running.</summary>
    private bool IsRunning => runTimer > 0f;

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
        float speed = moveSpeed * (IsRunning ? runMultiplier : 1f);
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
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
        float fps = walkFramesPerSecond * (IsRunning ? runMultiplier : 1f);
        float frameDuration = 1f / Mathf.Max(0.01f, fps);

        if (walkFrameTimer >= frameDuration)
        {
            walkFrameTimer -= frameDuration;
            currentWalkFrame++;
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
