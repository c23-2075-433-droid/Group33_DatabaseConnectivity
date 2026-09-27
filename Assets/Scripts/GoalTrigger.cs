using UnityEngine;

/// <summary>
/// Attach to the goal flag's GameObject. Its Collider2D must have
/// "Is Trigger" checked. Fires when the player reaches the goal.
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Tag used to identify the player GameObject. Leave as 'Player' " +
             "and tag your player object, or leave blank to accept any collider.")]
    public string playerTag = "Player";

    [Tooltip("If true, logs and reports even if the player object isn't tagged 'Player' " +
             "(useful while you're still setting up tags).")]
    public bool acceptAnyCollider = true;

    [Tooltip("Color the flag turns when the goal is reached.")]
    public Color completeColor = Color.green;

    [Tooltip("The 'Level Complete' UI object (e.g. the Text (Legacy) GameObject under Canvas). " +
             "Leave unassigned if you don't have one set up yet.")]
    public GameObject levelCompleteUI;

    private bool goalReached = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (goalReached) return;

        bool isPlayer = acceptAnyCollider || other.CompareTag(playerTag);
        if (!isPlayer) return;

        goalReached = true;
        Debug.Log("Level Complete! Player reached the goal.");

        // Show the Level Complete UI panel/text.
        if (levelCompleteUI != null) levelCompleteUI.SetActive(true);

        // Visual feedback: tint the flag to show completion.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = completeColor;

        // Freeze the player in place so the level clearly feels "done".
        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        MonoBehaviour playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        // Hook additional level-complete behavior here later, e.g.:
        // - load the next scene
        // - award vocabulary points
    }
}
