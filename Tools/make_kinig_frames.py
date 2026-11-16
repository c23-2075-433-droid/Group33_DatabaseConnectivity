"""Draw the four "Kinig" listening frames by moving Kylo's own head.

WHAT THEY ARE. Kylo sitting at his desk listening to Teacher Rachel: his head
tilts a little towards her, and he nods twice to show he is following. He stays
in his chair and nothing else about him moves, which is what the beat asks for -
the word is listening, not standing up.

WHY THIS ONE IS NOT A CANVA GENERATION, unlike every other frame tool here.

Docs/AnimationStandard.md's pipeline is: generate a strip of three poses from
the existing sprite, download it full size by hand, cut and fit it. That is the
right way to get a NEW pose - a reach, a sit, a hand going up - because the body
changes shape and no amount of moving pixels will invent an arm in a new place.

A listen is not a new pose. It is the pose he is already in with his head at a
slightly different angle, and his head is drawn once, in sitting_front_uniform,
with a hard black outline all the way round it. Rotating that drawing by five
degrees is the SAME drawing of the same boy - rule 7's consistent proportions
hold by construction rather than by a generation coming back lucky. It also
costs no generation, no manual download and no hair-cap normalisation, and it
can be re-run and tuned in seconds, which a strip cannot.

The limit is real and worth stating: this can only move what is already drawn.
He cannot turn his head to look at her, because there is no three-quarter view
of his face to turn to. A tilt and a nod are what a front-facing drawing can
honestly do, and they are enough for the beat.

HOW THE CUT WORKS, because it is the whole trick.

His neck is the narrowest row of the whole sprite - 86 pixels across at y=560,
against 355 at the shoulders - so it finds itself: scan the rows below the chin
for the thinnest one. Everything above that row is the head; everything below is
the body, which must not move.

The head piece is cut a little BELOW the neck (NECK_SKIRT rows of it) and laid
over a body that still has its own neck underneath. That is what makes the seam
invisible. A rotation about a pivot on the neck moves a point by r*sin(angle),
so at five degrees the crown - 430 pixels up - swings 37 pixels, which is the
tilt you see, while the bottom of the piece - 18 pixels down - moves under two,
which is the seam you do not. Clearing the body's head first is what stops the
original head showing beside the rotated one.

The nod needs no rotation at all. Sliding the head piece down tucks the chin
towards the collar and opens a gap above the hair, which is exactly what a nod
does to a silhouette seen from the front.

CANVAS. All four frames are written at the source's own 663x921 with nothing
resized, because the seated poses share one canvas whose bottom edge is the
desk's far edge - see the notes in BuildPaaralanScene2.cs. Change the canvas and
he floats above the desk or sinks through it. The .meta is copied from
sitting_front_uniform's, which is what keeps these at its 100 pixels per unit:
a frame imported at Unity's default 100 against a base at 120 is how Kylo once
swelled a fifth every time he waved.

RUN IT AFTER THE TWO REPAIR TOOLS, not before. These frames are copies of
sitting_front_uniform with its head moved, so they inherit whatever is wrong
with it: run this on an unrepaired source and all four come out with the same
short shirt and the same white ring round them. The order is

    python Tools/fix_seated_hem.py
    python Tools/fix_seated_halo.py
    python Tools/make_kinig_frames.py

and both repair tools are idempotent, so running them again afterwards costs
nothing and proves it.

Writes  Assets/Sprites/kinig_frame_1..4_uniform.png

Run from the repository root:  python Tools/make_kinig_frames.py
"""
import io
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")
SRC = "sitting_front_uniform"

# Where to look for the neck: below the chin, above the desk line. The chin of
# this drawing is at about y=520 and the shoulders have taken over by y=640.
NECK_BAND = (500, 660)

# How far below the neck the head piece is cut, so it still covers the join
# once it has moved. Two pixels of travel at this distance, against 37 at the
# crown.
NECK_SKIRT = 18

# The tilt, in degrees, and the nod, in pixels. Both are deliberately small:
# this plays under Rachel's voice while the child is being asked to speak, and
# a child's attention should be on the word rather than on Kylo bobbing.
TILT_HALF = 2.5
TILT_FULL = 5.0
NOD_DROP = 14

# Which way he leans. Negative tilts the crown of his head towards the right of
# the frame, away from the lockers he came in by and towards the front of the
# class. Flip the sign if the shot is ever re-staged the other way round.
TILT_SIGN = -1.0


