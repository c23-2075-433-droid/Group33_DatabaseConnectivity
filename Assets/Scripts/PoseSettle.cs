using UnityEngine;

/// <summary>
/// Eases a pose into place when it is switched on, instead of letting it
/// appear.
///
/// Scenes here change pose by swapping one object for another: "Upo" switches
/// the standing Kylo off and the seated Kylo on, in the same frame. That is one
/// picture replacing another with nothing in between, and it is the single
/// biggest reason the game reads as a slideshow of poses rather than a
/// character doing things.
///
/// Drawing the in-between frames is the proper fix and needs an artist. This is
/// the cheap nine-tenths: the new pose arrives a little high and a little
/// stretched and settles down into its resting place over a few hundred
/// milliseconds, decelerating as it lands. For sitting that happens to be very
/// nearly right - a body lowering onto a chair and compressing as it takes the
/// weight is exactly this shape of motion.
///
/// The resting place is wherever the builder put the object, read at the moment
/// it is enabled, so nothing needs configuring per scene.
/// </summary>
public class PoseSettle : MonoBehaviour
{
    [Tooltip("How far above its resting place the pose starts, in world units.")]
    public float dropFrom = 0.45f;

    [Tooltip("How far to one side the pose starts, in world units. Negative " +
             "comes in from the left. Used so a character who walked up from " +
             "the left seems to carry that momentum into the chair rather than " +
             "reappearing at it - keep it small, since a seated figure sliding " +
             "any distance reads as skating.")]
    public float driftFrom = 0f;

    [Tooltip("How long the settle takes. Long enough to read as movement, " +
             "short enough that it never delays the next spoken word.")]
    public float duration = 0.28f;

    [Tooltip("How much the figure squashes as it lands, as a fraction of its " +
             "height. Set to 0 for a straight drop with no give.")]
    [Range(0f, 0.2f)]
    public float squash = 0.05f;

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(Settle());
    }

    private System.Collections.IEnumerator Settle()
    {
        Vector3 restPosition = transform.localPosition;
        Vector3 restScale = transform.localScale;

        if (duration <= 0f) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            // Ease out: fast at the start, slowing as it arrives. A linear
            // drop reads as a lift rather than a sit.
            float eased = 1f - (1f - k) * (1f - k);
            transform.localPosition = restPosition + new Vector3(driftFrom * (1f - eased),
                                                                 dropFrom * (1f - eased),
                                                                 0f);

            // Squash peaks as he lands and recovers, rather than building up
            // over the whole move - the give is in the last part of a sit.
            float give = squash * Mathf.Sin(eased * Mathf.PI) * eased;
            transform.localScale = new Vector3(restScale.x * (1f + give * 0.5f),
                                               restScale.y * (1f - give),
                                               restScale.z);
            yield return null;
        }

        // Land exactly where the builder meant, never on a rounding error.
        transform.localPosition = restPosition;
        transform.localScale = restScale;
    }
}
