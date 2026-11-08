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
using UnityEngine.UI;

public static class VocabularyFont
{
    private const string FontFolder = "Assets/Fonts/Vocabulary";

    // Very slightly warm white rather than pure #FFFFFF, so it sits with the
    // painted lettering on the plaques instead of glaring next to it.
    public static readonly Color Chalk = new Color(1f, 0.988f, 0.957f);

    // Flat near-black behind it. Outline draws the glyphs four more times,
    // one per corner, so the distance is the stroke.
    public static readonly Color InkOutline = new Color(0.04f, 0.03f, 0.02f, 1f);
    public const float OutlineDistance = 3f;

    /// <summary>
    /// Puts a Text into the display face: the font, the weight, the colour
    /// and the stroke, as one thing.
    ///
    /// It lives here rather than in either builder because BOTH boards draw
    /// text next to lettering painted into their own art - the vocabulary
    /// rows on ui_words_board, the two figures after "TOTAL POINTS:" and
    /// "WORDS COMPLETED:" on ui_result_board - and two copies of these four
    /// values are two copies that drift.
    ///
    /// Size, alignment and position are NOT set here. Those are the caller's:
    /// the boards place their text differently and only agree on how it looks.
    /// </summary>
    public static void Apply(Text text)
    {
        if (text == null) return;

        text.font = Get();

        // NOT Bold. Both Gagalin and Luckiest Guy ship one weight, and it is
        // already a heavy one; with no bold face to switch to, Unity fakes
        // the weight by emboldening the outlines it has, which thickens the
        // letters into each other and - on Gagalin especially - fills in the
        // chipped edges that are the whole point of the face.
        text.fontStyle = FontStyle.Normal;
        text.color = Chalk;

        // Reuse the Outline the caller already added rather than stacking a
        // second one on top: two of them multiply the drawn quads and the
        // stroke comes out twice as heavy.
        Outline outline = text.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = InkOutline;
        outline.effectDistance = new Vector2(OutlineDistance, -OutlineDistance);
    }

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
