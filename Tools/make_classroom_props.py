"""Draw the two desk props Paaralan Scene 2 needs: a pencil and a notebook.

Scene 2 teaches "Kuha" (take out the notebook) and "Lapis" (take out the
pencil). Both objects exist in the classroom painting, but they are painted
into menu_bg.png on desks far from where Kylo sits, and a pencil is a thin
object lying across wood of almost its own colour - it does not separate on
colour the way the bathroom items or the uniform shirt did, so the cut-out
tools in this folder cannot lift it.

So these two are drawn instead of cut. They are small, they are seen at desk
size on a phone, and a clean shape with soft shading reads better there than
a smeared cut-out would. The palette is taken from the classroom art: warm
yellow, brown wood, the muted blue of the Filipino textbook on the front desk.

Writes, overwriting:

  Assets/Sprites/prop_lapis.png       a yellow school pencil, point to the left
  Assets/Sprites/prop_kuwaderno.png   a closed notebook, seen from the front
  Assets/Sprites/prop_aklat.png       an open textbook, for Scene 3's "Aklat"

Run from the repository root:  python Tools/make_classroom_props.py
"""
from PIL import Image, ImageDraw, ImageFilter

OUT = "Assets/Sprites/"

# Lifted off menu_bg.png so the props sit in the same light as the room.
YELLOW = (232, 178, 58)
YELLOW_DARK = (196, 141, 38)
YELLOW_LIGHT = (247, 206, 104)
WOOD = (214, 170, 120)
WOOD_DARK = (176, 132, 86)
GRAPHITE = (62, 56, 54)
FERRULE = (178, 180, 184)
FERRULE_DARK = (138, 141, 146)
ERASER = (226, 134, 126)
COVER = (74, 108, 148)        # the blue of "Filipino" on the front desk
AKLAT = (142, 66, 54)         # the red of "Matematika", so the two books differ
AKLAT_DARK = (108, 46, 38)
COVER_DARK = (52, 80, 114)
PAGE = (246, 241, 229)
PAGE_EDGE = (222, 214, 196)
SHADOW = (60, 40, 24, 70)


