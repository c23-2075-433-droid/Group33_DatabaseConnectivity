using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the painted background covering the whole screen on phones that are
/// wider than the 16:9 the artwork was drawn for.
///
/// Every scene camera is orthographic at size 5.4, which fixes the view at
/// 10.8 world units tall whatever the screen. The backgrounds are 16:9, so
/// they are 19.2 units wide. On a 16:9 phone the view is also 19.2 units wide
/// and the art fits exactly. On a 20:9 phone - which is most phones sold since
/// about 2018 - the view is 10.8 * (20/9) = 24 units wide, so 2.4 units of
/// empty camera colour show down each side. That is the black band in the
/// Appendix F screenshots.
///
/// The fix is to shrink the orthographic size until the background covers the
/// width again, which costs some height instead: on a 20:9 screen the size
/// drops from 5.4 to 4.32 and the top and bottom 10% of the art fall outside
/// the view. The floor (y = -3.3) and the player stay well inside that.
///
/// Nothing needs to be added to a scene by hand. The bootstrapper below
/// attaches this to the main camera on every scene load, and the width it
/// fits to is measured from the scene's own Background renderers, so a scene
/// whose art is already wider than the screen is left alone.
///
/// This relies on the cameras being pinned: every CameraFollow in the project
/// sets clampHorizontally with minX = maxX = 0 and lockVertically with
/// fixedY = 0, so the camera sits at the centre of the artwork and shrinking
/// the view keeps it there. If a room is ever widened and the camera allowed
/// to pan, this needs revisiting alongside those clamps.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraAspectFit : MonoBehaviour
{
    /// <summary>Sorting layer the scene backgrounds are drawn on.</summary>
    private const string BackgroundLayer = "Background";

    private Camera cam;
    private float designSize;            // the orthographic size the scene was authored at
    private float backgroundHalfWidth;   // half the widest background, in world units
    private int lastWidth;
    private int lastHeight;

    /// <summary>
    /// Attaches the component to the main camera of the starting scene and of
    /// every scene loaded afterwards, so no scene asset has to be edited.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        AttachToMainCamera();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToMainCamera();
    }

    private static void AttachToMainCamera()
    {
        Camera main = Camera.main;
        if (main == null || !main.orthographic) return;
        if (main.GetComponent<CameraAspectFit>() != null) return;
        main.gameObject.AddComponent<CameraAspectFit>();
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        designSize = cam.orthographicSize;
        backgroundHalfWidth = MeasureBackgroundHalfWidth();
    }

    private void OnEnable()
    {
        // Force the next LateUpdate to fit, whatever the screen was last time.
        lastWidth = 0;
        lastHeight = 0;
    }

    /// <summary>
    /// Re-fits only when the screen size actually changes, so this costs two
    /// integer comparisons a frame in the normal case. LateUpdate so it runs
    /// after CameraFollow has moved the camera.
    /// </summary>
    private void LateUpdate()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight) return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        Fit();
    }

    private void Fit()
    {
        if (cam == null || backgroundHalfWidth <= 0f) return;

        float aspect = cam.aspect;
        if (aspect <= 0.0001f) return;

        // Never grow past the size the scene was authored at: a screen
        // narrower than the art should keep the full height and let the art
        // overflow sideways, not zoom in.
        cam.orthographicSize = Mathf.Min(designSize, backgroundHalfWidth / aspect);
    }

    /// <summary>
    /// Half the width of the widest background in the scene, in world units.
    /// Inactive renderers are included because a scene can hold more than one
    /// room with only the current one switched on - the Banyo scene keeps the
    /// hallway and the bathroom stacked at x = 0 and toggles between them.
    /// Returns 0 when the scene has no background, in which case the camera is
    /// left exactly as the scene authored it.
    /// </summary>
    private float MeasureBackgroundHalfWidth()
    {
        float half = 0f;
        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;
            if (sr.sortingLayerName != BackgroundLayer) continue;

            // Taken from the sprite and the transform rather than from
            // sr.bounds, which is not populated for a renderer that is
            // switched off.
            float halfThis = sr.sprite.bounds.extents.x
                             * Mathf.Abs(sr.transform.lossyScale.x);
            half = Mathf.Max(half, halfThis);
        }

        return half;
    }
}
