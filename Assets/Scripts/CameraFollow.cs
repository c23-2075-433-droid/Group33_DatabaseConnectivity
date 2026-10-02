using UnityEngine;

/// <summary>
/// Attach to the Main Camera. Makes it follow a target (the player) smoothly
/// on the X and Y axes, keeping the camera's own Z position.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("The object the camera should follow (drag player_character here).")]
    public Transform target;

    [Tooltip("How quickly the camera catches up. Higher = snappier, lower = smoother/laggier.")]
    public float smoothSpeed = 5f;

    [Tooltip("Offset from the target's position (keep Z at the camera's original distance, e.g. -10).")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Bounds")]
    [Tooltip("Stop the camera before it runs off the edge of the artwork. Needed when a room " +
             "is only as wide as the screen, otherwise following the player reveals empty space.")]
    public bool clampHorizontally = false;
    public float minX = 0f;
    public float maxX = 0f;

    [Tooltip("Hold the camera at a fixed height. Use when rooms are exactly one screen tall, " +
             "so jumping doesn't show above the ceiling.")]
    public bool lockVertically = false;
    public float fixedY = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (clampHorizontally) desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        if (lockVertically) desiredPosition.y = fixedY;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
