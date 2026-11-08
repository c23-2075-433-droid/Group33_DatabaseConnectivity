// Assets/Editor/VocabularyFont.cs
//
// The font the Filipino words and their English meanings are set in on the
// WORDS LEARNED board - and nothing else on it. The title, the instruction
// line and the CONTINUE plank are painted into the art, and every other
// screen's text still comes from UIFont.Get().
//
// It is a separate lookup from UIFont rather than a second preference inside
// it because the two want different things. UIFont wants Fredoka: rounded and
// calm, legible in a leaderboard row. This wants the chunky distressed
// capitals in Art/words_learned_reference.png, which is a display face and
// would be wrong everywhere else.
//
// Drop a font into Assets/Fonts/Vocabulary/ and it is picked up on the next
// builder run. A filename containing "Gagalin" wins; otherwise Luckiest Guy;
// otherwise the first font in the folder. With the folder missing it falls
// back to UIFont.Get(), so the board still reads - just in the wrong face.
//
// See Assets/Fonts/Vocabulary/README.txt for why Gagalin is not committed and
// how to swap it in.

using UnityEditor;
using UnityEngine;

public static class VocabularyFont
{
    private const string FontFolder = "Assets/Fonts/Vocabulary";

    // In order. The first name that matches a filename in the folder wins.
    private static readonly string[] Preferred = { "Gagalin", "LuckiestGuy", "Luckiest Guy" };

    private static Font cached;
    private static bool warned;

    /// <summary>
    /// The vocabulary display font, falling back to the general UI font if
    /// the folder has nothing in it.
    /// </summary>
    public static Font Get()
    {
        if (cached != null) return cached;

        if (AssetDatabase.IsValidFolder(FontFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Font", new[] { FontFolder });

            string chosen = null;
            foreach (string want in Preferred)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.ToLowerInvariant().Contains(want.ToLowerInvariant()))
                    {
                        chosen = path;
                        break;
                    }
                }
                if (chosen != null) break;
            }

            // Something is in there, just not a name we know. Use it anyway -
            // dropping a font in this folder is how you ask for it.
            if (chosen == null && guids.Length > 0)
                chosen = AssetDatabase.GUIDToAssetPath(guids[0]);

            if (chosen != null)
            {
                cached = AssetDatabase.LoadAssetAtPath<Font>(chosen);
                if (cached != null)
                {
                    Debug.Log("[SALINLAHI] Vocabulary font: " + chosen);
                    return cached;
                }
            }
        }

        if (!warned)
        {
            warned = true;
            Debug.LogWarning("[SALINLAHI] No font in " + FontFolder + " - the WORDS LEARNED board " +
                             "will use the general UI font, which is not the distressed lettering " +
                             "the art is drawn around. See " + FontFolder + "/README.txt.");
        }
        return UIFont.Get();
    }
}
