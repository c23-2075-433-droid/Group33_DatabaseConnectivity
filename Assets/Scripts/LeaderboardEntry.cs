using System;

/// <summary>
/// One row of the global leaderboard, mirroring the `leaderboard` table.
/// Field names match the column names exactly - JsonUtility maps them by name
/// when PostgREST's rows are read back.
///
/// Four columns, and deliberately no more. The board shows who is playing and
/// how far they have got; which words a child found hard, how many tries they
/// took and when they played is a learning record, and that stays on the
/// device. See Docs/SupabaseSetup.md.
/// </summary>
[Serializable]
public class LeaderboardEntry
{
    public string player_id;    // uuid - the identity, so one row per player
    public string nickname;     // display name, changeable without a new row
    public int total_points;
    public int total_stars;
    public string updated_at;

    /// <summary>Filled in from the row's position once the list is sorted,
    /// or from the server when the player is off the end of it.</summary>
    [NonSerialized] public int rank;

    /// <summary>True for the row belonging to the child holding the phone.</summary>
    [NonSerialized] public bool isYou;
}

/// <summary>What the game POSTs to the submit-score Edge Function.
///
/// The token is the part that matters: it was made by the server when this
/// profile first registered and is the only thing that proves the submission
/// comes from the installation that owns the row. The player_id alone is not
/// a secret and is not treated as one.</summary>
[Serializable]
public class LeaderboardSubmission
{
    public string player_id;
    public string nickname;
    public int total_points;
    public int total_stars;
    public string token;
}

/// <summary>What the Edge Function answers with.</summary>
[Serializable]
public class LeaderboardSubmissionResult
{
    public bool ok;
    public string token;     // only sent back on the first registration
    public int rank;
    public string error;
}

/// <summary>
/// The argument the leaderboard_rank() function is called with.
///
/// A type for one string looks like ceremony, and it is not: the body used to
/// be built by concatenation, which quietly trusts the id to contain no quote,
/// brace or backslash. JsonUtility escapes whatever it is given, so a
/// hand-edited save file cannot write its own JSON into the request.
/// </summary>
[Serializable]
public class LeaderboardRankRequest
{
    public string p_player_id;
}

/// <summary>One row from the leaderboard_rank() function.</summary>
[Serializable]
public class LeaderboardRank
{
    public int rank;
    public int total_points;
    public int total_stars;
}
