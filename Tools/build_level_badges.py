"""Build each level's completion badge from one shared frame.

Every level awards a badge when it is finished. Generating four badges
separately would give four slightly different frames - different rope
thickness, different ribbon, different gold - so one frame is generated and
each level's illustration is dropped into its blank middle, the same way the
level-select signposts are built.

The blank centre is found rather than hardcoded: it is the largest flat
near-cream region in the frame, which is exactly what the generator was asked
to leave empty. That way the tool still works if the frame is ever redrawn at
a different size.

Run from the repository root:  python3 Tools/build_level_badges.py
Reads  Art/badge_frame_raw.png  and  Art/scene_<level>.png
Writes Assets/Sprites/badge_<level>.png
"""
from PIL import Image, ImageDraw
import numpy as np
from scipy import ndimage
import io, os, re, uuid

ART, SPRITES = 'Art', 'Assets/Sprites'
FRAME = 'badge_frame_raw'
LEVELS = ['bahay', 'paaralan', 'parke', 'palengke']

def disk(r):
    y, x = np.ogrid[-r:r+1, -r:r+1]
    return x*x + y*y <= r*r

def meta_for(name, model='item_tabo'):
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)

def load_frame():
    """The badge with its white canvas keyed off, plus where its centre is."""
    im = Image.open(os.path.join(ART, FRAME + '.png')).convert('RGB')
    a = np.asarray(im).astype(int)
    rgb = a
    H, W = rgb.shape[:2]

    # key the white canvas: only white that reaches the border
    nearly_white = rgb.min(2) >= 238
    lab, n = ndimage.label(nearly_white)
    border = set(lab[0]) | set(lab[-1]) | set(lab[:, 0]) | set(lab[:, -1])
    border.discard(0)
    outside = np.isin(lab, list(border))

    # the blank middle: the biggest flat light region that is NOT the canvas
    light = (rgb.min(2) >= 185) & ~outside
    light = ndimage.binary_opening(light, disk(4))
    lab2, n2 = ndimage.label(light)
    if n2 == 0:
        raise SystemExit('could not find the badge\'s blank centre')
    sizes = ndimage.sum(light, lab2, range(1, n2 + 1))
    centre = lab2 == (int(np.argmax(sizes)) + 1)
    ys, xs = np.where(centre)
    cx, cy = (xs.min() + xs.max()) / 2.0, (ys.min() + ys.max()) / 2.0
    radius = min(xs.max() - xs.min(), ys.max() - ys.min()) / 2.0

    alpha = np.where(outside, 0.0, 255.0)

    # Clear the blank middle too. It is opaque cream in the generated art, so
    # leaving it would simply cover the illustration placed underneath.
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.hypot(xx - cx, yy - cy)
    keep = np.clip((d - (radius - 3.0)) / 3.0, 0, 1)   # feathered at the rope
    alpha = alpha * keep

    out = np.zeros((H, W, 4), np.uint8)
    out[..., :3] = rgb
    out[..., 3] = np.clip(alpha, 0, 255).astype(np.uint8)
    return Image.fromarray(out), (cx, cy), radius

def circular(path, size):
    s = Image.open(path).convert('RGB')
    side = min(s.size)
    s = s.crop(((s.width - side)//2, (s.height - side)//2,
                (s.width + side)//2, (s.height + side)//2))
    s = s.resize((size, size), Image.LANCZOS).convert('RGBA')
    mask = Image.new('L', (size*4, size*4), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, size*4 - 1, size*4 - 1), fill=255)
    s.putalpha(mask.resize((size, size), Image.LANCZOS))
    return s

def main():
    src = os.path.join(ART, FRAME + '.png')
    if not os.path.exists(src):
        print('No %s yet - download the generated badge frame there first.' % src)
        return
    frame, (cx, cy), radius = load_frame()
    print('  frame %dx%d, blank centre at (%.0f, %.0f) radius %.0f'
          % (frame.width, frame.height, cx, cy, radius))

    for name in LEVELS:
        scene = os.path.join(ART, 'scene_%s.png' % name)
        if not os.path.exists(scene):
            print('  skip %-9s (no %s)' % (name, scene)); continue
        # a touch inside the rope, so the illustration tucks under it
        pic = circular(scene, int(radius * 2 * 0.97))
        out = Image.new('RGBA', frame.size, (0, 0, 0, 0))
        out.alpha_composite(pic, (int(cx - pic.width/2), int(cy - pic.height/2)))
        out.alpha_composite(frame)
        out.save(os.path.join(SPRITES, 'badge_%s.png' % name))
        meta_for('badge_%s' % name)
        print('  badge_%s.png' % name)

main()
