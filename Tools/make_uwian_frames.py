"""Draw the standing actions of Paaralan Scene 5 - Kunin, Paalam and Alis.

WHAT THIS REPLACES AND WHY.

Tools/make_tayo_frames.py gets Kylo out of his chair and leaves him standing
at his desk, front on, arms at his sides. Tools/make_kunin_frames.py then
tried to make that figure reach for his bag, and the note at the top of it is
honest about the one trick it had: the seated drawing stops flat at the desk's
far edge, so make_tayo_frames had to INVENT his forearms below that line, and
a limb the project draws for itself can be drawn going somewhere else.

The trouble is WHERE it bent him. The invented part starts at the desk line,
which on a standing boy is the top of his shorts - two thirds of the way down
his forearm - so "fifty-two degrees of reach" was fifty-two degrees of WRIST.
Played back, his hand moved about twenty pixels, the bag slid a third of a
unit sideways and DOWN behind the desk, and the next frame had the backpack on
his shoulders. That is the "disorganised and unnatural" the revision notes
describe, and no amount of re-timing fixes it: there was never a reach in the
frames to time.

SO THE ARM IS CUT AT ITS JOINTS AND EACH PIECE IS TURNED WHOLE.

That is Tools/make_bati_front_frames.py's technique, which it uses on the
wide-shot figure and writes up at length: NOTHING INSIDE A RIGIDLY ROTATED
PIECE CAN DEFORM. The hand in every frame here is the hand that was drawn,
the same pixels at the same spacing, pointing somewhere else. There is no
wrist cut, no finger cut and no taper being stretched, so there is nothing
left that can break.

AND THERE ARE TWO JOINTS, NOT ONE. The first pass of this file turned the
whole bare limb about a single point and called that point an elbow. One
point is one bone, whatever it is called: a stiff stick running from the
armpit to the fingertips. For a reach that is nearly right, because a child
reaching across a desk does straighten his arm, and the Kunin frames it
produced were only stiff. For the WAVE it was hopeless, and the screenshots
show it: the hand beside his head was that same stick pointing at the
ceiling, with a relaxed HANGING hand on the end of it, upside down, thumb
outboard, and no shoulder in the picture at all.

  the shoulder   at the hem of his sleeve, where the bare skin starts
  the elbow      fifty-six rows lower, where the forearm is narrowest

Each piece turns about its own joint, and each joint is a DISC centred on the
point of rotation, cut out of both pieces at the same radius - because a disc
about its own centre is the one shape a rotation leaves exactly where it was.
The two bones mate at every angle with no wedge to hide and no seam to ease,
and the "take more from the piece than you give up from the body" fudge the
single-bone version lived on is gone with them. The shoulder's own disc is
filled by carrying the arm's full width up under the cloth, and his shirt is
then laid back over a small disc at the joint, so the arm comes out from
under the sleeve and is clear of it at once.

AND THE WAVE IS A DIFFERENT DRAWING, NOT AN ANGLE OF THIS ONE.

talking_uniform is the same boy, the same shirt and the same scale with one
arm already up: a drawn sleeve, a drawn upper arm, a drawn elbow bent square,
and a drawn open palm with the fingers apart. make_tayo_frames already scales
that drawing to this one's head and grafts its lower body on at the waist, so
registration is not a new problem - mirrored, the two line up at the
shoulders to within a few pixels. Its ARM is grafted on as well, and the wave
is then one rotation of a forearm about an elbow somebody drew, which is
make_bati_front_frames' mechanism on the drawing it was written for.

Only what DIFFERS comes across. Two drawings of one boy in one white shirt
are the same colour nearly everywhere, and a rectangle of one laid over the
other shows as a tone step down his chest; the patch is the union of what is
not shirt-white in either, closed and filled, plus the sleeve's own rows.

THREE BEATS COME OUT OF ONE MECHANISM.

  Kunin   he reaches out and DOWN to the bag, closes on it, LIFTS it clear
          of the desk, brings it in and UP across his body, settles it on his
          back and pulls the strap down to sit. Two bones: the reach is
          mostly shoulder, the carry is mostly elbow. The bag prop rides his
          fist, and the fist is drawn here, so it is measured here - the
          world coordinates printed when this runs are what
          BuildPaaralanScene5 feeds PropCarry.

  Paalam  on the GRAFTED arm. It comes up in three steps and then the open
          palm sweeps beside his head - side to side, as many times as the
          scene asks for - and comes down again. He is wearing the bag for
          all of it, so the straps never leave his shoulders.

  Alis    both legs swing about their hips and his arms swing against them,
          while the whole figure bobs. He walks out of the left of the frame
          wearing the bag.

AND HIS LEGS REACH THE BOTTOM OF THE PICTURE. The 1521-row canvas ends at
world y -4.84, half a unit above the bottom of this camera's view, which is
invisible while he is behind the desk and a flat cut across his shins the
moment he walks out from behind it. So the canvas is taller at each end and
his shins are carried down out of the frame by copying their own last row -
make_tayo_frames' trick for the forearms, applied to the other end of him. He
walks off the side of the picture with his legs running off the bottom of it,
which is what a close-up of a boy at a desk does.

CANVAS. 1063x1761: the 663x1521 of the tayo_* frames with 120 columns added at
each side, for an arm that leaves his body, and 120 rows at each end, for the
legs. Symmetric, because a sprite draws centred on its object - padding one
side only would slide him sideways - and the crown lands on row 416, which is
the same distance above the canvas centre as it is in the tayo_* frames. The
two sets register pixel for pixel and the scene can cut between them.

The worn backpack is drawn too, and is not the one make_kunin_frames built:
see "The backpack he is wearing by the end of it" below for why its straps
came out as ragged navy bands that swallowed his sleeve, and what the drawing
had all along.

Writes  (both bags, by calling Tools/make_desk_bag.py)
        Assets/Sprites/prop_bag_uniform.png          the bag in his hand
        Assets/Sprites/prop_bag_desk_uniform.png     and the one on the desk
        Assets/Sprites/kunin_reach_1..2_uniform.png  out to the bag
        Assets/Sprites/kunin_grip_uniform.png        his fist closes on it
        Assets/Sprites/kunin_lift_uniform.png        and up off the desk
        Assets/Sprites/kunin_carry_1..2_uniform.png  in and up across his body
        Assets/Sprites/kunin_settle_uniform.png      onto his back
        Assets/Sprites/kunin_strap_1..2_uniform.png  the strap pulled to sit
        Assets/Sprites/kunin_stand_uniform.png       his idle, wearing it
        Assets/Sprites/paalam_lift_1..3_uniform.png  the arm on its way up
        Assets/Sprites/paalam_wave_in_uniform.png    the sweep, inner end
        Assets/Sprites/paalam_wave_mid_uniform.png   the middle of it
        Assets/Sprites/paalam_wave_out_uniform.png   the outer end
        Assets/Sprites/paalam_down_uniform.png       lowering it again
        Assets/Sprites/alis_turn_uniform.png         he turns to go
        Assets/Sprites/alis_walk_1..4_uniform.png    the walk cycle

Run from the repository root:  python Tools/make_uwian_frames.py
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import hair_cap                         # noqa: E402
from look_down import SPRITES, meta_for                      # noqa: E402
import make_tayo_frames as tayo                               # noqa: E402
import make_kunin_frames as kunin                             # noqa: E402

# --- the canvas ---
PAD_X = 200
PAD_Y = 120
WIDTH = tayo.WIDTH + 2 * PAD_X          # 1063
HEIGHT = tayo.HEIGHT + 2 * PAD_Y        # 1761
CROWN_ROW = tayo.SEAT_CROWN_ROW - tayo.RISE + PAD_Y          # 416, on his feet

# --- the joints ---
#
# THE ARM NOW HAS TWO BONES IN IT, and the version this replaces had one.
#
# That version cut the bare arm off at 606 - just inside the sleeve - and
# turned the whole of it about a point at 650, forty-four rows lower. It
# called that point the elbow, and the file said so at length. It was not an
# elbow. A rotation about one point turns a limb into one straight stick from
# the sleeve to the fingertips, however the point is named, and the
# screenshots that came back show exactly that: at the angles "Kunin" uses it
# reads as a stiff reach, and at the angles "Paalam" used it reads as a boy
# whose arm has come off at the shoulder and been glued back on pointing up.
# There is no elbow in those frames to bend because there was never a second
# bone to bend it against.
#
# SO THERE ARE TWO NOW, and they meet at a BALL. The upper bone runs from the
# hem of his sleeve to the crease of his elbow; the lower one runs from that
# crease, through the wrist, to the ends of his fingers. Each turns about its
# own joint, and each joint is a DISC centred on the point of rotation, cut
# out of both bones at the same radius: a disc is the one shape a rotation
# about its own centre leaves exactly where it was, so the two bones mate at
# every angle, with no wedge to hide and no seam to ease. That is the whole
# trick, and it is the reason nothing here needs the "take more from the
# piece than you give up from the body" fudge the single-bone version lived
# on.
#
# ROWS BELOW HIS CROWN, which is how every measurement in this family of
# files is carried. The bare skin starts at 612, where the sleeve ends; the
# crease of the elbow is drawn about fifty-six rows below that, which is also
# where the forearm is narrowest. Shoulder to elbow comes out at 56 and elbow
# to wrist at 153, and that is NOT a misprint: most of his upper arm is
# inside a short sleeve and cannot be seen, so the bone that shows is the
# stub below the hem. The sleeve covers the rest, which is the next block.
SHOULDER_REL = 630              # the hem of the sleeve: the arm's own top
SHOULDER_WIDE_REL = 648         # and a row below it, clear of the cloth,
                                # where the arm's full width can be read
ELBOW_REL = 672                 # the crease, on the hanging arm
BALL = 7                        # how far each joint disc reaches past the limb
TUCK = 46                       # how far the arm is carried UP under the
                                # sleeve, so the shoulder disc has material
ARM_GROW = 6                    # the drawn line round a limb, on this canvas

# --- the sleeve ---
# WHAT PUTS THE ARM BACK UNDER HIS SHIRT. The arm is composited over the
# finished figure, which is the simplest thing to do and would leave the
# shoulder disc drawn ON TOP of the sleeve - a bare shoulder over a sleeve,
# which is worse than the stump it replaces. So after the arm goes on, the
# figure's own pixels in these rows are laid over it again. Where the sleeve
# is, the sleeve wins and the arm disappears under it exactly as far as the
# cloth goes; where the sleeve is not, the restore has nothing in it and the
# arm is untouched. It costs one composite and it is why the joint never
# shows however far the arm swings.
# How far past the shoulder disc the restore reaches, as a multiple of that
# disc's own radius. Big enough to cover the wedge the turn opens at the
# joint, small enough that a forearm brought across his chest is still in
# front of him.
SLEEVE_CAP = 1.9

# --- the legs ---
# Where his shins are carried on from, below his crown, and how far past the
# end of the drawing they go. 170 rows is a little over a world unit, which
# puts their ends below this camera's bottom edge from any height he bobs to.
LEG_SRC_REL = 1216
LEG_EXTRA = 170
# THE LEG TURNS ON ITS HEM, not on the joint inside him. A leg rotated about
# the real hip would slide sideways where it leaves his shorts and leave a
# step in the cloth; turned about the hem itself the cloth is where the cloth
# was, which is what a loose school short does over a moving leg anyway.
HIP_REL = 1046

# --- how the scene reads the fist back ---
SIT_X = kunin.SIT_X
SIT_FEET_Y = kunin.SIT_FEET_Y
SEATED_HEIGHT = kunin.SEATED_HEIGHT
UNITS_PER_PX = kunin.UNITS_PER_PX
GRIP_ALONG = 0.45               # down the hand from the wrist, where a fist is


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")


def skin_mask(a):
    """His bare skin: face, forearms, hands, legs."""
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    return (al > 100) & (r > 200) & (g > 120) & (g < 215) & (b < 175) & (r - b > 60)


def world(x, y):
    """A canvas point in Paaralan Scene 5's world units."""
    return (SIT_X + (x - WIDTH / 2.0) * UNITS_PER_PX,
            SIT_FEET_Y + SEATED_HEIGHT * 0.5 - (y - HEIGHT / 2.0) * UNITS_PER_PX)


