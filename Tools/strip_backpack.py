"""Remove the baked-in backpack from the Kylo character frames.

Run from the repository root:  python3 Tools/strip_backpack.py
Reads Assets/Sprites/<frame>_with_bag.png where present (otherwise the plain
name) and writes the stripped frames to Tools/out_nobag/ for review. Copy them
over Assets/Sprites/<frame>.png once they look right - the .meta files, and so
the GUIDs the scenes reference, must stay as they are.


The bag is a set of navy regions that all sit above the shorts line, so they
can be told apart from the shorts and shoes (which are the same navy). The
black outline and buckles drawn on top of the bag are picked up as dark pixels
lying close to it. Once all that is erased, the notches the straps cut out of
the shoulder and waist are closed back up and repainted from the surrounding
shirt and skin.
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import os

OUT = "Tools/out_nobag"
SRC = "Assets/Sprites"

def disk(r):
    y, x = np.ogrid[-r:r+1, -r:r+1]
    return x*x + y*y <= r*r


def fill_gaps(rgb, src, gap):
    """Paint `gap` with the flat colour that belongs there.

    The art is flat-shaded, so the fill starts from the nearest surviving
    pixel and is then median-filtered a few times. A median keeps flat areas
    flat and respects the shirt/skin boundary; blurring or interpolating
    across the gap smears skin tone down the shirt instead.
    """
    _, idx = ndimage.distance_transform_edt(~src, return_indices=True)
    out = rgb.astype(float).copy()
    out[gap] = rgb[idx[0][gap], idx[1][gap]]
    for _ in range(4):
        med = np.stack([ndimage.median_filter(out[..., c], size=9) for c in range(3)], -1)
        out = np.where(gap[..., None], med, out)
    return out


def strip(name, split=0.55, close_r=26):
    # Prefer the archived original, so re-running never strips an already
    # stripped frame.
    path = os.path.join(SRC, name + '_with_bag.png')
    if not os.path.exists(path):
        path = os.path.join(SRC, name + '.png')
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im).astype(float)
    rgb, al = a[..., :3], a[..., 3]
    H, W = al.shape
    m = al > 128
    lum = rgb.mean(2)

    # --- 1. the bag's navy fill -------------------------------------------
    navy = m & (rgb[..., 2] > rgb[..., 0] + 25) & (rgb[..., 2] < 150) & (rgb[..., 0] < 90)
    lab, n = ndimage.label(navy)
    bag = np.zeros_like(m)
    for i in range(1, n + 1):
        sel = lab == i
        if sel.sum() < 40:
            continue
        if np.where(sel)[0].mean() < split * H:   # above the shorts
            bag |= sel

    # --- 2. the ink drawn on the bag --------------------------------------
    # The bag's outline and buckles are near-black, the same ink as the
    # child's own outline, so they can't be told apart by colour. They are
    # split by what they are drawn around instead: ink nearer to bag navy
    # than to the child's own shirt/skin belongs to the bag. Taking it by
    # proximity alone would strip the outline off Kylo's back, which runs
    # right alongside the bag.
    ink_mask = m & (lum < 60) & (rgb.max(2) - rgb.min(2) < 30)
    body_colour = m & ~ink_mask & ~bag
    d_bag = ndimage.distance_transform_edt(~bag)
    d_body = ndimage.distance_transform_edt(~body_colour)
    bag_ink = ink_mask & (d_bag < d_body)

    # That test alone keeps the straps' own outline and buckles, because they
    # are drawn across the shirt and so sit nearer to body colour than to the
    # bag. They are taken by a second test: ink well inside the silhouette and
    # close to bag navy. Kylo's back outline is on the silhouette's edge, so
    # eroding first is what keeps it safe.
    interior = ndimage.binary_erosion(ndimage.binary_fill_holes(m), disk(4))
    strap_ink = ink_mask & interior & (d_bag <= 6)

    removed = bag | bag_ink | strap_ink

    # Third pass: buckles and zip pulls are drawn in a blue-grey that is
    # neither navy enough for the first test nor close enough to the strap's
    # navy for the second, so they survive as specks on the shirt. They are
    # caught as small marks that lie almost entirely within reach of the bag;
    # the shirt's own hem and sleeve lines run far past it and are left alone.
    resid = (ink_mask | navy) & interior & ~removed
    rlab, rn = ndimage.label(resid)
    near = d_bag <= 20
    for i in range(1, rn + 1):
        sel = rlab == i
        k = sel.sum()
        if k < 2000 and (sel & near).sum() >= 0.8 * k:
            removed |= sel

    removed = ndimage.binary_dilation(removed, disk(1)) & m

    # --- 3. what is left of the child -------------------------------------
    kept = m & ~removed
    klab, kn = ndimage.label(kept)

    # Straps run across the shoulders, so cutting them can leave an arm as a
    # separate blob - in the celebrating pose both raised arms come away.
    # Simply keeping the biggest blob would throw those away, so a blob is
    # kept when it has real substance away from the bag. Leftover scraps of
    # the bag's own outline hug the bag and are the only thing dropped.
    away = ~ndimage.binary_dilation(bag, disk(8))
    core = np.zeros_like(m)
    for i in range(1, kn + 1):
        sel = klab == i
        if (sel & away).sum() >= 100:
            core |= sel

    # Straps cut notches into the shoulder and waist. Closing bridges notches
    # narrower than 2*close_r; intersecting with `removed` means only bag
    # pixels can ever come back.
    add = ndimage.binary_closing(core, disk(close_r)) & removed
    final = ndimage.binary_closing(core | add, disk(2))
    add = final & ~core

    # --- 4. repaint the gap -----------------------------------------------
    # Sampling excludes the child's own black outline, otherwise the nearest
    # pixel along an edge is black and the fill comes out streaked. The test
    # is "dark AND grey", not just dark: the shirt green is dark too (mean
    # luma about 60) and must stay available as a fill colour.
    src = core & ~ink_mask
    out = a.copy()
    filled = fill_gaps(a[..., :3], src, add)
    out[..., :3] = np.where(add[..., None], filled, a[..., :3])

    # Smooth the rebuilt contour before inking it, so the closing's stair-step
    # edges don't show up as a ragged outline.
    final = ndimage.gaussian_filter(final.astype(float), 1.2) > 0.5
    final |= core
    add = add & final
    out[..., 3] = np.where(final, 255, 0)

    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(os.path.join(OUT, name + '.png'))
    return im, Image.open(os.path.join(OUT, name + '.png'))

NAMES = ['standing', 'walk_frame_1', 'walk_frame_2', 'walk_frame_3', 'walk_frame_4', 'talking', 'celebrate']

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    pairs = [strip(nm) for nm in NAMES]
    H = max(max(b.height, c.height) for b, c in pairs)
    W = sum(b.width + c.width + 40 for b, c in pairs)
    sheet = Image.new('RGBA', (W, H), (245, 245, 245, 255))
    x = 0
    for b, c in pairs:
        sheet.paste(b, (x, H - b.height), b); x += b.width + 8
        sheet.paste(c, (x, H - c.height), c); x += c.width + 32
    sheet.save(os.path.join(OUT, '_compare.png'))
    print("wrote", OUT)