def shadow_under(img, box, blur=9, inset=6):
    """A soft contact shadow, so the prop sits on the desk instead of floating."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x0, y0, x1, y1 = box
    d.ellipse([x0 + inset, y1 - 14, x1 - inset, y1 + 10], fill=SHADOW)
    layer = layer.filter(ImageFilter.GaussianBlur(blur))
    return Image.alpha_composite(layer, img)


def make_pencil():
    """A classic school pencil lying flat, its point to the left."""
    W, H = 440, 150
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    top, bot = 54, 96          # the barrel
    mid = (top + bot) // 2
    tip_x, barrel_x0, barrel_x1 = 26, 112, 352

    # Wooden cone, then the graphite point itself.
    d.polygon([(tip_x, mid), (barrel_x0, top), (barrel_x0, bot)], fill=WOOD)
    d.polygon([(tip_x, mid), (barrel_x0, top), (barrel_x0, mid)], fill=WOOD_DARK)
    d.polygon([(tip_x, mid), (tip_x + 26, mid - 9), (tip_x + 26, mid + 9)], fill=GRAPHITE)

    # Barrel, with a lighter band along the top so it reads as round.
    d.rectangle([barrel_x0, top, barrel_x1, bot], fill=YELLOW)
    d.rectangle([barrel_x0, top, barrel_x1, top + 12], fill=YELLOW_LIGHT)
    d.rectangle([barrel_x0, bot - 11, barrel_x1, bot], fill=YELLOW_DARK)

    # Metal ferrule and the eraser on the end.
    d.rectangle([barrel_x1, top, barrel_x1 + 44, bot], fill=FERRULE)
    d.rectangle([barrel_x1, bot - 11, barrel_x1 + 44, bot], fill=FERRULE_DARK)
    for gx in range(barrel_x1 + 10, barrel_x1 + 40, 12):
        d.line([(gx, top + 3), (gx, bot - 3)], fill=FERRULE_DARK, width=2)
    d.rounded_rectangle([barrel_x1 + 44, top + 1, barrel_x1 + 86, bot - 1],
                        radius=12, fill=ERASER)

    img = shadow_under(img, (tip_x, top, barrel_x1 + 86, bot))
    img.save(OUT + "prop_lapis.png")
    return img.size


def make_notebook():
    """A closed notebook, pages to the right, seen square on."""
    W, H = 420, 320
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    x0, y0, x1, y1 = 40, 44, 380, 268

    # The block of pages, peeking out from under the cover.
    d.rounded_rectangle([x0 + 14, y0 + 10, x1 + 8, y1 + 8], radius=6, fill=PAGE)
    d.rounded_rectangle([x0 + 14, y1 - 2, x1 + 8, y1 + 8], radius=4, fill=PAGE_EDGE)

    # The cover on top of them.
    d.rounded_rectangle([x0, y0, x1, y1], radius=10, fill=COVER)
    d.rounded_rectangle([x0, y0, x0 + 34, y1], radius=10, fill=COVER_DARK)   # the spine
    # A paper label, like the textbooks on the front desk.
    d.rounded_rectangle([x0 + 70, y0 + 46, x1 - 46, y0 + 130], radius=6, fill=PAGE)
    for ly in range(y0 + 66, y0 + 122, 22):
        d.line([(x0 + 92, ly), (x1 - 72, ly)], fill=PAGE_EDGE, width=5)

    img = shadow_under(img, (x0, y0, x1 + 8, y1 + 8))
    img.save(OUT + "prop_kuwaderno.png")
    return img.size


def make_textbook():
    """
    An OPEN textbook, seen from slightly above - Scene 3's "Aklat".

    Open rather than closed, so it reads differently from the notebook sitting
    beside it on the same desk. Two closed books of different colours look
    like one object duplicated; an open one looks like a book being read.
    """
    W, H = 520, 300
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    spine_x, top, bot = W // 2, 70, 236

    # Two pages, each tilting away from the spine.
    d.polygon([(spine_x, top), (56, top + 26), (40, bot - 10), (spine_x, bot)], fill=PAGE)
    d.polygon([(spine_x, top), (W - 56, top + 26), (W - 40, bot - 10), (spine_x, bot)], fill=PAGE)
    # The page block's thickness along the outer edges.
    d.polygon([(40, bot - 10), (spine_x, bot), (spine_x, bot + 16), (34, bot + 4)], fill=PAGE_EDGE)
    d.polygon([(W - 40, bot - 10), (spine_x, bot), (spine_x, bot + 16), (W - 34, bot + 4)], fill=PAGE_EDGE)

    # The cover showing round the outside of the pages.
    d.line([(56, top + 26), (40, bot - 10)], fill=AKLAT, width=10)
    d.line([(W - 56, top + 26), (W - 40, bot - 10)], fill=AKLAT, width=10)
    d.line([(spine_x, top), (spine_x, bot)], fill=AKLAT_DARK, width=7)

    # Lines of text, converging towards the spine like the pages do.
    for i in range(5):
        y = top + 54 + i * 26
        inset = 16 + i * 3
        d.line([(74 + inset, y), (spine_x - 26, y - 6)], fill=PAGE_EDGE, width=6)
        d.line([(spine_x + 26, y - 6), (W - 74 - inset, y)], fill=PAGE_EDGE, width=6)

    img = shadow_under(img, (34, top, W - 34, bot + 16), blur=11)
    img.save(OUT + "prop_aklat.png")
    return img.size


if __name__ == "__main__":
    print("prop_lapis.png     ", make_pencil())
    print("prop_kuwaderno.png ", make_notebook())
    print("prop_aklat.png     ", make_textbook())