# ======================================================================
# The backpack he is wearing by the end of it
# ======================================================================
#
# WHAT THIS REPLACES. make_kunin_frames.placed_bag built the worn bag out of
# the difference between talking_uniform_with_bag and talking_uniform - one
# pose with and without it, so the difference is the bag - and then scaled
# that cut-out up to this figure's head. Played in the scene it came out as
# the thing the revision notes call broken: ragged dotted outlines, orange
# and white speckles down both straps, and a lumpy navy mass on his right
# that swallowed his sleeve and most of his arm. Two separate faults, and
# the drawing was never one of them.
#
# IT WAS CUT BEFORE IT WAS SCALED. A cut-out's transparent pixels still
# carry whatever RGB was there before - his skin, his shirt - and Lanczos
# does not know that alpha means "ignore this colour". Enlarged by two, the
# filter dragged that skin and shirt out across every edge of the bag, which
# is the orange fringe and the dotted line. So every soft pixel's colour is
# replaced with its nearest solid neighbour's BEFORE the resize, and the
# edge then enlarges into more of the bag instead of into his arm.
#
# AND THE RIGHT STRAP WAS NOT A DRAWING AT ALL. In the source his own
# forearm lies across it, so the difference has a hole in it, and that file
# filled the hole by laying one row of strap along a hand-measured path and
# redrawing the outline at either side. That band is the navy mass: too
# wide, splayed at the bottom, and not the shape the artist drew.
#
# HIS LEFT STRAP IS DRAWN WHOLE, because his left arm is raised in that
# pose and covers nothing. So the right strap is THE LEFT ONE MIRRORED - the
# same drawn band, the same buckle, the same outline weight, pointing the
# other way - which is this project's own rule for a front view: a mirror of
# a front view of a boy is the same boy. Nothing is reconstructed, and the
# two straps match because they are one strap.
#
# THE AXIS COMES FROM THE TWO DRAWN STRAPS, not from his silhouette. His
# outline at chest height includes a raised arm, which puts the middle of
# him nineteen pixels off; mirrored about that, the straps crossed in the
# centre of his chest. Measured instead as the midpoint between the drawn
# left strap and what can be seen of the drawn right one, row by row over
# the rows they share, it lands where the artist put them.

_WORN = {}


def solid_colour(a):
    """RGB under every soft pixel replaced by the nearest solid one's."""
    a = a.astype(np.uint8).copy()
    solid = a[:, :, 3] > 200
    soft = (a[:, :, 3] > 0) & ~solid
    if solid.any() and soft.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
        a[soft, :3] = a[iy[soft], ix[soft], :3]
    a[a[:, :, 3] == 0] = 0
    return a


def flip_about(a, axis):
    """`a` mirrored about the vertical line at `axis`, to the pixel.

    Flipping the whole row sends column x to (width-1-x); sliding the result
    by (2*axis - (width-1)) then sends it on to 2*axis - x, which is the
    reflection. Whatever falls off the canvas was never on him.
    """
    shift = int(round(2 * axis)) - (a.shape[1] - 1)
    out = np.zeros_like(a)
    flipped = a[:, ::-1]
    if shift >= 0:
        out[:, shift:] = flipped[:, :a.shape[1] - shift]
    elif shift < 0:
        out[:, :a.shape[1] + shift] = flipped[:, -shift:]
    return out


