"""Draw the textbook Kylo takes out of his bag in Paaralan Scene 3.

WHAT IT IS. A closed hardback seen face on - the thing his hands hold during
"Aklat" and hold up in front of him during "Basa". The open book already in
the project, prop_aklat, is the same book LYING ON THE DESK: cream pages, a
dark red cover showing round the outside. This one is that book shut, so the
object he carries and the object on the desk are recognisably one object.

WHERE THE DESIGN CAME FROM. Canva, asked for "a single school textbook for a
children's educational game sprite ... dark maroon red hardcover with a
slightly lighter red spine on the left edge and a thin stack of cream coloured
pages visible along the right edge. A plain cream label panel in the middle".
The generation is kept at Art/aklat_book_raw.png and this file is a redraw of
it, which needs saying plainly:

  THE CANVA API RETURNS A THUMBNAIL. The generation came back 150x199, and
  the book has to be about 250x295 in the seated canvas - so used directly it
  would be a 1.7x enlargement of a 150px image sitting against character art
  drawn at 663x921, soft and blurred against a crisp black outline.
  Docs/AnimationStandard.md's pipeline answers that for POSES by downloading
  the full-size file out of Canva Uploads by hand, because a drawing of a boy
  cannot be redrawn in code.

  A book can. It is a rounded rectangle, a spine band, a page block and a
  label - four shapes - so the generation serves as the design and this draws
  it at whatever size is asked for, perfectly sharp. That is also how every
  other prop in this project is made: see Tools/make_classroom_props.py, which
  draws prop_aklat, prop_lapis and prop_kuwaderno, and Tools/make_open_notebook.py.

THE PALETTE is prop_aklat's red moved towards the generation's deeper one, so
the shut book and the open one are the same book in the same classroom light.
Canva's cover is (133,16,27) and make_classroom_props' AKLAT is (142,66,54);
this sits between them.

THE OUTLINE IS BLACK, unlike the desk props, which have none. Those lie flat
on painted wood and read against it by their own edge. This one is held
against his white shirt, and the only thing that separates white paper from a
white tee is the outline the character art itself is drawn with - so the book
carries the same one, about 1.4% of its width, the weight of the notepad's
outline in place_frame_2.

Writes  Assets/Sprites/prop_aklat_held.png

Run from the repository root:  python Tools/make_aklat_book.py
"""
import io
import os
import re
import uuid

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")
OUT = os.path.join(SPRITES, "prop_aklat_held.png")

# The shape the holding poses want. place_frame_2 grips the notepad across
# 234x273 pixels of a 663x921 canvas, so this is drawn at the same ratio and
# at more than twice the resolution, which leaves room to rotate it into
# place_frame_1's tilted grip without softening the edges.
W, H = 560, 654

COVER = (150, 38, 44)          # between Canva's (133,16,27) and prop_aklat's (142,66,54)
COVER_DARK = (112, 26, 32)     # the spine, and the shaded lower edge
COVER_LIT = (178, 62, 64)      # the light down the top-left, as on his other props
PAGES = (243, 237, 222)        # prop_aklat's PAGE, a shade warmer
PAGE_LINE = (211, 202, 184)
LABEL = (246, 241, 228)
LABEL_EDGE = (203, 192, 170)
INK = (22, 20, 20)             # the character art's own outline colour

# 1.4% of the width. Measured off the notepad in place_frame_2, whose outline
# is 4px on a 234px object.
LINE = max(3, round(W * 0.019))


