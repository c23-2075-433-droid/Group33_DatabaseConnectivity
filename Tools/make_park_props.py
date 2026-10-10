"""Draw the banig the picnic in Parke Scene 4 happens on.

Two children sitting on open grass do not read as a picnic; they read as two
children sitting down. A mat under them is what names the scene, and a BANIG -
the woven Filipino mat - is what a Filipino family actually spreads out, which
makes it worth more here than a generic checked rug.

Drawn rather than cut. There is no mat anywhere in park_background to lift,
and the weave is a regular pattern, which is the one kind of thing that comes
out cleaner drawn than cut.

The weave is drawn in perspective: the mat is a trapezium, wider at the front
than the back, and the strips follow it. A flat rectangle laid on the grass of
a painting with depth in it looks like a sticker.

Writes  Assets/Sprites/prop_banig.png

Run from the repository root:  python Tools/make_park_props.py
"""
from PIL import Image, ImageDraw, ImageFilter

OUT = "Assets/Sprites/"

# Pandan-leaf colours: a banig is dried leaf, straw through to a dyed red.
STRAW = (214, 176, 112)
STRAW_DARK = (186, 146, 86)
DYE = (178, 86, 66)
EDGE = (142, 104, 58)
SHADOW = (40, 60, 24, 90)


def make_banig():
    W, H = 760, 300
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    # The mat in perspective: narrower along its far edge.
    back_in, front_in = 150, 30
    top, bottom = 70, 248
    corners = [(back_in, top), (W - back_in, top),
               (W - front_in, bottom), (front_in, bottom)]

    # A soft shadow on the grass, so it sits rather than floats.
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(shade).polygon(
        [(x, y + 10) for x, y in corners], fill=SHADOW)
    img = Image.alpha_composite(img, shade.filter(ImageFilter.GaussianBlur(9)))

    d = ImageDraw.Draw(img)
    d.polygon(corners, fill=STRAW)

    def lerp(a, b, t):
        return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)

    # Strips running away from the viewer, converging like the edges do.
    for i in range(1, 14):
        t = i / 14.0
        a = lerp(corners[0], corners[1], t)
        b = lerp(corners[3], corners[2], t)
        d.line([a, b], fill=STRAW_DARK, width=3)

    # Strips running across, spaced wider towards the front because that edge
    # is nearer. Every fourth one is dyed, which is how a banig is banded.
    for i in range(1, 11):
        t = (i / 11.0) ** 1.35
        a = lerp(corners[0], corners[3], t)
        b = lerp(corners[1], corners[2], t)
        colour = DYE if i % 4 == 0 else STRAW_DARK
        d.line([a, b], fill=colour, width=4 if i % 4 == 0 else 3)

    d.polygon(corners, outline=EDGE, width=5)
    img.save(OUT + "prop_banig.png")
    return img.size


if __name__ == "__main__":
    print("prop_banig.png", make_banig())
