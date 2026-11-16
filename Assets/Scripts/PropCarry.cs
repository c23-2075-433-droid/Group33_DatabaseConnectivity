using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Keeps a prop in a character's hand while the drawing moves it, and then
/// puts the prop inside whatever it was being carried to.
///
/// WHY THIS EXISTS, when PropToss already moves props about.
///
/// PropToss throws. It takes two points and an arc height and sends an object
/// between them on a curve of its own, and that is right for a thing that has
/// LEFT the hand - a wrapper on its way to the bin is not being held by
/// anybody and should not pretend to be. This is the opposite case: an object
/// that is still in his hand the whole time it is moving, where the only job
/// is to be WHERE THE DRAWN HAND IS on every frame.
///
/// Paaralan Scene 4's "Ligpit" is the beat it was written for. He picks his
/// pencil up off the desk and puts it in his bag, and the project has drawings
/// of a pencil in his fist on the desk and of an empty hand going into the
/// bag, and nothing in between. The pencil has to cross that gap, and the one
/// thing it must not do is cross it on an arc of its own: the hand gets to the
/// bag on the pose's clock and an arc gets there on its own, and when the two
/// disagree the pencil arrives after the hand has already come back out. That
/// is exactly what the first version of that beat did.
///
/// HOW THE TWO CLOCKS ARE MADE ONE. The carry does not have a duration. It has
/// a WAYPOINT PER DRAWN FRAME and the pose's own `framesPerSecond`, and it
/// spends exactly one frame's worth of time on each leg. So waypoint 0 is
/// where the prop is as the first frame is shown, waypoint 1 as the second is
/// shown, and so on, and the two cannot drift apart however the scene's Pace
/// is changed. Wire Play() to the PoseSequence's fireOnFrame for the frame
/// waypoint 0 belongs to and they start together as well.
///
/// STRAIGHT LEGS AND NO SPIN, which is the other half of not looking thrown.
/// Between two waypoints it goes in a straight line at a steady rate, because
/// that is what a hand carrying something does; the only curve in the path is
/// the one the waypoints themselves describe. It turns from `startAngle` to
/// `endAngle` across the whole carry, which is a wrist turning over, not an
/// object tumbling.
///
/// AND THEN IT IS LET GO, which is the part that has to read as "in" and not
/// as "gone". The prop sinks `insertDrop`, shrinks to `insertScale` and fades
/// out, all at once and all after the carry has arrived. A thing dropped into
/// a bag goes down, away from the camera, and into shadow, and that is the
/// cheapest honest version of all three. Only then is it switched off.
/// Switching it off on arrival instead is the teleport this component, like
/// PropToss, exists to avoid.
/// </summary>
public class PropCarry : MonoBehaviour
{
    [Tooltip("The object that is carried. Switched ON as the carry starts " +
             "and OFF once it is inside, so it needs no state of its own and " +
             "can start the scene switched off.\n\nLeave empty and this " +
             "object's own transform is used.")]
    public Transform prop;

    [Tooltip("The carried object's renderer, for the fade as it goes in. " +
             "Leave empty and it sinks and shrinks without fading, which " +
             "still reads, just less well.")]
    public SpriteRenderer propRenderer;

    [Tooltip("Where the prop is on each frame of the run it rides, in world " +
             "space. ONE PER DRAWN FRAME, starting with the frame Play() is " +
             "wired to.\n\nThe first one should be exactly where the DRAWN " +
             "object was on the frame before - that hand-over is what stops " +
             "the prop appearing somewhere the drawing had not left it.")]
    public Vector3[] waypoints;

    [Tooltip("The frame rate of the PoseSequence this rides. Each leg takes " +
             "one frame at this rate, which is what keeps the prop on the " +
             "hand.\n\nSet it from the same expression the pose is given, " +
             "not by hand, or a change to the scene's Pace moves one and not " +
             "the other.")]
    public float framesPerSecond = 11f;

    [Tooltip("The prop's rotation as the carry starts, in degrees. Match it " +
             "to the angle the object is DRAWN at in the frame it is taken " +
             "over from.")]
    public float startAngle;

    [Tooltip("Its rotation as it arrives, in degrees. The difference between " +
             "this and startAngle is a wrist turning over the length of the " +
             "carry; keep it small.")]
    public float endAngle;

    [Tooltip("How far it sinks into the opening once the carry has arrived, " +
             "in world units. This is the part that reads as 'in' rather " +
             "than 'at'.")]
    public float insertDrop = 0.30f;

    [Tooltip("What it shrinks to as it goes in, as a fraction of its size.")]
    [Range(0.1f, 1f)]
    public float insertScale = 0.65f;

