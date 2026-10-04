// Assets/Editor/VoiceClipLibrary.cs
//
// Finds a word's recorded instruction clip by name, so the scene builders can
// attach it themselves.
//
// This matters because the scenes are generated. Assigning 95 clips by hand in
// the Inspector would look like it worked, and then every one of those
// assignments would be wiped the next time anyone ran Build Bahay Scene 2 -
// which happens constantly. Naming the files after the words instead means a
// rebuild re-attaches them, and recording a clip is just dropping a file in a
// folder.
//
// Expected layout:
//   Assets/Audio/VO/word_<word>.wav      e.g. word_bangon.wav for "Bangon"
//
// The names match the File Name column of Docs/VoiceOverScript.csv, so the
// recording script and the project agree without anyone maintaining a mapping.

using UnityEditor;
using UnityEngine;

public static class VoiceClipLibrary
{
    public const string Folder = "Assets/Audio/VO/";

    /// <summary>
    /// The recorded clip for a word, or null if it has not been recorded yet.
    /// Silent when missing: most words have no clip during development, and
    /// SceneObjectiveController already says so at runtime if one is replayed.
    /// </summary>
    public static AudioClip ForWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return null;

        string stem = Folder + "word_" + word.Trim().ToLowerInvariant();
        foreach (string ext in new[] { ".wav", ".mp3", ".ogg" })
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(stem + ext);
            if (clip != null) return clip;
        }
        return null;
    }

    /// <summary>How many of the given words have a clip, for a builder's log line.</summary>
    public static int CountRecorded(params string[] words)
    {
        int n = 0;
        foreach (string w in words) if (ForWord(w) != null) n++;
        return n;
    }
}
