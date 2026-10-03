using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps part of a scene in darkness until the player says the light word,
/// then fades the darkness away. Used for "Ilaw" in Bahay Scene 2.
///
/// This is a tinted sprite laid over the room rather than a second, darker
/// background image. Two separately drawn rooms never match exactly, so
/// swapping between them reads as the room changing rather than the light
/// coming on. Fading one overlay guarantees it is the same room.
///
/// The overlay sits above the background but below the player, so the child
/// can still see the character while the room is dark.
/// </summary>
public class LightSwitchOverlay : MonoBehaviour
{
    [Tooltip("The dark sprite covering the room. Usually a plain colour quad.")]
    public SpriteRenderer overlay;

    [Tooltip("How dark the room is before the light is switched on (0 = clear, 1 = solid).")]
    [Range(0f, 1f)]
    public float darkAlpha = 0.88f;

    [Tooltip("Seconds the light takes to come up.")]
    public float fadeDuration = 0.8f;

    [Tooltip("Start the room dark. Turn off if a scene should begin already lit.")]
    public bool startDark = true;

    private Coroutine fadeRoutine;

    void Start()
    {
        if (overlay == null) return;
        SetAlpha(startDark ? darkAlpha : 0f);
    }

    /// <summary>
    /// Fades the darkness out. Wire this to the "Ilaw" objective's onCorrect
    /// event in SceneObjectiveController.
    /// </summary>
    public void TurnOnLight() => FadeTo(0f);

    /// <summary>Fades the darkness back in, for a future scene that needs it.</summary>
    public void TurnOffLight() => FadeTo(darkAlpha);

    private void FadeTo(float target)
    {
        if (overlay == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = overlay.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(start, target, elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(target);
        fadeRoutine = null;
    }

    private void SetAlpha(float a)
    {
        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