def bag_pieces():
    """The worn bag in the source drawing's own pixels: straps, and body.

    `front` is what lies on his shirt - both straps, the left one as drawn
    and the right one mirrored from it. `behind` is what shows past his
    outline, which is the bag itself behind his shoulders.
    """
    aw = np.asarray(load(kunin.WITH_BAG)).astype(int)
    ap = np.asarray(load(kunin.NO_BAG)).astype(int)
    mask = kunin.bag_mask(aw, ap)
    body = ndimage.binary_dilation(ap[:, :, 3] > 60, kunin.disk(2))

    rows = np.where(body.any(axis=1))[0]
    band = body[int(rows.min() + 0.30 * len(rows)):
                int(rows.min() + 0.55 * len(rows))]
    xs = np.where(band.any(axis=0))[0]
    rough = (xs.min() + xs.max()) / 2.0

    front = mask & body
    labels, count = ndimage.label(ndimage.binary_closing(front, np.ones((5, 5))))
    sizes = ndimage.sum(front, labels, range(1, count + 1))

    def biggest(side):
        best, best_n = None, 0
        for i in range(1, count + 1):
            cols = np.where((labels == i).any(axis=0))[0]
            if side * (cols.mean() - rough) > 0 and sizes[i - 1] > best_n:
                best, best_n = i, sizes[i - 1]
        assert best is not None, "no strap found on that side"
        return best

    li, ri = biggest(-1), biggest(+1)
    axes = []
    for row in range(front.shape[0]):
        xl = np.where(labels[row] == li)[0]
        xr = np.where(labels[row] == ri)[0]
        if len(xl) and len(xr):
            axes.append(((xl.min() + xl.max()) + (xr.min() + xr.max())) / 4.0)
    assert axes, "the two straps share no row to measure an axis on"
    axis = float(np.mean(axes))

    strap = (labels == li) & front
    a = np.zeros(aw.shape, np.uint8)
    a[strap] = aw[strap].astype(np.uint8)
    left = solid_colour(a)

    front_im = Image.fromarray(left, "RGBA")
    front_im.alpha_composite(Image.fromarray(flip_about(left, axis), "RGBA"))

    # AND THE BAG BEHIND HIM IS MIRRORED TOO, for the same reason and one
    # more. In the source his LEFT arm is up and his right one hangs, so the
    # bag shows past one shoulder and one hip and is hidden on the other
    # side - which on a figure whose arms are somewhere else reads as a navy
    # fin stuck to one shoulder rather than as a bag he is wearing. Laid over
    # its own mirror it peeks over both shoulders, which is what a backpack
    # seen from the front does, and the asymmetry stops being visible because
    # there is nothing left to compare it against.
    outside = mask & ~body
    b = np.zeros(aw.shape, np.uint8)
    b[outside] = aw[outside].astype(np.uint8)
    behind_im = Image.fromarray(solid_colour(b), "RGBA")
    behind_im.alpha_composite(Image.fromarray(flip_about(
        np.asarray(behind_im), axis), "RGBA"))
    return front_im, behind_im, axis


def worn_bag(cap):
    """Both bag layers, head-matched and placed on THIS canvas.

    Scaled after the colour under the soft edge has been made good, and
    returned on the uwian canvas rather than make_kunin_frames' narrower one,
    so the callers composite them at the origin.
    """
    if cap in _WORN:
        return _WORN[cap]
    front, behind, axis = bag_pieces()
    src = load(kunin.WITH_BAG)
    c, b, cx = hair_cap(src)
    s = cap / float(b - c)
    size = (int(round(src.size[0] * s)), int(round(src.size[1] * s)))
    at = (WIDTH // 2 - int(round(cx * s)) + tayo.LOWER_DX,
          CROWN_ROW - int(round(c * s)))

    out = []
    for layer in (front, behind):
        o = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
        o.alpha_composite(layer.resize(size, Image.LANCZOS), at)
        out.append(o)
    _WORN[cap] = (out[0], out[1], axis)
    return _WORN[cap]


# ======================================================================
# The figure he does all of this from
# ======================================================================

