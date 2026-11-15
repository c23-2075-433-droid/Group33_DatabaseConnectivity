"""Put the textbook into Kylo's hands for Paaralan Scene 3's "Aklat".

WHAT THE BEAT IS. He reaches into his bag, takes the book out, lifts it, lays
it on the desk and puts his hands down. Paaralan Scene 2 has that whole
movement drawn already - kuha_frame_1..3, reach_bag_uniform,
place_frame_1..3 - because it is the same movement with a different object:
there he takes out his NOTEPAD. So the frames are not drawn again. The object
in his hands is changed and nothing else is.

WHY THAT IS ALLOWED HERE. Docs/AnimationStandard.md's "some frames are a part
of him moved, not a new drawing of him" is about reusing the drawing of the
BOY. This is the other half of the same idea: his hands, his arms, his lean
and his shoulders are a child carrying a flat rectangular object from a bag to
a desk, and a book is a flat rectangular object. Rule 7 - consistent
proportions - holds by construction, because he is not redrawn at all.

The limit is the same one and worth stating: this cannot change his GRIP. The
book is the size and the angle the notepad was, because that is where his
fingers close. A bigger book, or one held differently, is a generation.

HOW THE SWAP WORKS, because the order matters.

  1. The book is scaled and rotated to the notepad's own footprint and laid
     over it. It is drawn a little LARGER than the notepad on every side -
     see the BOOKS table - because anything of the notepad left sticking out,
     its outline most of all, reads as a white rim round a red book.
  2. His hands are then put back ON TOP, from the original frame. Without
     this the book covers his fingers and he carries it like a tray.

Step 2 is a mask rather than a rectangle, and the mask is three things:

  skin        the fingers and forearms themselves, found by colour. His
              skin runs about (240,167,102) against a white shirt at
              (248,248,248) and the book's cover at (150,38,44), so red
              minus green separates it from the shirt and green minus blue
              separates it from the book.
  nail        the pale highlights on his fingertips, too washed out to pass
              the skin test and obvious as holes if left behind.
  outline     the dark line round each finger. NOT every dark pixel near the
              hand - that would bring the notepad's own outline back with it -
              but dark pixels within a few of skin, which is exactly the line
              the fingers are drawn with.

A frame that has the bag in front of the object adds a fourth, `navy`:
reach_bag_uniform is drawn with the backpack OVER the notepad's lower corner,
and a book pasted flat would sit in front of a bag it is supposed to be
inside.

Writes  Assets/Sprites/aklat_bag_uniform.png
        Assets/Sprites/aklat_frame_1..2_uniform.png

Run from the repository root (after Tools/make_aklat_book.py):
    python Tools/make_aklat_frames.py
"""
import io
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

# How far a dark pixel may be from skin and still count as a finger's own
# outline. The character art is outlined at about four pixels on this canvas,
# so six reaches the far side of the line and no further.
OUTLINE_REACH = 6

# source frame            out name               book            cx   cy    w    h   deg  restore
#
# The footprints are read off the frames themselves with a 50px grid - see
# the note at the top about why the book has to match the notepad's own
# footprint rather than choose its own. Whatever is drawn has to COVER that
# footprint, 212..458 x 608..892 in place_frame_2, or a white rim of notepad
# is left round a red book.
SHUT = "prop_aklat_held"
OPEN = "prop_aklat_open"

BOOKS = [
    # In the bag, gripped at its top corner, the backpack across its foot.
    ("reach_bag_uniform",     "aklat_bag_uniform",     SHUT, 551, 836, 176, 300,  20.0, True),
    # Lifted clear, held one-handed beside his shoulder, tilted.
    ("place_frame_1_uniform", "aklat_frame_1_uniform", SHUT, 540, 655, 190, 296,  10.0, False),
    # Square in front of his chest, a hand at each bottom corner, SHUT. This
    # is what he carries to the desk and what he picks back up for "Basa".
    ("place_frame_2_uniform", "aklat_frame_2_uniform", SHUT, 335, 750, 250, 288,   0.0, False),
    # Opening. The same drawing of the open book, squeezed sideways - a book
    # halfway open IS the open one seen at a narrower angle, so one sprite
    # does both and the covers cannot disagree between the two frames.
    ("place_frame_2_uniform", "aklat_open_a_uniform",  OPEN, 335, 747, 300, 292,   0.0, False),
    # Open, held up to read. The frame "Basa" is built from.
    #
    # 364 WIDE, NOT 430, AND THAT IS THE WHOLE OF WHAT WAS WRONG WITH IT.
    # An open book really is about twice its shut width, and drawn that way
    # it came out wider than his shoulders - 430 against 355 - so his
    # forearms disappeared behind it and his hands, which grip at 205..455,
    # ended up flat in the MIDDLE of the covers holding nothing. It read as
    # a boy pressing his palms against a board.
    #
    # His grip is fixed: it is place_frame_2's, drawn for a notepad 228
    # across, and nothing here can move his fingers. So the book is sized to
    # the grip instead of to a textbook - 364, which puts its edges about
    # fifty pixels outside his fingers, close enough to read as holding it,
    # and leaves his forearms in view either side. A book held up to read is
    # tilted back at the top anyway, and a tilted book is foreshortened: 364
    # is what that looks like.
    ("place_frame_2_uniform", "aklat_open_b_uniform",  OPEN, 335, 747, 364, 292,   0.0, False),
]


