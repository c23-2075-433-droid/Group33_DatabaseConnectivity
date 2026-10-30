"""Cut the five Teacher Rachel frames for the narration ring.

Rachel is drawn head-to-chest and sits inside the hollow branch ring of
Assets/Sprites/ui_narration_panel.png, behind it, with the room showing
through around her - see cut_rachel_portrait.py, which cut the first one off
an old mockup. These frames are drawn instead: an idle face, three mouth
shapes and a blink, generated in Canva from one approved character and left
in Art/ exactly as they arrived.

The whole job of this script is REGISTRATION. Five separately generated
images are five slightly different drawings: a head a few pixels higher, a
shoulder a per cent wider. Shown one after another at nine frames a second,
that drift does not read as talking - it reads as a glitch. So:

  * The placement is measured ONCE, off the idle frame, and every other
    frame is laid down with that same transform rather than its own.
  * Each mouth frame is then nudged - a small search over shift and scale -
    to the position where its silhouette overlaps the idle's most closely.
    Only her mouth should move between frames, and this is what makes that
    true of the finished sprites rather than merely of the prompts.

Two things carried over from cut_rachel_portrait.py, for the same reasons it
gives: the sprite is written on the RING'S OWN SQUARE rather than cropped to
her, so UITheme's RingCentreX / RingCentreY / RingDiameter place her with no
further measuring; and the cut stops a little inside the circle (CLIP), where
the branch covers the seam.

The raw frames may arrive either already cut out or on a flat colour. Both
are handled: an image that is fully opaque has its background flooded away
from the border inwards, which cannot eat anything sealed inside her.

Reads:   Art/char_rachel_{idle,talk1,talk2,talk3,blink}_raw.png
Writes:  Assets/Sprites/char_rachel_portrait.png   (the idle - REPLACES the old one)
         Assets/Sprites/char_rachel_talk1.png
         Assets/Sprites/char_rachel_talk2.png
         Assets/Sprites/char_rachel_talk3.png
         Assets/Sprites/char_rachel_blink.png
         Art/char_rachel_portrait_previous.png     (backup, first run only)
         Art/char_rachel_frames_sheet.png          (a look at all five, not imported)

Run from the repository root:  python3 Tools/make_rachel_frames.py

Then run Tools > SALINLAHI > Refresh UI Theme in Unity, so the narration
panel picks the frames up.
"""
import os
import shutil
import sys

import numpy as np
from PIL import Image, ImageFilter

# name -> where it ends up. The idle keeps the existing sprite's path, and so
# its .meta and its GUID, which is what UITheme.asset already points at.
FRAMES = [
    ("idle",  "Assets/Sprites/char_rachel_portrait.png"),
    ("talk1", "Assets/Sprites/char_rachel_talk1.png"),
    ("talk2", "Assets/Sprites/char_rachel_talk2.png"),
    ("talk3", "Assets/Sprites/char_rachel_talk3.png"),
    ("blink", "Assets/Sprites/char_rachel_blink.png"),
]

RAW = "Art/char_rachel_%s_raw.png"
BACKUP = "Art/char_rachel_portrait_previous.png"
SHEET = "Art/char_rachel_frames_sheet.png"

# The sprite's square.
#
# The ring draws at about 178px at the 1920x1080 reference, and about 240 on
# the tallest tablet the canvas scales up for, so 256 is at or above native
# everywhere the game runs - the portrait it replaces was 236.
#
# It is also as much as there is to use: Canva's API hands an image back at
# 200px however large the drawing it generated, so writing these any bigger
# would only be an upscale wearing a larger file size. If the full-size
# originals are downloaded from Canva by hand into Art/, raise this to 512
# and run the script again.
SIZE = 256

# Stop this far inside the circle - the branch covers the band, and stopping
# short takes the last of the generated background's edge with it.
CLIP = 0.95

# How she is seated in the circle, as fractions of its diameter: how wide she
# is drawn, and how far below the top of the circle her hair starts. Drawn
# smaller than the hollow on purpose - scaled to fill it she is a head in a
# hole, and the chest the brief asks for is all below the cut.
SHOULDER_SPAN = 1.08
HEAD_ROOM = 0.10

# How far the registration search is allowed to move a frame.
SHIFT = 14          # pixels, in the sprite's own square
SCALE = 0.06        # +/- 6 per cent
POLISH = 3          # pixels swept around the measured answer


