using UnityEngine;

/// <summary>
/// Water running off Kylo after his bath: a handful of droplets that fall
/// from around his head and shoulders, fade, and start again.
///
/// WHY THIS EXISTS RATHER THAN WETTER HAIR. Rachel says "Kylo is clean, but
/// his hair is sticking up in every direction", and the drawing of him does
/// not really carry it - his post-bath hair is barely messier than his
/// normal hair. The obvious fix is to redraw the hair, and that turned out
/// not to be available: the generator refuses a child in a towel often
/// enough, and arbitrarily enough, that it is not something to build a
/// scene around. So the "he is soaking wet" reading is carried by motion
/// next to him instead, which costs one 20x30 sprite and no generation at
/// all - and motion reads faster than a hairstyle anyway.
///
/// It also gives Suklay a second thing to do. The comb already puts his
/// hair down; stopping the drips at the same moment is what makes the
/// difference between the two halves of the scene visible at a glance.
///
/// The drops do not follow a physics simulation. Each one picks a spot, a
/// speed and a delay, falls with a squared ease so it accelerates the way
/// water does, fades over the last third, waits, and goes again. Five of
/// them out of phase is enough to read as running water; a particle system
/// would be a heavier answer to the same question, and this scene is
/// generated, so everything has to be settable from a builder.
/// </summary>
public class DripEffect : MonoBehaviour
{
    [Tooltip("Whose shoulders the water runs off. The drops are positioned " +
             "against this every frame, so it is safe on a character that moves.")]
    public Transform follow;

    [Tooltip("Where the drops start, relative to follow. The default sits " +
             "around Kylo's head and shoulders.")]
    public Vector2 origin = new Vector2(0f, 2.0f);

    [Tooltip("How wide and how tall a band the drops start in. They never " +
             "all start from one point - that reads as a leaking pipe.")]
    public Vector2 spread = new Vector2(1.2f, 0.45f);

    [Tooltip("The droplet. fx_drop, drawn by Tools/make_water_drop.py.")]
    public Sprite dropSprite;

    [Range(1, 24)]
    public int dropCount = 5;

    [Tooltip("How far a drop falls before it has faded out, in world units.")]
    public float fallDistance = 1.25f;

    [Tooltip("Seconds for one fall. Each drop picks its own, so they do not march in step.")]
    public float minFall = 0.55f;
    public float maxFall = 1.0f;

    [Tooltip("Seconds a drop waits before starting again.")]
    public float maxRest = 1.6f;

    [Tooltip("Drawn on this layer, above the character.")]
    public string sortingLayer = "Player";
    public int sortingOrder = 10;

    [Tooltip("Start dripping as soon as the scene loads. Off for a scene " +
             "where the bath has not happened yet.")]
    public bool dripOnStart = false;

    private SpriteRenderer[] renderers;
    private float[] life;       // seconds into this drop's fall; negative is resting
    private float[] span;       // how long this drop's fall takes
    private Vector2[] spot;     // where it started, relative to origin
    private bool running;

    void Awake()
    {
        Build();
        if (!dripOnStart) HideAll();
        running = dripOnStart;
    }

    /// <summary>Wire to the word that leaves him wet.</summary>
    public void Begin()
    {
        if (renderers == null) Build();
        running = true;
    }

    /// <summary>
    /// Wire to the word that dries him off. Drops already falling are left
    /// to land rather than vanishing mid-air, which is the whole reason
    /// this is a flag and not a SetActive(false).
    /// </summary>
    public void Stop()
    {
        running = false;
    }

    private void Build()
    {
        renderers = new SpriteRenderer[dropCount];
        life = new float[dropCount];
        span = new float[dropCount];
        spot = new Vector2[dropCount];

        for (int i = 0; i < dropCount; i++)
        {
            GameObject go = new GameObject("Drop_" + (i + 1));
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dropSprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            renderers[i] = sr;
            // Staggered starts, so the first second is not five drops abreast.
            life[i] = -Random.Range(0f, maxRest);
            Respawn(i);
        }
    }

    private void Respawn(int i)
    {
        span[i] = Random.Range(minFall, maxFall);
        spot[i] = new Vector2(Random.Range(-spread.x, spread.x) * 0.5f,
                              Random.Range(-spread.y, spread.y) * 0.5f);
    }

    private void HideAll()
    {
        if (renderers == null) return;
        foreach (SpriteRenderer sr in renderers)
            if (sr != null) sr.enabled = false;
    }

    void LateUpdate()
    {
        if (renderers == null) return;
        Vector3 anchor = (follow != null ? follow.position : transform.position)
                         + (Vector3)origin;

        bool anyVisible = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            life[i] += Time.deltaTime;

            if (life[i] < 0f)
            {
                sr.enabled = false;
                continue;
            }

            if (life[i] >= span[i])
            {
                if (!running)
                {
                    sr.enabled = false;
                    continue;
                }
                life[i] = -Random.Range(0f, maxRest);
                Respawn(i);
                sr.enabled = false;
                continue;
            }

            float t = life[i] / span[i];
            // squared, so it accelerates downwards instead of drifting
            float y = -fallDistance * t * t;
            sr.transform.position = anchor + new Vector3(spot[i].x, spot[i].y + y, -0.01f);

            // fades over the last third of the fall
            float a = t < 0.67f ? 1f : Mathf.InverseLerp(1f, 0.67f, t);
            Color c = sr.color;
            c.a = a;
            sr.color = c;

            sr.enabled = true;
            anyVisible = true;
        }

        if (!running && !anyVisible) HideAll();
    }
}
