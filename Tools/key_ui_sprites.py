"""Cut the white background off generated user-interface sprites.

The image generator returns these on solid white. Simply deleting every white
pixel would also punch holes in the artwork - the plumeria flower on the name
plaque is white, and so are the highlights on the padlock - so only white that
is connected to the edge of the image is removed. White enclosed by artwork is
left alone.

Run from the repository root:  python3 Tools/key_ui_sprites.py
Reads  Art/<name>_raw.png
Writes Assets/Sprites/<name>.png
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import os

SPRITES = 'Assets/Sprites'   # where finished sprites go
ART = 'Art'                  # source illustrations, outside Unity's import path
NAMES = ['ui_name_plaque', 'ui_lock', 'ui_step']
WHITE = 238          # at or above this in every channel counts as background
FEATHER = 1.0        # softens the cut edge

def key(path_in, path_out):
    im = Image.open(path_in).convert('RGBA')
    a = np.asarray(im).astype(float)
    rgb = a[..., :3]

    nearly_white = (rgb.min(2) >= WHITE)
    lab, n = ndimage.label(nearly_white)

    # white that reaches the border is the canvas
    border = set(lab[0].tolist()) | set(lab[-1].tolist()) | \
             set(lab[:, 0].tolist()) | set(lab[:, -1].tolist())
    border.discard(0)
    bg = np.isin(lab, list(border))

    # White fully enclosed by artwork is usually part of it - the plumeria on
    # the name plaque - so it is kept. The gap inside the padlock's shackle is
    # the exception: it is a hole, and it gives itself away by being perfectly
    # flat. Shaded white belongs to the drawing; dead-flat white does not.
    for i in range(1, n + 1):
        if i in border:
            continue
        sel = lab == i
        if sel.sum() < 200:
            continue
        patch = rgb[sel]
        if patch.mean() >= 252 and patch.std() < 2.0:
            bg |= sel

    alpha = np.where(bg, 0.0, 255.0)
    alpha = ndimage.gaussian_filter(alpha, FEATHER)
    out = a.copy()
    out[..., 3] = np.clip(alpha, 0, 255)

    # trim to what is actually drawn
    ys, xs = np.where(out[..., 3] > 8)
    pad = 2
    y0, y1 = max(0, ys.min()-pad), min(out.shape[0], ys.max()+1+pad)
    x0, x1 = max(0, xs.min()-pad), min(out.shape[1], xs.max()+1+pad)
    Image.fromarray(out[y0:y1, x0:x1].astype(np.uint8)).save(path_out)
    return (x1-x0, y1-y0), float((out[..., 3] > 128).mean())

if __name__ == '__main__':
    done = 0
    for n in NAMES:
        src = os.path.join(ART, n + '_raw.png')
        if not os.path.exists(src):
            print('  skip %-16s (no %s_raw.png yet)' % (n, n)); continue
        size, solid = key(src, os.path.join(SPRITES, n + '.png'))
        print('  %-16s -> %dx%d, %.0f%% opaque' % (n + '.png', size[0], size[1], solid*100))
        done += 1
    if not done:
        print('\nNothing to do. Download the generated interface art into %s '
              'as <name>_raw.png first.' % SPRITES)
