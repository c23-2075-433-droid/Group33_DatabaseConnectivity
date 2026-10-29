using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One spoken line: the text Rachel says and, once it has been recorded, the
/// clip that says it. The id is the recording's file name without its
/// extension, so it matches the File Name column of Docs/VoiceOverScript.csv
/// and the files in Assets/Audio/VO/ without anyone maintaining a mapping.
///
/// A line with no clip is NOT an error - it is the normal state before a
/// recording session. VoiceNarrator shows its text as an on-screen caption
/// and holds the pause the clip would have taken, so the prompting system can
/// be played, tested and demonstrated before a single word is recorded.
/// </summary>
[Serializable]
public class NarratorLine
{
    [Tooltip("Recording file name without the extension, e.g. 'rachel_frame_sabihin' or 'word_lakad'.")]
    public string id;

    [Tooltip("What Rachel says. Shown as a caption while the line plays, and used on its own when the clip has not been recorded yet.")]
    [TextArea(1, 3)]
    public string text;

    [Tooltip("What the word means in English, from the CSV's English Gloss column. Empty for Rachel's own lines, which gloss nothing.")]
    public string gloss;

    [Tooltip("The recording, once it exists. Leave empty until it does.")]
    public AudioClip clip;
}

/// <summary>
/// Every line the narrator can say, in one asset at
/// Assets/Resources/NarratorVoiceBank.asset so it can be loaded at runtime
/// (VoiceClipLibrary cannot - it is an Editor class and uses AssetDatabase,
/// which does not exist in a build).
///
/// The asset is generated, not filled in by hand: run
/// Tools > SALINLAHI > Refresh Narrator Voice Bank and it is rebuilt from
/// Docs/VoiceOverScript.csv plus whatever is currently in Assets/Audio/VO/.
/// Re-run it after every recording session.
///
/// The bank is optional. With no asset at all the narrator falls back to
/// NarratorScript's built-in Filipino text, so the game still prompts
/// correctly on a fresh checkout.
/// </summary>
[CreateAssetMenu(fileName = "NarratorVoiceBank", menuName = "SALINLAHI/Narrator Voice Bank")]
public class NarratorVoiceBank : ScriptableObject
{
    /// <summary>Where Resources.Load looks for it. Must stay in a Resources folder.</summary>
    public const string ResourcePath = "NarratorVoiceBank";

    [Tooltip("Font for the on-screen caption. Filled in by the refresh tool from Assets/Fonts/Fredoka; a built-in font is used if it is empty.")]
    public Font captionFont;

    [Tooltip("Generated - see Tools > SALINLAHI > Refresh Narrator Voice Bank.")]
    public NarratorLine[] lines = new NarratorLine[0];

    private Dictionary<string, NarratorLine> index;

    /// <summary>The line with this id, or null if the bank has never heard of it.</summary>
    public NarratorLine Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (index == null)
        {
            index = new Dictionary<string, NarratorLine>();
            foreach (NarratorLine line in lines)
            {
                if (line == null || string.IsNullOrEmpty(line.id)) continue;
                index[line.id] = line;
            }
        }

        NarratorLine found;
        return index.TryGetValue(id, out found) ? found : null;
    }

    /// <summary>Drops the lookup table, so a bank edited in the Inspector takes effect without a domain reload.</summary>
    public void Invalidate()
    {
        index = null;
    }

    void OnValidate()
    {
        Invalidate();
    }

    /// <summary>The asset, or null if it has not been generated yet.</summary>
    public static NarratorVoiceBank Load()
    {
        return Resources.Load<NarratorVoiceBank>(ResourcePath);
    }
}
