using System;
using System.Text;

/// <summary>
/// What a nickname is allowed to be, in one place and in plain C#.
///
/// The rules themselves are not new - the submit-score Edge Function has
/// applied them server-side since the leaderboard was built (see
/// cleanNickname() in supabase/functions/submit-score/index.ts). What is new is
/// that the game applies the same three rules at the point the name is typed or
/// read, instead of sending whatever was in the box and letting the server
/// quietly keep a different string.
///
///   control characters  become spaces
///   runs of whitespace  collapse to one
///   length              capped at MaxLength
///
/// Both ends keep their own copy on purpose. The server's is the one that
/// protects the board, because it is the only one a modified client cannot
/// skip; this one exists so that what the device saved and what the board shows
/// are the same name.
///
/// Deliberately a plain static class with no UnityEngine in it. That is what
/// lets Tools/test_security_rules.sh compile these rules on their own and
/// assert against them without opening the editor - a validation rule nobody
/// can run a test on tends to stop being true.
/// </summary>
public static class NicknameRules
{
    /// <summary>Longest nickname kept. The nickname input field's
    /// characterLimit, NicknameSuggester.MaxLength and the submit-score
    /// function's MAX_NICKNAME are all this same number.</summary>
    public const int MaxLength = 16;

    /// <summary>
    /// A nickname reduced to what the game will actually keep. Returns "" when
    /// nothing usable is left, which is a valid state: an unnamed profile falls
    /// back to NicknameManager.DefaultNickname.
    /// </summary>
    public static string Clean(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return "";

        StringBuilder clean = new StringBuilder(nickname.Length);
        bool lastWasSpace = false;
        foreach (char c in nickname)
        {
            // A control character becomes a space rather than vanishing, so
            // "Kyla\nSam" stays two words instead of becoming one.
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                if (clean.Length > 0 && !lastWasSpace) clean.Append(' ');
                lastWasSpace = true;
                continue;
            }
            clean.Append(c);
            lastWasSpace = false;
        }

        string trimmed = clean.ToString().Trim();
        return trimmed.Length > MaxLength
            ? trimmed.Substring(0, MaxLength).Trim()
            : trimmed;
    }

    /// <summary>
    /// True when this looks like the UUID PlayerProfileStore.Create() writes,
    /// and not something else that ended up in the save file.
    ///
    /// The save file is ordinary JSON in the app's storage and can be
    /// hand-edited, so the id read from it is input. Everything it is then
    /// used for - the leaderboard row it keys, the argument the rank function
    /// is asked for - is better off refusing a value of the wrong shape than
    /// passing it on to find out what the other end does with it.
    /// </summary>
    public static bool IsValidPlayerId(string id)
    {
        return !string.IsNullOrEmpty(id) && Guid.TryParseExact(id, "D", out _);
    }
}
