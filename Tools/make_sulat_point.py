"""Draw the point of Kylo's pencil, on a layer that is allowed to touch the page.

THE PROBLEM, WHICH IS SORTING AND NOT ART.

Kylo is drawn at sorting order 0 and the near desk is laid over him at order
10, which is what lets every seated pose in this scene be cut off flat at the
desk's far edge and still read as a boy behind a desk. The notebook is a prop
at order 20, lying on the wood below that edge.

So the lowest pixel of Kylo that is ever SEEN is the desk's own painted top
edge - about world -2.94 where the notebook is - and the notebook's page
begins at -3.05. Between his hand and the paper there is a tenth of a world
unit of desk, and nothing drawn into his sprite can cross it. His pencil
stops in mid-air above the near edge, which is exactly what the writing
frames looked like: a boy holding a pencil over a notebook he never touches.

Lowering him is not available (the desk would stop hiding the cut) and
raising the notebook is not either (it is in front of him, so it would be
drawn over his hand rather than under it).

WHAT THIS IS. The last two centimetres of the pencil - barrel, wooden cone
and graphite - drawn as its own sprite, placed on Kylo's exact transform but
at sorting order 22, in front of the desk AND in front of the notebook. It
picks the pencil up where the drawn one disappears behind the desk's edge and
carries it down onto the page.

It is NOT a whole second Kylo. Nothing of him is duplicated, nothing has to
be erased from the body frames, and if it is ever removed the writing frames
go back to what they were rather than falling apart.

HOW IT STAYS WITH HIS HAND. PoseSequence.companion drives this renderer frame
for frame with the writing loop, out of the same coroutine - see the note on
that field. Two PoseSequences would have been two coroutines and two
WaitForSeconds, and a single frame of drift at five frames a second is a
pencil point a fifth of a second behind the hand holding it.

THE GEOMETRY, measured off lapis_frame_1_uniform, which every writing frame
is built from:

  the barrel      runs from (245, 843) to (283, 890), so 39 degrees off
                  vertical, leaning right. The continuation follows the same
                  line - a pencil is straight, and a kink at the desk edge
                  would be worse than the gap it replaces.
  the overlap     it starts at row 885, four rows ABOVE where the drawn one
                  ends, so the two meet under the desk's edge rather than
                  either side of it.
  the point       lands at about (321, 977) on this canvas, which is world
                  (2.80, -3.11). The notebook's page runs -3.05 to -3.78, so
                  that is a hand's breadth onto the paper.

CANVAS 663x1001: the seated canvas with 40 rows added at EACH end. Both ends,
so the middle of the canvas is still the middle and this sprite can take
Kylo's own position and scale unchanged; the forty at the bottom are the rows
below the desk's edge that his own sprite does not have.

Writes  Assets/Sprites/sulat_point_uniform.png

Run from the repository root:  python Tools/make_sulat_point.py
"""
import io
import math
import os
import re
import uuid

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

W, H = 663, 1001
PAD = (H - 921) // 2                 # 40

# The drawn pencil's own line, in lapis_frame_1's pixels. The continuation
# starts at the lower end of it.
BARREL = ((245, 843), (283, 890))

# HOW FAR OFF VERTICAL THE CONTINUATION LEANS, which is NOT the barrel's own
# 39 degrees, and the reason is worth setting out.
#
# Followed at 39 degrees the point comes down at world x 2.80. The notebook's
# centre fold is at 2.86. So the pencil wrote in the gutter - the one place
# on an open book nobody writes - and the only ways to move it were to move
# his arm (which cannot be done; see make_sulat_hand_frames) or to move the
# notebook (which cannot go more than 0.15 left before its near corner is off
# the desk).
#
# Straight down instead, and the point lands at 2.565, which is on the
# left-hand page near the end of a line. THE BEND IS NOT VISIBLE, because of
# where it is: the desk's painted top edge crosses this sprite at about row
# 909 and the page begins at about row 951, so rows 909 to 951 are behind the
# desk. The bend is at row 885, under it. What the eye actually gets is the
# barrel going under the desk's edge at world 2.66 and the point coming out
# on the paper at 2.565 - a tenth of a unit across, which reads as
# perspective rather than as a kink.
LEAN = 0.0

# Where the continuation starts and ends along that line, as rows of the
# ORIGINAL 921 canvas. START is above the end of the drawn barrel so the two
# overlap under the desk's edge.
START_ROW = 885
END_ROW = 950

# Taken from lapis_frame_1: the barrel is about 15 pixels across where it
# leaves his fingers.
WIDTH = 15

# The same offsets the writing frames slide his hand by - see
# Tools/make_sulat_hand_frames.py. They have to match, or the point walks
# away from the hand.
SHIFTS = [0]

BODY = (238, 186, 58)                # pencil yellow, from prop_lapis
BODY_DARK = (206, 152, 40)
WOOD = (226, 196, 150)
LEAD = (58, 54, 52)
INK = (22, 20, 20)

SS = 4                               # drawn at 4x and shrunk


def meta_for(name, src="sitting_front_uniform"):
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, src + ".png.meta"),
                   encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base,
               count=1, flags=re.M)
    t = t.replace(src + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def point_sprite(shift):
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    (ax, ay), (bx, by) = BARREL
    rad = math.radians(LEAN)
    ux, uy = math.sin(rad), math.cos(rad)   # along the continuation, downward
    px = -uy                                # across it, horizontally

    def at(row, across=0.0):
        """A point on the continuation at the given ORIGINAL-canvas row."""
        t = (row - by) / uy
        return ((bx + ux * t + px * across + shift) * SS,
                (row + PAD) * SS)

    half = WIDTH * 0.5
    line = max(2, int(0.004 * W * SS))

    # The barrel, up to where the wood is shaved back.
    cone_row = END_ROW - 26
    d.polygon([at(START_ROW, -half), at(START_ROW, half),
               at(cone_row, half), at(cone_row, -half)], fill=BODY)
    # A darker edge down the right-hand side, the same two-tone prop_lapis has.
    d.polygon([at(START_ROW, half * 0.3), at(START_ROW, half),
               at(cone_row, half), at(cone_row, half * 0.3)], fill=BODY_DARK)

    # The shaved wood, tapering to the lead.
    lead_row = END_ROW - 9
    d.polygon([at(cone_row, -half), at(cone_row, half),
               at(lead_row, half * 0.34), at(lead_row, -half * 0.34)],
              fill=WOOD)

    # The graphite.
    d.polygon([at(lead_row, -half * 0.34), at(lead_row, half * 0.34),
               at(END_ROW, 0)], fill=LEAD)

    # Outline round the whole thing, so it reads against the page the way the
    # drawn pencil reads against his shirt.
    d.line([at(START_ROW, -half), at(cone_row, -half), at(lead_row, -half * 0.34),
            at(END_ROW, 0), at(lead_row, half * 0.34), at(cone_row, half),
            at(START_ROW, half)], fill=INK, width=line, joint="curve")

    return img.resize((W, H), Image.LANCZOS)


def main():
    print("the point of the pencil, on its own layer:")
    for shift in SHIFTS:
        name = "sulat_point_uniform"
        point_sprite(shift).save(os.path.join(SPRITES, name + ".png"))
        meta_for(name)
        print("  %-26s hand %+3d px" % (name + ".png", shift))
    print("\nWire in BuildPaaralanScene3 as sulatFrames.companion, with")
    print("  companionLoopFrames  point_1, point_2, point_3, point_2")


main()
