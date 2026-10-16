"""Cut the rating stars out of the purchased sheet, and grey the spare set.

The sheet shows the trio four times over - none, one, two then three gold -
so the bottom row has all three in their designed arrangement: a small one
either side of a larger, slightly raised middle. Those three are what gets
cut, keeping that composition rather than scaling one star three times.

The sheet's own empty stars are drawn on a checkerboard that is baked into the
JPEG, and they are white, so there is no reliable way to tell the star from the
squares behind it. The empty set is made here instead, by draining the colour
out of the gold ones. That also guarantees the two states share a silhouette
exactly, so a star does not shift by a pixel as it fills in.

Neither key_ui_sprites.py nor slice_level_buttons.py fits: one keys out white,
the other near-black, and this background is teal.

Run from the repository root:  python3 Tools/slice_stars.py
Reads  Art/stars_raw.jpg
Writes Assets/Sprites/star_{gold,grey}_{left,mid,right}.png
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import io, os, re, uuid

SRC = 'Art/stars_raw.jpg'
SPRITES = 'Assets/Sprites'
META_MODEL = 'item_tabo'

# The all-gold row, measured off the sheet.
SLOTS = {
    'left':  (350, 3478, 801, 3943),
    'mid':   (837, 3186, 1526, 3846),
    'right': (1561, 3465, 2014, 3930),
}

FEATHER = 1.2
# The grey star's lightest and darkest tone. The gold runs from a dark amber
# outline to a white highlight; this is the range it is remapped onto.
GREY_FLOOR, GREY_CEIL = 152.0, 253.0
GREY_COOL = 1.03          # a touch of blue, so it reads as unlit rather than dirty


def keep_mask(rgb):
    """Gold and its highlights, never the teal backdrop or its shadow.

    Gold runs red >= green >= blue. Teal is the other way round, and so is the
    shadow it casts, which is only a darker teal - so the test throws the
    shadow away with the background instead of leaving a blue-grey smear under
    a star that will sit on brown wood.
    """
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    warm = (r >= g - 6) & (g >= b - 6) & (r > 100)
    highlight = (r > 200) & (g > 200) & (b > 195)
    return warm | highlight


def cut(a, box):
    x0, y0, x1, y1 = box
    rgb = a[y0:y1, x0:x1].astype(float)
    keep = keep_mask(rgb)

    # One star per crop: drop any speck that is not the main shape.
    lab, n = ndimage.label(keep)
    if n > 1:
        sizes = ndimage.sum(keep, lab, range(1, n + 1))
        keep = lab == (int(np.argmax(sizes)) + 1)
    keep = ndimage.binary_fill_holes(keep)

    alpha = ndimage.gaussian_filter(np.where(keep, 255.0, 0.0), FEATHER)
    out = np.dstack([rgb, np.clip(alpha, 0, 255)])

    ys, xs = np.where(out[..., 3] > 8)
    pad = 2
    yy0, yy1 = max(0, ys.min() - pad), min(out.shape[0], ys.max() + 1 + pad)
    xx0, xx1 = max(0, xs.min() - pad), min(out.shape[1], xs.max() + 1 + pad)
    return out[yy0:yy1, xx0:xx1]


def to_grey(star):
    """Drain the gold out, keeping every shade's place in the range."""
    out = star.copy()
    rgb = out[..., :3]
    lum = rgb @ np.array([0.299, 0.587, 0.114])
    lo, hi = lum.min(), lum.max()
    span = max(hi - lo, 1.0)
    level = GREY_FLOOR + (lum - lo) * (GREY_CEIL - GREY_FLOOR) / span
    out[..., 0] = level
    out[..., 1] = level
    out[..., 2] = np.clip(level * GREY_COOL, 0, 255)
    return out


def write_meta(name):
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, META_MODEL + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(META_MODEL + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)


def save(arr, name):
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).save(
        os.path.join(SPRITES, name + '.png'))
    write_meta(name)
    return arr.shape[1], arr.shape[0]


def main():
    a = np.asarray(Image.open(SRC).convert('RGB'))
    for slot, box in SLOTS.items():
        star = cut(a, box)
        w, h = save(star, 'star_gold_' + slot)
        save(to_grey(star), 'star_grey_' + slot)
        print('  star_gold_%-5s / star_grey_%-5s  %dx%d' % (slot, slot, w, h))


if __name__ == '__main__':
    main()
