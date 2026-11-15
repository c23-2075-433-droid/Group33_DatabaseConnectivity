"""Draw the textbook OPEN - held up to read, and lying shut on the desk.

Two sprites, both the same book as prop_aklat_held, because Paaralan Scene 3
now asks the book to be in three states and the child has to recognise it as
one object through all of them:

  prop_aklat_closed   shut, lying flat on the desk. This is what "Aklat"
                      leaves behind - he takes a BOOK out of his bag and puts
                      it down, shut, the way a book arrives on a desk. The
                      old prop_aklat was already open before he had touched
                      it, which is the thing "Basa" is supposed to do.
  prop_aklat_open     open, seen from BEHIND, for his hands during "Basa".

WHY THE OPEN ONE LOOKS LIKE THIS. A reader sees the pages; everyone in front
of the reader sees the back of the book. So this is two covers making a
shallow V away from the camera, with a sliver of page block along the top of
each where the leaves are seen edge-on, and the spine down the middle. It is
NOT two white pages - drawing those would be drawing the book from the
reader's side, which is a view the camera is not at, and a child holding a
book of blank white pages towards the class reads as showing it to them.

The covers tilt six degrees each. Flat they read as one wide board; much more
and the book looks folded back on itself, which paperbacks do and hardbacks
do not.

THE DESK ONE IS FLAT VECTOR, NO OUTLINE, like every other thing on that desk
- see Tools/make_classroom_props.py and make_open_notebook.py. The held ones
are outlined in black, because they sit against his white shirt and the
outline is the only thing that separates them from it. That split is already
the project's convention and this keeps it.

Both use prop_aklat_held's palette, which came from a Canva generation kept
at Art/aklat_book_raw.png. See Tools/make_aklat_book.py.

Writes  Assets/Sprites/prop_aklat_closed.png
        Assets/Sprites/prop_aklat_open.png

Run from the repository root:  python Tools/make_aklat_open_book.py
"""
import io
import os
import re
import uuid

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

# prop_aklat_held's palette, so the three states are one book.
COVER = (150, 38, 44)
COVER_DARK = (112, 26, 32)
COVER_LIT = (178, 62, 64)
PAGES = (243, 237, 222)
PAGE_LINE = (211, 202, 184)
PAGE_SHADE = (224, 217, 201)
LABEL = (246, 241, 228)
LABEL_EDGE = (203, 192, 170)
INK = (22, 20, 20)

SS = 3                              # drawn at 3x and shrunk


