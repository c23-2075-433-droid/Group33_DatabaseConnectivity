"""Draw the "Bati" greeting for Paaralan Scene 2: Kylo turns to US and waves.

WHAT THEY ARE, and why Scene 1's wave could not be reused.

Paaralan Scene 1 already has a drawn greeting - bati_frame_1..3_uniform_with_bag,
cut from a Canva strip by Tools/make_bati_frames.py. It is a PROFILE wave,
because at the school gate Kylo is greeting the guard, who stands beside him.
Scene 2 has nobody beside him: the teacher was cut from the locker-corner view
and the storyboard has only Kylo in it. The greeting there is aimed at the
CHILD PLAYING, so he has to turn out of his profile and wave at the camera.

Eight frames do it, and all eight are moved pixels rather than new drawings:

  bati_turn_a      his profile, narrowing as he comes round
  bati_turn_b      still narrow, now facing us
  bati_front       facing us, hand at his side as drawn
  bati_lift_a/b    the hand on its way up
  bati_wave_up     the top of the sweep, hand beside his ear
  bati_wave_mid    the middle of it, forearm vertical
  bati_wave_out    the bottom, hand out to the side

The pose underneath them all is talking_uniform_with_bag, which already exists:
he faces us, smiling, one hand up with the palm open and the other resting on
his backpack strap. It is the pose Scene 1 deliberately does not use because it
turns his back on the guard - which is exactly what makes it the right one here.

==========================================================================
HIS HAND IS NEVER CUT. That is the whole design of this version, and it is
what the two before it got wrong.

The hand broke twice, for two different reasons, and both are worth keeping
written down because they are the traps in this technique.

FIRST: the mask lost his fingers. The arm is found by taking skin-coloured
pixels and keeping the biggest connected blob - but every finger is drawn with
a black line all the way round it, and a line is not skin, so the skin in that
region is not one blob but SEVEN: the arm and palm at 3336 px, and four
fingertips at 122, 84, 47 and 39. Keeping the biggest left his fingers behind,
where they stayed while the palm rotated out from under them. Dilating BEFORE
labelling bridges those outlines and the seven become one.

SECOND: the wave was cut through the wrist. Tipping the hand about the wrist
does read as a wave, but it means a cut line across the hand, a rotated copy
over a stationary one, and two sets of finger outlines a few degrees apart. Any
cut through a hand shows, because a hand is all outline and no flat area.

So this version does not bend him at all. THE WHOLE FOREARM AND HAND ROTATE
TOGETHER, RIGIDLY, ABOUT HIS ELBOW. A rigid rotation cannot deform anything:
the hand in every frame is the hand that was drawn, the same pixels at the same
spacing, just pointing somewhere else. There is no wrist cut and no finger cut,
so there is nothing left that can break.

The one seam left is at the elbow, and it is handled the way make_kinig_frames
handles the neck: the body gives up more of the arm than the piece takes back,
so the wedge a rotation opens lands on arm rather than on a gap. What the arm
leaves behind is filled from its own edges - shirt where it lay on his shirt,
nothing where it lay against the room.

AND IT IS A REAL WAVE, not a raised hand. The forearm sweeps 11 degrees either
side of vertical, so his hand travels about 45 pixels across - a hand's width
and a half - three times over. Side to side, which is what a wave is.

WHAT ROTATING THE ELBOW CAN AND CANNOT DO. It can raise his hand from his side
to beside his ear: at 0 degrees his hand is at row 244 and out to the side, at
-26 it is at row 228 with the forearm vertical. It cannot raise it higher than
that, because rotating a bent arm about its elbow does not straighten it, and
past -48 his fingers cross his face. -37 is the far end of the sweep for that
reason - rule 6, no clipping.

HIS OTHER HAND is not touched by any of this. It rests on his backpack strap in
the pose underneath, and the mask is boxed to the raised side, so it cannot be.

==========================================================================
THE FEET. Every pose of Kylo's is 745 pixels tall, crown on the first row and
feet on the last, so swapping one for another never moves him up or down.
Nothing makes that true across the canvas, and it was not:

    standing_uniform_with_bag        feet 28.5 px right of the canvas centre
    talking_uniform_with_bag         feet 16.5 px right
    walk_frame_1_uniform_with_bag    feet  1.5 px right

A sprite is drawn centred on its object, so those offsets are where his feet
land: turning from the profile to the front slid him sideways while standing
still. Every frame here is padded with transparent columns until its feet sit
where the profile's do. The walk frames are 27 px out on the same measurement
and this does not touch them - they are shared with every other scene.

THE TURN takes two frames, not one. A single squashed frame held a third of a
second reads as a squashed boy; a turn has to pass THROUGH narrow rather than
stop there. His profile narrows, the front view is still narrow, then he is
facing us.

THEIR .META MUST SAY 120 PIXELS PER UNIT. Every standing sprite of Kylo's
imports at 120 and every sprite generated for him since takes Unity's default
of 100, which is twenty percent - "Bati" swapping him to a 100 frame and back
is how he once swelled a fifth every time he waved. The meta is copied from
talking_uniform_with_bag's, so these inherit its 120 and its pivot.

Writes  Assets/Sprites/bati_turn_a_uniform_with_bag.png
        Assets/Sprites/bati_turn_b_uniform_with_bag.png
        Assets/Sprites/bati_front_uniform_with_bag.png
        Assets/Sprites/bati_lift_a_uniform_with_bag.png
        Assets/Sprites/bati_lift_b_uniform_with_bag.png
        Assets/Sprites/bati_wave_up_uniform_with_bag.png
        Assets/Sprites/bati_wave_mid_uniform_with_bag.png
        Assets/Sprites/bati_wave_out_uniform_with_bag.png

Run from the repository root:  python Tools/make_bati_front_frames.py
"""
import io
import os
import re
import uuid

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

