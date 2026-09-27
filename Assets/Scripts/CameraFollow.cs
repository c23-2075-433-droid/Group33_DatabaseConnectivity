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

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
