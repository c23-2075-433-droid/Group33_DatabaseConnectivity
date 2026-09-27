// Assets/Editor/AddNicknameFieldToMainMenu.cs
//
// Adds the nickname entry box to the ALREADY-BUILT main menu scene without
// regenerating it, so any manual tweaks to the menu are preserved (unlike
// Build Main Menu Scene, which rebuilds from scratch).
//
// Without this, every saved play record uses NicknameManager's fallback name
// ("Bata"), because MainMenuController had a nicknameInput field but no
// on-screen field was ever created for it.
//
// Safe to re-run: the field is rebuilt rather than duplicated.
//
// Run via: Tools > SALINLAHI > Add Nickname Field To Main Menu

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AddNicknameFieldToMainMenu
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/SALINLAHI/Add Nickname Field To Main Menu")]
    public static void AddField()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SALINLAHI] Stop Play mode first - this edits the scene, which Unity doesn't allow while the game is running.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("[SALINLAHI] No MainMenuController found in " + ScenePath +
                            " - run 'Build Main Menu Scene' first.");
            return;
        }

        InputField input = NicknameFieldBuilder.Build(controller.transform);
        controller.nicknameInput = input;

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Nickname field added to " + ScenePath + ". " +
                   "Whatever the player types is saved via NicknameManager when Play is pressed, " +
                   "and becomes the player_name on their database record. " +
                   "Nudge the NicknameInput/NicknameLabel RectTransforms in the Scene view if the " +
                   "placement clashes with the menu art.");
    }
}
