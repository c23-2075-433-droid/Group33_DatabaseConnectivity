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
import numpy as np
from PIL import Image
from scipy import ndimage

OUT_DIR = 'Assets/Sprites'
TARGET_H = 745          # every one of Kylo's poses is this tall
WHITE = 236             # at or above this on all channels counts as paper
FEATHER = 0.7           # pixels


def cut_background(rgb):
    """Alpha for the artwork, with the paper it was drawn on taken away."""
    white = (rgb >= WHITE).all(2)
    labels, _ = ndimage.label(white)

    edge = np.concatenate([labels[0, :], labels[-1, :],
                           labels[:, 0], labels[:, -1]])
    outside = np.unique(edge)
    outside = outside[outside != 0]
    background = np.isin(labels, outside)

    alpha = np.where(background, 0.0, 255.0)
    return ndimage.gaussian_filter(alpha, FEATHER)


def main():
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    src, name = sys.argv[1], sys.argv[2]

    rgb = np.asarray(Image.open(src).convert('RGB'), dtype=int)
    alpha = cut_background(rgb)

    out = np.dstack([rgb, alpha]).astype(np.uint8)
    im = Image.fromarray(out, 'RGBA')

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


if __name__ == '__main__':
    main()
