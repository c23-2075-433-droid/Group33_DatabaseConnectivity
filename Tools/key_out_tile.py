"""Take the bathroom wall back out of an item that was cut from it.

item_tuwalya is 30% green tile. When the bathroom objects were cut out of
the painting the matte was generous, so the towel sprite carries a slab of
the wall it was hanging on - about ten thousand pixels of it, averaging
(125,145,101) against the towel's own (198,159,98).

Nobody saw it for the obvious reason: the sprite is drawn at the exact spot
it was cut from, over the very wall it contains, so the wall lines up with
itself perfectly. It only became visible when the towel was shrunk and put
on Kylo's shoulder, where it arrived inside a pale green box.

The key is easy here because the two have no colours in common. The towel
is cream and orange, both red-dominant; the tile is the only thing in the
sprite where green leads. So green-dominant pixels go, and then the largest
surviving piece is kept - which drops the speckle the key leaves behind in
the shadow under the towel without touching the towel itself.

    python3 Tools/key_out_tile.py Assets/Sprites/item_tuwalya.png

Writes in place, keeping the file name and so the GUID. Check the result
over a contrasting colour, not over the bathroom: against the wall, a
mistake here looks exactly like success.
"""
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

FEATHER = 0.6


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)

    for path in sys.argv[1:]:
        im = Image.open(path).convert('RGBA')
        a = np.array(im).astype(int)
        r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
        opaque = a[:, :, 3] > 8

        tile = opaque & (g > r) & (g >= b)
        keep = opaque & ~tile

        labels, count = ndimage.label(ndimage.binary_dilation(keep, np.ones((3, 3))))
        if count > 1:
            sizes = ndimage.sum(keep, labels, range(1, count + 1))
            keep &= labels == int(np.argmax(sizes)) + 1

        alpha = ndimage.gaussian_filter(np.where(keep, 255.0, 0.0), FEATHER)
        out = np.dstack([a[:, :, :3], alpha]).astype(np.uint8)
        Image.fromarray(out).save(path)
        print('%s  %d tile pixels keyed out, %d kept'
              % (path, int(tile.sum()), int(keep.sum())))


if __name__ == '__main__':
    main()