    [Tooltip("How long the going-in takes. Scenes set this as a number of " +
             "frames over framesPerSecond, so it keeps step with the pose " +
             "like everything else here.")]
    public float insertDuration = 0.26f;

    [Tooltip("Runs once it is out of sight, for a sound or for whatever the " +
             "scene does next.")]
    public UnityEvent onLanded = new UnityEvent();

    private Vector3 restScale;
    private Color restColor = Color.white;
    private bool measured;

    private Transform Target { get { return prop != null ? prop : transform; } }

    /// <summary>
    /// Pick it up and carry it. Wire this to the fireOnFrame of the frame
    /// waypoints[0] was measured in.
    /// </summary>
    public void Play()
    {
        Transform t = Target;
        if (t == null) return;

        if (waypoints == null || waypoints.Length == 0)
        {
            // Nothing to carry it along. Leave it off rather than leaving it
            // on screen in the wrong place, and let the scene carry on.
            t.gameObject.SetActive(false);
            onLanded?.Invoke();
            return;
        }

        // Read the resting scale and colour ONCE, and on the first carry
        // rather than in Awake: the builder sets the prop's scale from the
        // sprite's own bounds, and a second Play() that measured again would
        // measure the shrunken, faded values this component had just left
        // behind - so the prop would get smaller and fainter every time the
        // scene was replayed. PropToss learned this the same way.
        if (!measured)
        {
            restScale = t.localScale;
            if (propRenderer != null) restColor = propRenderer.color;
            measured = true;
        }

        StopAllCoroutines();
        if (!isActiveAndEnabled)
        {
            // Nothing can run. Put it away and leave the scene consistent.
            t.gameObject.SetActive(false);
            onLanded?.Invoke();
            return;
        }
        StartCoroutine(Carry());
    }

    private System.Collections.IEnumerator Carry()
    {
        Transform t = Target;
        t.localScale = restScale;
        if (propRenderer != null) propRenderer.color = restColor;
        t.position = waypoints[0];
        t.rotation = Quaternion.Euler(0f, 0f, startAngle);
        t.gameObject.SetActive(true);

        // --- along the hand's path, a frame to a leg ---
        int legs = waypoints.Length - 1;
        float hold = framesPerSecond > 0f ? 1f / framesPerSecond : 0.1f;
        float total = hold * legs;
        if (legs > 0)
        {
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / total);
                // Which leg, and how far along it. Straight and steady: the
                // waypoints are the shape of the movement and this adds
                // nothing to them.
                float along = u * legs;
                int leg = Mathf.Min((int)along, legs - 1);
                t.position = Vector3.Lerp(waypoints[leg], waypoints[leg + 1],
                                           along - leg);
                t.rotation = Quaternion.Euler(0f, 0f,
                                               Mathf.LerpAngle(startAngle, endAngle, u));
                yield return null;
            }
        }
        t.position = waypoints[legs];
        t.rotation = Quaternion.Euler(0f, 0f, endAngle);

        // --- and down inside ---
        Vector3 mouth = waypoints[legs];
        Vector3 sunk = mouth + Vector3.down * insertDrop;
        float insert = Mathf.Max(0.0001f, insertDuration);
        float gone = 0f;
        while (gone < insertDuration)
        {
            gone += Time.deltaTime;
            float k = Mathf.Clamp01(gone / insert);
            t.position = Vector3.Lerp(mouth, sunk, k);
            t.localScale = Vector3.Lerp(restScale, restScale * insertScale, k);
            if (propRenderer != null)
            {
                // Held at full strength for the first part of the drop and
                // faded over the rest. Fading from the first frame would
                // start it disappearing while it is still plainly above the
                // opening, which reads as the object evaporating rather than
                // as the bag closing over it.
                Color c = restColor;
                c.a = restColor.a * (1f - Mathf.InverseLerp(0.4f, 1f, k));
                propRenderer.color = c;
            }
            yield return null;
        }

        t.gameObject.SetActive(false);
        // Back to the prop the builder made, so the next run of the scene
        // does not start from a small faint one lying inside the bag.
        t.localScale = restScale;
        t.rotation = Quaternion.Euler(0f, 0f, startAngle);
        t.position = waypoints[0];
        if (propRenderer != null) propRenderer.color = restColor;
        onLanded?.Invoke();
    }

    /// <summary>
    /// Back to the start if the scene is switched off mid-carry, so a replay
    /// does not begin with a half-faded pencil hanging over the desk.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        Transform t = Target;
        if (t == null || !measured) return;
        t.localScale = restScale;
        t.rotation = Quaternion.Euler(0f, 0f, startAngle);
        if (propRenderer != null) propRenderer.color = restColor;
    }
}
