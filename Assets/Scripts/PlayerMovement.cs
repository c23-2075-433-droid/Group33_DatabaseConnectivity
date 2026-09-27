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

            // The lyingDownSprite art has the head on the right side, but the
            // bed's pillow is positioned at the left (headboard) end. Mirror
            // it so the head actually rests on the pillow. Bangon()/TayoUp()
            // reset flipX back to false once the character sits/stands up.
            spriteRenderer.flipX = true;
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
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
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
    public void WalkForward() => WalkInDirection(1f);

    /// <summary>"Kaliwa" - walk left.</summary>
    public void WalkLeft() => WalkInDirection(-1f);

    /// <summary>"Kanan" - walk right.</summary>
    public void WalkRight() => WalkInDirection(1f);

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
        Debug.Log("PlayerMovement: Tayo triggered - now standing, movement unlocked.");
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
        float frameDuration = 1f / Mathf.Max(0.01f, walkFramesPerSecond);

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