def main():
    missing = [n for n, _ in FRAMES if not os.path.exists(RAW % n)]
    if missing:
        sys.exit("Missing raw frames in Art/: " +
                 ", ".join("char_rachel_%s_raw.png" % n for n in missing))

    idle_path = FRAMES[0][1]
    if os.path.exists(idle_path) and not os.path.exists(BACKUP):
        shutil.copyfile(idle_path, BACKUP)
        print("Kept the old portrait at %s" % BACKUP)

    cut = {name: cutout(Image.open(RAW % name)) for name, _ in FRAMES}

    # The placement, measured once off the idle.
    place = measure(cut["idle"])
    print("Idle seats at scale %.3f, offset (%d, %d)" % place)

    idle_rgba = lay(cut["idle"], *place)
    idle_mask = np.asarray(idle_rgba)[:, :, 3] > 96
    idle_cut = np.asarray(clip(idle_rgba))[:, :, 3] > 96

    written = []
    for name, dst in FRAMES:
        if name == "idle":
            rgba, drift = idle_rgba, (0, 0, 1.0)
        else:
            rgba, drift = register(cut[name], place, idle_mask)

        out = clip(rgba)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        out.save(dst)
        written.append(out)

        mask = np.asarray(out)[:, :, 3] > 96
        print("%-44s %dx%d  %4.1f%% of the circle is her  "
              "nudged (%+d, %+d) x%.3f  overlap with idle %.1f%%"
              % (dst, out.width, out.height, 100.0 * mask.sum() / disc().sum(),
                 drift[0], drift[1], drift[2], 100.0 * iou(mask, idle_cut)))

    sheet(written)
    print("\nWrote %s to look at.\nNow run Tools > SALINLAHI > Refresh UI Theme "
          "in Unity, so the narration panel picks the frames up." % SHEET)


# ----------------------------------------------------------------------
# lifting her off whatever she arrived on
# ----------------------------------------------------------------------

def cutout(im):
    """
    Returns her as RGBA with a real alpha channel.

    An image that already has one is taken as it is. One that does not is on
    a flat colour, and the colour is flooded away from the BORDER inwards:
    starting outside means a patch of background colour sealed inside her -
    a glint in an eye, a highlight on her collar - is never reached, which is
    the same trick cut_rachel_portrait.py uses to keep her teeth.
    """
    im = im.convert("RGBA")
    a = np.asarray(im).astype(int)
    if (a[:, :, 3] < 250).mean() > 0.02:
        return im

    rgb = a[:, :, :3]
    h, w, _ = rgb.shape

    # The background colour, as the four corners agree on it.
    corners = np.concatenate([rgb[:8, :8].reshape(-1, 3), rgb[:8, -8:].reshape(-1, 3),
                              rgb[-8:, :8].reshape(-1, 3), rgb[-8:, -8:].reshape(-1, 3)])
    key = np.median(corners, axis=0)
    near = np.abs(rgb - key).sum(axis=2) < 130

    out = flood_from_border(near)

    # Bite one more pixel off her, scaled to how big the drawing is. The rim
    # where the key colour was blended into her outline is a halo that is
    # invisible on the colour it came from and obvious over a bedroom wall;
    # taking the blended pixels away is surer than trying to correct them.
    out = grow(out, max(1, int(round(min(h, w) / 400.0))))

    alpha = Image.fromarray(np.where(out, 0, 255).astype(np.uint8))
    alpha = alpha.filter(ImageFilter.GaussianBlur(0.7))

    rgba = im.copy()
    rgba.putalpha(alpha)
    return unfringe(rgba, key)


def flood_from_border(near):
    """Everything of the background colour reachable from the edge of the image."""
    h, w = near.shape
    out = np.zeros((h, w), bool)
    stack = []

    for x in range(w):
        for y in (0, h - 1):
            if near[y, x]:
                stack.append((y, x))
                out[y, x] = True
    for y in range(h):
        for x in (0, w - 1):
            if near[y, x] and not out[y, x]:
                stack.append((y, x))
                out[y, x] = True

    # Grown a row at a time with numpy rather than pixel by pixel: a 1264
    # square is 1.6 million pixels and a Python queue over that is slow
    # enough to be worth avoiding.
    while True:
        grown = out.copy()
        grown[1:, :] |= out[:-1, :]
        grown[:-1, :] |= out[1:, :]
        grown[:, 1:] |= out[:, :-1]
        grown[:, :-1] |= out[:, 1:]
        grown &= near
        if grown.sum() == out.sum():
            return out
        out = grown


def grow(mask, rounds):
    """Spreads a mask outwards by whole pixels."""
    for _ in range(rounds):
        grown = mask.copy()
        grown[1:, :] |= mask[:-1, :]
        grown[:-1, :] |= mask[1:, :]
        grown[:, 1:] |= mask[:, :-1]
        grown[:, :-1] |= mask[:, 1:]
        mask = grown
    return mask