def shrink(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


# --------------------------------------------------------------------------
# The shut book on the desk
# --------------------------------------------------------------------------

# The SAME BOX as prop_aklat, 520x300, so BuildPaaralanScene3's AklatX /
# AklatHeight keep meaning what they meant and the shut book lands where the
# open one used to. The ink inside it is smaller, because a shut book covers
# less desk than an open one - that is the point of it being shut.
CLOSED_W, CLOSED_H = 520, 300


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def closed_book():
    """The shut book, as a SOLID - a lid, a block of paper and a spine.

    The first version of this was a trapezium with a two-pixel lip of cream
    along its near edge, and on the desk it read as a maroon card someone had
    dropped there. It was flat because it WAS flat: one face, no thickness,
    nothing to tell an eye how deep the object is.

    A book on a desk shows three faces from this camera, and all three have
    to be drawn:

      the lid      the cover, a trapezium because the desk recedes - the far
                   edge shorter than the near one, the same reasoning as
                   make_open_notebook's.
      the block    the pages, seen edge-on below the near edge of the lid.
                   This is the face that does the work: its height IS the
                   thickness of the book, and the leaves ruled across it are
                   what stop it reading as a painted stripe.
      the spine    the left-hand end, dark and rounded, with its own narrow
                   face below the lid.

    THE COVER OVERHANGS, by about a fiftieth of the width on the near and
    right sides. A hardback's boards are bigger than its pages and the
    shadow that overhang casts on the block is most of what makes the thing
    read as bound rather than as printed on.

    SHADING GOES BY FACE, not by edge. The lid takes the light, the near
    block is a shade down from it, the spine's face darker still. Three flat
    values at three angles is how every other prop in this classroom is lit.
    """
    w, h = CLOSED_W * SS, CLOSED_H * SS
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx = w // 2
    # SIZED AGAINST THE NOTEBOOK BESIDE IT, not against its own box. At the
    # first pass this drew 0.81 x 0.54 world units next to an open notebook
    # 3.91 across, and it read as a playing card. A school textbook shut is
    # about a third of the notebook's spread.
    far_half, near_half = int(0.355 * w), int(0.415 * w)

    # THE FAR EDGE IS AT -3.01, not higher. This prop draws at sorting order
    # 20, above Kylo and above the desk, so anything of it that reaches past
    # the desk's own far edge is painted over the room behind. At x 5.30 that
    # edge is at about -2.99, and 0.19 of this box puts the lid's far corners
    # at -3.03, a hair inside it.
    top = int(0.19 * h)
    near = int(0.72 * h)                 # the near edge of the LID
    thick = int(0.115 * h)               # how deep the book is
    over = int(0.020 * w)                # the boards' overhang

    lid = [(cx - far_half, top), (cx + far_half, top),
           (cx + near_half, near), (cx - near_half, near)]

    # --- the page block, below the lid and inset by the overhang ---
    bl, br = cx - near_half + over, cx + near_half - over
    fl, fr = cx - far_half + over, cx + far_half - over
    d.polygon([(fl, top + thick // 2), (fr, top + thick // 2),
               (br, near + thick), (bl, near + thick)], fill=PAGES)
    # Its near face, a shade down - this is the one facing the camera.
    d.polygon([(bl, near), (br, near), (br, near + thick), (bl, near + thick)],
              fill=PAGE_SHADE)
    # The leaves across it. Four lines is enough to say "paper" and few
    # enough that they do not turn into a stripe pattern at sprite size.
    for i in range(1, 5):
        y = near + int(i * thick / 5.0)
        inset = int(i * over * 0.25)
        d.line([(bl + inset, y), (br - inset, y)], fill=PAGE_LINE,
               width=max(1, SS))

    # --- the spine, the left-hand end of the book ---
    spine_w = int(0.055 * w)
    d.polygon([(cx - far_half, top), (cx - far_half + spine_w, top),
               (cx - near_half + spine_w, near), (cx - near_half, near)],
              fill=COVER_DARK)
    # Its own face below the lid, darker again: it is turned away from the
    # window, which in this room is on the right.
    d.polygon([(cx - near_half, near), (cx - near_half + spine_w, near),
               (cx - near_half + spine_w, near + thick),
               (cx - near_half, near + thick)],
              fill=mix(COVER_DARK, (0, 0, 0), 0.25))

    # --- the lid ---
    d.polygon(lid, fill=COVER)
    d.polygon([(cx - far_half, top), (cx - far_half + spine_w, top),
               (cx - near_half + spine_w, near), (cx - near_half, near)],
              fill=COVER_DARK)
    # The hinge, where the board folds over the spine.
    d.line([(cx - far_half + spine_w, top), (cx - near_half + spine_w, near)],
           fill=mix(COVER_DARK, (0, 0, 0), 0.3), width=max(2, int(0.004 * w)))
    # Light along the far edge, and the board's own shadow falling on the
    # block along the near one.
    d.line([(cx - far_half, top), (cx + far_half, top)], fill=COVER_LIT,
           width=int(0.014 * w))
    d.line([(cx - near_half, near), (cx + near_half, near)],
           fill=mix(COVER, (0, 0, 0), 0.30), width=int(0.012 * w))

    # --- the label ---
    lw, lh = int(0.20 * w), int(0.20 * h)
    ly = top + int(0.32 * (near - top))
    skew = int(0.010 * w)
    d.polygon([(cx - lw // 2 + skew + spine_w // 2, ly),
               (cx + lw // 2 + skew + spine_w // 2, ly),
               (cx + lw // 2 + spine_w // 2, ly + lh),
               (cx - lw // 2 + spine_w // 2, ly + lh)],
              fill=LABEL, outline=LABEL_EDGE, width=max(1, SS))

    img = shrink(img, CLOSED_W, CLOSED_H)

    # --- what it casts on the desk ---
    # Tight under the near edge and spreading to the right, away from the
    # window. A book sitting ON something has a shadow that starts at its
    # own foot; one that floats has a shadow all round it.
    out = Image.new("RGBA", (CLOSED_W, CLOSED_H), (0, 0, 0, 0))
    shade = Image.new("RGBA", (CLOSED_W, CLOSED_H), (0, 0, 0, 0))
    s = CLOSED_W / float(w)
    ImageDraw.Draw(shade).polygon(
        [((cx - far_half) * s + 4, (top + thick // 2) * s + 6),
         ((cx + far_half) * s + 14, (top + thick // 2) * s + 6),
         ((cx + near_half) * s + 20, (near + thick) * s + 7),
         ((cx - near_half) * s + 6, (near + thick) * s + 7)],
        fill=(52, 34, 24, 95))
    out.alpha_composite(shade.filter(ImageFilter.GaussianBlur(7)))
    out.alpha_composite(img)
    return out


# --------------------------------------------------------------------------
# The open book in his hands
# --------------------------------------------------------------------------

# Wider than it is tall, which is what an open book is, and tall enough to
# cover the shut one's footprint in place_frame_2 so no red rectangle is left
# sticking out behind it.
OPEN_W, OPEN_H = 880, 640

TILT = 6.0                          # degrees each cover leans away


def open_book():
    """The book open, seen from behind: ONE silhouette, not two boards.

    The first version of this drew each cover separately and turned them six
    degrees apart. Rotating two rectangles about their own centres opens a V
    between them at the bottom, and a maroon shape with a notch up the middle
    is a pair of shorts. It looked like one.

    So the whole outline is a single polygon. The spine is the NEAREST part
    of a book held open towards the class, so it is the tallest - the outline
    peaks at the middle and falls away to the outer edges, which is the whole
    of the perspective this needs. The covers are then shaded apart rather
    than drawn apart.
    """
    w, h = OPEN_W * SS, OPEN_H * SS
    line = max(3, int(0.013 * OPEN_W)) * SS
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    pad = 2 * line
    x0, x1 = pad, w - pad
    cx = w // 2
    drop = int(0.055 * h)               # how far the outer edges fall away
    top, bot = pad + drop, h - pad - drop

    board = [(x0, top), (cx, pad), (x1, top), (x1, bot), (cx, h - pad), (x0, bot)]
    d.polygon(board, fill=COVER)

    # The leaves, seen edge-on along the top edge of each cover. A band that
    # follows the slope rather than a straight strip, or it would float off
    # the board at the outer corners.
    page = int(0.075 * h)
    d.polygon([(x0, top), (cx, pad), (x1, top),
               (x1, top + page), (cx, pad + page), (x0, top + page)],
              fill=PAGES)
    for i in (1, 2):
        off = int(page * (0.3 + 0.3 * i))
        d.line([(x0, top + off), (cx, pad + off)], fill=PAGE_LINE,
               width=max(1, line // 2))
        d.line([(cx, pad + off), (x1, top + off)], fill=PAGE_LINE,
               width=max(1, line // 2))

    # One cover catches the light and the other does not, which is what
    # separates them now that they are one shape.
    d.polygon([(x0, top + page), (cx, pad + page), (cx, h - pad), (x0, bot)],
              fill=COVER_LIT)
    d.polygon([(cx, pad + page), (x1, top + page), (x1, bot), (cx, h - pad)],
              fill=COVER)

    # The spine down the middle, over the join.
    sw = int(0.020 * w)
    d.polygon([(cx - sw, pad + page), (cx + sw, pad + page),
               (cx + sw, h - pad), (cx - sw, h - pad)], fill=COVER_DARK)

    # The outline last, round the whole silhouette, so the book reads against
    # his white shirt.
    d.line(board + [board[0]], fill=INK, width=line, joint="curve")
    d.line([(cx, pad + page), (cx, h - pad)], fill=INK, width=max(2, line // 2))
    d.line([(x0, top + page), (cx, pad + page)], fill=INK, width=max(2, line // 2))
    d.line([(cx, pad + page), (x1, top + page)], fill=INK, width=max(2, line // 2))

    return shrink(img, OPEN_W, OPEN_H)


def meta_for(name, src):
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, src + ".png.meta"),
                   encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base,
               count=1, flags=re.M)
    t = t.replace(src + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def main():
    c = closed_book()
    c.save(os.path.join(SPRITES, "prop_aklat_closed.png"))
    meta_for("prop_aklat_closed", "prop_aklat")
    print("prop_aklat_closed.png  %dx%d" % c.size)

    o = open_book()
    o.save(os.path.join(SPRITES, "prop_aklat_open.png"))
    meta_for("prop_aklat_open", "prop_aklat")
    print("prop_aklat_open.png    %dx%d" % o.size)


main()
