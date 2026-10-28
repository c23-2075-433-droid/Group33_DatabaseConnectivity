// Assets/Editor/NarratorVoiceBankTool.cs
//
// Builds Assets/Resources/NarratorVoiceBank.asset from Docs/VoiceOverScript.csv
// plus whatever is currently recorded in Assets/Audio/VO/.
//
// Run it after every recording session:
//     Tools > SALINLAHI > Refresh Narrator Voice Bank
//
// The CSV stays the single source of truth for what Rachel says - this tool
// only copies it into a form a BUILD can read. VoiceClipLibrary cannot be used
// at runtime (it is an Editor class and uses AssetDatabase), which is the whole
// reason the bank asset exists.
//
// Lines with no recording yet are kept, with their text and a null clip, so
// VoiceNarrator can caption them and hold the pause. That is what lets the
// prompting system be played and demonstrated before the voice-over exists.

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class NarratorVoiceBankTool
{
    private const string VoFolder    = "Assets/Audio/VO/";
    private const string AssetPath   = "Assets/Resources/NarratorVoiceBank.asset";
    private const string CsvRelative = "/../Docs/VoiceOverScript.csv";
    private const string FontFolder  = "Assets/Fonts/Fredoka";

    private static readonly string[] ClipExtensions = { ".wav", ".mp3", ".ogg" };

    [MenuItem("Tools/SALINLAHI/Refresh Narrator Voice Bank")]
    public static void Refresh()
    {
        NarratorVoiceBank bank = AssetDatabase.LoadAssetAtPath<NarratorVoiceBank>(AssetPath);
        bool isNew = bank == null;
        if (isNew)
        {
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            bank = ScriptableObject.CreateInstance<NarratorVoiceBank>();
            AssetDatabase.CreateAsset(bank, AssetPath);
        }

        List<NarratorLine> lines = new List<NarratorLine>();
        HashSet<string> seen = new HashSet<string>();

        // 1. Everything the recording script asks for, recorded or not.
        int fromCsv = 0;
        foreach (KeyValuePair<string, string> row in ReadScript())
        {
            if (!seen.Add(row.Key)) continue;
            lines.Add(new NarratorLine { id = row.Key, text = row.Value, clip = FindClip(row.Key) });
            fromCsv++;
        }

        // 2. Anything recorded that the script does not list, so a clip
        //    dropped in the folder is never silently ignored.
        int extras = 0;
        foreach (string id in RecordedIds())
        {
            if (!seen.Add(id)) continue;
            lines.Add(new NarratorLine { id = id, text = NarratorScript.DefaultText(id), clip = FindClip(id) });
            extras++;
            Debug.LogWarning("[SALINLAHI] " + id + " is recorded but not in Docs/VoiceOverScript.csv. " +
                             "Added to the bank - add a row to the CSV so it has a caption.");
        }

        bank.lines = lines.ToArray();
        bank.Invalidate();
        if (bank.captionFont == null) bank.captionFont = FindCaptionFont();

        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int recorded = 0;
        foreach (NarratorLine line in bank.lines) if (line.clip != null) recorded++;

        Debug.Log("[SALINLAHI] Narrator voice bank " + (isNew ? "created" : "refreshed") + " at " + AssetPath +
                  "\n  " + bank.lines.Length + " lines (" + fromCsv + " from the script" +
                  (extras > 0 ? ", " + extras + " extra recordings" : "") + ")" +
                  "\n  " + recorded + " recorded, " + (bank.lines.Length - recorded) +
                  " still waiting - those are captioned on screen instead." +
                  "\n  Caption font: " + (bank.captionFont != null ? bank.captionFont.name : "built-in"));

        Selection.activeObject = bank;
    }

    // ---------------------------------------------------------------------

    /// <summary>id (file name without extension) -> what Rachel says, from the CSV.</summary>
    private static List<KeyValuePair<string, string>> ReadScript()
    {
        List<KeyValuePair<string, string>> script = new List<KeyValuePair<string, string>>();

        string path = Application.dataPath + CsvRelative;
        if (!File.Exists(path))
        {
            Debug.LogWarning("[SALINLAHI] Docs/VoiceOverScript.csv not found at " + path +
                             " - the bank will only hold the clips that are already recorded.");
            return script;
        }

        List<string[]> rows = ParseCsv(File.ReadAllText(path));
        for (int i = 0; i < rows.Count; i++)
        {
            string[] row = rows[i];
            if (row.Length < 6) continue;

            string fileName = row[0].Trim();
            if (fileName.Length == 0) continue;
            if (fileName == "File Name") continue;                 // header

            string id = Path.GetFileNameWithoutExtension(fileName);
            string text = row[5].Trim();
            script.Add(new KeyValuePair<string, string>(id, text));
        }
        return script;
    }

    /// <summary>Every clip currently sitting in Assets/Audio/VO/, by file stem.</summary>
    private static List<string> RecordedIds()
    {
        List<string> ids = new List<string>();
        if (!Directory.Exists(VoFolder)) return ids;

        foreach (string file in Directory.GetFiles(VoFolder))
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (System.Array.IndexOf(ClipExtensions, ext) < 0) continue;
            ids.Add(Path.GetFileNameWithoutExtension(file));
        }
        return ids;
    }

    private static AudioClip FindClip(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (string ext in ClipExtensions)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoFolder + id + ext);
            if (clip != null) return clip;
        }
        return null;
    }

    /// <summary>The game's own font, so captions match the rest of the UI.</summary>
    private static Font FindCaptionFont()
    {
        if (!Directory.Exists(FontFolder)) return null;

        string[] guids = AssetDatabase.FindAssets("t:Font", new[] { FontFolder });
        Font best = null;
        foreach (string guid in guids)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
            if (font == null) continue;
            if (best == null) best = font;
            // Prefer a weight that reads well small and bold.
            if (font.name.Contains("SemiBold") || font.name.Contains("Medium")) return font;
        }
        return best;
    }

    /// <summary>Minimal RFC-4180 reader: quoted fields, "" escapes, commas and newlines inside quotes.</summary>
    private static List<string[]> ParseCsv(string text)
    {
        List<string[]> rows = new List<string[]>();
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c != '"') { field.Append(c); continue; }

                if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else inQuotes = false;
                continue;
            }

            if (c == '"') { inQuotes = true; }
            else if (c == ',') { fields.Add(field.ToString()); field.Length = 0; }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                fields.Add(field.ToString());
                field.Length = 0;
                rows.Add(fields.ToArray());
                fields.Clear();
            }
            else field.Append(c);
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add(fields.ToArray());
        }
        return rows;
    }
}