SRC = "talking_uniform_with_bag"       # faces us, one hand already up
PROFILE = "standing_uniform_with_bag"  # what he turns out of and back into

# How narrow he gets halfway through the turn. A figure seen three-quarters on
# IS narrower than the same figure seen square on, which is why the cheat works
# at all; 0.70 is the narrowest that still reads as him rather than as a fault.
TURN_SQUASH = 0.70

# THE WAVE, as angles of the whole forearm about his elbow. Negative lifts.
#
# REST is where his hand sits while he is waving - forearm vertical, hand up
# beside his ear. SWING is how far it sweeps either side of that, so the hand
# travels twice this and the motion is side to side rather than a hand held up.
REST = -26.0
SWING = 11.0

# The raise: two frames between his hand at his side and the wave, so the hand
# goes UP rather than appearing up.
LIFT = (-9.0, -18.0)

# The cut across the arm, in pixels back from the elbow along the forearm. The
# piece is always taken from further back than the body is cleared, and the gap
# between them is what the rotation's wedge lands on.
PIECE_BACK = 30
BODY_BACK = 10

# How far the arm mask is grown past his skin, to take the black line drawn
# round it - and, just as importantly, to bridge the lines between his fingers
# so the hand comes out as one piece. Four pixels is that line on this canvas.
OUTLINE = 4


