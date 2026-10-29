"""Cut the gameplay HUD art out of the 1920x1080 sheets it was drawn on.

The HUD pieces - Teacher Rachel's narration plaque, the status plank, the word
signpost, the round menu button and a wooden microphone - arrived as six flat
PNGs, each one element floating in the middle of an otherwise white
1920x1080 canvas, with no alpha channel at all. Imported as they are, every
one of them would draw as a white rectangle with a small picture in the
centre, and Unity would letterbox them into whatever rect the layout gives.

So each sheet gets three things done to it:

  1. The white background is keyed out, by whiteness alone rather than by
     what the border can reach. Reachability is the safer rule in general -
     it keeps white that the art encloses - but here it is the wrong one:
     the hollow of Teacher Rachel's branch ring is enclosed, and flooding
     in from the edge leaves it a solid white disc with the room supposed to
     be showing through it. Nothing in these five pieces is white; the menu
     button's rim is cream, which is far enough off-white on one channel to
     survive the test, and the wood highlights are nowhere near it.

  2. The one-pixel ring where the art fades into the background is given
     partial alpha and un-blended back towards its own colour, so edges stay
     soft instead of turning into a jagged light fringe. The same halo problem
     cut_teacher.py solves by eroding; here there is a clean white backdrop to
     measure against, so the edge can be recovered properly rather than
     trimmed away.

  3. The result is cropped to the art's own bounds. A sprite that is mostly
     empty canvas cannot be positioned - its pivot is wherever the drawing
     happens to sit - and it wastes most of its texture memory on nothing.

Sheet 3 carries two pieces stacked with a gap between them, the microphone
and a plank that repeats sheet 4, so only its upper band is taken. The
microphone was also drawn with a soft drop shadow, which is a shadow onto the
white sheet and belongs to the sheet rather than to the microphone - kept, it
follows the sprite around as a pale grey smear over the bedroom floor. It is
flat grey where the art is strongly brown, so it comes off by colour.

The ring on the narration plaque is deliberately left hollow. In the mockup
the room shows through it around Rachel, and that is the look: the portrait
goes in at run time, behind the ring, with the scene visible around her.
Where her circle sits is printed at the end, as a fraction of the sprite, so
the layout can place her without anyone measuring pixels by hand.

Writes to Assets/Sprites/:
    ui_narration_panel.png   branch ring + wooden plaque (sheet 2)
    ui_status_plank.png      the plank under the mic / the level label (sheet 4)
    ui_word_signpost.png     the word-to-say signpost (sheet 5)
    btn_menu_round.png       round hamburger button, top right (sheet 6)
    mic_icon_wood.png        wooden microphone (sheet 3, upper band only)

Run from the repository root:
    python3 Tools/cut_hud_assets.py ~/Downloads/Robo
"""
import os
import sys

import numpy as np
from PIL import Image

DST = "Assets/Sprites"

# A pixel counts as background only if it is this close to white AND reachable
# from the border. 243 keeps the cream rim on the menu button (240,224,192 -
# white enough on one channel, far from it on another) out of the background.
WHITE = 243
CHROMA = 12

# How far into the white a pixel can be and still be treated as a blended
# edge rather than background. Covers the anti-aliased ring, nothing more.
EDGE = 46

# The fraction of the plaque's width the branch ring occupies, searched for
# the hollow it frames. The plaque itself fills everything to the right.
RING_SEARCH = 0.25

# Grey that is a drop shadow onto the sheet, not part of the art: flat enough
# to have almost no colour, light enough not to be an outline. Every piece
# here is brown or cream wood, so nothing real is this colourless.
SHADOW_CHROMA = 14
SHADOW_LUM = 150

# sheet -> (output name, vertical band to take, has a drop shadow to remove)
PIECES = [
    ("2", "ui_narration_panel", 0, False),
    ("4", "ui_status_plank",    0, False),
    ("5", "ui_word_signpost",   0, False),
    ("6", "btn_menu_round",     0, False),
    ("3", "mic_icon_wood",      0, True),   # upper band: the mic, not the repeated plank
]


def background_mask(rgb, drop_shadow=False):
    """
    Near-white, wherever it is. Enclosed white counts too - see the note at
    the top about the branch ring, whose hollow is exactly that case.

    With drop_shadow, the sheet's soft grey shadow goes with it.
    """
    hi = rgb.max(axis=2)
    lo = rgb.min(axis=2)
    chroma = hi - lo

    bg = (hi >= WHITE) & (chroma <= CHROMA)
    if drop_shadow:
        bg |= (chroma <= SHADOW_CHROMA) & (hi >= SHADOW_LUM)
    return bg


def bands(mask):
    """Runs of rows that hold art, top to bottom."""
    rows = mask.any(axis=1)
    out, start = [], None
    for y, on in enumerate(rows):
        if on and start is None:
            start = y
        elif not on and start is not None:
            out.append((start, y - 1))
            start = None
    if start is not None:
        out.append((start, len(rows) - 1))
    return out