def main():
    # Drawn at 3x and shrunk. These are curves and thin bands on a sprite that
    # will be rotated into place, and a rounded corner drawn at final size and
    # then turned reads as a staircase.
    s = 3
    img = Image.new("RGBA", (W * s, H * s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    w, h, line = W * s, H * s, LINE * s
    pad = line                                   # room for the outline itself
    x0, y0, x1, y1 = pad, pad, w - pad, h - pad
    radius = int(0.055 * w)

    # --- the page block, peeking out along the right edge and the bottom ---
    # A real book's pages are inset from the cover on three sides; drawing the
    # block first and the cover over it is what gives that lip without having
    # to line two shapes up by hand.
    d.rounded_rectangle([x0, y0, x1, y1], radius=radius, fill=PAGES, outline=INK,
                        width=line)

    # The leaves. Faint, and only along the lip, so they read as a stack
    # rather than as ruling.
    lip = int(0.045 * w)
    for i in range(1, 7):
        yy = y1 - int(i * lip / 7.0)
        d.line([(x1 - lip, yy), (x1 - int(0.004 * w), yy)], fill=PAGE_LINE,
               width=max(1, s))
    for i in range(1, 7):
        xx = x1 - int(i * lip / 7.0)
        d.line([(xx, y0 + lip), (xx, y1 - int(0.004 * w))], fill=PAGE_LINE,
               width=max(1, s))

    # --- the cover, inset from the pages on the right and the bottom ---
    cx1, cy1 = x1 - lip, y1 - lip
    d.rounded_rectangle([x0, y0, cx1, cy1], radius=radius, fill=COVER,
                        outline=INK, width=line)

    # The light along the top-left and the shade along the bottom-right, the
    # same two-tone treatment make_classroom_props gives the shut notebook.
    d.line([(x0 + line, y0 + line * 2), (x0 + line, cy1 - radius)],
           fill=COVER_LIT, width=int(line * 1.2))
    d.line([(x0 + radius, y0 + line), (cx1 - radius, y0 + line)],
           fill=COVER_LIT, width=int(line * 1.2))

    # --- the spine, a band down the left edge ---
    spine = int(0.115 * w)
    d.rounded_rectangle([x0, y0, x0 + spine, cy1], radius=radius,
                        fill=COVER_DARK)
    # Square off the band's right-hand corners: a rounded rectangle that wide
    # curves away from the cover and reads as a second, smaller book.
    d.rectangle([x0 + spine - radius, y0, x0 + spine, cy1], fill=COVER_DARK)
    d.line([(x0 + spine, y0 + line), (x0 + spine, cy1 - line)], fill=INK,
           width=max(2, line // 2))

    # --- the label panel ---
    lw, lh = int(0.46 * w), int(0.20 * h)
    lx = x0 + spine + ((cx1 - (x0 + spine)) - lw) // 2
    ly = y0 + int(0.22 * h)
    d.rounded_rectangle([lx, ly, lx + lw, ly + lh], radius=int(0.02 * w),
                        fill=LABEL, outline=LABEL_EDGE, width=max(2, line // 2))
    # NO TEXT ON IT. The game is teaching the Tagalog word for "book" and the
    # badge on screen already carries that word; a title painted on the cover
    # would be a second, smaller word for a child to read instead.

    img = img.resize((W, H), Image.LANCZOS)

    # A shadow under the lower right, so it sits in the room's light rather
    # than floating. Same treatment as shadow_under in make_classroom_props.
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(shade).rounded_rectangle(
        [int(0.06 * W), int(0.06 * H), W - 2, H - 2],
        radius=int(0.055 * W), fill=(50, 30, 24, 70))
    out.alpha_composite(shade.filter(ImageFilter.GaussianBlur(int(0.016 * W))))
    out.alpha_composite(img)

    out.save(OUT)
    meta_for("prop_aklat_held")
    print("prop_aklat_held.png  %dx%d" % out.size)


def meta_for(name):
    """Import settings copied from a prop that is already right.

    Without a .meta of its own a PNG gets Unity's defaults, which in this
    project is not a Sprite at all - SpriteImportFixer exists because of that.
    Copying prop_aklat's keeps it at the same pixels per unit as the book it
    is the shut version of.
    """
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    src = "prop_aklat"
    base = io.open(os.path.join(SPRITES, src + ".png.meta"), encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base,
               count=1, flags=re.M)
    t = t.replace(src + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


main()
