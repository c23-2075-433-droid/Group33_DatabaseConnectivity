using UnityEngine;

/// <summary>
/// Gentle breathing for a character who is standing still.
///
/// A scene opens on a child waiting to be spoken to, and until a word is said
/// nothing on screen moves at all - which makes a carefully drawn boy read as a
/// sticker pasted on the background. Every frame of held stillness is also a
/// frame in which a player wonders whether the game has frozen.
///
/// This fixes that without a single new drawing. It does not touch the sprite:
/// it breathes the TRANSFORM, squashing and stretching the figure by about one
/// percent on a slow sine, with a matching nudge upward so his feet stay put
/// rather than sinking into the floor as he swells.
///
/// What it deliberately does NOT do:
///
///   * It never runs while he is walking. The walk cycle is real animation and
///     a breath laid over it would just make him wobble, so this stands down
///     the moment PlayerMovement reports movement and restores the exact scale
///     it started from.
///   * It never writes to position, only localPosition's Y offset from the
///     place it found him. A character moved by a Rigidbody2D is the physics
///     engine's to position, and two things writing a transform is how objects
///     start to jitter.
///
/// Attach it to the same object as the SpriteRenderer. With no PlayerMovement
/// alongside it breathes unconditionally, which is what a background character
/// wants.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class IdleBreath : MonoBehaviour
{
    [Tooltip("How much the figure swells, as a fraction of its height. One " +
             "percent is about right - enough to see, far too little to read " +
             "as a bounce.")]
    [Range(0f, 0.06f)]
    public float depth = 0.012f;

    [Tooltip("Breaths per second. About one breath every three seconds, which " +
             "is a calm child waiting rather than one out of breath.")]
    public float cyclesPerSecond = 0.32f;

    [Tooltip("Leave empty to find one on this object. With none anywhere, the " +
             "breathing simply never pauses.")]
    public PlayerMovement player;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private float phase;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerMovement>();
        baseScale = transform.localScale;
        basePosition = transform.localPosition;

        // Start part-way in, so several characters in one scene are not all
        // inhaling on the same beat.
        phase = Random.value * Mathf.PI * 2f;
    }

    private void LateUpdate()
    {
        // Re-read the resting position every frame he is NOT breathing, so
        // that whatever moved him - a walk, a cutscene - becomes the new place
        // he breathes around when he stops.
        if (IsMoving())
        {
            basePosition = transform.localPosition;
            transform.localScale = baseScale;
            return;
        }

        phase += Time.deltaTime * cyclesPerSecond * Mathf.PI * 2f;
        float swell = Mathf.Sin(phase) * depth;

        // Taller and very slightly narrower, the way a chest actually works -
        // a figure that grows in both directions reads as zooming, not
        // breathing.
        transform.localScale = new Vector3(baseScale.x * (1f - swell * 0.35f),
                                           baseScale.y * (1f + swell),
                                           baseScale.z);

        // CreateFigure positions these sprites by their CENTRE, so growing
        // taller would push his feet down through the floor by half the gain.
        // Lift him by that half to pin the feet.
        float height = baseScale.y * GetHeight();
        transform.localPosition = basePosition + new Vector3(0f, height * swell * 0.5f, 0f);
    }

    private bool IsMoving()
    {
        if (player == null) return false;
        if (player.IsVoiceWalking) return true;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        return rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.01f;
    }

    private float GetHeight()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null ? sr.sprite.bounds.size.y : 0f;
    }
}
