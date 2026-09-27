// Assets/Editor/AddNicknameDialogToMainMenu.cs
//
// Adds the "Enter Nickname" popup to the ALREADY-BUILT main menu scene
// without regenerating it, so manual tweaks to the menu are preserved.
//
// Replaces the earlier inline name plank: pressing Play now opens a modal
// dialog (dimmed backdrop, bamboo panel, name slot, OKAY / CANCEL) instead
// of going straight to the journey map. Any leftover NicknameInput/
// NicknameLabel objects from that older version are removed.
//
// Safe to re-run: the dialog is rebuilt rather than duplicated.
//
// Run via: Tools > SALINLAHI > Add Nickname Dialog To Main Menu

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AddNicknameDialogToMainMenu
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/SALINLAHI/Add Nickname Dialog To Main Menu")]
    public static void AddDialog()
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

        // Clear out the older inline plank field, which the dialog replaces.
        RemoveByName(scene, "NicknameLabel");
        foreach (GameObject go in FindByName(scene, "NicknameInput"))
        {
            // Only the old top-level one; the dialog's own slot shares the name
            // but lives under the dialog root, which doesn't exist yet here.
            Object.DestroyImmediate(go);
        }

        NicknameDialogBuilder.Dialog dialog = NicknameDialogBuilder.Build(controller.transform);

        controller.nicknamePanel = dialog.root;
        controller.nicknameInput = dialog.input;
        controller.nicknameOkayButton = dialog.okay;
        controller.nicknameCancelButton = dialog.cancel;

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Nickname dialog added to " + ScenePath + ". " +
                   "Play now opens the popup; OKAY saves the name via NicknameManager and loads the " +
                   "journey map, CANCEL closes it. The name becomes player_name on the database record.");
    }

    private static void RemoveByName(Scene scene, string name)
    {
        foreach (GameObject go in FindByName(scene, name)) Object.DestroyImmediate(go);
    }

    private static System.Collections.Generic.List<GameObject> FindByName(Scene scene, string name)
    {
        var matches = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects()) Collect(root.transform, name, matches);
        return matches;
    }

    private static void Collect(Transform t, string name, System.Collections.Generic.List<GameObject> matches)
    {
        if (t.name == name) matches.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), name, matches);
    }
}
