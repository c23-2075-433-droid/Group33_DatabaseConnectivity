using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Saves and retrieves SALINLAHI play records from Supabase over its REST
/// API, using plain UnityWebRequest - no SDK needed.
///
/// Two operations, matching what Activity 5 requires:
///   SaveScore()        -> POST a new row   (save)
///   FetchRecentScores() -> GET recent rows (retrieve, then displayed by
///                          SessionScoreTracker on the level-complete panel)
///
/// Credentials come from a git-ignored SupabaseConfig asset, never from
/// hardcoded strings - see SupabaseConfig.cs.
/// </summary>
public class SupabaseClient : MonoBehaviour
{
    [Tooltip("Supabase URL + anon key. Create via Tools > SALINLAHI > Create Supabase Config.")]
    public SupabaseConfig config;

    /// <summary>
    /// POSTs one play record. onDone(success, error) - error is null on success.
    /// </summary>
    public void SaveScore(ScoreInsert record, Action<bool, string> onDone)
    {
        if (!HasValidConfig(onDone)) return;
        StartCoroutine(SaveRoutine(record, onDone));
    }

    /// <summary>
    /// GETs the most recent records, newest first.
    /// onDone(records, error) - error is null on success.
    /// </summary>
    public void FetchRecentScores(int limit, Action<List<ScoreRecord>, string> onDone)
    {
        if (!HasValidConfig((ok, err) => onDone?.Invoke(null, err))) return;
        StartCoroutine(FetchRoutine(limit, onDone));
    }

    private bool HasValidConfig(Action<bool, string> onDone)
    {
        if (config == null)
        {
            string msg = "SupabaseClient: no SupabaseConfig assigned in the Inspector.";
            Debug.LogWarning(msg);
            onDone?.Invoke(false, msg);
            return false;
        }
        if (!config.IsConfigured)
        {
            string msg = "SupabaseClient: SupabaseConfig is missing its project URL or anon key. " +
                          "Fill it in via Tools > SALINLAHI > Create Supabase Config.";
            Debug.LogWarning(msg);
            onDone?.Invoke(false, msg);
            return false;
        }
        return true;
    }

    private IEnumerator SaveRoutine(ScoreInsert record, Action<bool, string> onDone)
    {
        string json = JsonUtility.ToJson(record);

        // UnityWebRequest.Post(url, json, contentType) would also work, but the
        // explicit upload handler makes the raw JSON body unambiguous.
        using (UnityWebRequest request = new UnityWebRequest(config.TableEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");
            // Ask Supabase to echo the inserted row back, so the response
            // confirms exactly what was stored.
            request.SetRequestHeader("Prefer", "return=representation");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string msg = "Save failed (" + request.responseCode + "): " + request.error +
                              " | " + request.downloadHandler.text;
                Debug.LogError("SupabaseClient: " + msg);
                onDone?.Invoke(false, msg);
            }
            else
            {
                Debug.Log("SupabaseClient: saved record -> " + request.downloadHandler.text);
                onDone?.Invoke(true, null);
            }
        }
    }

    private IEnumerator FetchRoutine(int limit, Action<List<ScoreRecord>, string> onDone)
    {
        // PostgREST query: newest first, capped to `limit` rows.
        string url = config.TableEndpoint + "?select=*&order=created_at.desc&limit=" + Mathf.Max(1, limit);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            ApplyHeaders(request);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string msg = "Fetch failed (" + request.responseCode + "): " + request.error +
                              " | " + request.downloadHandler.text;
                Debug.LogError("SupabaseClient: " + msg);
                onDone?.Invoke(null, msg);
                yield break;
            }

            List<ScoreRecord> records = ParseRecords(request.downloadHandler.text);
            Debug.Log("SupabaseClient: fetched " + records.Count + " record(s).");
            onDone?.Invoke(records, null);
        }
    }

    private void ApplyHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("apikey", config.anonKey);

        // Legacy "anon" keys are JWTs (they start with "eyJ"), and PostgREST
        // accepts them as the bearer token too. The newer sb_publishable_...
        // keys are NOT JWTs - sending one as a bearer token makes PostgREST
        // try to parse it as a JWT and reject the request with a 401. So only
        // set Authorization for the JWT style; the apikey header above is
        // what authenticates either kind.
        if (config.anonKey.StartsWith("eyJ"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + config.anonKey);
        }
    }

    /// <summary>
    /// JsonUtility can't parse a top-level JSON array, which is exactly what
    /// PostgREST returns - so wrap it in an object first.
    /// </summary>
    private static List<ScoreRecord> ParseRecords(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
            return new List<ScoreRecord>();

        string wrapped = "{\"items\":" + json + "}";
        RecordList parsed = JsonUtility.FromJson<RecordList>(wrapped);
        return parsed?.items != null ? new List<ScoreRecord>(parsed.items) : new List<ScoreRecord>();
    }

    [Serializable]
    private class RecordList
    {
        public ScoreRecord[] items;
    }
}
