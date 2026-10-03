using UnityEngine;

/// <summary>
/// Holds the player's nickname across scenes. Static + PlayerPrefs-backed, so
/// any scene (Home, School, Park, ...) can read the nickname without needing
/// a reference wired in the Inspector — e.g. so Teacher Rachel's narration
/// can address the player by name later ("Tara, gising na!").
///
/// No scene wiring needed: just call NicknameManager.Nickname to read it, or
/// NicknameManager.SetNickname(text) to save a new one (MainMenuController
/// does this when the Play button is pressed).
/// </summary>
public static class NicknameManager
{
    private const string PlayerPrefsKey = "SALINLAHI_Nickname";
    private const string DefaultNickname = "Bata"; // "child" - friendly fallback if none was entered

    private static string cachedNickname;

    /// <summary>The current nickname. Falls back to a friendly default if none has been set yet.</summary>
    public static string Nickname
    {
        get
        {
            if (cachedNickname == null)
            {
                cachedNickname = PlayerPrefs.GetString(PlayerPrefsKey, DefaultNickname);
            }
            return cachedNickname;
        }
    }

    /// <summary>Saves a new nickname (trimmed; falls back to the default if left blank).</summary>
    public static void SetNickname(string nickname)
    {
        string trimmed = string.IsNullOrWhiteSpace(nickname) ? DefaultNickname : nickname.Trim();
        cachedNickname = trimmed;
        PlayerPrefs.SetString(PlayerPrefsKey, trimmed);
        PlayerPrefs.Save();
    }
}
