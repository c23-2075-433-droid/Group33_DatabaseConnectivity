"""Draw the point of Kylo's pencil, on a layer that is allowed to touch the page.

THE PROBLEM, WHICH IS SORTING AND NOT ART.

Kylo is drawn at sorting order 0 and the near desk is laid over him at order
10, which is what lets every seated pose in this scene be cut off flat at the
desk's far edge and still read as a boy behind a desk. The notebook is a prop
at order 20, lying on the wood below that edge.

So the lowest pixel of Kylo that is ever SEEN is the desk's own painted top
edge - about world -2.93 where the notebook is - and the notebook's page
begins at -3.04. Between his hand and the paper there is a tenth of a world
unit of desk, and nothing drawn into his sprite can cross it. His pencil
stops in mid-air above the near edge, which is exactly what the writing
frames looked like: a boy holding a pencil over a notebook he never touches.

Lowering him is not available (the desk would stop hiding the cut) and
raising the notebook is not either (it is in front of him, so it would be
drawn over his hand rather than under it).

WHAT THIS IS. The last few centimetres of the pencil - barrel, shaved wood
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
is built from. It is ONE LINE, not two:

  the axis      the drawn pencil runs from the middle of its eraser at
                (230, 828) through the middle of its tip where the canvas
                cuts it off, 0.90 of a pixel right for every pixel down -
                42 degrees off vertical, leaning right. Drawing the
                continuation along that same line is the whole of what makes
                it the same pencil.
  the overlap   it starts at row 902, a few rows ABOVE the desk's painted
                edge, and fades in over the fourteen rows below that. His
                index finger crosses the barrel around there, so anything
                drawn at full strength that high would be painted over his
                fingertip; what the fade gives is a pencil that is already
                solid by the time the wood lets go of it.
  the point     lands at (366, 979) on the 921-row canvas, which is world
                (3.07, -3.36). The notebook's gutter is the pale strip from
                2.75 to 2.93 and the right-hand page starts there, so at
                every one of the five hand positions - 2.98 to 3.15 - the
                point is on the page, near the left margin, a third of the
                way down. The first version of this file came down the
                vertical instead, to avoid writing in the gutter, and what
                that bought was a second pencil: a yellow bar standing
                straight up out of his fist while the drawn one leaned away
                from it at 42 degrees.

WHY IT IS NOT STILL. The hand travels - see Tools/make_sulat_hand_frames.py -
so there is one of these per writing frame, shifted by the same number of
pixels. SHIFTS below and SHIFTS there are the same list, and they have to
stay the same list.

CANVAS 663x1061: the seated canvas with 70 rows added at EACH end. Both ends,
so the middle of the canvas is still the middle and this sprite can take
Kylo's own position and scale unchanged; the seventy at the bottom are the
rows below the desk's edge that his own sprite does not have.

Writes  Assets/Sprites/sulat_point_1..5_uniform.png

Run from the repository root:  python Tools/make_sulat_point.py
"""
import io
import os
import re
import uuid

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

W, H = 663, 1061
PAD = (H - 921) // 2                 # 70

# The drawn pencil's own line: a point on it, and how far right it goes for
# every row down. Both measured off lapis_frame_1_uniform.
AXIS_X, AXIS_ROW = 230.0, 828.0
SLOPE = 0.90

# Where the continuation starts and ends along that line, as rows of the
# ORIGINAL 921 canvas, and how far down it is fully opaque.
START_ROW = 902
FADE_ROW = 916
END_ROW = 979

# Taken from lapis_frame_1: the barrel is about fifteen pixels across where
# it leaves his fingers. The wood is shaved back over the last 32 rows and
# the graphite is the last 8 of those.
WIDTH = 15
CONE_ROWS = 32
LEAD_ROWS = 8

# One per writing frame. THE SAME LIST IS IN Tools/make_sulat_hand_frames.py,
# which slides the hand by these; they have to match, or the point walks away
# from the hand that is holding it. The eased slide there is already at full
# stretch by row 845, and every row drawn here is below that, so a flat
# shift is the right one.
SHIFTS = [-14, -7, 0, 7, 14]

# Sampled off lapis_frame_1_uniform itself rather than off prop_lapis, so the
# continuation is the colour of the pencil it continues.
BODY = (250, 181, 12)                # the barrel
BODY_DARK = (206, 135, 17)           # its shaded right-hand side
WOOD = (245, 131, 92)                # the shaved cone
LEAD = (58, 54, 52)
INK = (20, 18, 18)                   # the outline round the whole thing

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

    def at(row, across=0.0):
        """A point on the pencil's line at the given ORIGINAL-canvas row.

        `across` is measured square to the canvas rather than square to the
        line, which is what keeps the drawn width the width the barrel has
        where it leaves his fingers - that is measured across a row too.
        """
        return ((AXIS_X + SLOPE * (row - AXIS_ROW) + across + shift) * SS,
                (row + PAD) * SS)

    half = WIDTH * 0.5
    line = max(2, int(0.004 * W * SS))

    cone_row = END_ROW - CONE_ROWS
    lead_row = END_ROW - LEAD_ROWS

    # The barrel, down to where the wood is shaved back.
    d.polygon([at(START_ROW, -half), at(START_ROW, half),
               at(cone_row, half), at(cone_row, -half)], fill=BODY)
    # A darker edge down the right-hand side, the same two-tone the drawing has.
    d.polygon([at(START_ROW, half * 0.3), at(START_ROW, half),
               at(cone_row, half), at(cone_row, half * 0.3)], fill=BODY_DARK)

    # The shaved wood, tapering to the lead.
    d.polygon([at(cone_row, -half), at(cone_row, half),
               at(lead_row, half * 0.34), at(lead_row, -half * 0.34)],
              fill=WOOD)

    # The graphite.
    d.polygon([at(lead_row, -half * 0.34), at(lead_row, half * 0.34),
               at(END_ROW, 0)], fill=LEAD)

    # Outline round the whole thing, so it reads against the page the way the
    # drawn pencil reads against his shirt. Open at the top: that end is
    # under the desk, and a cap across it would be a line drawn over the
    # barrel it is supposed to continue.
    d.line([at(START_ROW, -half), at(cone_row, -half), at(lead_row, -half * 0.34),
            at(END_ROW, 0), at(lead_row, half * 0.34), at(cone_row, half),
            at(START_ROW, half)], fill=INK, width=line, joint="curve")

    img = img.resize((W, H), Image.LANCZOS)

    # Fade the top in, so whatever shows above the desk's edge is too faint
    # to be a second pencil over his fingers. See THE GEOMETRY above.
    alpha = img.getchannel("A").load()
    for row in range(START_ROW, FADE_ROW + 1):
        f = (row - START_ROW) / float(FADE_ROW - START_ROW)
        y = row + PAD
        for x in range(W):
            if alpha[x, y]:
                alpha[x, y] = int(alpha[x, y] * f)
    return img


def main():
    print("the point of the pencil, on its own layer:")
    for i, shift in enumerate(SHIFTS, start=1):
        name = "sulat_point_%d_uniform" % i
        point_sprite(shift).save(os.path.join(SPRITES, name + ".png"))
        meta_for(name)
        print("  %-26s hand %+3d px" % (name + ".png", shift))
    print("\nWire in BuildPaaralanScene3 as sulatFrames.companion, with")
    print("  companionLoopFrames  sulat_point_1 .. 5, against the same loop")


main()
