using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Walks the character INTO the picture rather than across it.
///
/// PlayerMovement only moves along X. It is driven by a Rigidbody2D with
/// gravity and a ground collider underneath, which is right for a room seen
/// side-on - the bedroom, the bathroom - where walking is sideways and the
/// floor is a line. The school gate is not that. It is a one-point perspective
/// looking THROUGH an arch at a building set well back, and "Pasok" means
/// going in, which on that painting is away from the camera.
///
/// There is no away from the camera in a 2D orthographic scene, so this does
/// what the painting itself does: moves him UP the frame and shrinks him at
/// the same time. That reads as distance because every other object in the art
/// is drawn by the same rule.
///
/// It takes the character over completely for the move, because it has to:
/// gravity would drag him straight back down the moment he left the floor
/// line, and PlayerMovement's own Update would fight the position every frame.
/// Both stay off afterwards - once he is standing up the path he is no longer
/// on the ground collider, and handing him back would drop him through the
/// bottom of the scene. From that point the walks are depth moves, which is
/// why the scene has one of these for "Pasok" and another for "Takbo".
/// </summary>
public class WalkIntoScene : MonoBehaviour
{
    [Tooltip("The character who walks in. His walkFrames are borrowed for the animation.")]
    public PlayerMovement player;

    [Tooltip("Where he ends up, in world units. Up the frame is further away.")]
    public Vector2 destination;

    [Tooltip("How much smaller he is on arrival, relative to his size when this starts. 0.6 means he ends up 60% as tall, which is what reading as 'further away' needs.")]
    public float endScaleMultiplier = 0.6f;

    [Tooltip("Seconds the move takes.")]
    public float duration = 1.6f;

    [Tooltip("Walk frames per second. Raise it for a run.")]
    public float framesPerSecond = 8f;

    [Tooltip("Back-view frames, cycled for the whole move instead of the profile walk cycle. This is how he faces AWAY: walking into the picture while drawn in profile reads as sliding sideways, because the side-on legs say he is going across. One frame holds still; several animate. Leave empty to use the profile frames as usual.")]
    public Sprite[] facingAwayFrames;

    [Tooltip("Runs once he arrives.")]
    // Initialised here, NOT left to Unity. A UnityEvent field on a component
    // created with AddComponent is null until something deserialises it, and
    // UnityEventTools.AddPersistentListener dereferences it without checking -
    // so a builder wiring this to a brand-new component throws a
    // NullReferenceException and takes the whole build down with it, leaving
    // an unsaved half-built scene behind. Invoking is null-safe here, which is
    // exactly why this stayed hidden until the first builder wired it.
    public UnityEvent onArrived = new UnityEvent();

    private bool moving;

    /// <summary>
    /// True while he is walking away. SceneObjectiveController waits on this
    /// before it moves the signpost on, the same way it waits on a
    /// PoseSequence: "Pasok" is him walking through the gate, so the next word
    /// should not appear while he is still in the gateway.
    /// </summary>
    public bool IsMoving { get { return moving; } }

    /// <summary>Wire this to the word's objective in SceneObjectiveController.</summary>
    public void Begin()
    {
        if (player == null || moving) return;
        StartCoroutine(Move());
    }

    private IEnumerator Move()
    {
        moving = true;

        // Take him off the physics floor. Freezing the body rather than
        // destroying it keeps the collider around for anything that asks.
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        player.enabled = false;

        // Stand the BREATHING down too, which is not optional.
        //
        // IdleBreath writes localPosition and localScale in LateUpdate -
        // after this coroutine has written them - so while it runs it simply
        // undoes this move, every frame. Its own guard does not catch this
        // move: it watches PlayerMovement.IsActionPlaying and the
        // Rigidbody2D's velocity, and neither says anything here, because the
        // body was made kinematic and stopped two lines above.
        //
        // Measured in Paaralan Scene 1 before this line existed: Kylo cycled
        // through the whole back-view walk and never moved or shrank, pinned
        // at the spot "Lakad" left him, so "Pasok" and "Takbo" read as
        // running on the spot OUTSIDE the gate rather than going through it.
        //
        // Switching it back on at the end re-anchors it - see its OnEnable -
        // so he breathes at the size and place he arrives at rather than
        // snapping back to where he started.
        IdleBreath breath = player.GetComponent<IdleBreath>();
        if (breath != null) breath.enabled = false;

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        Transform t = player.transform;

        Vector3 fromPos = t.position;
        Vector3 toPos = new Vector3(destination.x, destination.y, fromPos.z);
        Vector3 fromScale = t.localScale;
        Vector3 toScale = fromScale * endScaleMultiplier;

        // Which set of frames is animated for this move. The back-view set
        // wins when it is there, because a side-on stride carrying him up the
        // frame reads as sliding: the legs would say one direction and the
        // movement another.
        bool away = facingAwayFrames != null && facingAwayFrames.Length > 0;
        Sprite[] frames = away ? facingAwayFrames : player.walkFrames;
        if (away && sr != null && frames[0] != null) sr.sprite = frames[0];

        float elapsed = 0f;
        float frameTimer = 0f;
        int frame = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            // Ease out: he sets off at a walking pace and settles rather than
            // stopping dead, which at this size is the only cue that he has
            // arrived rather than simply got smaller.
            float eased = 1f - (1f - k) * (1f - k);

            t.position = Vector3.Lerp(fromPos, toPos, eased);
            t.localScale = Vector3.Lerp(fromScale, toScale, eased);

            if (sr != null && frames != null && frames.Length > 1)
            {
                frameTimer += Time.deltaTime;
                float step = 1f / Mathf.Max(0.01f, framesPerSecond);
                while (frameTimer >= step)
                {
                    frameTimer -= step;
                    frame = (frame + 1) % frames.Length;
                    if (frames[frame] != null) sr.sprite = frames[frame];
                }
            }
            yield return null;
        }

        t.position = toPos;
        t.localScale = toScale;
        // He stays facing away, on the first frame of the set - the one with
        // his feet together, so he settles rather than freezing mid-stride.
        // Turning back to the profile would spin him round on the spot the
        // moment he stopped, and he is on his way into the school.
        if (sr != null)
        {
            if (away) { if (frames[0] != null) sr.sprite = frames[0]; }
            else if (player.standingSprite != null) sr.sprite = player.standingSprite;
        }

        // Breathing resumes where he stopped, re-anchored by OnEnable.
        if (breath != null) breath.enabled = true;

        moving = false;
        onArrived?.Invoke();
    }
}
