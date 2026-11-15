"""Draw the soft ellipse that puts Bahay Scene 5's breakfast on the table.

The dishes were composited straight onto the painted tabletop with nothing
underneath them, so each one reads as floating a little above the wood
rather than resting on it. A contact shadow is the whole fix: a soft dark
ellipse, widest and darkest directly under the object and fading out fast.

Deliberately NOT a drop shadow offset to one side. The kitchen's light comes
from the window on the right and is diffuse; what sells contact is the small
dark core where the object meets the surface, not a long cast shadow. A cast
shadow would also have to change direction per object, and get it wrong and
the scene looks worse than with none at all.

Soft all the way out: the alpha falls off as a squared cosine from the
centre, which has no hard rim to show up as a grey disc on the wood.

Writes Assets/Sprites/fx_contact_shadow.png.

Run from the repository root:  python3 Tools/make_contact_shadow.py
"""
import io
import os
import re

import numpy as np
from PIL import Image

OUT = "Assets/Sprites/fx_contact_shadow.png"
W, H = 512, 192
PEAK = 0.42          # alpha directly under the object


def main():
    y, x = np.mgrid[0:H, 0:W]
    # Normalised distance from the centre of the ellipse, 0 at the middle
    # and 1 at the rim.
    r = np.sqrt(((x - W / 2) / (W / 2)) ** 2 + ((y - H / 2) / (H / 2)) ** 2)
    fall = np.clip(1.0 - r, 0.0, 1.0)
    alpha = (np.cos((1.0 - fall) * np.pi / 2) ** 2) * PEAK

    rgba = np.zeros((H, W, 4), dtype=np.uint8)
    # A warm near-black, not pure black: the tabletop is a warm brown and a
    # neutral shadow on it reads blue.
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 48, 32, 18
    rgba[..., 3] = (alpha * 255).round().astype(np.uint8)

    guid = None
    if os.path.exists(OUT + ".meta"):
        m = re.search(r"^guid: ([0-9a-f]{32})$",
                      io.open(OUT + ".meta", encoding="utf-8").read(), re.M)
        guid = m.group(1) if m else None

    Image.fromarray(rgba).save(OUT)
    print("wrote %s at %dx%d, peak alpha %.2f%s"
          % (OUT, W, H, PEAK, "  guid %s kept" % guid[:8] if guid else ""))


if __name__ == "__main__":
    main()
