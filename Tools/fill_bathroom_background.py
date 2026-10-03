"""Repaint the bathroom background where the loose objects were cut out.

Filling these holes from their own edges does not work: every object rests on
something, so the nearest surviving pixel below it is shelf wood or the dark
inside of the water jar, and that colour spreads up over the tiles.

What the wall actually is, is repetitive, so each hole is copied from a clean
stretch of the same wall at the same height, offset by a whole number of tile
widths (76-78px, measured) to put the grout lines back where they belong.
Below the surface an object sits on - the shelf plank, the jar lid - a copy
from sideways would be wrong, so those rows are interpolated along each row
instead, which suits a plank or a lid since both are even along their length.

The copy also has to be toned: the wall is lit slightly unevenly, so a patch
taken from 157px away arrives too light and leaves a visible ghost. The
difference between the two is measured on the surviving pixels around the
hole and blended back in.
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import os, json

# the original painting, before the objects were taken out of it
SRC = 'Assets/Sprites/bathroom_background_with_items.png'
ITEMS = 'Tools/out_items'
OUT = 'Tools/out_items'

# shift: where to copy the wall from (+ = from the right). A whole number of
# tile widths, far enough that the source never overlaps the hole.
# surface_y: rows at or below this are interpolated along the row instead,
# because they are the shelf plank or the jar lid, not wall.
# shift: where to copy the wall from (+ = from the right), and it must be a
# whole number of tile widths or the copied grout lines land beside the real
# ones and the patch reads as a ghost. Grout sits at x = 19.4 + 76.14k across
# most of the wall, so the usable shifts are 152, 228 and 304; near the towel
# the painted spacing is a little wider and 157 lines up better. Each shift is
# also chosen so the source is clean wall at those rows - not the mirror, the
# shelf or the jar.
# surface_y: rows at or below this are filled along the row instead, because
# they are the shelf plank or the jar lid rather than wall. 'jar_rim' measures
# that boundary per column instead of taking it as a flat line.
PLAN = {
    'tabo':    dict(shift=+228, surface_y='jar_rim', grow=4, surface_fill='left'),
    'sabon':   dict(shift=-152, surface_y=357, grow=3, surface_fill='interp'),
    'suklay':  dict(shift=-152, surface_y=357, grow=3, surface_fill='interp'),
    # from the right: to the left at these rows is the mirror, not wall
    'baso':    dict(shift=+152, surface_y=357, grow=3, surface_fill='interp'),
    'sipilyo': dict(shift=+152, surface_y=None, grow=3),
    'tuwalya': dict(shift=-157, surface_y=None, grow=14),
}
BAR = dict(rows=(196, 242), x=(1298, 1395), clean_col=1388, grow=3)

def disk(r):
    y, x = np.ogrid[-r:r+1, -r:r+1]
    return x*x + y*y <= r*r

def tone_match(rgb, shifted, region, sigma=18.0):
    """Local brightness correction, measured on the pixels we are keeping.

    Only pixels where the copy already lines up are allowed to vote. Around
    the shelf items the surviving neighbours include the shelf plank, which
    does not exist at the source offset, so letting every neighbour vote
    measures wall-against-wood and swamps the correction - that is what
    turned the holes into flat olive patches.
    """
    diff = rgb - shifted
    agrees = np.abs(diff).mean(2) < 30
    known = ((~region) & agrees).astype(float)
    num = np.stack([ndimage.gaussian_filter(diff[..., c] * known, sigma) for c in range(3)], -1)
    den = ndimage.gaussian_filter(known, sigma)[..., None]
    corr = num / np.maximum(den, 1e-3)
    corr[np.broadcast_to(den, corr.shape) < 1e-2] = 0.0
    return shifted + corr

def row_fill(rgb, gap, known, mode):
    """Fill each row from its surviving pixels.

    'interp' blends across from both ends, which is right for the shelf plank
    because it is the same along its whole length. 'left' carries the pixel
    on the left across instead: the jar's lid opening is dark on the left of
    the hole and bright tile on the right, so blending the two drags a dark
    smear out over the lid where the tabo's handle used to lie.
    """
    out = rgb.astype(float).copy()
    H, W = gap.shape
    for y in range(H):
        xs = np.where(gap[y])[0]
        if not len(xs):
            continue
        ks = np.where(known[y])[0]
        if len(ks) < 2:
            continue
        for c in range(3):
            if mode == 'left':
                left = ks[ks < xs.min()]
                if not len(left):
                    continue
                out[y, xs, c] = rgb[y, left[-1], c]
            else:
                out[y, xs, c] = np.interp(xs, ks, rgb[y, ks, c])
    return out

def jar_rim(rgb, tabo_mask):
    """Height of the jar's lid rim for every column, across the tabo's gap.

    The rim is an ellipse seen slightly from above, so it is not a flat line:
    it sits near y=400 to the left of the tabo and dips lower out at the
    right, under where the handle lay. Treating it as flat fills tile rows
    with the lid's dark interior and drags a black bar out across the wall.
    """
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    jar = ~((g > r) & (g > b)) & ~tabo_mask       # jar is anything not tile green
    xs = np.arange(rgb.shape[1])
    rim = np.full(rgb.shape[1], np.nan)
    for x in range(0, 340):
        ys = np.where(jar[380:470, x])[0]
        if len(ys):
            rim[x] = 380 + ys.min()
    known = ~np.isnan(rim)
    # the tabo's own columns have no reading; bridge them from either side
    rim[~known] = np.interp(xs[~known], xs[known], rim[known])
    return ndimage.uniform_filter1d(rim, 25)

def main():
    rgb = np.asarray(Image.open(SRC).convert('RGB')).astype(float)
    H, W = rgb.shape[:2]
    info = json.load(open(os.path.join(ITEMS, 'items.json')))
    full = np.load(os.path.join(ITEMS, 'mask.npy'))

    # recover each item's own mask at full-image size
    masks = {}
    lab, n = ndimage.label(full)
    for i in range(1, n + 1):
        sel = lab == i
        yy, xx = np.where(sel)
        cx, cy = xx.mean(), yy.mean()
        best, bd = None, 1e9
        for nm, v in info.items():
            wx, wy = v['world']
            px, py = wx / 0.0125 + W / 2, H / 2 - wy / 0.0125
            d = (px - cx) ** 2 + (py - cy) ** 2
            if d < bd:
                bd, best = d, nm
        masks.setdefault(best, np.zeros((H, W), bool))
        masks[best] |= sel

    out = rgb.copy()
    all_region = np.zeros((H, W), bool)

    for name, cfg in PLAN.items():
        m = masks.get(name)
        if m is None or not m.any():
            print('  !! no mask for', name); continue
        region = ndimage.binary_dilation(m, disk(cfg['grow']))

        if name == 'tuwalya':
            # the towel's cast shadow has to go too, or a shadow is left
            # hanging on bare tile. Compare against the copy to find it, but
            # not over the bar - the bar is dark for its own reasons.
            sh = np.roll(rgb, -cfg['shift'], axis=1)
            shadow = (ndimage.binary_dilation(m, disk(40)) &
                      (rgb.mean(2) < sh.mean(2) - 12))
            shadow[:BAR['rows'][1], :] = False
            region |= shadow
            # keep the bar's ends and screws: grow only a little over it
            b0, b1 = BAR['rows']
            region[b0:b1] = ndimage.binary_dilation(m, disk(BAR['grow']))[b0:b1]

        all_region |= region

        shifted = np.roll(rgb, -cfg['shift'], axis=1)
        toned = tone_match(rgb, shifted, region)

        sy = cfg['surface_y']
        if sy == 'jar_rim':
            rim = jar_rim(rgb, m)
            yy = np.arange(H)[:, None]
            below = yy >= rim[None, :]
        elif sy is not None:
            below = np.zeros((H, W), bool); below[sy:] = True
        else:
            below = np.zeros((H, W), bool)

        wall = region & ~below
        out = np.where(wall[..., None], toned, out)

        if sy is not None:
            surf = region & below
            if surf.any():
                out = np.where(surf[..., None],
                               row_fill(out, surf, ~region, cfg.get('surface_fill', 'interp')),
                               out)

    # ---- put the towel bar back across the stretch the towel hid ----------
    b0, b1 = BAR['rows']; bx0, bx1 = BAR['x']
    band = all_region[b0:b1, bx0:bx1]
    col = rgb[b0:b1, BAR['clean_col']]
    patch = out[b0:b1, bx0:bx1]
    patch[band] = np.broadcast_to(col[:, None, :], patch.shape)[band]
    out[b0:b1, bx0:bx1] = patch

    # Soften the join. Even a correctly toned patch shows up if its edge is a
    # hard line, so the last couple of pixels blend back into what was there.
    a = ndimage.gaussian_filter(all_region.astype(float), 1.6)[..., None]
    a = np.clip((a - 0.15) / 0.7, 0, 1)
    out = out * a + rgb * (1 - a)

    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(OUT + '/bathroom_clean.png')
    print('filled %d px' % all_region.sum())

    # ---- give the towel back its shadow, on the sprite ------------------
    # The shadow was painted on the wall. Leaving it there strands a shadow
    # on bare tile once the towel is taken, so it was removed above - but
    # then the towel has none while it hangs. Carrying it on the sprite as
    # partial transparency means it comes and goes with the towel. Alpha
    # follows how much darker the original is than the repaint, so it fades
    # out at the shadow's edge instead of ending on a line.
    clean = np.clip(out, 0, 255)
    towel = masks['tuwalya']
    ys, xs = np.where(towel)
    pad = 26
    X0, Y0 = max(0, xs.min() - pad), max(0, ys.min() - pad)
    X1, Y1 = min(W, xs.max() + 1 + pad), min(H, ys.max() + 1 + pad)
    alpha = np.clip((clean[Y0:Y1, X0:X1].mean(2) - rgb[Y0:Y1, X0:X1].mean(2)) / 25.0, 0, 1)
    sprite = np.zeros((Y1 - Y0, X1 - X0, 4), np.uint8)
    sprite[..., :3] = rgb[Y0:Y1, X0:X1].astype(np.uint8)
    sprite[..., 3] = (alpha * 255).astype(np.uint8)
    sprite[..., 3] = np.maximum(sprite[..., 3], np.where(towel[Y0:Y1, X0:X1], 255, 0))
    Image.fromarray(sprite).save(os.path.join(ITEMS, 'item_tuwalya.png'))

    cx, cy = (X0 + X1 - 1) / 2.0, (Y0 + Y1 - 1) / 2.0
    info['tuwalya']['world'] = [round((cx - W / 2) * 0.0125, 3), round((H / 2 - cy) * 0.0125, 3)]
    json.dump(info, open(os.path.join(ITEMS, 'items.json'), 'w'), indent=2)
    print('towel sprite %dx%d with shadow, world %s' %
          (X1 - X0, Y1 - Y0, info['tuwalya']['world']))

    a = Image.open(SRC).convert('RGB'); b = Image.open(OUT + '/bathroom_clean.png')
    for nm, box in {'towel': (1250, 170, 1460, 560), 'jar': (0, 300, 340, 460),
                    'shelf': (840, 250, 1140, 400)}.items():
        w, h = box[2] - box[0], box[3] - box[1]
        sc = max(1, min(3, 900 // w))
        s = Image.new('RGB', (w * sc, h * sc * 2 + 10), (255, 255, 255))
        s.paste(a.crop(box).resize((w*sc, h*sc), Image.LANCZOS), (0, 0))
        s.paste(b.crop(box).resize((w*sc, h*sc), Image.LANCZOS), (0, h*sc+10))
        s.save(OUT + '/cmp_' + nm + '.png')

main()
