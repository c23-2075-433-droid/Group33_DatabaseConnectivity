using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires up the "Ang Iyong Paglalakbay" journey map: the Bahay, Paaralan,
/// Palaruan and Palengke nodes plus the Back button. Attached to the scene's
/// Canvas by BuildLevelSelectScene.cs.
///
/// Each node shows its unlocked (color) or locked (grayscale) art and is only
/// clickable once LevelProgress.IsUnlocked() says the player has reached it.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [System.Serializable]
    public class LevelNode
    {
        public Button button;
        public Image icon;
        public Sprite unlockedSprite;
        public Sprite lockedSprite;
        [Tooltip("1-based level index checked against LevelProgress.")]
        public int levelIndex;
        [Tooltip("Scene to load when this node is unlocked and clicked.")]
        public string sceneName;
    }

    [Tooltip("Scene to return to when Back is pressed.")]
    public string mainMenuSceneName = "MainMenu";

    public Button backButton;
    public LevelNode[] nodes;

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackPressed);

        foreach (var node in nodes)
        {
            bool unlocked = LevelProgress.IsUnlocked(node.levelIndex);

            if (node.icon != null)
                node.icon.sprite = unlocked ? node.unlockedSprite : node.lockedSprite;

            if (node.button != null)
            {
                node.button.interactable = unlocked;
                string sceneName = node.sceneName; // capture for the closure
                node.button.onClick.AddListener(() => OnNodePressed(sceneName));
            }
        }
    }

    private void OnNodePressed(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LevelSelectController: this node has no scene assigned yet.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    private void OnBackPressed()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
