using System;

/// <summary>
/// What came back when the game asked to keep a nickname.
///
/// Three outcomes rather than a bool, because "the name belongs to someone
/// else" and "the check did not work" are different things that must lead to
/// different behaviour: the first one keeps the dialog open with a free name
/// filled in, and the second one lets the child through. Collapsing them into
/// one false would either refuse a name over a dropped connection or hand the
/// same name to two children over a working one.
/// </summary>
public enum NicknameClaimResult
{
    /// <summary>The name is now this device's, and nobody else can take it.</summary>
    Claimed,

    /// <summary>Somebody got there first. Offer something else.</summary>
    Taken,

    /// <summary>The question could not be asked. Not an answer of "taken".</summary>
    Failed,
}

/// <summary>
/// One row of the claims table: a name, and the installation that filed it.
///
/// The device is here because a claim has to be able to say "yours". A child
/// who renames their profile away from a name and back again would otherwise
/// be refused by the row they put there themselves.
/// </summary>
[Serializable]
public class NicknameClaim
{
    public string nickname;
    public string device_id;
}
