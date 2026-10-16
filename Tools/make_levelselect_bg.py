"""Turn the meadow illustration into a backdrop for the level select screen.

The level buttons are mid-brown wood with pale outlines. Dropped straight onto
the meadow at full brightness they sink into it - the grass is nearly as light
as the tiles, so the eye has to hunt for the edges. Knocking the picture back
and drawing the corners down leaves the middle of the screen, where the tiles
sit, as the quietest part of the frame.

This is the same painting the journey map used, minus the road: the road was
drawn on separately, so the raw file never had one.

Run from the repository root:  python3 Tools/make_levelselect_bg.py
Reads  Art/journey_map_bg_raw.png
Writes Assets/Sprites/levelselect_bg.png
"""
from PIL import Image, ImageEnhance
import numpy as np
import io, os, re, uuid

SRC = 'Art/journey_map_bg_raw.png'
SPRITES = 'Assets/Sprites'
DST = os.path.join(SPRITES, 'levelselect_bg.png')
META_MODEL = 'item_tabo'

SIZE = (1920, 1080)
DARKEN = 0.55      # how far the whole picture is turned down
SATURATION = 0.75  # greens this bright fight brown wood
VIGNETTE = 0.45    # extra darkening towards the corners


def write_meta(name):
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, META_MODEL + '.png.meta'),
                   encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex,
               base, count=1, flags=re.M)
    t = t.replace(META_MODEL + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)


def main():
    im = Image.open(SRC).convert('RGB').resize(SIZE, Image.LANCZOS)
    im = ImageEnhance.Color(im).enhance(SATURATION)

    a = np.asarray(im).astype(float) * DARKEN

    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    radius = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a *= (1.0 - VIGNETTE * np.clip(radius / 1.35, 0, 1) ** 2)[..., None]

    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(DST)
    write_meta('levelselect_bg')
    print('wrote %s  (%dx%d)' % (DST, w, h))


if __name__ == '__main__':
    main()
