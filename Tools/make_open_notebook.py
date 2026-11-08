"""Draw the open notebook that lies on Kylo's desk in Paaralan Scene 2.

The storyboard's "Lapis" panel has an OPEN exercise book flat on the desk,
ruled pages either side of the fold, with him holding the pencil above it.
prop_kuwaderno is the wrong object for that beat: it is a shut blue hardback,
drawn face-on, which reads as a book put down rather than a book being
written in.

Drawn rather than cut, for the same reason the banig in Tools/make_park_props
is drawn: ruled lines and a fold are a regular pattern, and regular patterns
come out cleaner drawn than traced.

In perspective, like the banig. The desk recedes, so the book is a trapezium
- its far edge shorter than its near edge - and the rules follow that taper.
A flat rectangle laid on a desk with depth in it looks like a sticker.

Writes  Assets/Sprites/prop_notebook_open.png

Run from the repository root:  python Tools/make_open_notebook.py
"""
from PIL import Image, ImageDraw, ImageFilter

OUT = "Assets/Sprites/prop_notebook_open.png"

PAGE = (252, 250, 243)
PAGE_SHADE = (232, 229, 218)
RULE = (188, 198, 214)
FOLD = (205, 200, 188)
EDGE = (120, 112, 98)
SHADOW = (60, 45, 30, 80)

# Wide and shallow on purpose. The book has to fit between the desk's far
# edge (-3.01) and its near edge (-3.96) - under a world unit of height -
# because anything taller pokes up past the seated sprite's bottom edge and
# gets drawn across his shirt. Width is the only way left to give it presence,
# and a book seen at this shallow an angle IS wide and flat.
W, H = 980, 300


def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def main():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    back_in, front_in = 124, 23
    top, bottom = 84, 250
    tl, tr = (back_in, top), (W - back_in, top)
    br, bl = (W - front_in, bottom), (front_in, bottom)

    # Sits on the desk rather than floating.
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(shade).polygon(
        [(x, y + 9) for x, y in (tl, tr, br, bl)], fill=SHADOW)
    img = Image.alpha_composite(img, shade.filter(ImageFilter.GaussianBlur(8)))

    d = ImageDraw.Draw(img)
    d.polygon([tl, tr, br, bl], fill=PAGE)

    # The gutter: both pages dip towards the centre fold.
    mid_top = lerp(tl, tr, 0.5)
    mid_bottom = lerp(bl, br, 0.5)
    for side in (0, 1):
        a = lerp(tl, mid_top, 1.0) if side else lerp(mid_top, tl, 1.0)
        gutter = [mid_top, mid_bottom,
                  lerp(mid_bottom, bl if side == 0 else br, 0.07),
                  lerp(mid_top, tl if side == 0 else tr, 0.07)]
        d.polygon(gutter, fill=PAGE_SHADE)
    d.line([mid_top, mid_bottom], fill=FOLD, width=3)

    # Ruled lines, converging with the page edges, skipping the gutter.
    for page, (o_top, o_bot, i_top, i_bot) in enumerate(
            ((tl, bl, mid_top, mid_bottom), (mid_top, mid_bottom, tr, br))):
        for i in range(1, 8):
            t = (i / 8.0) ** 1.12
            a = lerp(o_top, o_bot, t)
            b = lerp(i_top, i_bot, t)
            inset_a = lerp(a, b, 0.10)
            inset_b = lerp(a, b, 0.90)
            d.line([inset_a, inset_b], fill=RULE, width=2)

    d.polygon([tl, tr, br, bl], outline=EDGE, width=4)

    # Crop away the transparent margin. Without this the sprite carries about
    # 28% padding at the top and 17% at the bottom, so the DRAWN book ends up
    # far smaller than the world height the builder gives it - which left a
    # band of bare desk above and below the book and made it look like it had
    # been dropped in the middle of the table rather than laid at its edge.
    # Cropped to its own ink, the builder's numbers mean what they say.
    img = img.crop(img.getbbox())
    img.save(OUT)
    print(OUT, img.size, "(cropped to the drawing)")


if __name__ == "__main__":
    main()
