"""Cut the folded uniform and the socks out of the painted bed art.

Art/bed_clothes_raw.png is the Scene 4 bedroom with the clothes painted onto
the bed - the same frame the game draws, repainted. Everything in it except
the clothes is bedroom_background.png, which this repository already has, so
the two can be SUBTRACTED and whatever is left is the thing to cut. That is
far better than keying on colour: white cloth on a cream sheet does not
separate on colour, and the soft shadow the clothes cast on the blanket does
not separate on anything - but both come straight out of a difference.

How the two images are lined up: the raw art is a crop of the room at 2.48x,
found by normalised cross-correlation of the headboard and pillow against
the background (0.983, which is as good as a JPEG round-trip allows). The
offset is then refined to the pixel on a strip of floor with no clothes in
it. Both numbers are printed when this runs - if the raw art is ever
replaced, check them rather than trusting the constants.

THE SHADOW IS PART OF THE CUT, BUT ONLY THE DARKENING. Under the clothes
the blanket is darkened rather than covered, so those pixels differ by a
little instead of a lot. Taking only the strong difference gives a sprite
that floats. Taking the whole weak difference is worse: the raw art is a
repaint, so the blanket differs slightly EVERYWHERE, and the first cut came
out wearing a yellow halo of other people's blanket.

So a shadow pixel has to be DARKER than the background, not merely
different, and what is kept is how much darker - painted as neutral brown at
that strength rather than as the colour that was there. A shadow built that
way is the right shadow on any blanket, not a photograph of this one.

Writes, overwriting:

  Assets/Sprites/prop_damit_folded.png    white shirt folded on navy shorts
  Assets/Sprites/prop_medyas_folded.png   the pair of socks, lying flat

and prints where each belongs in world units, which is where the constants
in Assets/Editor/BuildBahayScene4.cs and Tools/place_uniform_on_bed.py come
from. Nothing here writes to the scene; place_uniform_on_bed.py does that.

This replaces Tools/make_folded_uniform.py, which DREW the folded uniform
out of rounded rectangles. Four passes of that never stopped looking like
furniture, because a drawn prop has to earn the painted room's lighting and
perspective from scratch and this one is painted into it already.

Run from the repository root:  python3 Tools/cut_folded_uniform.py
"""
from PIL import Image
from scipy import ndimage
import numpy as np
import io, os, re, uuid

RAW = "Art/bed_clothes_raw.png"
SPRITES = "Assets/Sprites"
BACKGROUND = "Assets/Sprites/bedroom_background.png"
MODEL = "prop_medyas"              # the sprite whose import settings these copy

SCALE = 2.48                       # the raw art's zoom over the background
ORIGIN = (0, 719)                  # where it sits in the scaled background

# How the background maps to the world. Same numbers as BuildBahayScene4:
# the room art imports at 169 pixels per unit and is drawn at 1.9458.
UNITS_PER_PIXEL = 1.9458 / 169.0
BG_CENTRE = (840.0, 472.0)
PPU = 100.0                        # what these sprites import at

STRONG = 60.0                      # cloth: covers the bed outright
DARKEN = 8.0                       # shadow: this much darker than the bed
DARKEST = 52.0                     # as dark as the shadow is ever taken to be
HALO = 20                          # how far from the cloth a shadow may reach
SHADOW_RGB = (58, 38, 26)          # what the shadow is painted in
SHADOW_MAX = 165                   # and how opaque it gets at DARKEST
BLANKET = 45.0                     # this much redder than blue is the blanket

# Each piece, and a box in raw-art pixels that contains it and nothing else.
PIECES = [
    ("prop_damit_folded", (590, 520, 920, 740)),
    ("prop_medyas_folded", (900, 560, 1070, 730)),
]


def aligned_background(raw_size):
    bg = Image.open(BACKGROUND).convert("RGB")
    big = bg.resize((int(round(bg.size[0] * SCALE)), int(round(bg.size[1] * SCALE))),
                    Image.LANCZOS)
    x0, y0 = ORIGIN
    w, h = raw_size
    return np.asarray(big.crop((x0, y0, x0 + w, y0 + h))).astype(np.float32)