def cut(path, band_index, drop_shadow=False):
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    bg = background_mask(rgb, drop_shadow)
    art = ~bg

    # Soften the edge: a pixel sitting next to the background was drawn blended
    # into white, so recover how much of it is really there and un-blend it.
    alpha = art.astype(np.float32)
    touching = art & (
        np.roll(bg, 1, 0) | np.roll(bg, -1, 0) | np.roll(bg, 1, 1) | np.roll(bg, -1, 1)
    )
    if touching.any():
        lum = rgb.max(axis=2)
        a = np.clip((255.0 - lum[touching]) / EDGE, 0.0, 1.0)
        a = np.maximum(a, 0.25)          # never erase an edge outright
        alpha[touching] = a
        # c = a*src + (1-a)*white  ->  src = (c - (1-a)*255) / a
        px = rgb[touching]
        rgb[touching] = np.clip(
            (px - (1.0 - a)[:, None] * 255.0) / a[:, None], 0.0, 255.0
        )

    rows = bands(art)
    if not rows:
        raise SystemExit("%s: nothing but background" % path)
    if band_index >= len(rows):
        raise SystemExit("%s: wanted band %d, found %d" % (path, band_index, len(rows)))
    y0, y1 = rows[band_index]

    strip = art[y0:y1 + 1]
    cols = np.where(strip.any(axis=0))[0]
    x0, x1 = cols.min(), cols.max()

    out = np.dstack([rgb[y0:y1 + 1, x0:x1 + 1],
                     alpha[y0:y1 + 1, x0:x1 + 1] * 255.0])
    return Image.fromarray(out.round().astype(np.uint8))


def ring_centre(image):
    """
    Where the hollow of the branch ring sits, as a fraction of the sprite.

    Measured rather than written down, so re-cutting a redrawn plaque moves
    Rachel with it instead of leaving her off-centre.

    Not "the transparency the outside cannot reach": the branch is twigs with
    daylight between them, so the hollow and the empty canvas around the
    plaque are one connected region and no flood can tell them apart. What
    does tell them apart is shape - the hollow is a big round gap, and the
    space around the art is thin. So this takes the largest circle that fits
    inside transparency, by distance transform, looking only at the left
    quarter where the ring is; the plaque fills everything to the right of it.
    """
    a = np.asarray(image)[:, :, 3]
    h, w = a.shape

    BIG = 1e9
    d = np.where(a < 8, BIG, 0.0)
    for y in range(h):                       # forward pass
        for x in range(w):
            if d[y, x] == 0:
                continue
            best = d[y, x]
            if y:               best = min(best, d[y - 1, x] + 1)
            if x:               best = min(best, d[y, x - 1] + 1)
            if y and x:         best = min(best, d[y - 1, x - 1] + 1.414)
            if y and x < w - 1: best = min(best, d[y - 1, x + 1] + 1.414)
            d[y, x] = best
    for y in range(h - 1, -1, -1):           # backward pass
        for x in range(w - 1, -1, -1):
            if d[y, x] == 0:
                continue
            best = d[y, x]
            if y < h - 1:                best = min(best, d[y + 1, x] + 1)
            if x < w - 1:                best = min(best, d[y, x + 1] + 1)
            if y < h - 1 and x < w - 1:  best = min(best, d[y + 1, x + 1] + 1.414)
            if y < h - 1 and x:          best = min(best, d[y + 1, x - 1] + 1.414)
            d[y, x] = best

    left = d[:, :int(w * RING_SEARCH)]
    y, x = np.unravel_index(left.argmax(), left.shape)
    radius = left[y, x]
    if radius < 8:
        return None
    return x / w, y / h, 2 * radius / w


def main():
    src = os.path.expanduser(sys.argv[1] if len(sys.argv) > 1 else "~/Downloads/Robo")
    os.makedirs(DST, exist_ok=True)

    panel = None
    for sheet, name, band, drop_shadow in PIECES:
        path = os.path.join(src, sheet + ".png")
        if not os.path.exists(path):
            print("  skipped %-22s (%s not found)" % (name, path))
            continue
        img = cut(path, band, drop_shadow)
        out = os.path.join(DST, name + ".png")
        img.save(out)
        print("  %-22s %4dx%-4d  <- %s" % (name + ".png", img.width, img.height, path))
        if name == "ui_narration_panel":
            panel = img

    if panel is not None:
        found = ring_centre(panel)
        if found:
            cx, cy, d = found
            print("\n  narration ring hollow: centre x=%.4f y=%.4f of the sprite, "
                  "face %.4f of its width across.\n  (These three are "
                  "RingCentreX / RingCentreY / RingDiameter in GameplayHUDLayout.)"
                  % (cx, cy, d))

    print("\nNow run Tools > SALINLAHI > Fix Sprite Import Settings in Unity, "
          "or the new PNGs import as plain textures and draw as white boxes.")


if __name__ == "__main__":
    main()
