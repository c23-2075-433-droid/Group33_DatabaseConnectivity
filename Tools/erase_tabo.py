"""Take the dipper off the water jar in the bathroom background, for good.

Tabo left the word list, so Bahay Scene 3 no longer places `item_tabo.png`
over the jar - and that sprite was the only thing hiding what
`fill_bathroom_background.py` left underneath it. Of the six objects that
script cut out, the tabo was the one it could not repair: the others sit
against flat tile or a flat shelf, but the dipper straddles the boundary
between the wall and the curved lid of the jar, and the per-row trick that
works on a shelf plank does not work on an ellipse. What it left behind is a
pale flat ghost across the tiles and a ragged black gouge where the lid
should be.

Two different repairs, because they are two different surfaces:

  WALL (everything above the lid, and everything to either side of the jar)
  is repetitive, so it is copied from three tile widths to the right -
  228px, a whole number of the measured 76.14px tile pitch, which puts the
  grout lines back where they belong. The copy arrives slightly too light,
  so the difference between copy and original is measured on a ring of
  surviving pixels around the hole, blurred, and added back.

  THE LID is an ellipse seen nearly edge-on, and every column of it is the
  same vertical profile - rim, lip, dark recess - just shifted up or down by
  where the ellipse's top edge falls. So a damaged column is rebuilt from
  CLEAN COLUMNS AT THE SAME HEIGHT ALONG THAT EDGE, not from the pixels
  beside it. The edge itself is fitted as a parabola through the one stretch
  of lid the dipper never covered, forced to be symmetric about the jar's
  centre.

The jar's own mirror symmetry does the rest: the dipper's handle wiped out
every clean column on the right of the lid, so those columns are rebuilt
from their mirror images on the left.

Run from the project root:

    python3 Tools/erase_tabo.py            # writes the sprite
    python3 Tools/erase_tabo.py --preview  # writes a before/after to /tmp

`item_tabo.png` is left in the project. Nothing places it any more, but it
is still what this file was removing, and the artwork it came out of is kept
as `bathroom_background_with_items.png`.
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

BG = 'Assets/Sprites/bathroom_background.png'
ORIG = 'Assets/Sprites/bathroom_background_with_items.png'

# The jar, measured off the painting: the lid spans these columns and is
# symmetric about this centre.
JAR_L, JAR_R, JAR_C = 46, 265, 156.0
LID_DEPTH = 62          # rows of lid profile to rebuild, from its top edge down
TILE_SHIFT = 228        # 3 x the 76.14px tile pitch - keeps the grout aligned


def tabo_mask(orig, cur):
    """Where the dipper used to be: the two plates differ nowhere else."""
    m = np.abs(orig - cur).max(2) > 8
    m[:, 400:] = False                      # the other five objects
    return ndimage.binary_dilation(m, np.ones((7, 7)))


def lid_edge(img, width):
    """The top of the lid, per column, as a parabola symmetric about the jar."""
    xs, ys = [], []
    for x in range(JAR_L, 112):             # the only stretch the dipper missed
        col = img[385:470, x]
        r, g, b = col[:, 0], col[:, 1], col[:, 2]
        hit = np.nonzero((r > b + 35) & (r > g + 10))[0]
        if len(hit):
            xs.append(x)
            ys.append(385 + hit[0])
    xs, ys = np.array(xs, float), np.array(ys, float)
    A = np.stack([(xs - JAR_C) ** 2, np.ones(len(xs))], 1)
    a, y0 = np.linalg.lstsq(A, ys, rcond=None)[0]
    return a * (np.arange(width) - JAR_C) ** 2 + y0


def repair_wall(img, wall, mask):
    """Clean tile, copied a whole number of tiles across and toned to fit."""
    src = np.roll(img, -TILE_SHIFT, axis=1)
    ring = ndimage.binary_dilation(wall, np.ones((41, 41))) & ~mask
    out = img.copy()
    patch = np.empty_like(img)
    for c in range(3):
        d = np.where(ring, img[..., c] - src[..., c], 0.0)
        num = ndimage.gaussian_filter(d, 22.0)
        den = ndimage.gaussian_filter(ring.astype(float), 22.0)
        patch[..., c] = src[..., c] + num / np.maximum(den, 1e-6)
    f = ndimage.gaussian_filter(wall.astype(float), 1.6)[..., None]
    return out * (1 - f) + np.clip(patch, 0, 255) * f


MARGIN = 1              # the jar's very outermost column, which is outline, not lid
SHADE = (68, 88)        # intact jar body below the lid, used to match shading


def repair_lid(img, lid, mask):
    """Rebuild the lid in ellipse coordinates.

    The lid is straightened first: row `d` of the working image is the pixels
    `d` rows below the lid's top edge, whatever that edge's height is in that
    column. In those coordinates the lid is horizontal bands - rim, lip, dark
    recess - so a hole can be filled along a row, from whichever columns of
    THAT BAND survived, instead of from the pixels above and below it, which
    belong to different parts of the lid.
    """
    out = img.copy()
    top = np.round(lid).astype(int)
    cols = np.arange(JAR_L, JAR_R)
    depths = np.arange(-4, LID_DEPTH)

    # straighten
    rows = top[cols][None, :] + depths[:, None]
    flat = img[rows, cols[None, :], :]
    hole = mask[rows, cols[None, :]]

    usable = ~hole
    usable[:, :MARGIN] = False
    usable[:, -MARGIN:] = False

    # The jar narrows towards its sides, so a column near the right edge is
    # not the same shape as one in the middle: copying the middle's rim
    # outwards paints a tab of lid sticking out past the jar. Only the
    # mirror column has the right silhouette, so it is strongly preferred -
    # and the lighting difference between the lit left and the shaded right
    # is taken out by comparing the intact jar body below the lid in both.
    lum = np.array([img[top[x] + SHADE[0]:top[x] + SHADE[1], x, :].mean()
                    for x in cols])

    for d in range(flat.shape[0]):
        good = np.nonzero(usable[d])[0]
        bad = np.nonzero(hole[d])[0]
        if not len(good) or not len(bad):
            continue
        for i in bad:
            left = good[good < i]
            right = good[good > i]
            mirror = int(round(2 * (JAR_C - JAR_L) - i))
            picks = []
            if len(left):
                picks.append((i - left[-1], left[-1]))
            if len(right):
                picks.append((right[0] - i, right[0]))
            if 0 <= mirror < flat.shape[1] and usable[d, mirror]:
                picks.append((2.0, mirror))
            w = np.array([1.0 / (dist + 4.0) for dist, _ in picks])
            w /= w.sum()
            val = np.zeros(3)
            for wi, (_, j) in zip(w, picks):
                k = np.clip(lum[i] / max(lum[j], 1e-6), 0.75, 1.35)
                val += wi * flat[d, j, :] * k
            flat[d, i, :] = val

    out[rows, cols[None, :], :] = flat

    touched = np.zeros(mask.shape, bool)
    touched[rows, cols[None, :]] = hole
    sm = np.dstack([ndimage.gaussian_filter(out[..., c], 1.1) for c in range(3)])
    f = ndimage.gaussian_filter(touched.astype(float), 1.1)[..., None]
    return out * (1 - f) + sm * f


def main():
    cur = np.asarray(Image.open(BG).convert('RGB'), float)
    orig = np.asarray(Image.open(ORIG).convert('RGB'), float)
    h, w, _ = cur.shape

    mask = tabo_mask(orig, cur)
    lid = lid_edge(cur, w)
    yy, xx = np.mgrid[0:h, 0:w]
    # Only the jar's own columns have a lid. Everything else the dipper
    # touched - including the tiles its handle reached across, to the right
    # of the jar - is wall, however far down the row is.
    on_jar = (xx >= JAR_L) & (xx < JAR_R)
    lid_px = mask & on_jar & (yy >= lid[None, :] - 2)
    wall_px = mask & ~lid_px

    out = repair_wall(cur, wall_px, mask)
    out = repair_lid(out, lid, lid_px)
    out = np.clip(out, 0, 255).astype(np.uint8)

    if '--preview' in sys.argv:
        box = (16, 300, 336, 500)
        bw, bh, s = box[2] - box[0], box[3] - box[1], 3
        sheet = Image.new('RGB', (bw * s, bh * s * 2 + 10), (255, 0, 0))
        for i, im in enumerate([Image.open(ORIG).convert('RGB'),
                                Image.fromarray(out)]):
            sheet.paste(im.crop(box).resize((bw * s, bh * s), Image.LANCZOS),
                        (0, i * (bh * s + 10)))
        sheet.save('/tmp/erase_tabo_preview.png')
        print('preview -> /tmp/erase_tabo_preview.png')
        return

    Image.fromarray(out).save(BG)
    print('wrote %s - the dipper is gone from the jar' % BG)


if __name__ == '__main__':
    main()
