"""Cut the wooden level buttons out of the purchased UI sheet.

The sheet is one 4375x4375 illustration on a near-black background: numbered
tiles, locked tiles, stars and arrows. key_ui_sprites.py cannot be used on it,
because that one keys out WHITE - here the background is the dark end of the
range and the artwork's own outline is the light end.

The tiles come with their stars attached underneath. Stars would have to mean
something to be worth drawing, and nothing in the game scores a scene out of
three yet, so each tile is cut at the neck between the tile and its stars. The
neck is found by measuring, not guessed: it is the narrowest row in the lower
part of the shape.

Run from the repository root:  python3 Tools/slice_level_buttons.py
Reads  Art/level_buttons_raw.jpg
Writes Assets/Sprites/btn_level_*.png and ui_select_levels.png
"""
from PIL import Image, ImageFilter
import numpy as np
from scipy import ndimage
import io, os, re, uuid

SRC = 'Art/level_buttons_raw.jpg'
SPRITES = 'Assets/Sprites'
META_MODEL = 'item_tabo'

LIT = 70        # above this counts as artwork rather than backdrop
DARK = 58       # below this, and reachable from the edge, counts as backdrop
FEATHER = 1.0

# Column bands of the four tiles in a row, measured off the sheet.
COLS = [(530, 1241), (1374, 2084), (2233, 2944), (3102, 3813)]
ROW_TOPS = [922, 1928, 2839]

# Which cell holds which sprite. Row 2 is all padlocks; one is enough.
TILES = {
    (0, 0): 'btn_level_1', (0, 1): 'btn_level_2',
    (0, 2): 'btn_level_3', (0, 3): 'btn_level_4',
    (1, 0): 'btn_level_5', (1, 1): 'btn_level_6',
    (1, 2): 'btn_level_7', (1, 3): 'btn_level_locked',
}

# Sprites that are not tiles, so have no stars to cut away.
PLAIN = {
    'ui_select_levels': (755, 292, 3586, 719),
}


def lit_mask(a):
    return a.mean(2) > LIT


def band_bottom(mask, x0, x1, y0):
    """Where the tile stops and its stars start."""
    sub = mask[y0:, x0:x1]
    rows = np.where(sub.sum(1) > 3)[0]
    # the shape runs from y0 to the first blank row after it
    end = rows[0]
    for r in rows:
        if r - end > 8:
            break
        end = r
    height = end + 1

    widths = []
    for r in range(height):
        idx = np.where(sub[r])[0]
        widths.append(idx.max() - idx.min() + 1 if len(idx) else 0)
    widths = np.array(widths)

    lower = int(height * 0.55)
    neck = lower + int(np.argmin(widths[lower:]))
    return y0 + neck


def cut(im, box):
    """Crop, drop the backdrop, trim to the artwork."""
    crop = im.crop(box).convert('RGBA')
    a = np.asarray(crop).astype(float)
    lum = a[..., :3].mean(2)

    dark = lum < DARK
    lab, n = ndimage.label(dark)
    edge = set(lab[0].tolist()) | set(lab[-1].tolist()) | \
           set(lab[:, 0].tolist()) | set(lab[:, -1].tolist())
    edge.discard(0)
    # Only backdrop reaches the border. The padlock and the wood grain are
    # dark too, but the artwork's pale outline seals them in.
    bg = np.isin(lab, list(edge))

    alpha = ndimage.gaussian_filter(np.where(bg, 0.0, 255.0), FEATHER)
    out = a.copy()
    out[..., 3] = np.clip(alpha, 0, 255)

    ys, xs = np.where(out[..., 3] > 8)
    pad = 2
    y0, y1 = max(0, ys.min() - pad), min(out.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(out.shape[1], xs.max() + 1 + pad)
    return Image.fromarray(out[y0:y1, x0:x1].astype(np.uint8))


def write_meta(name):
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, META_MODEL + '.png.meta'),
                   encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex,
               base, count=1, flags=re.M)
    t = t.replace(META_MODEL + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)


def main():
    im = Image.open(SRC).convert('RGB')
    mask = lit_mask(np.asarray(im).astype(float))

    for (row, col), name in sorted(TILES.items()):
        x0, x1 = COLS[col]
        y0 = ROW_TOPS[row]
        y1 = band_bottom(mask, x0, x1, y0)
        spr = cut(im, (x0 - 6, y0 - 6, x1 + 6, y1))
        spr.save(os.path.join(SPRITES, name + '.png'))
        write_meta(name)
        print('  %-18s %dx%d  (cut at y=%d)' % (name + '.png', spr.width, spr.height, y1))

    for name, box in PLAIN.items():
        spr = cut(im, box)
        spr.save(os.path.join(SPRITES, name + '.png'))
        write_meta(name)
        print('  %-18s %dx%d' % (name + '.png', spr.width, spr.height))


if __name__ == '__main__':
    main()
