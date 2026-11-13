"""Draw the water droplet that falls off Kylo after his bath.

Canva will not redraw his hair wetter - the request is a child in a towel
and their child-safety classifier refuses it about half the time, with no
rule behind it you can comply with. So the "he is soaking wet" reading is
carried by something next to him instead of by his hair, and a droplet is
a shape simple enough to draw here rather than generate.

WHAT A CARTOON DROPLET IS. Not a teardrop outline. Three things stacked:

  body       a circle at the bottom with a tapering neck above it - the
             classic drop silhouette, in a pale blue that is almost white,
             because water in this art style reads as a tint rather than a
             colour.
  rim        a slightly darker edge, so it does not vanish against his skin
             the way a flat pale shape would. This is the whole reason the
             drop is visible over an arm.
  glint      one small white highlight, up and to the left. Without it the
             shape reads as a bead of wax; with it, as water.

Drawn at eight times the final size and shrunk, because the only thing that
makes a 20px sprite look drawn rather than aliased is supersampling.

    python3 Tools/make_water_drop.py

Writes Assets/Sprites/fx_drop.png at 120 pixels per unit, the same scale as
Kylo's own art, so DripEffect can size it against him without a magic
number.
"""
import os
import re
import uuid

from PIL import Image, ImageDraw

OUT_DIR = 'Assets/Sprites'
NAME = 'fx_drop'
META_TEMPLATE = os.path.join(OUT_DIR, 'standing_towel_messy.png.meta')

W, H = 20, 30           # final size, px - about 4% of Kylo's height
SS = 8                  # supersampling factor

WATER = (205, 232, 246, 215)
RIM = (138, 190, 222, 235)
GLINT = (255, 255, 255, 235)


def main():
    w, h = W * SS, H * SS
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # The bulb: a circle filling the bottom two thirds.
    r = w / 2.0
    cy = h - r
    d.ellipse([0, cy - r, w, cy + r], fill=WATER, outline=RIM, width=SS)

    # The neck: a triangle from the top point down to the bulb's widest part,
    # overlapping it so the join disappears.
    d.polygon([(w / 2.0, 0), (w * 0.04, cy + r * 0.18), (w * 0.96, cy + r * 0.18)],
              fill=WATER)
    # ...and its two edges, drawn separately so the rim follows the neck but
    # does not cut a line across the bulb.
    d.line([(w / 2.0, 0), (w * 0.04, cy + r * 0.2)], fill=RIM, width=SS)
    d.line([(w / 2.0, 0), (w * 0.96, cy + r * 0.2)], fill=RIM, width=SS)

    # The glint: a small ellipse up and to the left inside the bulb.
    gx, gy, gw, gh = w * 0.3, cy - r * 0.25, w * 0.2, h * 0.09
    d.ellipse([gx, gy, gx + gw, gy + gh], fill=GLINT)

    im = im.resize((W, H), Image.LANCZOS)

    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, NAME + '.png')
    im.save(path)
    print('%s  %dx%d' % (path, im.width, im.height))

    meta = path + '.meta'
    if os.path.exists(meta):
        print('%s already exists, left alone' % meta)
        return
    with open(META_TEMPLATE, encoding='utf-8') as f:
        text = f.read()
    text = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex,
                  text, count=1, flags=re.M)
    with open(meta, 'w', encoding='utf-8') as f:
        f.write(text)
    print('%s  (120 px per unit)' % meta)


if __name__ == '__main__':
    main()
