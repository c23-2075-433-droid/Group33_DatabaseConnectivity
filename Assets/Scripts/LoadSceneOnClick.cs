using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Loads a scene when its button is clicked. Used by the level-complete
/// panel's OKAY button to send the player back to the journey map.
///
/// Deliberately self-contained rather than living in SessionScoreTracker:
/// the panel is also built into scenes that have no score tracking, and the
/// button should still work there.
/// </summary>
[RequireComponent(typeof(Button))]
public class LoadSceneOnClick : MonoBehaviour
{
    [Tooltip("Scene to load when this button is pressed. Must be in Build Settings.")]
    public string sceneName = "LevelSelect";

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Load);
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LoadSceneOnClick: no sceneName set - nothing to load.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}
