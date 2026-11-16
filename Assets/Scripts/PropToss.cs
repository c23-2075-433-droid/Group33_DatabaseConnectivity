using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Carries a prop through the air from one place to another, and puts it away
/// once it has arrived.
///
/// WHY THIS EXISTS, when PoseSequence already animates everything else.
///
/// PoseSequence swaps SPRITES. That is the right shape for a character, whose
/// every position is a separate drawing, and it is the wrong shape for an
/// object in flight: a thrown wrapper is one drawing in a hundred places, and
/// drawing it a hundred times to animate it by sprite swap would be absurd.
/// What it needs is a TRANSFORM moved along a path, which is what this is.
///
/// Paaralan Scene 4's "Tapon" is the beat it was written for. The project's
/// animation rules say an object must not simply vanish when a word is
/// recognised - rule 5 and rule 6 between them - and "throw the rubbish away"
/// is the one action in the game where the whole meaning of the word IS the
/// object's journey. A child watching has to see the wrapper leave his hand,
/// cross the gap and land in the bin, or the scene has taught them that
/// "Tapon" means "rubbish disappears".
///
/// HOW THE ARC IS MADE. A straight line from hand to bin with a sine lifted
/// over it. Not real physics: gravity integrated from a throw strength is
/// three more numbers to tune and lands wherever it lands, and this has to
/// land INSIDE a bin that is 1.4 units wide every single time. A curve that
/// is told both of its ends cannot miss.
///
/// WHAT HAPPENS AT THE END, which is the part worth not simplifying. The arc
/// finishes at the mouth of the bin, and then there is a second, shorter
/// movement: the prop sinks `settleDrop` further and shrinks to
/// `settleScale`, which is a thing falling away from the camera into a hole.
/// Only after that does it switch off. Landing and vanishing in the same
/// frame is the teleport the whole component exists to avoid - it would read
/// as the wrapper bouncing off the rim.
///
/// NOT FOR AN OBJECT THAT IS STILL IN A HAND. This was briefly used for the
/// pencil going into the bag in Paaralan 4's "Ligpit", and that is the one
/// thing it is wrong for: a toss has a duration of its own, so it arrives
/// when its own clock says to rather than when the drawn hand gets there, and
/// the pencil reached the bag after the hand had been in and come back out.
/// PropCarry is the component for that case - a waypoint per drawn frame and
/// no duration of its own. Use this one only once the object has been let go.
/// </summary>
public class PropToss : MonoBehaviour
{
    [Tooltip("The object that flies. Switched ON at the start of the throw " +
             "and OFF once it has landed and settled, so it needs no state " +
             "of its own and can start the scene switched off.\n\nLeave " +
             "empty and this object's own transform is used.")]
    public Transform prop;

    [Tooltip("Where it leaves his hand, in world space. This should be the " +
             "same place the DRAWN object is in the pose frame that fires " +
             "the throw - the hand-over between the drawing and this is what " +
             "stops the object jumping as it is released.")]
    public Vector3 from;

    [Tooltip("Where it arrives, in world space: the middle of the opening " +
             "it is going into, not the top of the rim.")]
    public Vector3 to;

    [Tooltip("How high the arc rises above the straight line between the " +
             "two, in world units. Zero is a slide; this scene throws a " +
             "light ball of paper a short distance, which arcs a lot.")]
    public float arcHeight = 1.3f;

    [Tooltip("How long the flight takes. Long enough to be followed by a " +
             "six-year-old, short enough not to hold up the next word.")]
    public float duration = 0.55f;

    [Tooltip("Degrees per second of tumble on the way. A thrown object turns; " +
             "zero for something dropped carefully, like a pencil into a bag.")]
    public float spin = 180f;

    [Tooltip("How far it sinks into the opening after the arc has landed, " +
             "in world units. This is the part that reads as 'in' rather " +
             "than 'on'.")]
    public float settleDrop = 0.45f;

    [Tooltip("What it shrinks to as it sinks, as a fraction of its size. " +
             "Something dropping away from the camera gets smaller; this is " +
             "the cheapest honest version of that.")]
    [Range(0.1f, 1f)]
    public float settleScale = 0.6f;

    [Tooltip("How long the sinking takes.")]
    public float settleDuration = 0.22f;

    [Tooltip("Runs once it is out of sight, for a sound or for whatever the " +
             "scene does next.")]
    public UnityEvent onLanded = new UnityEvent();

    private Vector3 restScale;
    private bool measured;

    private Transform Target { get { return prop != null ? prop : transform; } }

    /// <summary>Throw it. Wire this to the frame of the pose where it leaves his hand.</summary>
    public void Play()
    {
        Transform t = Target;
        if (t == null) return;

        // Read the resting scale ONCE, and on the first throw rather than in
        // Awake: the builder sets the prop's scale from the sprite's own
        // bounds, and a second Play() that measured again would measure the
        // shrunken scale this component had just left behind and the prop
        // would get smaller every time the scene was replayed.
        if (!measured)
        {
            restScale = t.localScale;
            measured = true;
        }

        StopAllCoroutines();
        if (!isActiveAndEnabled)
        {
            // Nothing can run. Put it where it was going and leave it off,
            // so a scene in a strange state still ends up consistent.
            t.gameObject.SetActive(false);
            onLanded?.Invoke();
            return;
        }
        StartCoroutine(Fly());
    }

    private System.Collections.IEnumerator Fly()
    {
        Transform t = Target;
        t.localScale = restScale;
        t.position = from;
        t.rotation = Quaternion.identity;
        t.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            // The straight line, plus a half sine over it. Sine rather than
            // a parabola because it leaves and arrives flat, and a wrapper
            // that leaves his hand travelling sideways looks thrown where one
            // that leaves travelling upwards looks dropped from above.
            Vector3 p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
            t.position = p;
            if (spin != 0f) t.Rotate(0f, 0f, spin * Time.deltaTime);
            yield return null;
        }
        t.position = to;

        // --- and down into it ---
        Vector3 sunk = to + Vector3.down * settleDrop;
        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, settleDuration));
            t.position = Vector3.Lerp(to, sunk, k);
            t.localScale = Vector3.Lerp(restScale, restScale * settleScale, k);
            yield return null;
        }

        t.gameObject.SetActive(false);
        // Put it back the size it was, so the next run of the scene starts
        // from the prop the builder made rather than from a shrunken one.
        t.localScale = restScale;
        t.position = from;
        t.rotation = Quaternion.identity;
        onLanded?.Invoke();
    }

    /// <summary>
    /// Back to the start if the scene is switched off mid-flight, so a
    /// replay does not begin with a wrapper hanging over the desk.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        Transform t = Target;
        if (t == null || !measured) return;
        t.localScale = restScale;
        t.rotation = Quaternion.identity;
    }
}
