using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fades a sprite in, holds it, then fades it out again. Used for the water
/// and suds that wash over the screen when the child says "Maligo".
///
/// This is the opposite of LightSwitchOverlay, which fades a cover away once
/// and leaves it off: this one comes and goes, so the moment reads as
/// something happening rather than something changing.
///
/// Wire Play() to the word's objective in SceneObjectiveController. onFinished
/// runs once the overlay has cleared, for anything that should happen after
/// the moment rather than during it.
/// </summary>
public class TimedOverlay : MonoBehaviour
{
    [Tooltip("The sprite to fade in and out. Usually a full-screen effect.")]
    public SpriteRenderer overlay;

    [Tooltip("How visible the effect gets at its peak.")]
    [Range(0f, 1f)]
    public float peakAlpha = 0.8f;

    [Tooltip("Seconds to fade in.")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Seconds to stay at full strength.")]
    public float holdDuration = 1.2f;

    [Tooltip("Seconds to fade back out.")]
    public float fadeOutDuration = 0.9f;

    [Tooltip("Runs once the overlay has cleared.")]
    public UnityEvent onFinished;

    private Coroutine playing;

    void Start()
    {
        SetAlpha(0f);
    }

    /// <summary>
    /// Plays the effect once. Saying the word again restarts it rather than
    /// stacking a second fade on top of the first.
    /// </summary>
    public void Play()
    {
        if (overlay == null) return;
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return Fade(CurrentAlpha(), peakAlpha, fadeInDuration);
        if (holdDuration > 0f) yield return new WaitForSeconds(holdDuration);
        yield return Fade(peakAlpha, 0f, fadeOutDuration);
        playing = null;
        onFinished?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private float CurrentAlpha() => overlay != null ? overlay.color.a : 0f;

    private void SetAlpha(float a)
    {
        if (overlay == null) return;
        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
