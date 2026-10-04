"""Cut the white background off generated user-interface sprites.

The image generator returns these on solid white. Simply deleting every white
pixel would also punch holes in the artwork - the plumeria flower on the name
plaque is white, and so are the highlights on the padlock - so only white that
is connected to the edge of the image is removed. White enclosed by artwork is
left alone.

Run from the repository root:  python3 Tools/key_ui_sprites.py
Reads  Art/<name>_raw.png (or .jpg/.jpeg)
Writes Assets/Sprites/<name>.png

Downloads land in ~/Downloads/Sprites, so anything named <name>_raw.* there is
moved into Art/ first. Doing that by hand was the step most often forgotten,
and forgetting it just prints "skip" - which reads like the tool deciding the
sprite was not needed.
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import io, os, shutil

SPRITES = 'Assets/Sprites'   # where finished sprites go
ART = 'Art'                  # source illustrations, outside Unity's import path
INBOX = os.path.expanduser('~/Downloads/Sprites')   # where downloads land
NAMES = ['ui_name_plaque', 'ui_lock', 'ui_step',
         'prop_medyas', 'prop_sapatos', 'prop_bag',
         'char_guard',
         'talking_barefoot', 'talking_socks',
         'standing_towel', 'talking_towel']

# Poses of Kylo himself. The generator returns these far larger than the
# original art and at whatever size it feels like, which left him twice his own
# height the moment he changed clothes. Every pose is scaled to the same body
# height as 'standing' and imported with the same settings, so a costume change
# only changes the costume.
CHARACTER = {'talking_barefoot': 'standing', 'talking_socks': 'standing',
             'standing_towel': 'standing', 'talking_towel': 'standing'}
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
        # The flatness allowance has to cover JPEG noise: the same hole in a
        # PNG measures a standard deviation near 0, but around 2.8 once the
        # image has been through JPEG. Shaded white that belongs to the
        # drawing - the socks, the plumeria - varies far more than this.
        if patch.mean() >= 250 and patch.std() < 4.0:
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

def match_pose(name, model):
    """Scale a pose to the body height of the art it stands in for."""
    path = os.path.join(SPRITES, name + '.png')
    im = Image.open(path)
    want = Image.open(os.path.join(SPRITES, model + '.png')).size[1]
    if im.size[1] == want:
        return im.size
    w = max(1, int(round(im.size[0] * want / float(im.size[1]))))
    im.resize((w, want), Image.LANCZOS).save(path)
    return (w, want)


def write_meta(name, model='item_tabo'):
    """Copy a working sprite's import settings so Unity imports this as a Sprite."""
    import re, uuid
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)

def collect_downloads(names):
    """Move any freshly downloaded <name>_raw.* from the inbox into Art/."""
    if not os.path.isdir(INBOX):
        return
    for n in names:
        if any(os.path.exists(os.path.join(ART, n + '_raw' + e))
               for e in ('.png', '.jpg', '.jpeg')):
            continue
        for e in ('.png', '.jpg', '.jpeg'):
            src = os.path.join(INBOX, n + '_raw' + e)
            if os.path.exists(src):
                shutil.copy2(src, os.path.join(ART, n + '_raw' + e))
                print('  picked up %s from Downloads' % os.path.basename(src))
                break

if __name__ == '__main__':
    collect_downloads(NAMES)
    done = 0
    for n in NAMES:
        # Canva hands these back as .jpeg as often as .png, so take either
        # rather than making every import a manual convert-and-rename.
        src = None
        for ext in ('.png', '.jpg', '.jpeg'):
            cand = os.path.join(ART, n + '_raw' + ext)
            if os.path.exists(cand):
                src = cand
                break
        if src is None:
            print('  skip %-16s (no %s_raw.png/.jpg/.jpeg yet)' % (n, n)); continue
        size, solid = key(src, os.path.join(SPRITES, n + '.png'))
        if n in CHARACTER:
            size = match_pose(n, CHARACTER[n])
        write_meta(n, CHARACTER.get(n, 'item_tabo'))
        print('  %-16s -> %dx%d, %.0f%% opaque' % (n + '.png', size[0], size[1], solid*100))
        done += 1
    if not done:
        print('\nNothing to do. Download the generated interface art into %s '
              'as <name>_raw.png first.' % SPRITES)
