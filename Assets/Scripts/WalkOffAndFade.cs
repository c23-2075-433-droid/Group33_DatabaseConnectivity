using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Walks a character OUT of the picture, sideways, and takes the light with
/// him.
///
/// WHY THIS IS NOT WalkIntoScene. That one is for the school gate, where
/// "going in" on a one-point-perspective painting means moving UP the frame
/// and shrinking; it owns a PlayerMovement, takes it off its ground collider
/// and leaves it off. Paaralan Scene 5 is a close-up of one desk with a fixed
/// camera, the figure is a plain SpriteRenderer with no PlayerMovement of its
/// own, and leaving at the end of the day means walking ACROSS the frame and
/// off the side of it at the size he already is.
///
/// WHAT IT DOES AND WHAT IT LEAVES TO OTHERS. It moves the transform and
/// nothing else: the WALK ITSELF is a PoseSequence on the same figure, looping
/// its four drawn frames, because that is how every other drawn action in this
/// project is built and because the two then share the scene's Pace. Start
/// both from the same word and they run together - the legs swing while the
/// boy crosses the room, which is the whole of not sliding.
///
/// THE FADE IS PART OF THE SAME BEAT, which is why it is here rather than in
/// SceneFader. SceneFader's own overlay sorts above everything in the scene
/// including the level-complete panel, so fading with it and leaving it black
/// would black out the panel as well; and its FadeOutAndLoad is for leaving
/// for another scene, which this is not - the room empties and the summary
/// comes up over it. So this fades an image of its own, set by the builder to
/// sort under the panels and over the room, and LEAVES IT black.
///
/// THE EASE IS IN AT THE START AND NOWHERE ELSE. He is standing still when
/// this begins, so he accelerates into his stride over the first fraction of
/// the move; he does not slow down at the end, because he is leaving rather
/// than arriving and there is nothing on the far side of the frame to arrive
/// at. A walk that eases out looks like a boy stopping just past the edge.
/// </summary>
public class WalkOffAndFade : MonoBehaviour
{
    [Tooltip("The figure that walks. Its transform is moved; its sprite is " +
             "left to whatever PoseSequence is driving it.")]
    public Transform figure;

    [Tooltip("Where he ends up, in world x. Far enough past the edge of the " +
             "view that the whole sprite is outside it - half a sprite width " +
             "beyond the frame, not level with it, or his trailing arm is " +
             "still on screen when the fade starts.")]
    public float endX = -11.6f;

    [Tooltip("Seconds before he sets off, for a turn or a last look. The " +
             "walk's own PoseSequence plays its lead-in over this.")]
    public float startDelay = 0.35f;

    [Tooltip("Seconds the walk itself takes. Match it to the PoseSequence " +
             "driving the legs or he arrives with them still going.")]
    public float duration = 3.2f;

    [Tooltip("Fraction of the walk spent getting up to speed. A child does " +
             "not reach walking pace instantly and a figure that does reads " +
             "as being dragged.")]
    [Range(0f, 0.5f)]
    public float easeIn = 0.14f;

    [Tooltip("The full-screen image this fades to black once he is gone. " +
             "Leave empty for a walk with no fade.")]
    public Image fadeImage;

    [Tooltip("Seconds after he leaves the frame before the fade starts.")]
    public float fadeDelay = 0.25f;

    [Tooltip("Seconds the fade takes. It is left black afterwards: the room " +
             "is empty and the level is over.")]
    public float fadeDuration = 0.9f;

    // Instantiated here, not left to Unity. A builder that does
    // AddComponent<WalkOffAndFade>() and hands this straight to
    // UnityEventTools.AddPersistentListener gets a NullReferenceException off
    // a null field, which is how more than one scene in this project has come
    // out half-built. Every UnityEvent in these components is newed up for
    // the same reason.
    [Tooltip("Runs once he is gone and the screen is black.")]
    public UnityEvent onFinished = new UnityEvent();

    private bool moving;

    /// <summary>
    /// True from the moment the word is accepted to the moment the screen is
    /// black. SceneObjectiveController waits on this the way it waits on a
    /// PoseSequence, so the level-complete panel cannot come up over a boy
    /// who is still halfway across the room.
    /// </summary>
    public bool IsMoving { get { return moving; } }

    /// <summary>Wire this to the word's objective in SceneObjectiveController.</summary>
    public void Begin()
    {
        if (figure == null || moving) return;
        StartCoroutine(Walk());
    }

    private IEnumerator Walk()
    {
        moving = true;

        // Stand the breathing down, which is not optional: IdleBreath writes
        // localPosition in LateUpdate - after this coroutine has written it -
        // so while it runs it simply undoes the move every frame and he walks
        // on the spot. WalkIntoScene learned this at the school gate.
        IdleBreath breath = figure.GetComponent<IdleBreath>();
        if (breath != null) breath.enabled = false;

        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        Vector3 from = figure.position;
        Vector3 to = new Vector3(endX, from.y, from.z);
        float elapsed = 0f;
        float span = Mathf.Max(0.01f, duration);

        while (elapsed < span)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / span);
            figure.position = Vector3.Lerp(from, to, Accelerated(k));
            yield return null;
        }
        figure.position = to;

        if (fadeDelay > 0f) yield return new WaitForSeconds(fadeDelay);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            float fade = Mathf.Max(0.01f, fadeDuration);
            float t = 0f;
            while (t < fade)
            {
                t += Time.deltaTime;
                c.a = Mathf.Clamp01(t / fade);
                fadeImage.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeImage.color = c;
        }

        moving = false;
        onFinished?.Invoke();
    }

    /// <summary>
    /// Distance covered by fraction `k` of the walk: a short acceleration
    /// into a constant pace, and no slowing down at the end.
    ///
    /// Written as the AREA under a speed curve that ramps from nothing to one
    /// over `easeIn` and then stays there, scaled so the whole walk still
    /// covers the whole distance in the whole time. Easing the position
    /// directly - a smoothstep, say - would have him crossing the room fast
    /// in the middle and crawling at both ends, which is a boy being carried.
    /// </summary>
    private float Accelerated(float k)
    {
        float e = Mathf.Clamp(easeIn, 0f, 0.5f);
        if (e <= 0.0001f) return k;
        float total = 1f - e * 0.5f;              // the area of the whole curve
        float covered = k < e ? k * k / (2f * e)
                              : e * 0.5f + (k - e);
        return covered / total;
    }
}
