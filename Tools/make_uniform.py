"""Recolour Kylo's green shirt into a white school uniform.

Saying "Damit" should visibly dress him, not just make a prop disappear. The
cheapest honest way to do that is a recolour rather than new art: his green
tee is the only green on the character (nothing else passes g > r+8 and
g > b+8), and he already wears navy shorts, so a white top reads immediately
as a Filipino school uniform.

Shading is kept, not flattened. Each shirt pixel's brightness is mapped from
the green's range onto a white one, so every fold, sleeve shadow and crease
survives in the same place - a flat white fill would make him look like a
paper cut-out beside the rest of the art.

The range is measured once across EVERY pose, not per sprite. Normalising
each frame to its own darkest and lightest green makes frames with less
contrast come out grey, and the shirt then flickers between white and grey
as he walks.

Run from the repository root:  python3 Tools/make_uniform.py
Reads  Assets/Sprites/<pose>.png and <pose>_with_bag.png
Writes Assets/Sprites/<pose>_uniform.png and <pose>_uniform_with_bag.png
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import io, os, re, uuid

SPRITES = 'Assets/Sprites'
# 'talking' is the front-facing pose. Scene 4 uses it instead of the side-on
# 'standing', because the child is dressing him and should see him face on.
# talking_barefoot and talking_socks are Scene 4's dressing stages. Their
# uniform versions are derived here rather than generated, so the shirt is
# identical across all of them.
# Poses to recolour. 'sitting_desk' is Scene 5's "Upo" - Kylo already wears
# the uniform by breakfast, so the seated pose needs a white shirt like the
# rest.
POSES = ['standing', 'talking', 'talking_barefoot', 'talking_socks',
         'walk_frame_1', 'walk_frame_2', 'walk_frame_3', 'walk_frame_4',
         'sitting_desk']

# The green-to-white brightness range is measured across THESE poses only, not
# across POSES. It has to stay fixed: re-measuring it every time a pose is
# added shifts the white a little on every other sprite, so the shirt would
# change shade across the whole game each time someone adds one frame.
RANGE_POSES = ['standing', 'talking', 'talking_barefoot', 'talking_socks',
               'walk_frame_1', 'walk_frame_2', 'walk_frame_3', 'walk_frame_4']

# where the white shirt's brightness lands: deep fold -> lit highlight
SHADOW, HIGHLIGHT = 168.0, 250.0

def meta_for(name, model):
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)

def shirt_mask(a):
    rgb, al = a[..., :3], a[..., 3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (al > 128) & (g > r + 8) & (g > b + 8)

def green_range(sources):
    """Where the shirt's brightness really sits, across every pose.

    Percentiles, not the outright darkest and lightest pixel: a handful of
    anti-aliased pixels along the shirt's edge run almost white, and letting
    one of them set the top of the range squashes the whole shirt into the
    grey half of the scale.
    """
    vals = []
    for src in sources:
        a = np.asarray(Image.open(os.path.join(SPRITES, src + '.png')).convert('RGBA')).astype(float)
        m = shirt_mask(a)
        if m.any():
            vals.append(a[..., :3].mean(2)[m])
    allv = np.concatenate(vals)
    return float(np.percentile(allv, 2)), float(np.percentile(allv, 98))

def recolour(src, dst, lo, hi):
    im = Image.open(os.path.join(SPRITES, src + '.png')).convert('RGBA')
    a = np.asarray(im).astype(float)
    rgb = a[..., :3]

    shirt = shirt_mask(a)
    if not shirt.any():
        print('  !! no green found in', src)
        return 0

    lum = rgb.mean(2)
    t = np.clip((lum - lo) / max(hi - lo, 1e-6), 0, 1)
    white = SHADOW + t * (HIGHLIGHT - SHADOW)

    out = a.copy()
    # a touch cooler in the folds, like cotton in shade
    out[..., 0] = np.where(shirt, white * 0.995, out[..., 0])
    out[..., 1] = np.where(shirt, white * 0.998, out[..., 1])
    out[..., 2] = np.where(shirt, np.minimum(255, white * 1.012), out[..., 2])

    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(
        os.path.join(SPRITES, dst + '.png'))
    meta_for(dst, src)
    return int(shirt.sum())

if __name__ == '__main__':
    sources = [p + s for p in RANGE_POSES for s in ('', '_with_bag')
               if os.path.exists(os.path.join(SPRITES, p + s + '.png'))]
    lo, hi = green_range(sources)
    print('green brightness across all poses: %.0f..%.0f -> white %.0f..%.0f\n'
          % (lo, hi, SHADOW, HIGHLIGHT))

    made = 0
    for pose in POSES:
        for suffix, tag in [('', '_uniform'), ('_with_bag', '_uniform_with_bag')]:
            src = pose + suffix
            if not os.path.exists(os.path.join(SPRITES, src + '.png')):
                print('  skip %-28s (missing)' % src)
                continue
            n = recolour(src, pose + tag, lo, hi)
            if n:
                print('  %-32s %6d shirt px recoloured' % (pose + tag + '.png', n))
                made += 1
    print('\n%d uniform sprites written' % made)
