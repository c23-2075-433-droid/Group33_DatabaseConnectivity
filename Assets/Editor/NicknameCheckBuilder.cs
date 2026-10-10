// Assets/Editor/NicknameCheckBuilder.cs
//
// Wires the "nickname already exists" check into whichever menu scene is
// open: a SupabaseClient for the menu to read names with, and the
// NicknameAvailability component that uses it to answer OKAY.
//
// Shared by both menu builders, because they are two doors into the same
// scene - BuildMainMenuScene rebuilds the menu from scratch, while
// AddNicknameDialogToMainMenu patches the one already there. When this lived
// inside only one of them, a rebuild silently dropped the check.
//
// The menu had no database object of its own before this; the level scenes
// get theirs from DatabaseDemoBuilder. This makes one on the same terms,
// reading the same git-ignored config asset, and reusing it if it is already
// there.

using UnityEditor;
using UnityEngine;

public static class NicknameCheckBuilder
{
    private const string ConfigPath = "Assets/Resources/SupabaseConfig.asset";
    public const string HostName = "Database";

    /// <summary>
    /// Adds (or refreshes) the check and returns it, ready to assign to
    /// MainMenuController.nicknameAvailability.
    /// </summary>
    public static NicknameAvailability Build(NicknameDialogBuilder.Dialog dialog)
    {
        GameObject host = GameObject.Find(HostName);
        if (host == null) host = new GameObject(HostName);

        SupabaseClient client = host.GetComponent<SupabaseClient>();
        if (client == null) client = host.AddComponent<SupabaseClient>();
        client.config = AssetDatabase.LoadAssetAtPath<SupabaseConfig>(ConfigPath);

        NicknameAvailability check = host.GetComponent<NicknameAvailability>();
        if (check == null) check = host.AddComponent<NicknameAvailability>();
        check.supabaseClient = client;
        check.input = dialog.input;
        check.statusText = dialog.status;
        check.okayButton = dialog.okay;

        if (client.config == null)
        {
            Debug.LogWarning("[SALINLAHI] " + ConfigPath + " not found - the nickname check is wired " +
                              "but accepts every name until the config exists. Run " +
                              "Tools > SALINLAHI > Create Supabase Config.");
        }
        return check;
    }
}