def meta_for(name):
    """Give a new sprite the same import settings as the one it sits beside."""
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, MODEL + ".png.meta"), encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(MODEL + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def world_of(raw_x, raw_y):
    """Raw-art pixel -> world units."""
    bx = (raw_x + ORIGIN[0]) / SCALE
    by = (raw_y + ORIGIN[1]) / SCALE
    return ((bx - BG_CENTRE[0]) * UNITS_PER_PIXEL,
            (BG_CENTRE[1] - by) * UNITS_PER_PIXEL)


def main():
    raw = Image.open(RAW).convert("RGB")
    R = np.asarray(raw).astype(np.float32)
    C = aligned_background(raw.size)

    # A strip of floor below the bed, where nothing was repainted, says how
    # well the two line up. Anything much over ~15 means the alignment
    # constants are stale.
    check = np.abs(C[820:1000, 80:1400] - R[820:1000, 80:1400]).mean()
    print("  alignment residual %.2f (on bare floor; under ~15 is right)" % check)

    diff = np.abs(C - R).max(2)

    for name, box in PIECES:
        x0, y0, x1, y1 = box
        inbox = np.zeros(diff.shape, bool)
        inbox[y0:y1, x0:x1] = True

        # Blanket that changed enough to pass STRONG is still blanket. The
        # uniform is white (r = b) and navy (b > r); only the bed is orange,
        # so one channel test drops the specks of it that came through, and
        # they are what made the first cut look like a torn sticker.
        blanket = (R[..., 0] - R[..., 2]) > BLANKET

        core = inbox & (diff > STRONG) & ~blanket
        core = ndimage.binary_opening(core, np.ones((5, 5)))
        core = ndimage.binary_closing(core, np.ones((5, 5)))
        core = ndimage.binary_fill_holes(core)
        lab, n = ndimage.label(core)
        if n == 0:
            raise SystemExit("%s: nothing found in %s" % (name, box))
        sizes = ndimage.sum(core, lab, range(1, n + 1))
        core = lab == (int(np.argmax(sizes)) + 1)

        # The shadow: darker than the bed, and only just beside the cloth.
        near = ndimage.distance_transform_edt(~core) <= HALO
        darker = (C.mean(2) - R.mean(2))
        shadow = inbox & near & ~core & (darker > DARKEN)

        alpha = np.zeros(diff.shape, np.float32)
        alpha[shadow] = np.clip((darker[shadow] - DARKEN) / (DARKEST - DARKEN),
                                0, 1) * SHADOW_MAX
        alpha = ndimage.gaussian_filter(alpha, 1.2)
        alpha[core] = 255

        ys, xs = np.where(alpha > 3)
        pad = 2
        cx0, cy0 = max(0, xs.min() - pad), max(0, ys.min() - pad)
        cx1, cy1 = min(diff.shape[1], xs.max() + 1 + pad), min(diff.shape[0], ys.max() + 1 + pad)

        out = np.zeros((cy1 - cy0, cx1 - cx0, 4), np.uint8)
        rgb = R[cy0:cy1, cx0:cx1].copy()
        # The cloth keeps its painted colour; the shadow is neutral brown, so
        # it darkens whatever it is laid on instead of tinting it yellow.
        rgb[~core[cy0:cy1, cx0:cx1]] = SHADOW_RGB
        out[..., :3] = rgb.astype(np.uint8)
        out[..., 3] = np.clip(alpha[cy0:cy1, cx0:cx1], 0, 255).astype(np.uint8)
        Image.fromarray(out).save(os.path.join(SPRITES, name + ".png"))
        meta_for(name)

        # Where it belongs: the centre of what was cut, and how tall it is.
        wx, wy = world_of((cx0 + cx1) / 2.0, (cy0 + cy1) / 2.0)
        height = (cy1 - cy0) / SCALE * UNITS_PER_PIXEL
        print("  %-20s %dx%d px   world (%.3f, %.3f)   %.3f units tall"
              % (name + ".png", cx1 - cx0, cy1 - cy0, wx, wy, height))


main()
