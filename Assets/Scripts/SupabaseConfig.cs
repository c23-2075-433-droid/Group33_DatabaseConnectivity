using UnityEngine;

/// <summary>
/// Holds the Supabase project URL and anon (public) API key used by
/// SupabaseClient.
///
/// The actual asset (Assets/Resources/SupabaseConfig.asset) is DELIBERATELY
/// git-ignored so credentials never land in the repository - see the Activity
/// 5 security reminder. Each group member creates their own copy once via:
///
///     Tools > SALINLAHI > Create Supabase Config
///
/// then pastes the project URL and anon key from the Supabase dashboard
/// (Project Settings > API). Assets/Resources/SupabaseConfig.example.txt
/// documents the same steps for anyone cloning the repo.
///
/// NOTE: the anon key is a PUBLIC key - it ships inside any client app and
/// can always be read by a determined user. What actually protects the data
/// is Supabase's Row Level Security policies (see the SQL in
/// Docs/SupabaseSetup.md), which limit the anon role to inserting and reading
/// this one table and nothing else.
/// </summary>
[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "SALINLAHI/Supabase Config")]
public class SupabaseConfig : ScriptableObject
{
    [Tooltip("Supabase project URL, e.g. https://abcdefgh.supabase.co (no trailing slash).")]
    public string projectUrl = "";

    [Tooltip("Supabase 'anon public' API key from Project Settings > API.")]
    public string anonKey = "";

    [Tooltip("Table that game records are saved to and read back from.")]
    public string tableName = "player_scores";

    [Tooltip("Table the global leaderboard is read from. One row per player, " +
              "readable by anyone and writable only by the Edge Function.")]
    public string leaderboardTable = "leaderboard";

    [Tooltip("Edge Function that validates and stores a score submission. " +
              "The client is never trusted with a direct write to the leaderboard table.")]
    public string submitScoreFunction = "submit-score";

    [Tooltip("Postgres function that counts how many players are ahead of one player, " +
              "so a rank can be shown to someone outside the top of the board.")]
    public string rankFunction = "leaderboard_rank";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(projectUrl) && !string.IsNullOrWhiteSpace(anonKey);

    /// <summary>REST endpoint for the records table, e.g. https://xxx.supabase.co/rest/v1/player_scores</summary>
    public string TableEndpoint => projectUrl.TrimEnd('/') + "/rest/v1/" + tableName;

    /// <summary>REST endpoint for the leaderboard table. Read-only to the game:
    /// the anon role has a select policy and nothing else.</summary>
    public string LeaderboardEndpoint => projectUrl.TrimEnd('/') + "/rest/v1/" + leaderboardTable;

    /// <summary>Where a score submission is POSTed. An Edge Function rather than
    /// the table, because a score the client simply asserts is worth nothing -
    /// see Docs/SupabaseSetup.md.</summary>
    public string SubmitScoreEndpoint =>
        projectUrl.TrimEnd('/') + "/functions/v1/" + submitScoreFunction;

    /// <summary>Where a player's own rank is asked for.</summary>
    public string RankEndpoint =>
        projectUrl.TrimEnd('/') + "/rest/v1/rpc/" + rankFunction;
}
