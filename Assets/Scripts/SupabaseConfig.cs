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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(projectUrl) && !string.IsNullOrWhiteSpace(anonKey);

    /// <summary>REST endpoint for the records table, e.g. https://xxx.supabase.co/rest/v1/player_scores</summary>
    public string TableEndpoint => projectUrl.TrimEnd('/') + "/rest/v1/" + tableName;
}
