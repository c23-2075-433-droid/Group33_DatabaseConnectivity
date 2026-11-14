using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// The one thing SALINLAHI still sends to Supabase: the global leaderboard.
///
/// Everything a child learns stays on the device (PlayerProfileStore). What
/// leaves it is four values - the player id, the nickname and the two running
/// totals - and only so that one board can show every player.
///
/// Reading and writing go different ways round on purpose:
///
///   read   straight off the table over PostgREST. The anon role has a select
///          policy and nothing else, so this is as far as the game's key gets.
///
///   write  through the submit-score Edge Function, never the table. A score
///          the client simply asserts is worth nothing: the function checks
///          it against what the row already holds, caps it at what the game
///          can actually award, and only it holds a key that may write.
///
/// A locally made UUID is not authentication - anyone who learned another
/// child's player_id could otherwise post scores as them. So the first
/// submission registers the player and the server hands back a token, which
/// is kept on the device and presented on every later one. The server stores
/// only its hash. That is what actually owns the row.
///
/// Offline is normal, not exceptional: the game is played on classroom tablets
/// that may have no signal at all. Nothing here blocks play, nothing is lost,
/// and a submission that fails simply leaves the profile marked as needing one
/// - PushIfNeeded() tries again next time the menu opens.
/// </summary>
public class LeaderboardClient : MonoBehaviour
{
    [Tooltip("Supabase URL + anon key. Create via Tools > SALINLAHI > Create Supabase Config.")]
    public SupabaseConfig config;

    [Tooltip("How many rows to ask the board for. The panel has six slots, but " +
              "the top ten is what is fetched so a rank can be worked out without " +
              "a second call.")]
    public int fetchLimit = 10;

    [Tooltip("Seconds to wait before giving up on a call. Short, because a child " +
              "is looking at a board that says 'Loading...'.")]
    public int timeoutSeconds = 10;

    private bool pushing;

    /// <summary>True when there is a project URL and key to talk to at all.</summary>
    public bool IsConfigured => config != null && config.IsConfigured;

    // ---------- writing ----------

    /// <summary>
    /// Sends the playing profile's totals if they have moved since the last
    /// successful send. Safe to call on every menu load: when nothing has
    /// changed it makes no request at all.
    /// </summary>
    public void PushIfNeeded(Action<bool, string> onDone = null)
    {
        PlayerProfile profile = PlayerProfileStore.Active;

        if (!profile.NeedsLeaderboardSync())
        {
            onDone?.Invoke(true, null);
            return;
        }

        // An unnamed profile has nothing to show on a board of names.
        if (string.IsNullOrWhiteSpace(profile.nickname))
        {
            onDone?.Invoke(false, "No nickname yet.");
            return;
        }

        // A profile whose id is not a UUID cannot own a row: the function
        // refuses it, and rightly. Stop here rather than spend a request
        // finding that out.
        if (!profile.HasValidId())
        {
            Debug.LogWarning("LeaderboardClient: this profile's id is not a UUID, " +
                              "so it has no row to submit to.");
            onDone?.Invoke(false, "This profile cannot be put on the board.");
            return;
        }

        if (!IsConfigured)
        {
            onDone?.Invoke(false, "Leaderboard is not configured.");
            return;
        }

        if (pushing)
        {
            onDone?.Invoke(false, "A submission is already in flight.");
            return;
        }

        pushing = true;
        StartCoroutine(PushRoutine(profile, onDone));
    }

    private IEnumerator PushRoutine(PlayerProfile profile, Action<bool, string> onDone)
    {
        LeaderboardSubmission body = new LeaderboardSubmission
        {
            player_id = profile.playerId,
            nickname = profile.nickname,
            total_points = profile.totalPoints,
            total_stars = profile.TotalStars(),
            token = profile.leaderboardToken,
        };

        using (UnityWebRequest request =
               new UnityWebRequest(config.SubmitScoreEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.downloadHandler = new DownloadHandlerBuffer();
            SupabaseClient.ApplyHeaders(request, config);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.Max(1, timeoutSeconds);

            yield return request.SendWebRequest();

            pushing = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                // A warning, not an error: being offline is an ordinary day
                // here, and the profile still carries everything it earned.
                // The code and the body go to the log; the caller gets a line
                // that can be shown to anyone.
                Debug.LogWarning("LeaderboardClient: submission failed (" +
                                  request.responseCode + "): " + request.error + " | " +
                                  (request.downloadHandler != null ? request.downloadHandler.text : ""));
                onDone?.Invoke(false, "Your progress is saved on this device.");
                yield break;
            }

            LeaderboardSubmissionResult result = null;
            try
            {
                result = JsonUtility.FromJson<LeaderboardSubmissionResult>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("LeaderboardClient: could not read the reply - " + e.Message);
            }

            if (result == null || !result.ok)
            {
                // The function's own reason is written for us, not for a
                // child - "That is a bigger jump in points than the game can
                // award at once" is a developer's sentence. Log it; show the
                // one thing that is true either way.
                Debug.LogWarning("LeaderboardClient: the server refused the submission - " +
                                  (result != null && !string.IsNullOrEmpty(result.error)
                                      ? result.error : "no reason given"));
                onDone?.Invoke(false, "Your progress is saved on this device.");
                yield break;
            }

            // Registration hands back the token that owns this row from now
            // on. Keep it before anything else: a token received and not
            // saved means every later submission is refused.
            if (!string.IsNullOrEmpty(result.token)) profile.leaderboardToken = result.token;

            profile.syncedPoints = body.total_points;
            profile.syncedStars = body.total_stars;
            profile.syncedNickname = body.nickname;
            PlayerProfileStore.Save();

            onDone?.Invoke(true, null);
        }
    }

