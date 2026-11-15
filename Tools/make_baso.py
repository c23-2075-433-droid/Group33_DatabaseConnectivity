"""Redraw item_baso, because the one in the project is a 54x61 thumbnail.

WHY IT HAD TO BE REDRAWN RATHER THAN CLEANED. Bahay Scene 5 draws the cup
1.30 world units tall, which at 100 pixels per unit is 130 screen pixels
from a 61 pixel source - a 2.1x enlargement of an image the size of a
favicon. It arrives soft and blocky next to a 782px bowl of rice, and it
carries a brown matte box from whatever it was cut out of, visible as a
rectangle around it on the tabletop. No filter fixes a missing pixel.

So this draws a new one at 400x480, in the old one's own palette - sampled
off it before it was replaced:

    outline  (111, 96, 29) contaminated by the matte; a darker green is
             used instead, since the olive-brown was never the cup
    shadow   (148, 151, 83)
    mid      (188, 204, 133)
    light    (221, 231, 166)

Drawn at 4x and reduced, so the curves come out smooth rather than
stair-stepped, and composited on full transparency so there is no matte to
trim afterwards.

"Baso" is a drinking glass, and the scene's own note says the cup is opaque
on purpose - an empty one and a full one have to look the same, because
"Inom" lights it up rather than emptying it. So this is an opaque tumbler:
no handle, no contents, nothing that would have to change when he drinks.

Writes Assets/Sprites/item_baso.png in place, keeping the GUID.

Run from the repository root:  python3 Tools/make_baso.py
"""
import io
import os
import re

from PIL import Image, ImageDraw

OUT = "Assets/Sprites/item_baso.png"
SS = 4                      # supersample, reduced at the end
W, H = 400, 480

OUTLINE = (86, 99, 52)
SHADOW = (148, 151, 83)
MID = (188, 204, 133)
LIGHT = (221, 231, 166)
RIM_TOP = (214, 228, 170)
RIM_IN = (120, 136, 78)


def main():
    w, h = W * SS, H * SS
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # A tumbler: wider at the lip than the base, which is what stops a
    # straight-sided box reading as a tin can.
    top_w, bot_w = 300 * SS, 248 * SS
    top_y, bot_y = 70 * SS, 430 * SS
    cx = w // 2
    rim_h, base_h = 54 * SS, 38 * SS
    stroke = 9 * SS

    def body(inset=0):
        tw, bw = top_w / 2 - inset, bot_w / 2 - inset
        return [(cx - tw, top_y), (cx + tw, top_y),
                (cx + bw, bot_y), (cx - bw, bot_y)]

    # Outline first, as a slightly larger silhouette behind everything.
    d.polygon(body(-stroke), fill=OUTLINE)
    d.ellipse([cx - top_w / 2 - stroke, top_y - rim_h / 2 - stroke,
               cx + top_w / 2 + stroke, top_y + rim_h / 2 + stroke], fill=OUTLINE)
    d.ellipse([cx - bot_w / 2 - stroke, bot_y - base_h / 2 - stroke,
               cx + bot_w / 2 + stroke, bot_y + base_h / 2 + stroke], fill=OUTLINE)

    # The glass itself.
    d.polygon(body(), fill=MID)
    d.ellipse([cx - bot_w / 2, bot_y - base_h / 2,
               cx + bot_w / 2, bot_y + base_h / 2], fill=SHADOW)

    # Shading: a lit left cheek and a shadowed right one, both kept inside
    # the silhouette by clipping to a copy of the body.
    shade = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    sd.polygon([(cx - top_w / 2, top_y), (cx - top_w / 2 + 62 * SS, top_y),
                (cx - bot_w / 2 + 52 * SS, bot_y), (cx - bot_w / 2, bot_y)], fill=LIGHT)
    sd.polygon([(cx + top_w / 2 - 78 * SS, top_y), (cx + top_w / 2, top_y),
                (cx + bot_w / 2, bot_y), (cx + bot_w / 2 - 66 * SS, bot_y)], fill=SHADOW)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).polygon(body(), fill=255)
    im.paste(shade, (0, 0), Image.composite(mask, Image.new("L", (w, h), 0),
                                            shade.split()[3]))

    # The lip, drawn last so it sits over the body.
    d.ellipse([cx - top_w / 2, top_y - rim_h / 2,
               cx + top_w / 2, top_y + rim_h / 2], fill=RIM_TOP)
    inset = 15 * SS
    d.ellipse([cx - top_w / 2 + inset, top_y - rim_h / 2 + inset // 2,
               cx + top_w / 2 - inset, top_y + rim_h / 2 - inset // 2], fill=RIM_IN)

    im = im.resize((W, H), Image.LANCZOS)

    guid = None
    if os.path.exists(OUT + ".meta"):
        m = re.search(r"^guid: ([0-9a-f]{32})$",
                      io.open(OUT + ".meta", encoding="utf-8").read(), re.M)
        guid = m.group(1) if m else None
    im.save(OUT)
    print("wrote %s at %dx%d  (was 54x61)%s"
          % (OUT, W, H, "  guid %s kept" % guid[:8] if guid else ""))


if __name__ == "__main__":
    main()
