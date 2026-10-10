using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fades the screen to black before loading another scene, and fades back in
/// when a scene starts. Without this, walking out of the bedroom cuts
/// straight to the hallway, which is jarring for a young player.
///
/// Nothing needs to reference this directly: ExitDoorTrigger looks for one in
/// the scene and uses it if present, otherwise it loads the scene straight
/// away, so scenes without a fader still work.
/// </summary>
public class SceneFader : MonoBehaviour
{
    [Tooltip("Full-screen black image this fades in and out.")]
    public Image fadeImage;

    [Tooltip("Seconds to fade out before loading the next scene.")]
    public float fadeOutDuration = 0.6f;

    [Tooltip("Seconds to fade back in once the new scene starts.")]
    public float fadeInDuration = 0.6f;

    private bool isLoading = false;

    void Start()
    {
        if (fadeImage == null) return;
        // Scenes begin covered, then reveal, so the hand-off looks continuous.
        SetAlpha(1f);
        StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration, null));
    }

    /// <summary>
    /// Fades to black, then loads the named scene. Ignores repeat calls so a
    /// second trigger can't start two loads.
    /// </summary>
    public void FadeOutAndLoad(string sceneName)
    {
        if (isLoading || string.IsNullOrEmpty(sceneName)) return;
        isLoading = true;

        // Every scene hands on through here, so this is the one place that
        // knows a scene has been finished. The map unlocks step by step now,
        // not level by level, and only the last scene of a level has a
        // SessionScoreTracker - without this the middle scenes would open
        // nothing and the path would stay locked behind Scene 1. Marked after
        // the guard, so a scene with nowhere to go does not count as passed.
        LevelProgress.MarkSceneComplete(SceneManager.GetActiveScene().name);
        StartCoroutine(FadeRoutine(CurrentAlpha(), 1f, fadeOutDuration,
            () => SceneManager.LoadScene(sceneName)));
    }

    /// <summary>
    /// Fades to black, runs an action while the screen is covered, then fades
    /// back in. Used to move between rooms inside one scene, so swapping the
    /// artwork is hidden behind the fade.
    /// </summary>
    public void FadeOutAndThen(System.Action whileBlack, System.Action onDone = null)
    {
        StartCoroutine(FadeThroughRoutine(whileBlack, onDone));
    }

    private IEnumerator FadeThroughRoutine(System.Action whileBlack, System.Action onDone)
    {
        yield return FadeRoutine(CurrentAlpha(), 1f, fadeOutDuration, null);
        whileBlack?.Invoke();
        yield return FadeRoutine(1f, 0f, fadeInDuration, null);
        onDone?.Invoke();
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, System.Action onDone)
    {
        if (fadeImage == null)
        {
            onDone?.Invoke();
            yield break;
        }

        // Block clicks while the screen is covered.
        fadeImage.raycastTarget = to > 0.5f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(to);
        fadeImage.raycastTarget = to > 0.5f;
        onDone?.Invoke();
    }

    private float CurrentAlpha() => fadeImage != null ? fadeImage.color.a : 0f;

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
