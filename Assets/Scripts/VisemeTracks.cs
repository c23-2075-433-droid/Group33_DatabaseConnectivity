using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When Teacher Rachel's mouth should change shape: the lip-sync keyframes
/// for every recording.
///
/// Assets/Resources/VisemeTracks.json is written by Tools/bake_lipsync.py,
/// which runs Rhubarb Lip Sync over each WAV in Assets/Audio/VO - the
/// phonemes are found in the recording itself, the way Adobe Animate's Auto
/// Lip-Sync does it, so the keyframes match the audio as it is on disk.
/// Each entry is a line id and a list of [time, mouth] changes:
///
///     "rachel_l1_s1_bangon_done": [[0.0,"E"],[0.34,"Idle"],[0.5,"E"], ...]
///
/// One file rather than one per clip: Unity loads it once as a TextAsset,
/// where 434 tiny assets would each carry a .meta and each land in the build
/// separately.
///
/// A line with no track is NOT an error: it is a recording added since the
/// last bake, and RachelPortraitAnimator listens to it live instead. Re-run
/// the bake to give it keyframes.
/// </summary>
public static class VisemeTracks
{
    public const string ResourcePath = "VisemeTracks";

    /// <summary>One mouth change: when, and which sprite.</summary>
    public struct Change
    {
        public float time;
        public string mouth;
    }

    private static Dictionary<string, Change[]> tracks;

    /// <summary>
    /// The track for a line, or null if it has none. The id is the clip's
    /// name, which is the recording's file name without its extension - the
    /// same id the voice bank and the CSV use.
    /// </summary>
    public static Change[] For(string id)
    {
        if (tracks == null) Load();
        if (string.IsNullOrEmpty(id)) return null;

        Change[] found;
        return tracks.TryGetValue(id, out found) ? found : null;
    }

    /// <summary>Forgets the loaded tracks, so a regenerated file is picked up.</summary>
    // Also on every entry to Play mode: with domain reload turned off, a
    // static survives between runs and a fresh bake would not be seen.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Invalidate()
    {
        tracks = null;
    }

    private static void Load()
    {
        tracks = new Dictionary<string, Change[]>();

        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null) return;

        // Hand-parsed rather than JsonUtility, which cannot deserialise a
        // dictionary or a mixed [number, string] pair without a wrapper type
        // per line. The shape is fixed and written by our own tool, so the
        // parsing can be this direct.
        string text = asset.text;
        int at = text.IndexOf("\"tracks\"");
        if (at < 0) return;

        // Past the opening brace of the tracks object, so the first quote
        // found below is the first line id and not part of the word "tracks".
        at = text.IndexOf('{', at);
        if (at < 0) return;

        // Each pass reads one "id": [ ... ] pair and resumes immediately after
        // that pair's closing bracket. Resuming there is the whole trick: the
        // only characters between two pairs are a comma and the next id's
        // quotes, while the mouth names ("AI", "MBP") sit INSIDE the brackets
        // we have just stepped over - so the next quote found is always a key.
        while (true)
        {
            int keyStart = text.IndexOf('"', at);
            if (keyStart < 0) break;
            int keyEnd = text.IndexOf('"', keyStart + 1);
            if (keyEnd < 0) break;

            string id = text.Substring(keyStart + 1, keyEnd - keyStart - 1);

            int listStart = text.IndexOf('[', keyEnd);
            if (listStart < 0) break;
            int listEnd = MatchingBracket(text, listStart);
            if (listEnd < 0) break;

            tracks[id] = ParseChanges(text, listStart, listEnd);
            at = listEnd + 1;
        }
    }

    private static int MatchingBracket(string s, int open)
    {
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '[') depth++;
            else if (s[i] == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static Change[] ParseChanges(string s, int from, int to)
    {
        List<Change> list = new List<Change>();
        int i = from + 1;

        while (i < to)
        {
            int pairStart = s.IndexOf('[', i);
            if (pairStart < 0 || pairStart > to) break;
            int pairEnd = s.IndexOf(']', pairStart);
            if (pairEnd < 0 || pairEnd > to) break;

            string pair = s.Substring(pairStart + 1, pairEnd - pairStart - 1);
            int comma = pair.IndexOf(',');
            if (comma > 0)
            {
                float t;
                if (float.TryParse(pair.Substring(0, comma).Trim(),
                                   System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out t))
                {
                    list.Add(new Change
                    {
                        time = t,
                        mouth = pair.Substring(comma + 1).Trim().Trim('"'),
                    });
                }
            }
            i = pairEnd + 1;
        }
        return list.ToArray();
    }
}
