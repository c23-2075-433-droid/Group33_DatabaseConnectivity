// Assets/Editor/AddSettingsAboutToMainMenu.cs
//
// Restyles the Settings and About popups in the ALREADY-BUILT main menu scene
// without regenerating it, so manual tweaks to the menu are preserved.
//
// Swaps the plain grey Unity panels for the bamboo frame, the OKAY button and
// the wooden volume slider, matching the nickname dialog and the menu buttons.
//
// Safe to re-run: both popups are rebuilt rather than duplicated.
//
// Run via: Tools > SALINLAHI > Restyle Settings And About Popups

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AddSettingsAboutToMainMenu
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/SALINLAHI/Restyle Settings And About Popups")]
    public static void Restyle()
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

        // The builder removes any previous panel of the same name first, so the
        // old grey versions disappear here rather than stacking up.
        SettingsAboutDialogBuilder.Dialog settings = SettingsAboutDialogBuilder.BuildSettings(controller.transform);
        SettingsAboutDialogBuilder.Dialog about = SettingsAboutDialogBuilder.BuildAbout(controller.transform);

        controller.settingsPanel = settings.root;
        controller.settingsCloseButton = settings.close;
        controller.volumeSlider = settings.slider;
        controller.aboutPanel = about.root;
        controller.aboutCloseButton = about.close;

        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SALINLAHI] Settings and About popups restyled in " + ScenePath +
                   ". Both now use the bamboo panel, the OKAY button and the Fredoka UI font; " +
                   "the volume slider uses the wooden track/fill/knob art.");
    }
}