def meta_for(name, source):
    """Give a new sprite the same import settings as the one it came from."""
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, source + ".png.meta"), encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(source + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")


def feet_centre(img):
    """The x his weight is over: the middle of the ink in his bottom rows.

    His shoes, in other words. Everything above them can be reaching off to one
    side; his feet are where he is standing.
    """
    alpha = np.asarray(img)[:, :, 3]
    rows = np.where(alpha.max(axis=1) > 8)[0]
    band = alpha[rows.max() - 40:rows.max() + 1]
    xs = np.where(band.max(axis=0) > 8)[0]
    return (xs.min() + xs.max()) / 2.0


def align_feet(img, target_offset):
    """Pad the canvas until his feet sit where the profile's feet sit.

    Transparent columns on one side only. Adding n of them moves his feet n to
    the right and the canvas centre n/2, so the offset between them grows by
    n/2 - which is the whole of the arithmetic.
    """
    offset = feet_centre(img) - img.size[0] / 2.0
    pad = int(round(2 * (target_offset - offset)))
    if pad == 0:
        return img
    w, h = img.size
    out = Image.new("RGBA", (w + abs(pad), h), (0, 0, 0, 0))
    out.alpha_composite(img, (pad if pad > 0 else 0, 0))
    return out


def skin_mask(img):
    """His bare arms and hands, which is how the elbow is found."""
    a = np.asarray(img).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    return (al > 100) & (r > 200) & (g > 120) & (g < 215) & (b < 175) & (r - b > 60)


def raised_arm(img):
    """The pixels of the raised forearm and hand, and the elbow to swing them on.

    He has skin in four places - face, both arms, both legs - so the search is
    boxed to the band between his chin and his shorts on the side the raised arm
    is on. His OTHER hand is outside that box and is never touched.

    GROW IT BEFORE YOU LABEL IT. Each finger is drawn inside its own black line,
    so the skin here is seven blobs rather than one; dilating first bridges the
    lines, and the label step then finds a whole hand instead of a palm with its
    fingertips left behind. That was the first way this broke.
    """
    w, h = img.size
    mask = skin_mask(img)
    box = np.zeros_like(mask)
    box[int(0.28 * h):int(0.58 * h), : int(0.45 * w)] = True   # chin to shorts
    mask &= box

    grown = ndimage.binary_dilation(
        mask, ndimage.generate_binary_structure(2, 2), iterations=OUTLINE)
    labels, count = ndimage.label(grown)
    assert count, "no bare arm found on the raised side"
    sizes = ndimage.sum(grown, labels, range(1, count + 1))
    grown = labels == int(np.argmax(sizes)) + 1

    # The joints are measured on the SKIN inside that blob, not on the grown
    # edge, so growing it by another pixel does not move his elbow.
    ys, xs = np.where(mask & grown)
    low, high = ys.max(), ys.min()
    elbow = (float(xs[ys >= low - 6].mean()), float(low))
    hand = (float(xs[(ys >= high) & (ys <= high + 40)].mean()), float(high + 25))
    return grown, elbow, hand


def squash(img, centre, factor):
    """The same drawing, narrower, about the line through his feet."""
    w, h = img.size
    # PIL's AFFINE maps output back to input, so the inverse scale goes in.
    inv = 1.0 / factor
    return img.transform(
        (w, h), Image.AFFINE, (inv, 0, centre - centre * inv, 0, 1, 0),
        resample=Image.BICUBIC, fillcolor=(0, 0, 0, 0))


def swing(front, arm, elbow, hand, degrees):
    """His forearm and hand, rotated together about his elbow as one rigid piece.

    Nothing inside the arm moves relative to anything else inside it, so the
    hand cannot deform however far this goes. Only the elbow seam has to be
    hidden, and that is what the two cut lines are for.
    """
    ex, ey = elbow
    axis = np.array([hand[0] - ex, hand[1] - ey], float)
    axis /= np.linalg.norm(axis)

    a = np.asarray(front)
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    along = (xx - ex) * axis[0] + (yy - ey) * axis[1]

    # Take the arm off and fill what it was covering from its own edges: shirt
    # where it lay on his shirt, nothing where it lay against the room.
    lifted = arm & (along > -BODY_BACK)
    body = a.copy()
    body[lifted, 3] = 0
    if lifted.any():
        _, (iy, ix) = ndimage.distance_transform_edt(lifted, return_indices=True)
        body[lifted] = body[iy[lifted], ix[lifted]]

    piece = a.copy()
    piece[~(arm & (along > -PIECE_BACK)), 3] = 0
    before = int((piece[:, :, 3] > 8).sum())
    piece = Image.fromarray(piece).rotate(
        degrees, resample=Image.BICUBIC, center=(ex, ey), fillcolor=(0, 0, 0, 0))

    # A hand that swings off the edge of the canvas comes back with its fingers
    # sliced off, and the frame still looks plausible until you see it move. The
    # rotation keeps the canvas, so count the arm before and after: anything
    # that left is a piece of him that is now missing.
    after = int((np.asarray(piece)[:, :, 3] > 8).sum())
    assert after > before * 0.97, (
        "the arm is clipped by the canvas at %+.0f degrees: %d px of %d left it. "
        "Pad the source wider before rotating." % (degrees, before - after, before))

    out = Image.fromarray(body)
    out.alpha_composite(piece)
    return out


def hand_top(img, arm, elbow, hand, degrees):
    """The topmost row his HAND reaches, for the report.

    Measured on the rotated arm piece alone. Measuring it on the whole frame
    reports row 7, which is the top of his hair.
    """
    ex, ey = elbow
    axis = np.array([hand[0] - ex, hand[1] - ey], float)
    axis /= np.linalg.norm(axis)
    a = np.asarray(img).copy()
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    along = (xx - ex) * axis[0] + (yy - ey) * axis[1]
    a[~(arm & (along > -PIECE_BACK)), 3] = 0
    rotated = Image.fromarray(a).rotate(degrees, resample=Image.BICUBIC,
                                        center=(ex, ey), fillcolor=(0, 0, 0, 0))
    rows = np.where(np.asarray(rotated)[:, :, 3].max(axis=1) > 8)[0]
    return int(rows.min())


def main():
    front = load(SRC)
    profile = load(PROFILE)

    target = feet_centre(profile) - profile.size[0] / 2.0
    print("%s %dx%d, feet %+.1f px from its centre - every frame is padded to match"
          % (PROFILE, profile.size[0], profile.size[1], target))

    arm, elbow, hand = raised_arm(front)
    print("raised arm: %d px, elbow (%.0f, %.0f), hand (%.0f, %.0f)"
          % (arm.sum(), elbow[0], elbow[1], hand[0], hand[1]))

    fc, pc = feet_centre(front), feet_centre(profile)
    plan = [
        ("bati_turn_a_uniform_with_bag", squash(profile, pc, TURN_SQUASH), PROFILE,
         "his profile, narrowing"),
        ("bati_turn_b_uniform_with_bag", squash(front, fc, TURN_SQUASH), SRC,
         "facing us, still narrow"),
        ("bati_front_uniform_with_bag", front.copy(), SRC,
         "facing us, hand as drawn"),
        ("bati_lift_a_uniform_with_bag", swing(front, arm, elbow, hand, LIFT[0]), SRC,
         "the hand starting up  (%+.0f deg)" % LIFT[0]),
        ("bati_lift_b_uniform_with_bag", swing(front, arm, elbow, hand, LIFT[1]), SRC,
         "nearly up            (%+.0f deg)" % LIFT[1]),
        ("bati_wave_up_uniform_with_bag", swing(front, arm, elbow, hand, REST - SWING), SRC,
         "top of the sweep     (%+.0f deg)" % (REST - SWING)),
        ("bati_wave_mid_uniform_with_bag", swing(front, arm, elbow, hand, REST), SRC,
         "middle of it         (%+.0f deg)" % REST),
        ("bati_wave_out_uniform_with_bag", swing(front, arm, elbow, hand, REST + SWING), SRC,
         "bottom of the sweep  (%+.0f deg)" % (REST + SWING)),
    ]

    for name, img, source, what in plan:
        img = align_feet(img, target)
        off = feet_centre(img) - img.size[0] / 2.0
        assert abs(off - target) < 1.0, (name, off, target)
        assert img.size[1] == profile.size[1], (name, img.size)
        img.save(os.path.join(SPRITES, name + ".png"))
        meta_for(name, source)
        print("  %-34s %3dx%d  feet %+.1f   %s"
              % (name + ".png", img.size[0], img.size[1], off, what))

    print("\nhand reaches row %d at rest, %d at the top of the sweep "
          "(his jaw is at 215, his shoulder at 245)"
          % (hand_top(front, arm, elbow, hand, REST),
             hand_top(front, arm, elbow, hand, REST - SWING)))
    print("\nWire them in BuildPaaralanScene2 as the 'Bati' PoseSequence:")
    print("  frames      turn_a, turn_b, front, lift_a, lift_b")
    print("  loopFrames  wave_up, wave_mid, wave_out, wave_mid")
    print("  tailFrames  lift_b, lift_a, front, turn_b, turn_a")
    print("  destination %s   then the walk" % PROFILE)


main()
