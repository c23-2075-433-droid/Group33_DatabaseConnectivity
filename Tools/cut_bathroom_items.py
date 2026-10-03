"""Cut the loose objects out of the painted bathroom background.

Each noun word ("Sabon", "Tabo", ...) needs its object as its own sprite so
the game can take it away when the child says the word. Everything is painted
into one flat background, so each object is segmented out, saved on its own,
and the hole it leaves is repainted (see fill_background.py).

No single rule works for all six, because the artwork re-uses colours:

  tabo / sabon / suklay  - clearly different in colour from what they sit on,
      so a background palette taken from the edge of a box around them is
      enough.
  baso (cup)             - its green is close to the tile green (185,208,164
      vs 196,213,140) but much more yellow, so it splits on g-b.
  sipilyo (toothbrush)   - near-white against the yellow wall; splits on blue.
  tuwalya (towel)        - cream (249,219,167) against a yellow wall
      (252,219,138) is nearly the same colour, and its dark outline is no
      darker than the tile grout, so neither colour nor a dark-edge threshold
      works. Its contour is tracked row by row instead, each row searching
      near where the previous row's edge was.
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import os, json

# the original painting, before the objects were taken out of it
SRC = 'Assets/Sprites/bathroom_background_with_items.png'
OUT = 'Tools/out_items'

PALETTE = {
    'tabo':   dict(box=(108, 324, 302, 424), thr=34),
    'sabon':  dict(box=(860, 330, 926, 366), thr=30),
    'suklay': dict(box=(930, 336, 1048, 366), thr=30),
}
CHANNEL = {
    # Cup: yellow-green (g-b about 60-80) where the tile is blue-green (44).
    # b>90 is what keeps the shelf's wood out, since wood shares the cup's
    # g-b. That also drops the cup's own dark rim (b about 70), which is why
    # the mask is closed and its holes filled: the rim is enclosed by the
    # lighter cup around it and comes back that way. Dropping the old g>150
    # test matters too - it cut the cup's shaded right edge into notches.
    'baso':    dict(box=(1058, 308, 1112, 366), close=4, grow=3,
                    rule=lambda r, g, b: (g - b > 55) & (b > 90)),
    # Toothbrush: silver, so barely saturated, against a strongly saturated
    # yellow wall and brown rail. The box stops above the tiles, which are
    # unsaturated enough to pass the same test. Its head and handle are split
    # by a dark outline, so parts are kept by size rather than keeping only
    # the largest - that was losing the handle and leaving a floating head.
    'sipilyo': dict(box=(1092, 276, 1120, 314), close=3, keep_all=25, grow=2,
                    rule=lambda r, g, b: ((np.maximum.reduce([r, g, b]) -
                                           np.minimum.reduce([r, g, b])) < 60) &
                                         ((r + g + b) / 3 > 70)),
}
TOWEL = dict(seed_y=240, seed=(1325, 1370), y_top=190, y_bot=503, win=7)

def disk(r):
    y, x = np.ogrid[-r:r+1, -r:r+1]
    return x*x + y*y <= r*r

def clean(m, r=2):
    m = ndimage.binary_closing(m, disk(r))
    m = ndimage.binary_fill_holes(m)
    lab, n = ndimage.label(m)
    if n == 0:
        return m
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    return ndimage.binary_fill_holes(lab == (int(np.argmax(sizes)) + 1))

def seg_palette(rgb, box, thr):
    x0, y0, x1, y1 = box
    sub = rgb[y0:y1, x0:x1].astype(float)
    h, w = sub.shape[:2]
    ring = np.zeros((h, w), bool)
    ring[0:2, :] = ring[-2:, :] = ring[:, 0:2] = ring[:, -2:] = True
    pal = np.unique(sub[ring].reshape(-1, 3) // 8 * 8, axis=0).astype(float)
    d = np.linalg.norm(sub[:, :, None, :] - pal[None, None, :, :], axis=3).min(2)
    return clean(d > thr, 3)

def seg_channel(rgb, box, rule, close=2, keep_all=None, grow=0):
    x0, y0, x1, y1 = box
    sub = rgb[y0:y1, x0:x1].astype(float)
    m = rule(sub[..., 0], sub[..., 1], sub[..., 2])
    if keep_all is None:
        m = clean(m, close)
    else:
        m = ndimage.binary_fill_holes(ndimage.binary_closing(m, disk(close)))
        lab, n = ndimage.label(m)
        out = np.zeros_like(m)
        for i in range(1, n + 1):
            sel = lab == i
            if sel.sum() >= keep_all:
                out |= sel
        m = out
    if grow:
        # These rules match an object's fill but not the near-black line drawn
        # around it, so without this the outline stays on the wall and the
        # sprite sits in a pale halo of its own missing edge.
        m = ndimage.binary_dilation(m, disk(grow))
    return m

def seg_towel(rgb):
    """Follow the towel's outline down (then up) from a row we know."""
    lum = rgb.mean(2)
    cfg = TOWEL
    win = cfg['win']
    edges = {cfg['seed_y']: cfg['seed']}

    def track(ys, start, narrowing):
        l, r = start
        for y in ys:
            # the darkest pixel near where this edge was on the previous row
            lo = max(0, l - win); hi = min(lum.shape[1] - 1, l + win)
            nl = lo + int(np.argmin(lum[y, lo:hi + 1]))
            lo = max(0, r - win); hi = min(lum.shape[1] - 1, r + win)
            nr = lo + int(np.argmin(lum[y, lo:hi + 1]))
            if narrowing:
                # Going up, the towel only ever gets narrower. Without this the
                # tracker jumps onto the wooden rail the towel hangs over,
                # which is darker and much wider, and the rail comes away as
                # part of the towel.
                nl, nr = max(nl, l), min(nr, r)
            else:
                nl, nr = min(nl, l + 2), max(nr, r - 2)
            l, r = nl, nr
            if r - l < 2:
                break
            edges[y] = (l, r)

    track(range(cfg['seed_y'] + 1, cfg['y_bot'] + 1), cfg['seed'], False)
    track(range(cfg['seed_y'] - 1, cfg['y_top'] - 1, -1), cfg['seed'], True)

    m = np.zeros(lum.shape, bool)
    for y, (l, r) in edges.items():
        m[y, l:r + 1] = True
    return clean(ndimage.binary_closing(m, disk(3)), 2)

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    rgb = np.asarray(Image.open(SRC).convert('RGB'))
    H, W = rgb.shape[:2]

    masks = {}
    for name, cfg in PALETTE.items():
        x0, y0, _, _ = cfg['box']
        m = np.zeros((H, W), bool)
        m[y0:cfg['box'][3], x0:cfg['box'][2]] = seg_palette(rgb, **cfg)
        masks[name] = m
    for name, cfg in CHANNEL.items():
        x0, y0, _, _ = cfg['box']
        m = np.zeros((H, W), bool)
        m[y0:cfg['box'][3], x0:cfg['box'][2]] = seg_channel(rgb, **cfg)
        masks[name] = m
    masks['tuwalya'] = seg_towel(rgb.astype(float))

    full = np.zeros((H, W), bool)
    report, tiles = {}, []
    for name, m in masks.items():
        full |= m
        ys, xs = np.where(m)
        pad = 3
        cx0, cy0 = max(0, xs.min()-pad), max(0, ys.min()-pad)
        cx1, cy1 = min(W-1, xs.max()+pad), min(H-1, ys.max()+pad)
        out = np.zeros((cy1-cy0+1, cx1-cx0+1, 4), np.uint8)
        out[..., :3] = rgb[cy0:cy1+1, cx0:cx1+1]
        out[..., 3] = np.where(m[cy0:cy1+1, cx0:cx1+1], 255, 0)
        Image.fromarray(out).save(os.path.join(OUT, 'item_%s.png' % name))
        px, py = (cx0+cx1)/2.0, (cy0+cy1)/2.0
        report[name] = {'size': [int(cx1-cx0+1), int(cy1-cy0+1)], 'mask_px': int(m.sum()),
                        'world': [round((px-W/2)*0.0125, 3), round((H/2-py)*0.0125, 3)]}
        tiles.append((name, Image.open(os.path.join(OUT, 'item_%s.png' % name))))

    np.save(os.path.join(OUT, 'mask.npy'), full)
    json.dump(report, open(os.path.join(OUT, 'items.json'), 'w'), indent=2)
    for k, v in report.items():
        print('%-9s %-9s mask=%6d px  world=(%7.3f,%6.3f)' %
              (k, 'x'.join(map(str, v['size'])), v['mask_px'], *v['world']))

    hh = max(t[1].height for t in tiles) + 24
    ww = sum(t[1].width + 16 for t in tiles) + 16
    sheet = Image.new('RGBA', (ww, hh), (128, 128, 128, 255))
    x = 8
    for nm, t in tiles:
        sheet.alpha_composite(t, (x, hh - t.height - 8)); x += t.width + 16
    sheet.save(os.path.join(OUT, '_items.png'))