def base_figure(wear, drop=()):
    """The standing boy on the big canvas: arms down, legs carried on.

    Everything above his shins is make_tayo_frames' graft, unchanged and
    composited in that file's own order - the lower body, the carried
    forearms, then the seated drawing over the top of both - with
    make_kunin_frames' worn backpack behind him and over his shirt when
    `wear` is set.

    `drop` LEAVES LIMBS OFF, and that is what makes a swing clean rather than
    patched. make_bati_front_frames takes an arm off a finished drawing and
    fills what it was covering from its own edges, because on that drawing
    there is nothing else to do. Here the drawing is assembled, so a limb can
    simply not be put on - and what was behind it is then the thing that was
    behind it: his shirt where the arm lay over his shirt, the room where it
    hung against the room, his shorts complete underneath his hands, because
    talking_uniform drew them with its own arms raised. Nothing is invented
    and nothing is smeared outwards, which the fill does if you let it.

    Each entry is ("arm", side) or ("leg", side), side +1 for his right as we
    see it and -1 for his left.
    """
    seat = load(tayo.BASE)
    s_crown, s_bottom, s_cx = hair_cap(seat)
    cap = s_bottom - s_crown
    inner_crown = CROWN_ROW - PAD_Y          # the same row on tayo's canvas

    out = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    front = behind = None
    if wear:
        front, behind, _ = worn_bag(cap)
        out.alpha_composite(behind)

    lower = tayo.lower_body(inner_crown, cap, tayo.shirt_white(seat))
    for part, side in drop:
        if part == "leg":
            lower = without_limb(lower, side, inner_crown + LEG_SRC_REL - 180,
                                 lower.size[1], tayo.WIDTH)
    out.alpha_composite(lower, (PAD_X, 0 + PAD_Y))

    arms = tayo.forearms(inner_crown, cap)
    seat_cut = Image.new("RGBA", seat.size, (0, 0, 0, 0))
    seat_cut.alpha_composite(seat)
    for part, side in drop:
        if part != "arm":
            continue
        # The carried forearm, which is a whole layer of its own and so comes
        # off by the column it is on ...
        arms = half(arms, side, tayo.WIDTH)
        # ... and the drawn one, which is inside the seated drawing and comes
        # off by its own skin.
        seat_cut = without_limb(seat_cut, side, tayo.ARM_SRC_ROW - 180,
                                seat.size[1], seat.size[0])
    out.alpha_composite(arms, (PAD_X, PAD_Y))
    out.alpha_composite(seat_cut, (WIDTH // 2 - s_cx, CROWN_ROW - s_crown))
    if wear:
        # The straps go over his shirt and under his hands - make_kunin_frames
        # has the long version. The forearms are composited a second time,
        # cropped to the rows below the seated drawing, so the strap that ends
        # at his hip passes behind the arm hanging in front of it.
        out.alpha_composite(front)
        cut = CROWN_ROW + tayo.SEAT_CUT_REL
        out.alpha_composite(arms.crop((0, cut - PAD_Y, tayo.WIDTH, tayo.HEIGHT)),
                            (PAD_X, cut))
    return extend_shins(out)


def half(im, side, width):
    """A layer with everything on one side of his centre line taken off."""
    a = np.asarray(im).copy()
    if side > 0:
        a[:, width // 2:, 3] = 0
    else:
        a[:, :width // 2, 3] = 0
    return Image.fromarray(a, "RGBA")


def without_limb(im, side, top, bottom, width):
    """A layer with one bare limb, and the line drawn round it, taken off.

    The limb is found as skin, boxed to its own side and to the rows it lives
    in, and grown by the width of that line so the line comes away with it -
    a limb cut out at the skin leaves its own outline behind as a hair of
    black where the limb used to be.
    """
    a = np.asarray(im).copy()
    mask = skin_mask(a)
    box = np.zeros_like(mask)
    if side > 0:
        box[top:bottom, width // 2:] = True
    else:
        box[top:bottom, :width // 2] = True
    mask &= box
    if not mask.any():
        return im

    grown = ndimage.binary_dilation(
        mask, ndimage.generate_binary_structure(2, 2), iterations=ARM_GROW)
    labels, count = ndimage.label(grown)
    sizes = ndimage.sum(grown, labels, range(1, count + 1))
    grown = labels == int(np.argmax(sizes)) + 1
    a[grown, 3] = 0
    return Image.fromarray(a, "RGBA")


def extend_shins(im):
    """His shins carried down past the end of the drawing, out of the frame.

    The tayo_* canvas stops at mid-shin, which is right while the desk is in
    front of him and a flat cut the moment he walks out from behind it. His
    legs are vertical and unchanging for the last rows of that drawing, so
    they are continued the way make_tayo_frames continues his forearms: by
    copying one row. Nothing is invented but length.
    """
    src_row = CROWN_ROW + LEG_SRC_REL
    a = np.asarray(im)
    if src_row >= a.shape[0] or not (a[src_row, :, 3] > 60).any():
        return im
    slab = im.crop((0, src_row, WIDTH, src_row + 1))
    out = im.copy()
    for i in range(1, min(LEG_EXTRA, HEIGHT - src_row - 1) + 1):
        out.alpha_composite(slab, (0, src_row + i))
    return out


# ======================================================================
# One arm, in two bones
# ======================================================================

def arm_piece(a, side):
    """The bare arm and hand on one side, as a mask.

    GROW IT BEFORE YOU LABEL IT, which is make_bati_front_frames' first trap:
    every finger is drawn inside its own black line, and a line is not skin,
    so the skin here is a palm and four fingertips rather than one hand.
    Dilating first bridges those lines; the join is then one blob from the
    sleeve to the fingernails.

    The search is boxed to the band between the sleeve and his shorts on the
    chosen side, so his face, his other arm and both legs are outside it and
    cannot be picked up.
    """
    mask = skin_mask(a)
    box = np.zeros_like(mask)
    top = CROWN_ROW + SHOULDER_REL - 14
    bottom = CROWN_ROW + LEG_SRC_REL - 180
    if side > 0:
        box[top:bottom, WIDTH // 2:] = True
    else:
        box[top:bottom, :WIDTH // 2] = True
    mask &= box

    grown = ndimage.binary_dilation(
        mask, ndimage.generate_binary_structure(2, 2), iterations=ARM_GROW)
    labels, count = ndimage.label(grown)
    assert count, "no bare arm found on that side"
    sizes = ndimage.sum(grown, labels, range(1, count + 1))
    grown = labels == int(np.argmax(sizes)) + 1
    return grown & (mask | ndimage.binary_dilation(
        mask, ndimage.generate_binary_structure(2, 2), iterations=ARM_GROW))


def joints(a, side):
    """The two points his arm turns on, and how wide it is at each.

    Measured on the SKIN, in the rows the constants name, so growing the mask
    by another pixel to pick up more of the drawn line does not move either
    joint. The radius is half the limb's own width there plus BALL, which is
    enough to carry the line drawn round it into the disc - a joint cut at
    the skin leaves its own outline behind as a black hair across the crease.

    THE SHOULDER IS MEASURED LOWER DOWN THAN IT SITS. At the hem itself the
    sleeve is over the arm, so the skin there is only as wide as the bit of
    it that shows, and a disc cut to that leaves the OUTER corner of the hem
    with nothing under it - a grey triangle between cloth and arm that opens
    as soon as he reaches. The width comes from a row clear of the cloth and
    the disc is then centred back up on the hem, which is wider than the
    sleeve's opening and covers it at every angle.
    """
    piece = arm_piece(a, side)
    bare = piece & skin_mask(a)

    def span(rel):
        # ON THE SKIN, not on the grown piece. The piece carries ARM_GROW
        # pixels of slack on each side so the drawn line comes away with the
        # limb, and a radius read off it is a dozen pixels too big - which
        # here is the difference between two joints and one, because the
        # elbow's disc would then swallow the shoulder's and the upper bone
        # with it. BALL is the allowance for that line, and it is seven.
        xs = np.where(bare[CROWN_ROW + rel])[0]
        assert len(xs), "no arm at row %d below his crown" % rel
        return float(xs.min() + xs.max()) / 2.0, (xs.max() - xs.min()) / 2.0

    cx, half = span(SHOULDER_WIDE_REL)
    shoulder = ((cx, float(CROWN_ROW + SHOULDER_REL)), half + BALL)
    cx, half = span(ELBOW_REL)
    elbow = ((cx, float(CROWN_ROW + ELBOW_REL)), half + BALL)
    return piece, shoulder, elbow


def tucked(arm, shoulder, radius):
    """The arm carried up under the sleeve and rounded into its shoulder.

    THE TOP OF A CUT LIMB IS A STRAIGHT EDGE, and a straight edge turned about
    a point behind it opens a wedge of daylight on one side. The cure is to
    make the top a DISC centred on the point it turns about, because a disc
    about its own centre is the one shape a rotation cannot move. So the arm's
    topmost drawn row is copied upwards far enough to fill one - the trick
    make_tayo_frames uses on his forearms and extend_shins uses on his
    shins - and everything outside the disc, above the joint, is cut away
    again. What is left meets the hem of the sleeve at every angle.
    """
    cx, cy = shoulder
    # The slab is taken from BELOW the hem, where the arm is at its full
    # width, and carried up past the joint; copied from a row the sleeve is
    # over, it would be a stripe as narrow as the gap in the cloth.
    #
    # AND IT GOES UNDER THE ARM, NOT OVER IT. The first version composited
    # the copied rows onto the arm, which drew a flat band of one row's
    # colour over the eighteen rows of real arm between the measuring row and
    # the hem - a pale stripe across the top of his forearm in every frame,
    # which is exactly the sort of seam this whole file exists to avoid. The
    # fill is built on its own and the drawn arm is laid over it, so not one
    # pixel of him is covered by a copy of himself.
    #
    # THE ROW IS STRIPPED OF ITS OUTLINE FIRST. An arm is drawn inside a black
    # line, so the row has three or four dark pixels at each end - and copied
    # up sixty times that line becomes two dark stripes running up the ball,
    # which turn with the arm and come out from under the sleeve as a bar
    # across his shoulder. The ball wants the colour of the inside of his arm
    # and nothing else; the line round it comes from the drawn arm, which is
    # composited over this and still has its own.
    top = CROWN_ROW + SHOULDER_WIDE_REL
    slab = np.asarray(arm.crop((0, top, WIDTH, top + 1))).copy()
    bare = skin_mask(slab)[0]
    if bare.any():
        xs = np.where(bare)[0]
        keep = np.clip(np.arange(WIDTH), xs.min(), xs.max())
        near = xs[np.abs(xs[None, :] - keep[:, None]).argmin(axis=1)]
        slab[0, :, :3] = slab[0, near, :3]
    slab = Image.fromarray(slab, "RGBA")
    fill = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    for i in range(1, TUCK + int(SHOULDER_WIDE_REL - SHOULDER_REL) + 1):
        fill.alpha_composite(slab, (0, top - i))

    a = np.asarray(fill).copy()
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    a[((xx - cx) ** 2 + (yy - cy) ** 2) > radius ** 2, 3] = 0
    # AND THE DRAWN ARM IS CUT BELOW THE HEM LINE BEFORE IT GOES ON. The
    # piece is the skin grown by ARM_GROW so the line round the limb comes
    # away with it, and six pixels above the topmost skin on this arm is not
    # the line round the limb - it is the SLEEVE'S OWN HEM, which is drawn
    # just as dark and is part of his shirt. Carried along and turned, it
    # swept across the top of his arm as a black bar at every angle but zero.
    # So the arm starts a few rows below the hem and the ball fills the rest.
    drawn = np.asarray(arm).copy()
    drawn[:CROWN_ROW + SHOULDER_REL + 5, :, 3] = 0
    out = Image.fromarray(a, "RGBA")
    out.alpha_composite(Image.fromarray(drawn, "RGBA"))

    a = np.asarray(out).copy()
    inside = ((xx - cx) ** 2 + (yy - cy) ** 2) <= radius ** 2
    a[(yy < cy) & ~inside, 3] = 0
    return Image.fromarray(a, "RGBA")


def hinge(arm, elbow, radius, degrees):
    """The arm bent at the elbow: the lower bone turned, the upper one not.

    Both bones keep the joint's disc, so the overlap between them is exactly
    that disc however far the hinge goes: the upper bone's disc fills whatever
    the lower one opens on the outside of the bend, and the lower one's fills
    what the upper opens on the inside. There is nothing else to fill.

    NEGATIVE IS FLEXION on his right side - the forearm swinging in towards
    his chest, which is the way an elbow actually goes. Positive would
    straighten it past true, so nothing here asks for much of it.
    """
    if abs(degrees) < 0.01:
        return arm
    cx, cy = elbow
    a = np.asarray(arm)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    disc = ((xx - cx) ** 2 + (yy - cy) ** 2) <= radius ** 2

    low = a.copy()
    low[~((yy >= cy) | disc), 3] = 0
    high = a.copy()
    high[~((yy <= cy) | disc), 3] = 0

    out = Image.fromarray(high, "RGBA")
    out.alpha_composite(Image.fromarray(low, "RGBA").rotate(
        degrees, resample=Image.BICUBIC, center=(cx, cy),
        fillcolor=(0, 0, 0, 0)))
    return out


def turned(im, pivot, degrees, what="limb"):
    """One rigid turn, with the canvas checked.

    A hand that swings off the edge of the canvas comes back with its fingers
    sliced off, and the frame still looks plausible until you see it move. So
    count the ink before and after: anything that left is a piece of him that
    is now missing.
    """
    if abs(degrees) < 0.01:
        return im
    before = int((np.asarray(im)[:, :, 3] > 8).sum())
    out = im.rotate(degrees, resample=Image.BICUBIC, center=pivot,
                    fillcolor=(0, 0, 0, 0))
    after = int((np.asarray(out)[:, :, 3] > 8).sum())
    assert after > before * 0.97, (
        "the %s is clipped by the canvas at %+.0f degrees: %d px of %d left "
        "it. Widen the canvas." % (what, degrees, before - after, before))
    return out


def arm_only(whole, piece):
    """Just the arm, alone on the canvas."""
    a = np.asarray(whole).copy()
    a[~piece, 3] = 0
    return Image.fromarray(a, "RGBA")


def sleeve_over(out, body, shoulder, radius):
    """His shirt laid back over the arm, so the joint goes under the cloth.

    A DISC ROUND THE JOINT AND NOTHING ELSE. The first version of this
    restored a rectangle of rows - everything the sleeve is drawn in, right
    across his chest - and that worked perfectly until an elbow bent: a
    forearm brought up in front of his shirt, which is where a forearm goes
    when you carry a bag to your shoulder or pull a strap down, lands inside
    those rows and the restore paints his shirt straight over it. Three of
    Kunin's frames came out with no arm in them at all.

    What actually has to go under the cloth is the joint, which is one disc
    at one place. Outside it the arm is in front of him, which is where an
    arm crossing his chest belongs. Inside it, the restore only puts back
    what the body HAS: above the hem that is sleeve, below and outboard of
    the hem it is nothing, so the arm comes out from under the cloth at the
    hem and is clear of it at once.
    """
    cx, cy = shoulder
    r = int(round(radius * SLEEVE_CAP))
    box = (max(0, int(cx) - r), max(0, int(cy) - r),
           min(WIDTH, int(cx) + r), min(HEIGHT, int(cy) + r))
    patch = np.asarray(body.crop(box)).copy()
    yy, xx = np.mgrid[box[1]:box[3], box[0]:box[2]]
    patch[((xx - cx) ** 2 + (yy - cy) ** 2) > r * r, 3] = 0
    out.alpha_composite(Image.fromarray(patch, "RGBA"), (box[0], box[1]))
    return out


def bent_arm(whole, body, side, shoulder_deg, elbow_deg):
    """His arm on `side`, abducted `shoulder_deg` and flexed `elbow_deg`.

    POSITIVE IS AWAY FROM HIS BODY on whichever side is asked for, which is
    why `side` multiplies both angles: Pillow turns an image anticlockwise,
    and anticlockwise carries a hanging arm to the viewer's right - out on his
    right side and across his body on his left.

    `body` is the same boy with that arm LEFT OFF rather than scrubbed out, so
    there is no hole to fill and nothing to smear: see base_figure.
    """
    piece, (shoulder, r_s), (elbow, r_e) = joints(np.asarray(whole), side)
    arm = tucked(arm_only(whole, piece), shoulder, r_s)
    arm = hinge(arm, elbow, r_e, elbow_deg * side)
    arm = turned(arm, shoulder, shoulder_deg * side, "arm")

    out = body.copy()
    out.alpha_composite(arm)
    return sleeve_over(out, body, shoulder, r_s)


def spin(point, pivot, degrees):
    """One point carried round by the same turn the pixels get.

    The canvas has y DOWN, so the sines change places against the usual form.
    A hanging arm at +90 ends up pointing along +x, which is out.
    """
    t = math.radians(degrees)
    dx, dy = point[0] - pivot[0], point[1] - pivot[1]
    return (pivot[0] + dx * math.cos(t) + dy * math.sin(t),
            pivot[1] - dx * math.sin(t) + dy * math.cos(t))


def fist(im, shoulder_deg, elbow_deg, side=1):
    """Where his fist closes at that pose, in Scene 5's world units.

    The arm is drawn by this file, so the only honest place to measure the
    hand is here. The point is taken part of the way DOWN the hand from the
    wrist - a strap is held in a fist, not pinched between fingertips - and is
    then carried through BOTH turns, in the order the pixels get them: round
    the elbow first, then round the shoulder, which also moves the elbow.
    """
    a = np.asarray(im)
    piece, (shoulder, _), (elbow, r_e) = joints(a, side)
    ys, xs = np.where(piece)
    wrist = CROWN_ROW + tayo.WRIST_REL
    grip_y = wrist + GRIP_ALONG * float(ys.max() - wrist)
    grip_x = float(xs[np.abs(ys - grip_y) <= 6].mean())

    grip = spin((grip_x, grip_y), elbow, elbow_deg * side)
    grip = spin(grip, shoulder, shoulder_deg * side)
    return world(*grip)


# ======================================================================
# The legs, for the walk
# ======================================================================

def leg_piece(a, side):
    """One bare leg below the shorts, as a mask, with the hip it turns on."""
    mask = skin_mask(a)
    box = np.zeros_like(mask)
    top = CROWN_ROW + LEG_SRC_REL - 180
    if side > 0:
        box[top:, WIDTH // 2:] = True
    else:
        box[top:, :WIDTH // 2] = True
    mask &= box

    grown = ndimage.binary_dilation(
        mask, ndimage.generate_binary_structure(2, 2), iterations=ARM_GROW)
    labels, count = ndimage.label(grown)
    assert count, "no bare leg found on that side"
    sizes = ndimage.sum(grown, labels, range(1, count + 1))
    grown = labels == int(np.argmax(sizes)) + 1

    ys, xs = np.where(mask & grown)
    row = CROWN_ROW + HIP_REL
    near = np.abs(ys - row) <= 14
    hip = (float(xs[near].mean()) if near.any() else float(xs.mean()),
           float(row))
    return grown, hip


def swung_leg(whole, body, degrees, side):
    """One leg turned `degrees` about its hem, Pillow's way round.

    ONE BONE, WHICH IS WHAT A LEG NEEDS HERE. The arm above has two because
    an elbow is the whole of what a reach and a wave are made of; a knee seen
    square on from the front, on a boy whose shins run off the bottom of the
    picture, is not visible at any angle this walk uses. So the leg keeps the
    plain rigid turn the first version of this file gave everything.

    Anticlockwise carries the foot to the viewer's RIGHT, which for a boy
    walking left is the leg trailing behind him; clockwise is the one
    reaching forward.
    """
    piece, hip = leg_piece(np.asarray(whole), side)
    out = body.copy()
    out.alpha_composite(turned(arm_only(whole, piece), hip, degrees, "leg"))
    return out


def bob(im, rows):
    """The whole figure lifted `rows` up the canvas, for the walk's rise."""
    if rows == 0:
        return im
    out = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    out.alpha_composite(im, (0, -rows))
    return out


def squash(im, factor):
    """The same drawing, narrower, about the line down his middle.

    HOW HE TURNS, and it is make_bati_front_frames' trick rather than a new
    one: a figure seen three-quarters on IS narrower than the same figure
    seen square on, so narrowing a front view reads as turning away from the
    camera. It is used here for ONE frame - the moment he turns to go - and
    not held, because a narrowing he keeps is a boy with different
    proportions rather than a boy who has turned.
    """
    if abs(factor - 1.0) < 0.001:
        return im
    inv = 1.0 / factor
    centre = WIDTH / 2.0
    return im.transform(
        (WIDTH, HEIGHT), Image.AFFINE,
        (inv, 0, centre - centre * inv, 0, 1, 0),
        resample=Image.BICUBIC, fillcolor=(0, 0, 0, 0))


# ======================================================================
# Writing them out
# ======================================================================

def despeckle(im, floor=1500):
    """Anything left over that is not joined to him.

    A BOY IS ONE PIECE. Every mechanism in this file moves a limb that is
    still attached at a joint, so the finished frame is one connected run of
    ink and anything else in it is a crumb some cut left behind - a fingertip
    outside a dilation, a few pixels of a crease. They are too small to see
    in a still and perfectly visible the moment the frame changes, because
    they are the only thing on screen that does not move. This is the net
    that catches them, and the floor is far below the smallest real part of
    him: his hand alone is twenty thousand pixels.
    """
    a = np.asarray(im).copy()
    labels, count = ndimage.label(a[:, :, 3] > 40)
    if count < 2:
        return im
    sizes = ndimage.sum(np.ones_like(labels), labels, range(1, count + 1))
    keep = np.zeros(count + 1, bool)
    keep[int(np.argmax(sizes)) + 1] = True
    for i, n in enumerate(sizes):
        if n >= floor:
            keep[i + 1] = True
    dropped = int(sizes.sum() - sizes[keep[1:]].sum())
    assert dropped < 4000, "despeckle would take %d px off the figure" % dropped
    a[~keep[labels], 3] = 0
    return Image.fromarray(a, "RGBA")


def write(im, name):
    assert im.size == (WIDTH, HEIGHT), (name, im.size)
    despeckle(im).save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, tayo.BASE)


# "Kunin", as (shoulder, elbow) for his right arm, and whether the bag is on.
#
# SHOULDER IS ABDUCTION - the whole arm swinging out from his side. ELBOW is
# FLEXION and goes NEGATIVE, because an elbow bends one way: the forearm comes
# in towards his chest and never past straight.
#
# THE SHAPE OF THE BEAT IS OUT, DOWN, GRIP, UP, IN, ON. Read the elbow column
# down the page and it is the whole story of a child picking something up:
# straight while the arm is going out, a few degrees as the hand turns onto
# the handle, straightening again as he takes the weight, and then folding
# steadily as the bag comes up and in to his shoulder. A reach with no elbow
# in it was the single-bone version's whole problem, and these nine frames
# are what two bones bought.
#
#   reach_1   the arm leaves his side, still straight
#   reach_2   out over the desk, the hand turning down onto the bag
#   grip      the fist closes - a beat of its own, because a bag that is
#             lifted on the same frame the hand arrives is a bag nobody saw
#             him take hold of
#   lift      up and clear of the wood, elbow straightening under the weight
#   carry_1   in across his body, elbow folding
#   carry_2   elbow shut, the bag up at his chest with his fist on the strap
#
# AND THE CARRY GOES UP, NOT ACROSS. The first pass brought his hand in at
# HIP height, which is where a hand that is only swinging ends up - and a bag
# hanging off a hand at that height has its base within a few hundredths of
# the desk's own top edge, drawn in front of it. For most of a second it sat
# there looking like a bag being PUT DOWN on the desk he had just taken it
# off. A bag going onto a back goes up past the chest first, which is both
# what a child does and the one path that keeps it clear of the wood.
#   settle    on his back - the straps are drawn here, and this is where the
#             bag in his hand is faded out
#   strap_1   his hand up at the strap on his chest
#   strap_2   pulling it down to sit, which is what everyone does
#   stand     his idle, wearing it
KUNIN = (("kunin_reach_1_uniform", 26.0, 0.0, False),
         ("kunin_reach_2_uniform", 50.0, -8.0, False),
         ("kunin_grip_uniform", 54.0, -14.0, False),
         ("kunin_lift_uniform", 72.0, -6.0, False),
         ("kunin_carry_1_uniform", 56.0, -44.0, False),
         ("kunin_carry_2_uniform", 26.0, -90.0, False),
         ("kunin_settle_uniform", 18.0, -82.0, True),
         ("kunin_strap_1_uniform", 10.0, -62.0, True),
         ("kunin_strap_2_uniform", 7.0, -34.0, True),
         ("kunin_stand_uniform", 0.0, 0.0, True))

# "Paalam", as the angle of his FOREARM about his drawn elbow. See
# raised_figure below for where the arm comes from and why it is not the one
# above: these frames are a different mechanism and the numbers are not in
# the same units as Kunin's.
#
# NEGATIVE IS DOWN, towards the hanging arm he starts from; positive carries
# the hand up past his shoulder and beside his head.
#
# THE SWEEP IS THE WAVE. 54 is where his hand rests while he is waving - open
# palm level with his jaw - and the sweep runs twelve degrees either side of
# it, so the hand travels about its own width across and back. A hand held up
# is not a goodbye, which is what the revision notes say in as many words and
# what the single-bone version could only ever manage.
PAALAM_REST = 54.0
PAALAM_SWING = 12.0
# AND THE LAST FRAME DOWN IS THE LOWEST OF THEM, not one in the middle of the
# lift. paalam_down is what the arm passes through on its way back to the
# hanging idle Kunin left him in, so it is further down than lift_1 rather
# than between lift_1 and lift_2; the tail reads lift_3, lift_2, lift_1,
# down, and then kunin_stand, which is one steady fall.
PAALAM = (("paalam_lift_1_uniform", -26.0),
          ("paalam_lift_2_uniform", 8.0),
          ("paalam_lift_3_uniform", 34.0),
          ("paalam_wave_in_uniform", PAALAM_REST - PAALAM_SWING),
          ("paalam_wave_mid_uniform", PAALAM_REST),
          ("paalam_wave_out_uniform", PAALAM_REST + PAALAM_SWING),
          ("paalam_down_uniform", -46.0))

# "Alis". A turn and then four frames of a walk - step, pass, step, pass -
# with the whole figure rising on the passing frames, which is what makes it
# a walk rather than legs waving under a boy standing still.
#
# THE LEGS OPEN AND CLOSE, THEY DO NOT CROSS. Seen from the front a stride is
# mostly foreshortened away; what is left of it is the legs opening on the
# step and closing as the body passes over them. The first version swung them
# opposite each other - one forward, one back, as a profile walk does - and
# from the front that scissored them past one another below the frame, which
# is not a thing legs do.
#
# WHAT TELLS THE TWO STEPS APART IS THE ARMS. Both contact frames have the
# same legs, so without something else they would read as one pose flickering
# twice a cycle rather than as a left step and a right step; the arm that
# leaves his hip changes sides between them, which is also what a walking
# child's arms do. They move a few degrees and no more, because a front view
# shows almost none of an arm swing.
#
# Each row is: the frame, his right leg, his left leg, his right arm, his
# left arm, and how many rows the whole figure rises. Out is positive on
# whichever side the limb is on.
ALIS = (("alis_walk_1_uniform", 15.0, 15.0, 9.0, 4.0, 0),
        ("alis_walk_2_uniform", 0.0, 0.0, 5.0, 5.0, 9),
        ("alis_walk_3_uniform", 15.0, 15.0, 4.0, 9.0, 0),
        ("alis_walk_4_uniform", 0.0, 0.0, 5.0, 5.0, 9))

# How narrow he gets on the one frame where he turns to go. 0.74 is
# make_bati_front_frames' reading of the same cheat: narrow enough to read as
# a boy who has turned, not so narrow that it reads as a fault.
TURN_SQUASH = 0.74


_FIGURES = {}


def figure(wear, drop=()):
    """base_figure, remembered - it is built a dozen times per run."""
    key = (bool(wear), tuple(sorted(drop)))
    if key not in _FIGURES:
        _FIGURES[key] = base_figure(wear, key[1])
    return _FIGURES[key]


def posed(wear, arms=(), legs=()):
    """The boy with each named limb turned to its own angle.

    `arms` is a list of (side, shoulder, elbow); `legs` a list of (side,
    degrees). The body is built ONCE with every one of them left off, and
    each is then laid back on turned, so no limb has to be cut out of a
    finished drawing and nothing is filled in behind one.
    """
    drop = [("arm", s) for s, _, _ in arms] + [("leg", s) for s, _ in legs]
    whole = figure(wear)
    out = figure(wear, drop)
    for side, shoulder, elbow in arms:
        out = bent_arm(whole, out, side, shoulder, elbow)
    for side, degrees in legs:
        out = swung_leg(whole, out, degrees, side)
    return out


# ======================================================================
# The arm he waves with, which is a DRAWN one
# ======================================================================
#
# EVERY FRAME ABOVE IS AN ARM THAT HANGS, TURNED. That is right for a reach -
# a child reaching across a desk does straighten his arm, and the elbow above
# only has to take up the last of it - and it is wrong for a wave, which is
# the one gesture that is almost all elbow. Turned far enough to put the hand
# beside his head, a hanging arm arrives UPSIDE DOWN: the fingers that were
# drawn pointing at the floor now point at the ceiling, the thumb is on the
# outside, and the whole limb is one straight line from the armpit to the
# fingertips. That is the "broken, unnatural" the screenshots show, and no
# amount of re-timing or re-angling fixes it, because the drawing in those
# frames is a drawing of an arm at rest.
#
# SO THE WAVE USES AN ARM THAT WAS DRAWN RAISED. talking_uniform is this same
# boy, same shirt, same scale, with one arm up: a drawn sleeve, a drawn upper
# arm, a drawn elbow bent at a right angle, and a drawn open palm with the
# fingers apart. make_tayo_frames already scales that drawing to this one's
# head and grafts its lower body on at the waist, so the registration is not
# a new problem - the two figures line up to a few pixels at the shoulders,
# which is what makes this possible at all.
#
# WHAT IS TAKEN, AND WHAT IS LEFT ALONE. Only the pixels that DIFFER: the
# raised arm, the sleeve round it, and the hanging arm it replaces. His chest
# is left as the seated drawing has it, because the two shirts are both flat
# white and a rectangle of one laid over the other shows up as a tone step
# across his front - which is what the first attempt at this did. So the
# patch is the union of what is not shirt-white in either drawing, closed and
# filled, plus the sleeve's own rows; everywhere else the two are the same
# colour and there is nothing to carry across.
#
# AND THE WAVE IS THEN ONE ROTATION. Forearm and hand about the drawn elbow,
# which is make_bati_front_frames' mechanism exactly, on the drawing it was
# written for. Nothing inside the piece moves relative to anything else
# inside it, so the hand in every frame is the hand that was drawn.
GRAFT_CUT_X = 624                  # inboard of the armpit, down the chest
GRAFT_TOP_REL = 418
GRAFT_SLEEVE_BOT_REL = 706         # the sleeve's rows go across whole
GRAFT_BOT_REL = 748                # and the arm's reach down to his hem
GRAFT_ELBOW_BACK = 34              # how far back along the forearm the piece
GRAFT_BODY_BACK = 12               # is cut, and the body cleared
GRAFT_FILL_REACH = 14              # how far what it leaves behind is filled


def _nonwhite(img):
    a = np.asarray(img).astype(int)
    lo, hi = a[..., :3].min(axis=2), a[..., :3].max(axis=2)
    return (a[..., 3] > 60) & ~((lo > 150) & (hi - lo < 50))


def mirrored_talking(body):
    """talking_uniform, head-matched, placed on this canvas, and flipped.

    Flipped because the drawing raises his LEFT arm and this scene wants his
    right one - the bag is on that side, the desk is on that side, and the
    whole of Kunin happens there. A mirror of a front view of a boy is the
    same boy; nothing in this drawing is lettered or handed.

    Its shirt is then matched to the seated drawing's TWICE: once by
    make_tayo_frames' own gain, which is per-channel over the whole garment,
    and once more against the chest it will actually sit next to in these
    rows. The second pass is worth two hundredths of a stop and is the
    difference between a seam you cannot find and one you can.
    """
    seat = load(tayo.BASE)
    c, b, _ = hair_cap(seat)
    im, crown, cx, _ = tayo.scaled_to_base(tayo.LOWER, b - c)
    im = tayo.match_shirt_white(im, tayo.shirt_white(seat))
    layer = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    layer.alpha_composite(im, (WIDTH // 2 - int(round(cx)) + tayo.LOWER_DX,
                               CROWN_ROW - int(round(crown))))
    mir = layer.transpose(Image.FLIP_LEFT_RIGHT)

    a = np.asarray(mir).astype(float)
    bb = np.asarray(body).astype(float)

    def pale(x):
        lo, hi = x[..., :3].min(axis=2), x[..., :3].max(axis=2)
        return (x[..., 3] > 200) & (lo > 170) & (hi - lo < 40)

    band = np.zeros(a.shape[:2], bool)
    band[CROWN_ROW + GRAFT_TOP_REL: CROWN_ROW + GRAFT_BOT_REL,
         GRAFT_CUT_X - 120: GRAFT_CUT_X + 10] = True
    both = pale(a) & pale(bb) & band
    assert both.sum() > 500, "no shirt in common to match the graft against"
    gain = bb[both][:, :3].mean(axis=0) / a[both][:, :3].mean(axis=0)
    shirt = pale(a)
    a[shirt, :3] = np.clip(a[shirt, :3] * gain, 0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA"), gain


_RAISED = {}


def raised_figure(wear):
    """The standing boy with talking_uniform's raised arm on his right side."""
    key = bool(wear)
    if key in _RAISED:
        return _RAISED[key]

    body = figure(False, (("arm", 1),))
    mir, gain = mirrored_talking(body)

    band = np.zeros((HEIGHT, WIDTH), bool)
    band[CROWN_ROW + GRAFT_TOP_REL: CROWN_ROW + GRAFT_BOT_REL,
         GRAFT_CUT_X:] = True
    reg = (_nonwhite(mir) | _nonwhite(body)) & band
    reg = ndimage.binary_closing(reg, np.ones((9, 9)))
    reg = ndimage.binary_fill_holes(reg)
    reg = ndimage.binary_dilation(reg, np.ones((5, 5))) & band
    # The sleeve goes across whole, white and all: its silhouette is the
    # raised one's, not the hanging one's, and a white blob left over from the
    # arm that is no longer there would stand outside him against the room.
    reg[CROWN_ROW + GRAFT_TOP_REL: CROWN_ROW + GRAFT_SLEEVE_BOT_REL,
        GRAFT_CUT_X:] = True

    a = np.asarray(body).copy()
    a[reg] = np.asarray(mir)[reg]
    out = Image.fromarray(a, "RGBA")

    if wear:
        # THE BAG GOES ROUND THE GRAFT, not under it. worn_bag draws the part
        # of the backpack that shows behind him and the straps that show in
        # front, and the graft would paint over both - so they are put back,
        # the body behind him and the straps over his shirt, with the raised
        # arm in front of the straps where it crosses them. An arm waving
        # from behind his own strap is the one thing worse than no strap at
        # all.
        crown, bottom, _ = hair_cap(load(tayo.BASE))
        front, behind, _ = worn_bag(bottom - crown)
        layered = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
        layered.alpha_composite(behind)
        layered.alpha_composite(out)
        layered.alpha_composite(front)
        arm, _, _ = raised_arm(out)
        layered.alpha_composite(arm_only(out, arm))
        out = layered

    _RAISED[key] = (out, gain)
    return _RAISED[key]


def raised_arm(im):
    """The raised forearm and hand, and the drawn elbow they turn on."""
    a = np.asarray(im)
    mask = skin_mask(a)
    box = np.zeros_like(mask)
    box[CROWN_ROW + 400: CROWN_ROW + GRAFT_SLEEVE_BOT_REL,
        WIDTH // 2 + 60:] = True
    mask &= box
    # SEVEN, not four. The fingertips are drawn inside a line of their own
    # that runs a few pixels past the skin, and a dilation that stops short
    # of it leaves those pixels behind when the hand swings away - four dark
    # commas hanging in the air beside his head, in every frame, at exactly
    # the place the first version of the hand had been.
    grown = ndimage.binary_dilation(
        mask, ndimage.generate_binary_structure(2, 2), iterations=ARM_GROW + 1)
    labels, count = ndimage.label(grown)
    assert count, "no raised arm in the graft"
    sizes = ndimage.sum(grown, labels, range(1, count + 1))
    grown = labels == int(np.argmax(sizes)) + 1

    ys, xs = np.where(mask & grown)
    # THE SHIRT IS NOT PART OF THE ARM. Growing the skin by four picks up the
    # line drawn round the limb, which has to come with it, and also four
    # pixels of whatever the limb is lying against - here his sleeve and his
    # chest. Turned, that came round as a white rind on the outside of every
    # finger, which against a navy strap is a hand with a halo. So the piece
    # keeps the skin and the dark line and gives the pale back.
    lo = a[..., :3].min(axis=2).astype(int)
    hi = a[..., :3].max(axis=2).astype(int)
    grown &= ~((lo > 150) & (hi - lo < 55))
    elbow = (float(xs[ys >= ys.max() - 10].mean()), float(ys.max()))
    hand = (float(xs[ys <= ys.min() + 50].mean()), float(ys.min() + 30))
    return grown, elbow, hand


def waved(wear, degrees):
    """The raised arm with its forearm and hand turned about the elbow.

    The two cut lines are make_bati_front_frames': the piece is taken from
    further back along the forearm than the body is cleared from, so the
    wedge the rotation opens lands on arm rather than on a gap, and what the
    arm leaves behind is filled from its own edges - shirt where it lay on
    his shirt, nothing where it hung against the room.
    """
    whole, _ = raised_figure(wear)
    arm, elbow, hand = raised_arm(whole)

    ex, ey = elbow
    axis = np.array([hand[0] - ex, hand[1] - ey], float)
    axis /= np.linalg.norm(axis)
    a = np.asarray(whole)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    along = (xx - ex) * axis[0] + (yy - ey) * axis[1]

    # WHAT THE ARM LEAVES BEHIND, filled from its own edges - shirt where it
    # lay on his shirt, nothing where it hung against the room.
    #
    # ONLY AS FAR AS THE EDGE REACHES, though, which make_bati_front_frames
    # does not have to say because on that drawing the arm is against his
    # body for its whole length. Here most of it is out in the open, and a
    # fill with no limit hunts for the nearest solid pixel however far away
    # that is: the room filled up with fans of smeared shirt and a lump of
    # his own hair, hanging in the air beside the hand. Past REACH the hole
    # is simply a hole, because past REACH there was nothing behind the arm.
    lifted = arm & (along > -GRAFT_BODY_BACK)
    body = a.copy()
    body[lifted, 3] = 0
    if lifted.any():
        far, (iy, ix) = ndimage.distance_transform_edt(lifted, return_indices=True)
        near = lifted & (far <= GRAFT_FILL_REACH)
        body[near] = body[iy[near], ix[near]]

    piece = a.copy()
    piece[~(arm & (along > -GRAFT_ELBOW_BACK)), 3] = 0
    out = Image.fromarray(body, "RGBA")
    out.alpha_composite(turned(Image.fromarray(piece, "RGBA"), elbow,
                               degrees, "waving forearm"))
    return out


def main():
    print("Uwian - the standing half of Paaralan Scene 5.")
    print("  canvas %dx%d, crown on row %d" % (WIDTH, HEIGHT, CROWN_ROW))
    piece, (shoulder, r_s), (elbow, r_e) = joints(np.asarray(figure(False)), 1)
    print("  his right arm: shoulder (%.0f, %.0f) r %.0f,  elbow (%.0f, %.0f) r %.0f"
          % (shoulder[0], shoulder[1], r_s, elbow[0], elbow[1], r_e))

    # His bag on its own, for the prop that rides his fist, and the one that
    # stands on the desk before he takes it. Both are Tools/make_desk_bag.py's
    # now - the same cut-out this file used to write, with the shirt its
    # dilation borrowed taken back off and, for the desk, the light and the
    # shadow this room would put on it.
    import make_desk_bag                                      # noqa: E402
    make_desk_bag.main()

    print("\n  Kunin - he takes his bag off the desk and puts it on")
    for name, sh, el, wear in KUNIN:
        write(posed(wear, arms=[(1, sh, el)]), name)
        gx, gy = fist(figure(wear), sh, el)
        print("    %-26s sh %4.0f  el %4.0f   fist at (%5.2f, %5.2f)%s"
              % (name + ".png", sh, el, gx, gy, "   wearing it" if wear else ""))

    print("\n  Paalam - he waves goodbye, wearing it, on a DRAWN raised arm")
    _, gain = raised_figure(True)
    print("    grafted from %s, shirt trimmed by %.3f %.3f %.3f"
          % (tayo.LOWER, gain[0], gain[1], gain[2]))
    for name, theta in PAALAM:
        write(waved(True, theta), name)
        print("    %-26s forearm %+5.0f deg" % (name + ".png", theta))

    print("\n  Alis - he walks out of the left of the frame, wearing it")
    write(squash(figure(True), TURN_SQUASH), "alis_turn_uniform")
    print("    %-26s narrowed to %.0f%% - he turns to go"
          % ("alis_turn_uniform.png", TURN_SQUASH * 100))
    for name, rleg, lleg, rarm, larm, rise in ALIS:
        legs = [l for l in ((1, rleg), (-1, -lleg)) if abs(l[1]) > 0.01]
        arms = [a for a in ((1, rarm, 0.0), (-1, larm, 0.0)) if abs(a[1]) > 0.01]
        write(bob(posed(True, arms=arms, legs=legs), rise), name)
        print("    %-26s legs %+5.0f %+5.0f  arms %+5.0f %+5.0f  up %d"
              % (name + ".png", rleg, lleg, rarm, larm, rise))


if __name__ == "__main__":
    main()
