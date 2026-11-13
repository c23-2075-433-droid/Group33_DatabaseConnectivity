using UnityEngine;

/// <summary>
/// Makes the word prompt's arrow jab at the thing it is pointing at.
///
/// WHY A STILL ARROW WAS NOT ENOUGH. The prompt already put an arrow beside
/// each word badge, and it did not do its job for two reasons. It sat ABOVE
/// the badge pointing UP, while the badge already floats above the object -
/// so it pointed away from the soap rather than at it. And it did not move,
/// which on a busy painted background is close to invisible to a child who
/// does not yet know what they are looking for. The scene is for someone who
/// cannot read the badge; the arrow is the only part of the prompt that
/// tells them WHERE.
///
/// The motion is shaped rather than a sine wave. A sine spends most of its
/// time near the extremes and reads as floating; a cartoon point is a quick
/// jab towards the target and a slower drift back, which is what this does -
/// roughly a third of each cycle going out, two thirds coming back. That
/// asymmetry is the whole difference between "bobbing" and "pointing".
///
/// It animates anchoredPosition, so it belongs on the arrow inside a prompt,
/// not on the prompt root - moving the root would drag the badge with it.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ArrowBob : MonoBehaviour
{
    [Tooltip("Which way the jab goes, in the prompt's own space. Down, for " +
             "an arrow under a badge pointing at the object below it.")]
    public Vector2 direction = Vector2.down;

    [Tooltip("How far it travels at full stretch, in canvas units. The world " +
             "prompts are scaled by 0.01, so 30 here is 0.3 world units.")]
    public float distance = 30f;

    [Tooltip("Jabs per second. Fast enough to catch the eye, slow enough not " +
             "to look agitated.")]
    public float rate = 1.4f;

    [Tooltip("How much bigger it gets at full stretch. Small - this is a " +
             "lean into the point, not a throb.")]
    [Range(0f, 0.4f)]
    public float stretch = 0.12f;

    private RectTransform rect;
    private Vector2 home;
    private Vector3 homeScale;
    private float phase;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        home = rect.anchoredPosition;
        homeScale = rect.localScale;
    }

    void OnEnable()
    {
        // Each prompt starts its arrow at the beginning of the jab rather
        // than wherever the clock happens to be, so the first thing the
        // child sees when a word comes up is the arrow moving.
        phase = 0f;
        if (rect != null)
        {
            rect.anchoredPosition = home;
            rect.localScale = homeScale;
        }
    }

    void Update()
    {
        if (rect == null) return;

        phase += Time.deltaTime * rate;
        if (phase >= 1f) phase -= Mathf.Floor(phase);

        // out fast, back slow
        const float Out = 0.35f;
        float t = phase < Out
            ? Mathf.SmoothStep(0f, 1f, phase / Out)
            : Mathf.SmoothStep(1f, 0f, (phase - Out) / (1f - Out));

        rect.anchoredPosition = home + direction.normalized * (distance * t);
        rect.localScale = homeScale * (1f + stretch * t);
    }
}