def meta_for(name):
    """Give a new sprite the same import settings as the one it came from."""
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, SRC + ".png.meta"), encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(SRC + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def find_neck(alpha, band):
    """The narrowest row of ink between band[0] and band[1] - his neck."""
    best, best_row = None, None
    for y in range(band[0], band[1]):
        xs = np.where(alpha[y] > 8)[0]
        if len(xs) == 0:
            continue
        width = xs.max() - xs.min() + 1
        if best is None or width < best:
            best, best_row = width, y
    xs = np.where(alpha[best_row] > 8)[0]
    return best_row, int((xs.min() + xs.max()) / 2), int(best)


def head_frame(src, neck_y, neck_x, degrees, drop):
    """One frame: his head rotated and/or slid, on a body that has not moved."""
    # TWO cut lines, not one, and the gap between them is the fix for a seam
    # this had on the first run.
    #
    # The piece's bottom edge is a straight line through the pivot, so a
    # rotation takes one end of it BELOW the cut - over the body, which is
    # harmless - and the other end ABOVE it. With the body cleared to the same
    # line, that end left a transparent wedge beside his neck and the classroom
    # showed through it.
    #
    # So the body is cleared 18 rows higher than the piece is cut, and keeps its
    # own neck in between. The wedge now lands on that neck instead of on
    # nothing. Those rows are 86 pixels of plain throat barely two pixels from
    # the pivot, so the kept original and the rotated copy of it differ by less
    # than the outline they share.
    piece_cut = neck_y + NECK_SKIRT
    body_cut = neck_y

    # The body, with its head taken off so the old one cannot show beside the
    # new one. Its own neck stays, which is what the moved head lands on.
    body = np.asarray(src).copy()
    body[:body_cut, :, 3] = 0

    # The head, on its own transparent canvas the size of the whole sprite, so
    # rotating it needs no bookkeeping about where the crop was.
    head = np.asarray(src).copy()
    head[piece_cut:, :, 3] = 0
    piece = Image.fromarray(head)
    cut = piece_cut

    if degrees:
        # Rotation about a point on the neck. PIL rotates about `center` and
        # keeps the canvas, which is exactly what is wanted here.
        piece = piece.rotate(degrees, resample=Image.BICUBIC,
                             center=(neck_x, cut), fillcolor=(0, 0, 0, 0))
    if drop:
        piece = piece.transform(piece.size, Image.AFFINE, (1, 0, 0, 0, 1, -drop),
                                resample=Image.BICUBIC, fillcolor=(0, 0, 0, 0))

    out = Image.fromarray(body)
    out.alpha_composite(piece)
    return out


def main():
    path = os.path.join(SPRITES, SRC + ".png")
    src = Image.open(path).convert("RGBA")
    alpha = np.asarray(src)[:, :, 3]

    neck_y, neck_x, neck_w = find_neck(alpha, NECK_BAND)
    print("%s %dx%d - neck at row %d, %d px across, centred on x=%d"
          % (SRC, src.size[0], src.size[1], neck_y, neck_w, neck_x))

    frames = [
        ("kinig_frame_1_uniform", TILT_HALF * TILT_SIGN, 0,   "head starting to turn"),
        ("kinig_frame_2_uniform", TILT_FULL * TILT_SIGN, 0,   "tilted, listening"),
        ("kinig_frame_3_uniform", TILT_FULL * TILT_SIGN, NOD_DROP // 2, "the nod going down"),
        ("kinig_frame_4_uniform", TILT_FULL * TILT_SIGN, NOD_DROP,      "the bottom of the nod"),
    ]

    for name, degrees, drop, what in frames:
        out = head_frame(src, neck_y, neck_x, degrees, drop)
        assert out.size == src.size, (name, out.size, src.size)
        out.save(os.path.join(SPRITES, name + ".png"))
        meta_for(name)
        print("  %-22s %5.1f deg, %2d px down   %s" % (name + ".png", degrees, drop, what))

    print("\nWire them in BuildPaaralanScene2 as the 'Kinig' PoseSequence:")
    print("  frames      1, 2        he turns his head and settles")
    print("  loopFrames  3, 4, 2     the nod, repeated")
    print("  tailFrames  1           his head comes back")
    print("  destination %s" % SRC)


main()
