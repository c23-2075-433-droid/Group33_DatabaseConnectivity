"""Cut the school shirt out of the bedroom dresser sprite.

Scene 4 needs the hanging uniform shirt as its own sprite, so saying "Damit"
can take it off the rack. It is painted into dresser_v3.png along with the
mirror, the tie and the wall rack.

It separates cleanly on colour: the shirt and its hanger are almost grey
(saturation 7-21, brightness 172-248) while everything around them is warm
wood (saturation 86-93, brightness 80-101). The shirt also hangs clear of the
rack above it, so taking it away leaves nothing to repaint - unlike the
backpack on the chair, which overlaps the chair back and shares its browns.

dresser_v3.png itself is left untouched, because Scene 1's bedroom uses it.
This writes two new sprites instead:

  prop_damit.png        the shirt on its hanger
  dresser_v3_bare.png   the same dresser with the rack empty

A mirror used to be cut out of here too, for a "Salamin" word in Scene 4.
That word was dropped, so the mirror is no longer produced.

Run from the repository root:  python3 Tools/cut_bedroom_props.py
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import io, os, re, uuid

SPRITES = 'Assets/Sprites'
SRC = 'dresser_v3'
SHIRT_BOX = (345, 150, 503, 375)      # x0, y0, x1, y1 around the hanging shirt

def disk(r):
    y, x = np.ogrid[-r:r+1, -r:r+1]
    return x*x + y*y <= r*r

def meta_for(name):
    """Give a new sprite the same import settings as the one it came from."""
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, SRC + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(SRC + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)

def main():
    im = Image.open(os.path.join(SPRITES, SRC + '.png')).convert('RGBA')
    a = np.asarray(im).astype(int)
    rgb, al = a[..., :3], a[..., 3]
    H, W = al.shape

    x0, y0, x1, y1 = SHIRT_BOX
    inbox = np.zeros((H, W), bool)
    inbox[y0:y1, x0:x1] = True

    sat = rgb.max(2) - rgb.min(2)
    lum = rgb.mean(2)
    shirt = inbox & (al > 128) & (sat < 45) & (lum > 140)

    # close the gaps between buttons and creases, then keep the one blob
    shirt = ndimage.binary_closing(shirt, disk(3))
    shirt = ndimage.binary_fill_holes(shirt)
    lab, n = ndimage.label(shirt)
    sizes = ndimage.sum(shirt, lab, range(1, n + 1))
    shirt = lab == (int(np.argmax(sizes)) + 1)
    # grow to take the dark line drawn around it, which the colour test misses
    shirt = ndimage.binary_dilation(shirt, disk(2)) & (al > 128) & inbox

    ys, xs = np.where(shirt)
    pad = 3
    cx0, cy0 = max(0, xs.min() - pad), max(0, ys.min() - pad)
    cx1, cy1 = min(W - 1, xs.max() + pad), min(H - 1, ys.max() + pad)

    out = np.zeros((cy1 - cy0 + 1, cx1 - cx0 + 1, 4), np.uint8)
    out[..., :3] = rgb[cy0:cy1+1, cx0:cx1+1]
    out[..., 3] = np.where(shirt[cy0:cy1+1, cx0:cx1+1], 255, 0)
    Image.fromarray(out).save(os.path.join(SPRITES, 'prop_damit.png'))
    meta_for('prop_damit')

    bare = a.copy()
    bare[shirt, 3] = 0
    Image.fromarray(bare.astype(np.uint8)).save(os.path.join(SPRITES, 'dresser_v3_bare.png'))
    meta_for('dresser_v3_bare')

    print('  prop_damit.png       %dx%d, %d px' % (out.shape[1], out.shape[0], shirt.sum()))
    print('  dresser_v3_bare.png  %dx%d, rack now empty' % (W, H))

    # centre of the shirt within the dresser sprite, for placing it in a scene
    print('  shirt centre in dresser_v3: (%.1f, %.1f) of %dx%d'
          % ((cx0 + cx1) / 2, (cy0 + cy1) / 2, W, H))

main()
