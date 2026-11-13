"""Cut the soft rectangular halo off a sprite that was keyed out of a photo.

Not the same fault as clean_sprite_edges.py, and the two are easy to
confuse. That one fixes COLOUR hiding under fully transparent pixels, which
only shows once a filter reaches past the edge. This one fixes ALPHA: the
bathroom items were cut out of the painting with a soft matte, and each of
them carries a faint rectangle of half-transparent pale pixels around the
artwork - 9,241 of them on item_tuwalya, averaging a washed-out khaki.

It never showed while the items were drawn at the size they were cut at,
over the very background they were cut from. It showed the moment the towel
was shrunk onto Kylo's shoulder, where it arrived inside a visible pale box.

The rule: anything more than GRACE pixels away from the solid artwork is not
part of the drawing. The towel's own anti-aliased rim is one or two pixels
from solid and survives; a matte rectangle fifty pixels out does not.

    python3 Tools/trim_sprite_halo.py Assets/Sprites/item_tuwalya.png

Writes in place, keeping the file name and so the GUID.
"""
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

SOLID = 200     # alpha at or above this is certainly artwork
GRACE = 3       # pixels of anti-aliasing allowed outside the solid shape


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)

    for path in sys.argv[1:]:
        im = Image.open(path).convert('RGBA')
        a = np.array(im)
        alpha = a[:, :, 3]

        solid = alpha >= SOLID
        if not solid.any():
            print('%s  no solid artwork found, left alone' % path)
            continue

        keep = ndimage.binary_dilation(solid, np.ones((GRACE * 2 + 1,) * 2))
        removed = int(((alpha > 8) & ~keep).sum())
        a[:, :, 3] = np.where(keep, alpha, 0)

        Image.fromarray(a).save(path)
        print('%s  %d halo pixels removed' % (path, removed))


if __name__ == '__main__':
    main()
