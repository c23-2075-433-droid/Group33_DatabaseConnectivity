"""Give Kylo a hand to write with, and make it travel across the line.

WHAT WAS WRONG, and it is worth being exact because the frames look fine in a
thumbnail. sulat_frame_1..3_uniform are the only poses in the project with a
pencil point on the bottom edge of the canvas, which is the notebook's far
edge - that part was right, and it is why they were drawn. But there is NO
HAND in them. Tools/fix_seated_hands.py cuts every seated pose off below the
sleeve and replaces the hands with a forearm cross-section, so the arms read
as running down behind the desk, and these three inherited that. What is left
is a pencil hanging in the air beside his forearm with nothing holding it.

Seen at the size the game draws him, that is a boy sitting still while a
pencil floats next to him. It is not writing, and no amount of head movement
was going to make it writing.

WHAT IS USED INSTEAD. lapis_frame_1_uniform - the first frame of Paaralan
Scene 2's "Lapis", where he picks the pencil up off the desk. It has
everything the sulat frames do not:

  - his fingers CLOSED ROUND the pencil, drawn, with knuckles and a grip;
  - the pencil held point-down and leaning right, which is how a hand holds
    one to write rather than how it holds one to show it to someone
    (lapis_frame_2 and 3 both have it point-UP, past his chest - those are
    the pencil being raised, and rotating one into a writing grip turns the
    hand upside down with it);
  - the whole thing already at the BOTTOM of the canvas, around x 320, which
    at this scale is world 2.79 - the middle of the desk in front of him,
    where the notebook is.

So he is already holding a pencil over the notebook in a drawing the project
has had all along. What was missing was only that he was looking straight out
of the frame while he did it, and that NOTHING MOVED.

THE SECOND ONE IS THE ONE THAT MATTERED. The first version of this file
promised a travelling hand in its own docstring and did not have the code:
the three frames it wrote differed by a degree and a half of head tilt and by
nothing else, and the pencil point was one sprite shown four times. A boy
nodding very slightly over a pencil that is nailed to the page is not writing
either. The hand travels now, and Tools/make_sulat_point.py moves the point
by the same amounts so the two cannot come apart.

WHAT THIS ADDS

  the look      Tools/look_down.py: his head down on its own neck. Deeper
                than "Basa" uses, because a notebook flat on the desk is
                further below his chin than a book held at his chest. His
                EYES are left as drawn - that file explains why at length.
  the stroke    his forearm, his hand and the pencil in it slide sideways,
                eased in from the sleeve so the arm swings from the elbow
                instead of shearing off it. Five frames across a line.

HOW THE ARM IS CUT OUT. Not by a box. The writing arm is the only bare skin
in the bottom left of this drawing - his other hand is at x 420 and up, his
knees are below x 125 - so it can be found rather than measured: the skin and
pencil colours are taken as a mask, dilated so the black outline comes with
them, and the piece is whatever is connected to three seeds that are known to
be on it (forearm, barrel, eraser). The eraser is a separate blob, because
the silver ferrule between it and the wood is neither skin nor pencil
colour, so the three are closed together afterwards. If that closing is
dropped the ferrule stays behind and the pencil tears in half at the band.

WHAT IS LEFT BEHIND. The arm vacates a crescent of shirt - fourteen pixels of
it at the widest. It is filled per row by reading the first clean pixels on
each side of the hole and blending across; the MEDIAN of five is taken rather
than the one pixel against the edge, because the shirt has a fold line
running down beside the arm and one pixel of it smeared a grey band across
the whole row.

FOURTEEN PIXELS EACH WAY IS 0.085 WORLD UNITS, about the width of a letter.
It is small on purpose: a hand sweeping half the page per frame reads as
crossing something out. The five frames are played 1-2-3-4-5 and then round
to 1 again, and THAT JUMP IS THE POINT - it is the carriage return, his hand
going back to the left margin to start the next line. Played 1-2-3-2 the way
three frames had to be, the hand went out and came back along its own line,
which is a boy scrubbing at one word.

Writes  Assets/Sprites/sulat_hand_1..5_uniform.png

Run from the repository root:  python Tools/make_sulat_hand_frames.py
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_down import load, look_down, save                  # noqa: E402

SRC = "lapis_frame_1_uniform"

HEAD_DROP = 24          # his eyes stay as drawn - see Tools/look_down.py

# WHERE THE SLIDE STARTS AND WHERE IT IS FULL, as rows of the 921 canvas.
# ARM_TOP is the sleeve hem, the topmost row of bare arm: above it nothing
# moves at all, which is why there is no seam at the joint - the sleeve does
# not have to hide anything. ARM_FULL is the elbow. Between the two the shift
# eases in with a smoothstep, so the forearm leans into the movement and the
# hand below it carries the whole of it.
ARM_TOP = 751.0
ARM_FULL = 845.0

# One seed per blob of the writing arm, (x, y) in the source. The ferrule
# splits the pencil in two, so the eraser needs its own.
SEEDS = [(150, 800),            # the forearm, below the sleeve
         (245, 844),            # the barrel, between his fingers
         (230, 828)]            # the eraser, above his knuckles

# HOW FAR THE HAND IS ALONG THE LINE in each frame. Left margin to right and
# then back - see the note on the carriage return above. THE SAME LIST IS IN
# Tools/make_sulat_point.py, which draws the point of the pencil for each of
# these; they have to match or the point walks away from the hand.
SHIFTS = [-14, -7, 0, 7, 14]

# His head follows his hand: a degree and a half of tilt at each end of the
# line, straight on in the middle. Positive is anti-clockwise, which takes
# the crown of his head LEFT, so it runs against the shift.
TILT_AT_END = 1.5


def arm_mask(src):
    """The writing forearm, hand and pencil, as one solid region."""
    a = np.asarray(src).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    opaque = al > 128

    skin = (opaque & (r > 215) & (g > 140) & (g < 220)
            & (b > 70) & (b < 170) & (r - b > 80))
    pencil = opaque & (
        ((r > 200) & (g > 140) & (g < 215) & (b < 70))        # the barrel
        | ((r > 215) & (g < 175) & (b > 70) & (b < 140))      # the shaved wood
        | ((r > 210) & (g > 100) & (g < 175)                  # the eraser
           & (b > 100) & (b < 180)))

    # Dilated so the black outline round the arm comes with the colour inside
    # it, then cut back to what is actually drawn.
    lab, _ = ndi.label(ndi.binary_dilation(skin | pencil, iterations=4) & opaque)
    piece = np.zeros_like(opaque)
    for x, y in SEEDS:
        piece |= lab == lab[y, x]

    # Close the ferrule gap. border_value=1 on the erosion, or the five rows
    # at the bottom edge of the canvas - his wrist - are eaten by it.
    piece = ndi.binary_erosion(ndi.binary_dilation(piece, iterations=5),
                               iterations=5, border_value=1) & opaque
    return ndi.binary_fill_holes(piece)


def shirt_behind(a, mask):
    """The sprite with the arm taken out of it and the hole filled in.

    Only the rim of this is ever seen - the crescent the arm uncovers - so
    the fill only has to be right next to the edge, which is what reading
    outwards from the edge gives.
    """
    H, W, _ = a.shape
    hole = ndi.binary_dilation(mask, iterations=2)
    plate = a.copy()

    def clean(y, x, step):
        got = []
        while len(got) < 5 and 0 <= x < W:
            if not hole[y, x]:
                got.append(a[y, x])
            x += step
        return np.median(np.array(got), axis=0) if got else None

    for y in range(H):
        xs = np.nonzero(hole[y])[0]
        if not len(xs):
            continue
        cuts = np.nonzero(np.diff(xs) > 1)[0]
        for s, e in zip(np.r_[0, cuts + 1], np.r_[cuts, len(xs) - 1]):
            x0, x1 = xs[s], xs[e]
            left, right = clean(y, x0 - 1, -1), clean(y, x1 + 1, 1)
            if left is None and right is None:
                continue
            left = right if left is None else left
            right = left if right is None else right
            t = np.linspace(0, 1, x1 - x0 + 3)[1:-1][:, None]
            plate[y, x0:x1 + 1] = left * (1 - t) + right * t
    return plate


def slide(im, mask, plate, pixels):
    """The arm moved `pixels` sideways, eased in from the sleeve."""
    a = np.asarray(im).astype(np.float64)
    H, W, _ = a.shape
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float64)

    t = np.clip((yy - ARM_TOP) / (ARM_FULL - ARM_TOP), 0, 1)
    shift = pixels * (t * t * (3 - 2 * t))

    # Sampled with the alpha multiplied in, or the transparent pixels outside
    # the arm drag their colour into its outline.
    piece = a.copy()
    piece[..., 3] *= mask
    pre = np.concatenate([piece[..., :3] * piece[..., 3:4] / 255.0,
                          piece[..., 3:4]], axis=2)
    moved = np.empty_like(pre)
    for c in range(4):
        moved[..., c] = ndi.map_coordinates(pre[..., c], [yy, xx - shift],
                                            order=1, mode="constant", cval=0.0)

    alpha = np.clip(moved[..., 3:4], 0, 255)
    f = alpha / 255.0
    rgb = np.where(alpha > 0, moved[..., :3] * 255.0 / np.maximum(alpha, 1e-6), 0)

    out = plate.astype(np.float64).copy()
    out[..., :3] = rgb * f + out[..., :3] * (1 - f)
    out[..., 3] = np.clip(alpha[..., 0] + plate[..., 3] * (1 - f[..., 0]), 0, 255)
    # Rows that have not moved are left exactly as they were drawn, so the
    # sleeve and everything above it is the original file's own pixels.
    out = np.where((np.abs(shift) < 0.5)[..., None], a, out)
    return Image.fromarray(np.clip(out, 0, 255).astype("uint8"))


def main():
    src = load(SRC)
    mask = arm_mask(src)
    plate = shirt_behind(np.asarray(src).astype(np.float64), mask)
    widest = max(abs(s) for s in SHIFTS)

    print("writing, from %s:" % SRC)
    for i, pixels in enumerate(SHIFTS, start=1):
        name = "sulat_hand_%d_uniform" % i
        tilt = -TILT_AT_END * pixels / float(widest)
        out = look_down(src, head_drop=HEAD_DROP, tilt=tilt)
        out = slide(out, mask, plate, pixels)
        assert out.size == src.size, (name, out.size, src.size)
        save(out, name, SRC)
        print("  %-26s hand %+3d px   head %+0.2f deg" % (name + ".png", pixels, tilt))

    print("\nWire them in BuildPaaralanScene3 as the 'Sulat' loop:")
    print("  loopFrames  sulat_hand_1 .. 5, in order, and round again")


main()