def classify(a):
    """Skin, pale nail, dark outline and navy, as boolean masks."""
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    on = al > 60
    skin = on & (r > 170) & (g > 85) & (g < 210) & (b < 175) & (r - g > 35) & (g - b > 25)
    nail = on & (r > 225) & (g > 190) & (b > 165) & (r - b > 22) & (r - b < 70)
    dark = on & (r < 95) & (g < 95) & (b < 95)
    navy = on & (b > 70) & (b - r > 18) & (r < 130)
    return skin, nail, dark, navy


def grow(mask, radius):
    """Binary dilation by a square of the given radius, in numpy."""
    out = mask.copy()
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            out |= np.roll(np.roll(mask, dy, axis=0), dx, axis=1)
    return out


def place_book(book, size, cx, cy, w, h, degrees):
    """The book, scaled and turned, on a transparent canvas of `size`."""
    piece = book.resize((int(w), int(h)), Image.LANCZOS)
    if degrees:
        # expand=True so the corners are not clipped off by the old box; the
        # result is then pasted by its own centre, which is unchanged by the
        # rotation.
        piece = piece.rotate(degrees, resample=Image.BICUBIC, expand=True)
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    layer.alpha_composite(piece, (int(cx - piece.size[0] / 2),
                                  int(cy - piece.size[1] / 2)))
    return layer


def swap(src_name, out_name, book_name, cx, cy, w, h, degrees, keep_bag):
    src = Image.open(os.path.join(SPRITES, src_name + ".png")).convert("RGBA")
    book = Image.open(os.path.join(SPRITES, book_name + ".png")).convert("RGBA")

    a = np.asarray(src).astype(int)
    skin, nail, dark, navy = classify(a)

    hand = skin | nail
    # The finger outlines: dark, and close to skin. Reaching from the hand
    # outwards rather than taking every dark pixel is what keeps the
    # notepad's own outline from coming back with them.
    hand = hand | (grow(hand, OUTLINE_REACH) & dark)
    if keep_bag:
        # The bag back in front of the book, WITHOUT reaching out for its
        # outline the way the hands do. The backpack's outline runs along the
        # notepad's, so growing into the dark here brings the notepad's own
        # edge back as a black scratch across the cover - which is what the
        # first run of this did. The bag keeps the outline that is not under
        # the book and loses the rest, and at this size the loss does not
        # read.
        #
        # OPENED FIRST, so only the BODY of the bag comes back. The navy test
        # also catches the zip pull, the stitching and the dark side of the
        # notepad's own wire spiral, and those came back as a scatter of
        # specks across the cover - a book that looked spattered. Eroding by
        # three pixels and growing back by four loses anything thinner than
        # that and keeps the bag itself, which is two hundred pixels across.
        body = grow(~grow(~navy, 3), 4) & navy
        hand = hand | body

    out = src.copy()
    out.alpha_composite(place_book(book, src.size, cx, cy, w, h, degrees))

    # His hands back on top of it.
    arr = np.asarray(out).copy()
    arr[hand] = np.asarray(src)[hand]
    out = Image.fromarray(arr, "RGBA")

    out.save(os.path.join(SPRITES, out_name + ".png"))
    meta_for(out_name, src_name)
    print("  %-24s from %-22s %-18s %dx%d at (%d,%d) %+.0f deg"
          % (out_name + ".png", src_name, book_name, w, h, cx, cy, degrees))


def meta_for(name, src):
    """The source pose's import settings, so the swap cannot change his size.

    A frame imported at Unity's default 100 pixels per unit against a base at
    another number is how a character swells mid-animation; copying the meta
    of the frame this one was made from makes that impossible.
    """
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, src + ".png.meta"), encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base,
               count=1, flags=re.M)
    t = t.replace(src + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def main():
    for book in (SHUT, OPEN):
        p = os.path.join(SPRITES, book + ".png")
        if not os.path.exists(p):
            raise SystemExit("run Tools/make_aklat_book.py and "
                             "Tools/make_aklat_open_book.py first - " + p +
                             " is not there")
    print("the notepad becomes a textbook:")
    for row in BOOKS:
        swap(*row)


main()
