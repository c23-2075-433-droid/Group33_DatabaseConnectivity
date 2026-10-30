// Assets/Editor/SpriteEdgeHaloFixer.cs
//
// Removes the white "sticker" rim around the level-summary sprites.
//
// The panel, the three buttons and the stars were drawing with a pale outline
// hugging their silhouette, as if each one had been cut out with a border left
// on. There were TWO separate white edges stacked on top of each other, and
// both have to go or the rim survives. That is why this runs in two stages.
//
// STAGE 1 - THE OPAQUE KEYLINE (this one changes visible pixels)
//
// The art was cut out of a white background and the antialiased edge was then
// flattened to full opacity. What is left is a ring of fully opaque, nearly
// white pixels running around the silhouette, sitting OUTSIDE the dark brown
// outline the artist drew. Walking inward one ring at a time, as average
// luminance:
//
//     ui_results_panel   ring0 169 | ring1  44 | ring2  34     -> 1 px of keyline
//     btn_okay           ring0 187 | ring1  56 | ring2  30     -> 2 px
//     btn_replay         ring0 196 | ring1  93 | ring2  51     -> 2 px
//     btn_back           ring0 222 | ring1 181 | ring2 128 ... -> 4 px
//
// Those pixels are at alpha 255, so they are real artwork and they are drawn.
// No import setting and no filtering trick removes them. The only fix is to
// repaint them, which is why this stage is NOT free: it pushes the dark
// outline outward over the keyline, thickening that outline by 1 to 4 pixels.
// At the size these sprites are drawn, that is under one screen pixel.
//
// The depth is measured per sprite rather than assumed, and capped at
// MaxKeyline. A per-pixel guard then repaints a pixel only when it is really
// brighter than the colour replacing it, so artwork that is legitimately pale
// where it meets the silhouette, such as a flower petal, is left alone.
//
// STAGE 2 - THE SOFT SILHOUETTE BAND (this one changes visible pixels)
//
// The character art carries its white edge somewhere else. Kylo's sprites do
// have proper antialiasing, but the partly transparent pixels in that ramp
// kept the pale background they were cut from. Their colour IS drawn, weighted
// by their alpha, so a white one lays a translucent white rim over whatever is
// behind. standing_barefoot, by alpha band:
//
//     alpha   1-31    stored (214,213,212)   9,469 soft pixels in all
//     alpha  32-95    stored (249,249,249)   nearly pure white
//     alpha  96-159   stored (214,212,210)
//     alpha 160-223   stored (146,141,136)
//
// Un-matting was tried first and does not work: solving for the true colour
// against a white background sends 10.7% of channels out of range, some as far
// as -64770, and leaves the low-alpha bands pale anyway. The alpha was not
// generated from a white matte, so there is nothing to invert. Repainting the
// band from the neighbouring artwork is what actually fixes it, and the
// brighter-than-replacement guard keeps it off art that is pale on purpose.
// On the ten level-summary sprites this stage repaints nothing at all.
//
// STAGE 3 - COLOUR BEHIND THE TRANSPARENT EDGE (this one changes nothing visible)
//
// Separately, the transparent pixels were exported still holding white in
// their colour channels, (255, 255, 255, 0). That is invisible on its own,
// which is why it survives every review, but Unity's bilinear filter averages
// neighbouring texels WITHOUT regard for alpha. A sample taken on the edge of
// the bamboo mixes brown with white and comes back pale, laying a second rim
// on top of the first.
//
//     ui_results_panel   182,059 transparent pixels, 100.0% pure white
//     btn_okay           115,923 transparent pixels,  99.9% pure white
//     btn_replay         145,808 transparent pixels,  99.9% pure white
//     btn_back            47,083 transparent pixels,  99.8% pure white
//
// The fix is alpha bleeding: push the artwork's colour outward into the
// transparent region so the filter blends brown into brown. Only pixels at
// alpha exactly 0 are touched and alpha is never written, so not one visible
// pixel changes here. Note that "Alpha Is Transparency" is already enabled on
// these textures, by SpriteImportFixer, and is not enough on its own.
//
// The stages run in this order on purpose: stage 3 bleeds the edge that
// stages 1 and 2 have already corrected, rather than bleeding the white they
// just removed.
//
// Re-running converges rather than compounding: once a sprite is clean both
// stages reproduce its pixels exactly and the file is left alone. A sprite
// whose cutout edge is a soft ramp rather than a hard line can need a second
// click before it settles, because stripping the bulk of the ramp exposes the
// last pixel of it. btn_back behaves this way; everything else is done in one.
//
// Run via: Tools > SALINLAHI > Fix Level Summary Sprite Edges
//          Tools > SALINLAHI > Fix Character Sprite Edges

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SpriteEdgeHaloFixer
{
    // The scene-result / level-complete panel and everything drawn on it.
    // SceneResultPanelBuilder.cs is the authority for this list.
    private static readonly string[] SummarySprites =
    {
        "ui_results_panel",
        "btn_okay",
        "btn_replay",
        "btn_back",
        "star_gold_left", "star_gold_mid", "star_gold_right",
        "star_grey_left", "star_grey_mid", "star_grey_right",
    };

    // Stage 1. Past about 4 pixels we are no longer looking at a cutout
    // artifact but at the artist's own shading, so the cap guards real art.
    private const int MaxKeyline = 4;

    // A ring counts as keyline when it is this much brighter than the artwork
    // well inside the silhouette.
    private const float KeylineMargin = 25f;

    // Depth taken as "the real artwork", to compare the outer rings against.
    private const int BaselineRing = 6;

    // Per-pixel guard: only repaint a pixel this much brighter than its
    // replacement.
    private const float RepaintGuard = 20f;

    // Stage 2. How far the soft silhouette band is repainted. A cutout edge is
    // only a few pixels of antialiasing wide, so this never needs to be large.
    private const int SoftEdgePasses = 12;

    // Stage 3. Bilinear filtering reaches one texel past the edge, so 2 would
    // do; 16 leaves headroom in case mipmaps are ever switched on.
    private const int BleedPasses = 16;

    // Kylo's poses, the classmates, the teacher and the guard. Matched by
    // prefix rather than listed by name: there are around fifty of them and
    // new poses keep arriving from the art pipeline, so a hardcoded list would
    // be stale within a week.
    private static readonly string[] CharacterPrefixes =
    {
        "standing", "talking", "walk_frame", "sitting", "lying_down",
        "celebrate", "char_",
    };

    [MenuItem("Tools/SALINLAHI/Fix Level Summary Sprite Edges")]
    public static void FixSummaryMenu()
    {
        Run(SummarySprites, "level-summary", "Fix Level Summary Sprite Edges");
    }

    [MenuItem("Tools/SALINLAHI/Fix Character Sprite Edges")]
    public static void FixCharactersMenu()
    {
        List<string> names = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D",
                                                         new[] { SpriteImportFixer.SpriteFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            foreach (string prefix in CharacterPrefixes)
            {
                if (!name.StartsWith(prefix)) continue;
                names.Add(name);
                break;
            }
        }
        names.Sort();

        if (names.Count == 0)
        {
            Debug.LogWarning("[SALINLAHI] No character sprites found in " +
                             SpriteImportFixer.SpriteFolder + ".");
            return;
        }
        Run(names.ToArray(), "character", "Fix Character Sprite Edges");
    }

    private static void Run(string[] names, string label, string who)
    {
        if (!SpriteImportFixer.Verify(names, who)) return;

        int fixedCount = 0;
        int repaintedTotal = 0;

        foreach (string name in names)
        {
            string path = SpriteImportFixer.SpriteFolder + "/" + name + ".png";
            Result result = Process(path);

            if (result.error != null)
            {
                Debug.LogWarning("[SALINLAHI] " + name + ": " + result.error + ".");
                continue;
            }

            if (!result.changed)
            {
                Debug.Log("[SALINLAHI] " + name + ": already clean.");
                continue;
            }

            fixedCount++;
            repaintedTotal += result.repainted + result.softRepainted;
            Debug.Log("[SALINLAHI] " + name + ": " +
                      (result.keylineDepth > 0
                          ? "keyline " + result.keylineDepth + " px, " + result.repainted +
                            " visible px repainted, "
                          : "no keyline, ") +
                      result.softRepainted + " soft edge px repainted, " +
                      result.bled + " transparent px bled.");
        }

        if (fixedCount == 0)
        {
            Debug.Log("[SALINLAHI] All " + names.Length + " " + label +
                      " sprites were already clean. Nothing was written.");
            return;
        }

        Debug.Log("[SALINLAHI] Fixed " + fixedCount + " of " + names.Length + " " + label +
                  " sprite(s). " + repaintedTotal + " visible edge pixel(s) were repainted so " +
                  "the dark outline now covers the old white edge. Every alpha value was left " +
                  "untouched, so the silhouettes are exactly as they were.");
    }

    private class Result
    {
        public bool changed;
        public int keylineDepth;
        public int repainted;
        public int softRepainted;
        public int bled;
        public string error;
    }

    /// <summary>
    /// Runs both stages over one PNG and writes it back if anything moved.
    /// The file is read as raw bytes rather than through the importer, so the
    /// result does not depend on how the texture happens to be imported.
    /// </summary>
    private static Result Process(string path)
    {
        Result result = new Result();

        byte[] bytes;
        try
        {
            bytes = System.IO.File.ReadAllBytes(path);
        }
        catch (System.Exception e)
        {
            result.error = "could not be read (" + e.Message + ")";
            return result;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (!texture.LoadImage(bytes))
        {
            Object.DestroyImmediate(texture);
            result.error = "could not be decoded as a PNG";
            return result;
        }

        // Anything other than a full RGBA decode means the alpha channel would
        // be dropped on the way back out, so leave the file alone.
        if (texture.format != TextureFormat.RGBA32 && texture.format != TextureFormat.ARGB32)
        {
            string format = texture.format.ToString();
            Object.DestroyImmediate(texture);
            result.error = "decoded as " + format + " rather than RGBA, skipped so its alpha " +
                           "cannot be lost";
            return result;
        }

        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        Color32[] before = (Color32[])pixels.Clone();

        // ---- Stage 1: strip the opaque keyline ----
        bool[] opaque = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) opaque[i] = pixels[i].a == 255;

        result.keylineDepth = KeylineDepth(pixels, opaque, width, height);
        if (result.keylineDepth > 0)
        {
            bool[] core = Erode(opaque, width, height, result.keylineDepth);
            if (AnyTrue(core))
            {
                result.repainted = Bleed(pixels, core, width, height,
                                         result.keylineDepth, opaque, true);
            }
            else
            {
                // Too thin to erode that far; there is no artwork left to pull
                // outward, so report it as having no keyline rather than lie.
                result.keylineDepth = 0;
            }
        }

        // ---- Stage 2: repaint the soft silhouette band ----
        // The character art carries its white edge here instead, in the
        // antialiased pixels between the artwork and nothing. Their colour IS
        // drawn, weighted by their alpha, so a white one lays a translucent
        // white rim over whatever is behind. Repaint them from the artwork and
        // the rim becomes the dark outline the sprite is supposed to have.
        bool[] opaqueOnly = new bool[pixels.Length];
        bool[] anyAlpha = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            opaqueOnly[i] = pixels[i].a == 255;
            anyAlpha[i] = pixels[i].a > 0;
        }
        result.softRepainted = Bleed(pixels, opaqueOnly, width, height,
                                     SoftEdgePasses, anyAlpha, true);

        // ---- Stage 3: bleed colour behind the transparent edge ----
        bool[] solid = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) solid[i] = pixels[i].a > 0;

        result.bled = Bleed(pixels, solid, width, height, BleedPasses, null, false);
        result.bled += Flood(pixels, solid);

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r == before[i].r && pixels[i].g == before[i].g &&
                pixels[i].b == before[i].b && pixels[i].a == before[i].a) continue;
            result.changed = true;
            break;
        }

        if (result.changed)
        {
            texture.SetPixels32(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        Object.DestroyImmediate(texture);

        if (result.changed) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return result;
    }

    /// <summary>
    /// How many opaque rings at the silhouette are leftover cutout edge rather
    /// than artwork, judged by comparing each ring's luminance against the art
    /// well inside it.
    /// </summary>
    private static int KeylineDepth(Color32[] pixels, bool[] opaque, int width, int height)
    {
        List<float> rings = new List<float>();
        bool[] current = (bool[])opaque.Clone();

        for (int d = 0; d <= BaselineRing; d++)
        {
            bool[] inner = Erode(current, width, height, 1);
            double sum = 0;
            int count = 0;
            for (int i = 0; i < current.Length; i++)
            {
                if (!current[i] || inner[i]) continue;
                sum += (pixels[i].r + pixels[i].g + pixels[i].b) / 3.0;
                count++;
            }
            if (count == 0) break;
            rings.Add((float)(sum / count));
            current = inner;
        }

        // Without a few rings to compare there is nothing to judge against.
        if (rings.Count < 3) return 0;

        float baseline = rings[rings.Count - 1];
        int depth = 0;
        int limit = Mathf.Min(MaxKeyline, rings.Count);
        for (int d = 0; d < limit; d++)
        {
            if (rings[d] <= baseline + KeylineMargin) break;
            depth = d + 1;
        }
        return depth;
    }

    /// <summary>
    /// Pushes colour outward from the known region, one ring at a time.
    ///
    /// Walks a frontier rather than rescanning the whole image per ring: a
    /// full-image pass for each of the 16 bleed rings is around a hundred
    /// million operations on the 1014x793 panel, while the frontier only ever
    /// touches the pixels actually entering a ring.
    ///
    /// limit, when given, restricts which pixels may be filled.
    /// onlyIfBrighter repaints a pixel only when it is clearly brighter than
    /// the colour replacing it, which is what keeps stage 1 off pale artwork.
    /// Alpha is never written.
    /// </summary>
    private static int Bleed(Color32[] pixels, bool[] known, int width, int height,
                             int rings, bool[] limit, bool onlyIfBrighter)
    {
        int changed = 0;
        bool[] queued = new bool[pixels.Length];

        List<int> frontier = new List<int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                if (known[i] && HasFillableNeighbour(known, limit, x, y, width, height))
                {
                    frontier.Add(i);
                }
            }
        }

        for (int ring = 0; ring < rings && frontier.Count > 0; ring++)
        {
            List<int> next = new List<int>();
            foreach (int i in frontier)
            {
                int x = i % width;
                int y = i / width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int sy = y + dy;
                    if (sy < 0 || sy >= height) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int sx = x + dx;
                        if (sx < 0 || sx >= width) continue;

                        int j = sy * width + sx;
                        if (known[j] || queued[j]) continue;
                        if (limit != null && !limit[j]) continue;

                        queued[j] = true;
                        next.Add(j);
                    }
                }
            }
            if (next.Count == 0) break;

            // Average against the known set as it stands BEFORE any of this
            // ring lands, so every pixel in the ring is derived from the same
            // state rather than from whichever neighbour happened to go first.
            Color32[] fill = new Color32[next.Count];
            for (int k = 0; k < next.Count; k++)
            {
                fill[k] = NeighbourAverage(pixels, known, next[k], width, height);
            }

            for (int k = 0; k < next.Count; k++)
            {
                int j = next[k];
                if (onlyIfBrighter && Luminance(pixels[j]) <= Luminance(fill[k]) + RepaintGuard)
                {
                    continue;
                }
                pixels[j] = new Color32(fill[k].r, fill[k].g, fill[k].b, pixels[j].a);
                changed++;
            }

            // The whole ring becomes known whether or not it was repainted, so
            // the next ring averages against a complete neighbourhood.
            foreach (int j in next) known[j] = true;
            frontier = next;
        }

        return changed;
    }

    /// <summary>
    /// Fills whatever the bleed did not reach with the artwork's mean colour.
    /// Nothing out here is ever sampled; filling it just stops the file from
    /// changing a little more on every run.
    /// </summary>
    private static int Flood(Color32[] pixels, bool[] known)
    {
        long r = 0, g = 0, b = 0, count = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0) continue;
            r += pixels[i].r;
            g += pixels[i].g;
            b += pixels[i].b;
            count++;
        }
        if (count == 0) return 0;

        byte mr = (byte)(r / count);
        byte mg = (byte)(g / count);
        byte mb = (byte)(b / count);

        int filled = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (known[i]) continue;
            if (pixels[i].r == mr && pixels[i].g == mg && pixels[i].b == mb) continue;
            pixels[i] = new Color32(mr, mg, mb, pixels[i].a);
            filled++;
        }
        return filled;
    }

    private static bool HasFillableNeighbour(bool[] known, bool[] limit,
                                             int x, int y, int width, int height)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            int sy = y + dy;
            if (sy < 0 || sy >= height) continue;
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int sx = x + dx;
                if (sx < 0 || sx >= width) continue;

                int j = sy * width + sx;
                if (known[j]) continue;
                if (limit != null && !limit[j]) continue;
                return true;
            }
        }
        return false;
    }

    /// <summary>Mean colour of a pixel's eight neighbours, counting only known ones.</summary>
    private static Color32 NeighbourAverage(Color32[] pixels, bool[] known,
                                            int i, int width, int height)
    {
        int x = i % width;
        int y = i / width;
        int r = 0, g = 0, b = 0, count = 0;

        for (int dy = -1; dy <= 1; dy++)
        {
            int sy = y + dy;
            if (sy < 0 || sy >= height) continue;
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int sx = x + dx;
                if (sx < 0 || sx >= width) continue;

                int j = sy * width + sx;
                if (!known[j]) continue;
                r += pixels[j].r;
                g += pixels[j].g;
                b += pixels[j].b;
                count++;
            }
        }

        if (count == 0) return pixels[i];
        return new Color32((byte)Mathf.RoundToInt((float)r / count),
                           (byte)Mathf.RoundToInt((float)g / count),
                           (byte)Mathf.RoundToInt((float)b / count),
                           pixels[i].a);
    }

    /// <summary>
    /// Shrinks a mask by one pixel, the given number of times. Coordinates are
    /// clamped at the image border rather than wrapped, so colour is never
    /// pulled across from the opposite edge.
    /// </summary>
    private static bool[] Erode(bool[] mask, int width, int height, int times)
    {
        if (times <= 0) return (bool[])mask.Clone();

        bool[] source = mask;
        bool[] target = null;

        for (int pass = 0; pass < times; pass++)
        {
            target = new bool[mask.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool all = true;
                    for (int dy = -1; dy <= 1 && all; dy++)
                    {
                        int sy = Mathf.Clamp(y + dy, 0, height - 1);
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int sx = Mathf.Clamp(x + dx, 0, width - 1);
                            if (source[sy * width + sx]) continue;
                            all = false;
                            break;
                        }
                    }
                    target[y * width + x] = all;
                }
            }
            source = target;
        }

        return target;
    }

    /// <summary>
    /// How light a pixel reads. A plain channel average is enough here: the
    /// keyline is near white against a dark brown outline, so the two are
    /// about 130 apart and no perceptual weighting would change the verdict.
    /// </summary>
    private static float Luminance(Color32 colour)
    {
        return (colour.r + colour.g + colour.b) / 3f;
    }

    private static bool AnyTrue(bool[] mask)
    {
        for (int i = 0; i < mask.Length; i++)
        {
            if (mask[i]) return true;
        }
        return false;
    }
}