def unfringe(rgba, key):
    """
    Pulls the keyed colour out of the half-transparent rim.

    A green screen leaves a green halo one or two pixels wide where the edge
    was blended. It is invisible on green and obvious over a bedroom wall, so
    those pixels are pushed back towards their own colour before she is ever
    drawn on anything else.
    """
    a = np.asarray(rgba).astype(float)
    alpha = a[:, :, 3:4] / 255.0
    edge = ((alpha > 0.05) & (alpha < 0.95))
    if not edge.any():
        return rgba

    pull = np.where(edge, 1.0 - alpha, 0.0)
    a[:, :, :3] = np.clip(a[:, :, :3] - pull * (key - a[:, :, :3]), 0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


# ----------------------------------------------------------------------
# seating her in the circle
# ----------------------------------------------------------------------

def measure(im):
    """Scale and offset that seat the idle frame's silhouette in the circle."""
    box = Image.fromarray((np.asarray(im)[:, :, 3] > 96).astype(np.uint8) * 255).getbbox()
    if box is None:
        sys.exit("The idle frame is empty once its background is cut away.")

    x0, y0, x1, y1 = box
    scale = (SHOULDER_SPAN * SIZE) / float(x1 - x0)

    # Centred on where she actually IS rather than on the middle of the box
    # around her: a shoulder further out on one side than the other drags a
    # box sideways, and the eye reads her as off-centre in the ring. The
    # weight of the silhouette does not have that problem.
    mid = centroid(np.asarray(im)[:, :, 3] > 96)[1]

    dx = SIZE / 2.0 - mid * scale
    dy = HEAD_ROOM * SIZE - y0 * scale
    return scale, int(round(dx)), int(round(dy))


def lay(im, scale, dx, dy):
    """Draws one cut-out onto the sprite's square at the given placement."""
    w = max(1, int(round(im.width * scale)))
    h = max(1, int(round(im.height * scale)))
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    canvas.alpha_composite(im.resize((w, h), Image.LANCZOS), (dx, dy))
    return canvas


def register(im, place, idle_mask):
    """
    Lays a frame down on the idle's placement, then nudges it onto the idle.

    Measured, not searched. Two numbers say almost everything about how two
    drawings of the same person differ: how much of the frame she covers, and
    where the middle of her is. Matching the AREA gives the scale, matching
    the CENTROID then gives the shift, and both are one pass over the pixels.

    A mouth opening barely changes either - it is interior to her silhouette -
    so this corrects the drift between generations without being dragged
    about by the thing that is supposed to move. A last few-pixel sweep
    polishes what rounding left.
    """
    scale, dx, dy = place

    here = np.asarray(lay(im, scale, dx, dy))[:, :, 3] > 96
    if not here.any():
        return lay(im, scale, dx, dy), (0, 0, 1.0)

    ds = float(np.sqrt(idle_mask.sum() / float(here.sum())))
    ds = float(np.clip(ds, 1.0 - SCALE, 1.0 + SCALE))

    scaled = np.asarray(lay(im, scale * ds, dx, dy))[:, :, 3] > 96
    if not scaled.any():
        scaled, ds = here, 1.0

    ey, ex = (centroid(idle_mask) - centroid(scaled)).round().astype(int)
    ex = int(np.clip(ex, -SHIFT, SHIFT))
    ey = int(np.clip(ey, -SHIFT, SHIFT))

    best = (ex, ey, ds)
    best_score = iou(shift(scaled, ex, ey), idle_mask)

    for px in range(ex - POLISH, ex + POLISH + 1):
        for py in range(ey - POLISH, ey + POLISH + 1):
            score = iou(shift(scaled, px, py), idle_mask)
            if score > best_score:
                best_score, best = score, (px, py, ds)

    ex, ey, ds = best
    return lay(im, scale * ds, dx + ex, dy + ey), best


def centroid(mask):
    """The middle of a silhouette, as (row, column)."""
    ys, xs = np.nonzero(mask)
    return np.array([ys.mean(), xs.mean()])


def shift(mask, dx, dy):
    """Moves a mask by whole pixels, filling what it leaves behind with nothing."""
    out = np.zeros_like(mask)
    sx0, sx1 = max(0, -dx), mask.shape[1] - max(0, dx)
    sy0, sy1 = max(0, -dy), mask.shape[0] - max(0, dy)
    out[sy0 + dy:sy1 + dy, sx0 + dx:sx1 + dx] = mask[sy0:sy1, sx0:sx1]
    return out


def frange(lo, hi, step):
    out, v = [], lo
    while v <= hi + 1e-9:
        out.append(v)
        v += step
    return out


def iou(a, b):
    union = (a | b).sum()
    return 0.0 if union == 0 else (a & b).sum() / float(union)


def disc(clip=CLIP):
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    c = (SIZE - 1) / 2.0
    return ((xx - c) ** 2 + (yy - c) ** 2) <= (clip * SIZE / 2.0) ** 2


def clip(rgba):
    """Trims the frame to the ring's circle, with a soft edge."""
    mask = Image.fromarray((disc() * 255).astype(np.uint8))
    mask = mask.filter(ImageFilter.GaussianBlur(0.8))

    a = np.asarray(rgba).astype(float)
    a[:, :, 3] = a[:, :, 3] * (np.asarray(mask).astype(float) / 255.0)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def sheet(frames):
    """All five side by side, to check at a glance that only the mouth moves."""
    strip = Image.new("RGBA", (SIZE * len(frames), SIZE), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        strip.alpha_composite(f, (i * SIZE, 0))
    strip.save(SHEET)


if __name__ == "__main__":
    main()
