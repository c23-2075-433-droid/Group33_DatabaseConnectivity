"""Turn a generated towel pose into a sprite the scenes can use.

The poses come back from the generator as a tall RGB image on flat white,
at whatever size it felt like. The sprite set wants transparent PNGs 745px
tall, trimmed to the artwork, so Kylo is the same height in every pose and
the scene builders' PlayerScale and FeetBelowPivot numbers keep working.

Cutting the white out is the only part that needs care. A global "white is
background" test eats the cream stripes of the towel and the whites of his
eyes, because those are white too. What actually separates them is that the
background is CONNECTED TO THE EDGE of the picture and they are not, so the
white is labelled into regions and only the regions touching the border are
dropped.

The cut is then softened by a fraction of a pixel, because a hard alpha on
line art reads as a jagged outline once Unity scales it down.

    python3 Tools/import_towel_pose.py <downloaded.png> standing_towel_messy

Writes Assets/Sprites/<name>.png. Run Tools > SALINLAHI > Build Bahay
Scene 3 afterwards: the builder picks the file up on its own.
"""
import sys
import os
import re
import uuid
import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import bleed_edges  # noqa: E402

OUT_DIR = 'Assets/Sprites'
TARGET_H = 745          # every one of Kylo's poses is this tall
WHITE = 236             # at or above this on all channels counts as paper
FEATHER = 0.6           # pixels

# The generator anti-aliases the figure against the paper, so a rim of pale
# pixels survives the cut and reads as a white outline once he is over the
# bathroom tiles. A flat erosion would take it off and take a bite out of his
# fingers and the arm of his glasses with it, so the background GROWS into
# the halo instead - only into pixels pale enough to be paper rather than
# drawing. His black outline is nowhere near that pale, so the growth stops
# dead at the ink and the cream stripes of the towel, which sit behind the
# outline, are never reached. Same constants as fit_seated_pose.py, which
# worked them out.
HALO_LUMA = 205
HALO_ROUNDS = 40

# White the border cannot reach is usually still paper: the triangle between
# a raised arm and the chest, the gap under an elbow. Those have to go too or
# the pose carries a white patch over the tiles. What saves the eyes and the
# teeth, which are also enclosed and also white, is that they are SMALL - the
# arm gaps run to thousands of pixels and the largest eye to a few hundred.
# The towel is never at risk: its cream stripes are warm enough that they
# never enter the white mask in the first place.
ENCLOSED_GAP = 800      # px; above this an enclosed white region is paper

# The importer settings are copied from an existing pose rather than left to
# Unity. A PNG that arrives without a .meta is imported with the editor's
# defaults, which means 100 pixels per unit - and every one of Kylo's poses
# is 120. Getting that wrong does not look like a settings problem, it looks
# like he grew by a fifth the moment the pose switches. Only the GUID is
# changed, so the new pose matches the old one in every other respect.
META_TEMPLATE = os.path.join(OUT_DIR, 'talking_towel.png.meta')


def cut_background(rgb):
    """Alpha for the artwork, with the paper it was drawn on taken away."""
    white = (rgb >= WHITE).all(2)
    labels, _ = ndimage.label(white)

    edge = np.concatenate([labels[0, :], labels[-1, :],
                           labels[:, 0], labels[:, -1]])
    outside = np.unique(edge)
    outside = outside[outside != 0]
    background = np.isin(labels, outside)

    for region in range(1, labels.max() + 1):
        if region in outside:
            continue
        m = labels == region
        if m.sum() > ENCLOSED_GAP:
            background |= m

    lum = rgb.mean(axis=2)
    pale = lum > HALO_LUMA
    for _ in range(HALO_ROUNDS):
        touching = (np.roll(background, 1, 0) | np.roll(background, -1, 0)
                    | np.roll(background, 1, 1) | np.roll(background, -1, 1))
        grown = background | (touching & pale)
        if not grown[~background].any():
            break
        background = grown

    alpha = np.where(background, 0.0, 255.0)
    return ndimage.gaussian_filter(alpha, FEATHER)


def main():
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    src, name = sys.argv[1], sys.argv[2]

    rgb = np.asarray(Image.open(src).convert('RGB'), dtype=int)
    alpha = cut_background(rgb)

    im = bleed_edges(Image.fromarray(np.dstack([rgb, alpha]).astype(np.uint8)))

    box = im.getbbox()
    if box is None:
        raise SystemExit('nothing left after cutting the background out')
    im = im.crop(box)

    w = max(1, round(im.width * TARGET_H / im.height))
    im = im.resize((w, TARGET_H), Image.LANCZOS)

    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name + '.png')
    im.save(path)
    print('%s  %dx%d' % (path, im.width, im.height))

    meta_path = path + '.meta'
    if os.path.exists(meta_path):
        print('%s already exists, left alone' % meta_path)
    elif os.path.exists(META_TEMPLATE):
        with open(META_TEMPLATE, encoding='utf-8') as f:
            meta = f.read()
        meta = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex,
                      meta, count=1, flags=re.M)
        with open(meta_path, 'w', encoding='utf-8') as f:
            f.write(meta)
        print('%s  (120 px per unit, from %s)' % (meta_path, META_TEMPLATE))
    else:
        print('WARNING: no %s to copy import settings from - check the '
              'pixels per unit in the Inspector, it must be 120' % META_TEMPLATE)


if __name__ == '__main__':
    main()
