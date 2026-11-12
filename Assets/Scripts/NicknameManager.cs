/// <summary>
/// The playing child's nickname, readable from any scene.
///
/// The API has not changed - any scene can still read NicknameManager.Nickname
/// without wiring anything in the Inspector, and Teacher Rachel's narration
/// still addresses the player by name - but what is underneath it has. The
/// name used to be a PlayerPrefs string of its own; it is now one field of the
/// profile that also holds this child's points, stars and journey progress
/// (PlayerProfileStore).
///
/// That matters because the nickname is no longer an identity. A profile is
/// identified by its UUID, so changing the name leaves every point and star
/// exactly where it was - which is the whole reason CHANGE PROFILE can exist.
/// </summary>
public static class NicknameManager
{
    /// <summary>"child" - the friendly fallback when no name has been given yet.</summary>
    public const string DefaultNickname = "Bata";

    /// <summary>The current nickname, or the friendly default if none has been set.</summary>
    public static string Nickname => PlayerProfileStore.Active.DisplayName();

    /// <summary>
    /// True once the child has actually been asked and answered. The main
    /// menu uses this to decide whether to open the nickname dialog on a fresh
    /// install, which is the difference between a first-time player and a
    /// returning one.
    /// </summary>
    public static bool HasNickname =>
        !string.IsNullOrWhiteSpace(PlayerProfileStore.Active.nickname);

    /// <summary>The playing profile's UUID. What the leaderboard keys on.</summary>
    public static string PlayerId => PlayerProfileStore.Active.playerId;

    /// <summary>
    /// Saves a new nickname on the playing profile. Blank leaves the profile
    /// unnamed, and Nickname falls back to the default, exactly as before.
    ///
    /// Progress is untouched: this renames a profile, it does not make one.
    /// </summary>
    public static void SetNickname(string nickname)
    {
        PlayerProfileStore.Rename(PlayerProfileStore.Active, nickname);
    }
}
