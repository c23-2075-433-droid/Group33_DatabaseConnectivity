"""Take the white fringe off Bahay Scene 5's breakfast props.

THE FAULT, and why the project's two existing edge tools do not fix it.

clean_sprite_edges.py bleeds colour into FULLY transparent pixels and sets
alphaIsTransparency, so Unity's filter has something sane to find when it
reaches past the edge. Both are already done here - every food sprite's meta
reads alphaIsTransparency: 1.

trim_sprite_halo.py removes a matte RECTANGLE of half-transparent pixels
sitting well away from the artwork.

This is neither. The rim pixels are right against the art, one or two deep,
and they are the drawing's own anti-aliasing - but their COLOUR is nearly
white, because these props were cut off a white background and the soft
matte blended them towards it. Measured before this ran:

    prop_kanin        41,634 rim px, 92.3% of them near-white
    prop_itlog        29,341 rim px, 84.6%
    prop_kanin_empty  20,176 rim px, 90.1%
    prop_itlog_empty   7,330 rim px, 83.5%

Composited over a brown table that reads as a pale outline all the way
round each dish, which is exactly what it looks like in the scene.

THE FIX. Every part-transparent pixel takes the colour of the nearest
FULLY OPAQUE pixel, and keeps its own alpha. The edge stays just as soft -
nothing about the silhouette changes - but what shows through it is the
dish's own colour instead of the white it was cut out of.

Writes in place, keeping the file name and so the GUID.

Run from the repository root:  python3 Tools/clean_food_sprites.py
"""
import io
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

SPRITES = "Assets/Sprites/"

# Alpha at or above this is certainly the drawing; below SOFT_MIN is
# certainly outside it. Between the two is the anti-aliased rim this fixes.
OPAQUE = 248
SOFT_MIN = 8

TARGETS = [
    "prop_kanin",
    "prop_itlog",
    "prop_kanin_empty",
    "prop_itlog_empty",
]


def unmatte(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im)
    alpha = a[..., 3]

    solid = alpha >= OPAQUE
    rim = (alpha > SOFT_MIN) & (alpha < OPAQUE)
    if not solid.any() or not rim.any():
        return 0, None

    # For every pixel, the index of the nearest solid pixel. Run on the
    # INVERSE of solid, so the transform measures distance out of the
    # artwork and hands back where it came from.
    _dist, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)

    before = a[..., :3][rim].mean(axis=0)
    a[..., :3][rim] = a[..., :3][iy[rim], ix[rim]]
    after = a[..., :3][rim].mean(axis=0)

    Image.fromarray(a).save(path)
    return int(rim.sum()), (before.round().astype(int), after.round().astype(int))


def main():
    names = sys.argv[1:] or TARGETS
    for name in names:
        path = name if name.endswith(".png") else SPRITES + name + ".png"
        if not os.path.exists(path):
            print("%-22s missing" % os.path.basename(path))
            continue
        n, colours = unmatte(path)
        if n == 0:
            print("%-22s nothing to do" % os.path.basename(path))
        else:
            before, after = colours
            print("%-22s %6d rim px  rgb %s -> %s"
                  % (os.path.basename(path), n, before, after))


if __name__ == "__main__":
    main()
