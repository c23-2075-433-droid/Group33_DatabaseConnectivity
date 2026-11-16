"""Make Kylo look down at what is in front of him, by moving his own pixels.

Paaralan Scene 3 is a lesson at a desk, and three of its four words have him
attending to something below his chin - a book in his hands, a notebook on the
desk. The drawings do not: every seated pose in the project looks straight out
of the frame, because that is the one view of his face that exists.

Docs/AnimationStandard.md is blunt about the limit - "there is no
three-quarter view of his face, so he cannot turn his head to look at someone"
- and a head bowed towards a book is the same kind of drawing that does not
exist. What a front-facing drawing CAN do honestly is the other two thirds of
the gesture:

  the head drops       chin towards the collar, a gap opening above the hair.
                       Tools/make_kinig_frames.py already uses exactly this
                       for a nod and explains why it works from the front.

It moves pixels that are already drawn, so rule 7 - consistent proportions -
holds by construction, and it invents no view of him that was never drawn.

HIS EYES DO NOT MOVE, and two attempts at making them say so.

The brief asked for his eyes down as well as his head, and it is the better
cue of the two - it says DOWN rather than merely "lower". It cannot be had
from this drawing. His irises are large and nearly fill the eye, so there is
almost no sclera for one to slide into, and every version of the move left a
seam across the top of the eye where something had to fill what the iris
vacated:

  per-column clamp     repeat each column's topmost pixel down over the gap.
                       At the middle of the eye that pixel is sclera and it
                       works; at the corners and under the lid it is skin or
                       lid shadow, and it drew a grey tab hanging over each
                       iris. In the game it read as a bright band with
                       yellow streaks in it.
  eye-coloured mask    move only the white, the iris and the pupil, and fill
                       with the sclera's own mean. The mask's top edge is
                       anti-aliased and lands on a different row in every
                       column, so the fill came out as a comb of vertical
                       streaks across the whole eye. Worse than the first.

Both are plainly visible at the size this scene draws him. A boy whose head
is down and whose eyes are as drawn looks like a boy looking down; a boy with
a band of streaks across his eyes looks broken. So the eyes are left alone,
which is also what Tools/make_kinig_frames.py does for the same face.

HOW THE EYES ARE FOUND, and why not by hard-coded boxes. His head sits at the
same place in sitting_front_uniform and in the place_frame poses, but the
sulat frames came off a different Canva strip and their face is a few pixels
across from those. A box measured on one and applied to the other slides half
an iris out through the side of a lens.

So they are found instead: the only PURE WHITE in his face is the whites of
his eyes. His skin is orange, his hair and his glasses are black, and his
shirt - also white - is three hundred rows further down and outside the band
this searches. Two blobs, each becomes an ellipse, and everything inside each
ellipse slides down with the top filled back in white.

Clipped to the ellipse on the way, which is the detail that makes it read: an
iris slid down past the bottom of the eye is cut off by the lower lid, the
way a real one is.

This is a module, not a tool. Tools/make_basa_frames.py and
Tools/make_sulat_frames_down.py both use it.
"""
import io
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

# Where to look for the neck - the same band Tools/make_kinig_frames.py uses,
# for the same reason: the narrowest row of the figure is between the chin and
# the shoulders.
NECK_BAND = (500, 660)

# How far below the neck the head piece is cut, so it still covers the join
# once it has moved. See the long note in make_kinig_frames: the body is
# cleared higher than the piece is cut and keeps its own neck in between.
NECK_SKIRT = 18






def find_neck(alpha, band=NECK_BAND):
    """The narrowest row of ink in the band - his neck. (row, centre, width)"""
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


def head_moved(src, degrees=0.0, drop=0, neck=None):
    """His head rotated and/or slid, on a body that has not moved.

    Lifted from Tools/make_kinig_frames.py, whose long comment is the
    explanation for every number in here: the piece is cut NECK_SKIRT rows
    below the neck and the body is cleared at the neck itself, so the wedge a
    rotation opens lands on his own throat rather than on the classroom.
    """
    if not degrees and not drop:
        return src

    a = np.asarray(src)
    if neck is None:
        neck = find_neck(a[:, :, 3])
    neck_y, neck_x, _ = neck

    piece_cut = neck_y + NECK_SKIRT
    body_cut = neck_y

    body = a.copy()
    body[:body_cut, :, 3] = 0

    head = a.copy()
    head[piece_cut:, :, 3] = 0
    piece = Image.fromarray(head)

    if degrees:
        piece = piece.rotate(degrees, resample=Image.BICUBIC,
                             center=(neck_x, piece_cut), fillcolor=(0, 0, 0, 0))
    if drop:
        piece = piece.transform(piece.size, Image.AFFINE,
                                (1, 0, 0, 0, 1, -drop),
                                resample=Image.BICUBIC, fillcolor=(0, 0, 0, 0))

    out = Image.fromarray(body)
    out.alpha_composite(piece)
    return out


def look_down(im, head_drop=0, tilt=0.0):
    """His head down, and tilted, on a body that has not moved."""
    return head_moved(im, degrees=tilt, drop=head_drop)


def meta_for(name, src):
    """Give a new frame the import settings of the pose it was made from."""
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, src + ".png.meta"),
                   encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base,
               count=1, flags=re.M)
    t = t.replace(src + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")


def save(im, name, src):
    im.save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, src)