    // ---------- reading ----------

    /// <summary>
    /// Fetches the board, best first, with the playing child's own row marked
    /// and ranked. onDone(rows, yourRank, error) - error is null on success,
    /// and yourRank is 0 when this player has no row yet.
    ///
    /// The rows come back already sorted by the database: points descending,
    /// then stars, which is the tiebreak the design asks for.
    /// </summary>
    public void Fetch(Action<List<LeaderboardEntry>, int, string> onDone)
    {
        if (!IsConfigured)
        {
            onDone?.Invoke(null, 0, "Leaderboard is not configured.");
            return;
        }
        StartCoroutine(FetchRoutine(onDone));
    }

    private IEnumerator FetchRoutine(Action<List<LeaderboardEntry>, int, string> onDone)
    {
        string you = PlayerProfileStore.Active.playerId;
        string url = config.LeaderboardEndpoint +
                      "?select=player_id,nickname,total_points,total_stars,updated_at" +
                      "&order=total_points.desc,total_stars.desc,updated_at.asc" +
                      "&limit=" + Mathf.Max(1, fetchLimit);

        List<LeaderboardEntry> rows;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SupabaseClient.ApplyHeaders(request, config);
            request.timeout = Mathf.Max(1, timeoutSeconds);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("LeaderboardClient: fetch failed (" + request.responseCode +
                                  "): " + request.error);
                onDone?.Invoke(null, 0, "The board could not be loaded.");
                yield break;
            }

            rows = Parse(request.downloadHandler.text);
        }

        int yourRank = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].rank = i + 1;
            rows[i].isYou = rows[i].player_id == you;
            if (rows[i].isYou) yourRank = i + 1;
        }

        // Off the end of the fetched page: ask the database to count how many
        // players are ahead rather than pulling the whole board down to find
        // out. A failure here is not an error - the board still shows.
        if (yourRank == 0 && PlayerProfile.IsValidId(you))
        {
            yield return RankRoutine(you, found => yourRank = found);
        }

        onDone?.Invoke(rows, yourRank, null);
    }

    private IEnumerator RankRoutine(string playerId, Action<int> onDone)
    {
        // Serialized, never concatenated: the id is read from a file on the
        // device and so is input like any other. JsonUtility escapes it.
        string body = JsonUtility.ToJson(new LeaderboardRankRequest { p_player_id = playerId });
        using (UnityWebRequest request =
               new UnityWebRequest(config.RankEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            SupabaseClient.ApplyHeaders(request, config);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.Max(1, timeoutSeconds);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("LeaderboardClient: could not read this player's rank (" +
                                  request.responseCode + "). The board will show without it.");
                onDone?.Invoke(0);
                yield break;
            }

            string json = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
            {
                onDone?.Invoke(0);
                yield break;
            }

            RankList parsed = JsonUtility.FromJson<RankList>("{\"items\":" + json + "}");
            onDone?.Invoke(parsed != null && parsed.items != null && parsed.items.Length > 0
                ? parsed.items[0].rank : 0);
        }
    }

    /// <summary>
    /// JsonUtility can't parse a top-level JSON array, which is exactly what
    /// PostgREST returns - so wrap it in an object first. Same trick as
    /// SupabaseClient.ParseRecords.
    /// </summary>
    private static List<LeaderboardEntry> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
            return new List<LeaderboardEntry>();

        EntryList parsed = JsonUtility.FromJson<EntryList>("{\"items\":" + json + "}");
        return parsed?.items != null
            ? new List<LeaderboardEntry>(parsed.items)
            : new List<LeaderboardEntry>();
    }

    [Serializable]
    private class EntryList { public LeaderboardEntry[] items; }

    [Serializable]
    private class RankList { public LeaderboardRank[] items; }
}
