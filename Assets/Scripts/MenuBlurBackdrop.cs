using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Throws the menu out of focus while a dialog is open, by swapping every
/// sprite it draws for a soft copy of itself.
///
/// The obvious way to blur what is behind a dialog is a shader that samples
/// the screen, and it is the wrong way here. A grab of the screen cannot see
/// a Screen Space - Overlay canvas, which is what this menu is, so the blur
/// would miss the very thing it is meant to soften; capturing the frame
/// instead means a GPU readback on a phone every time a child presses
/// ADVENTURE. Neither is worth it for a screen that never moves.
///
/// So the soft copies are made once, offline, by
/// Tools/import_main_menu_art.py - every menu piece is written twice,
/// <name>.png and <name>_blur.png - and this swaps between them. It costs
/// nothing to run and looks the same on every device.
///
/// Sprites are matched by reference, not by name, so a renamed file or a
/// sprite with no soft twin simply never swaps.
/// </summary>
public class MenuBlurBackdrop : MonoBehaviour
{
    [Tooltip("The menu's own sprites. Index for index with 'blurred'.")]
    public Sprite[] sharp;
    [Tooltip("Their soft copies, in the same order as 'sharp'.")]
    public Sprite[] blurred;

    // What each Image was showing before the blur went on. Also the record of
    // whether the blur is on at all.
    private readonly Dictionary<Image, Sprite> restore = new Dictionary<Image, Sprite>();

    /// <summary>
    /// Softens (or sharpens again) every Image under this object. Safe to
    /// call twice in a row with the same value.
    /// </summary>
    public void SetBlurred(bool on)
    {
        if (on == (restore.Count > 0)) return;

        if (!on)
        {
            foreach (KeyValuePair<Image, Sprite> was in restore)
            {
                if (was.Key != null) was.Key.sprite = was.Value;
            }
            restore.Clear();
            return;
        }

        // Inactive ones too: the Settings and About panels are switched off
        // most of the time, and they would come back sharp over a soft menu.
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            Sprite soft = SoftCopyOf(image.sprite);
            if (soft == null) continue;
            restore[image] = image.sprite;
            image.sprite = soft;
        }
    }

    private Sprite SoftCopyOf(Sprite s)
    {
        if (s == null || sharp == null || blurred == null) return null;
        for (int i = 0; i < sharp.Length && i < blurred.Length; i++)
        {
            if (sharp[i] == s) return blurred[i];
        }
        return null;
    }
}
