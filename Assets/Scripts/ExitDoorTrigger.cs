using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the door GameObject in Level 1 ("Umaga na!"). Its Collider2D must
/// have "Is Trigger" checked. Fires when the player reaches the door, but only
/// once they've actually stood up (currentWakeStage == Standing) - matches the
/// "Alis" prompt only appearing after Bangon -> Tayo -> Lakad in the level design.
///
/// Same pattern as GoalTrigger.cs, adapted to load the next scene instead of
/// just showing a "Level Complete" panel.
/// </summary>
public class ExitDoorTrigger : MonoBehaviour
{
    [Tooltip("Tag used to identify the player GameObject. Leave as 'Player' " +
             "and tag your player object, or leave blank to accept any collider.")]
    public string playerTag = "Player";

    [Tooltip("If true, logs and reports even if the player object isn't tagged 'Player' " +
             "(useful while you're still setting up tags).")]
    public bool acceptAnyCollider = true;

    [Tooltip("Name of the scene to load next. Leave empty if there's nowhere to send the " +
             "player yet - LoadNextScene() just no-ops instead of erroring. Must be added " +
             "to Build Settings > Scenes In Build once set.")]
    public string nextSceneName = "";

    [Tooltip("The 'Level Complete' / transition UI object, shown briefly before the " +
             "scene loads. Leave unassigned if you don't have one set up yet.")]
    public GameObject levelCompleteUI;

    [Tooltip("Seconds to show levelCompleteUI before loading the next scene. " +
             "Set to 0 to load immediately.")]
    public float delayBeforeLoad = 1.0f;

    [Tooltip("Optional - the scene's objective controller. If assigned, " +
             "hides the current prompt once the player exits through the door.")]
    public SceneObjectiveController objectiveController;

    private bool exitTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exitTriggered) return;

        bool isPlayer = acceptAnyCollider || other.CompareTag(playerTag);
        if (!isPlayer) return;

        // Only allow exiting once the player has actually completed the wake-up
        // sequence and is standing/walking - not while lying down or sitting up.
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null && playerMovement.currentWakeStage != PlayerMovement.WakeStage.Standing)
        {
            Debug.Log("ExitDoorTrigger: player reached the door but hasn't stood up yet - ignoring.");
            return;
        }

        exitTriggered = true;
        Debug.Log("ExitDoorTrigger: player reached the door. Loading next scene: " + nextSceneName);

        if (objectiveController != null) objectiveController.HideAllPrompts();
        if (levelCompleteUI != null) levelCompleteUI.SetActive(true);

        if (delayBeforeLoad <= 0f)
        {
            LoadNextScene();
        }
        else
        {
            Invoke(nameof(LoadNextScene), delayBeforeLoad);
        }
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("ExitDoorTrigger: nextSceneName is empty - not loading anything.");
            return;
        }
        SceneManager.LoadScene(nextSceneName);
    }

    // Draws the trigger zone as an orange box in the Scene view (editor only,
    // never rendered in-game) - useful since this GameObject has no sprite of
    // its own and would otherwise be invisible/hard to find when positioning it.
    private void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.25f);
        Gizmos.DrawCube(col.offset, col.size);
        Gizmos.color = new Color(1f, 0.55f, 0f, 1f);
        Gizmos.DrawWireCube(col.offset, col.size);
    }
}
