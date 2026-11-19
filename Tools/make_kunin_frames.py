"""Draw the "Kunin" frames - Kylo takes his bag off the desk and puts it on.

SUPERSEDED AS A SCRIPT, STILL USED AS A MODULE. Running this writes nothing.
"Kunin" is now drawn from Canva strips by Tools/make_uwian_canva_frames.py,
with Kylo's own head and skin put back by Tools/kylo_own_head.py; the version
described below bent him near the WRIST and its reach was about twenty pixels
of hand travel. Docs/AnimationStandard.md has the history.

What stays is everything this file worked out about HIS BAG, which the new one
imports rather than reimplements - loose_bag, bag_layers, placed_bag,
strap_infill and the measurements around them. Subtracting
talking_uniform_with_bag from talking_uniform is still the only way the
project has of getting a worn backpack with no new art, and none of it needed
redoing. Everything below is left as it was written so that reasoning stays
where it was found.

THE BEAT. Paaralan Scene 5's third word. He has packed the afternoon lesson
away ("Ligpit") and stood up out of his chair ("Tayo"); his bag is on the
strip of desk he has just cleared. The child says "Kunin" and he reaches out,
takes it, and swings it onto his back. The level ends with him standing at
his desk WEARING it, which is how Kylo is drawn everywhere else in Level 2 -
Paaralan Scene 2 is a whole scene of a boy with that backpack on.

WHY THIS FILE EXISTS. Until now "Kunin" was a prop and a gleam: the bag was
switched on upon the desk and nothing about the boy changed. The note at the
top of BuildPaaralanScene5.cs is honest about why - every reaching pose in
the project is SEATED, every standing one is a wide shot with a floor in it,
and klasrum_interior_background has no floor. It also says what the three
undrawn words were waiting for: "they now have a pose to start and end on -
tayo_stand_uniform - which is the one thing they were missing."

This is that. Two pieces, and neither is a new drawing of him:

  THE ARM IS DRAWN, NOT GRAFTED. Tools/make_tayo_frames.py had to invent his
  forearms to get him out of the chair: the seated sprite stops flat at the
  desk's far edge, so it extends the forearm by copying its last row - the
  arm is vertical and a constant 67 pixels wide for a hundred rows, so
  continuing it is copying a row - tapers it into a wrist, and puts
  standing_uniform's hanging hand on the end. A limb that is DRAWN BY THIS
  PROJECT rather than taken from a drawing can be drawn going somewhere
  else, and that is the whole trick here: the same copied row, laid along a
  path that bends away from his side, with the same hand rigidly rotated to
  point along it.

  That is why this beat is possible and "Paalam" still is not. A wave needs
  his arm ABOVE his shoulder and the shoulder is drawn; a reach to a desk
  needs it below his elbow, and below his elbow there is nothing drawn to
  contradict.

  Docs/AnimationStandard.md warns that rotating a forearm "tears away from
  the sleeve past about 20 degrees". That is about rotating a DRAWN forearm
  inside a drawing, where the sleeve above it stays put. Nothing tears here
  because nothing is cut: the bend starts 24 rows INSIDE the seated
  drawing's own arm, under pixels that are composited over it afterwards,
  and the angle is eased in over the next fifty rows rather than turned on.
  What leaves the sleeve is a copy of the sleeve's own last row.

  THE BAG IS THE DIFFERENCE BETWEEN TWO DRAWINGS. talking_uniform and
  talking_uniform_with_bag are the same front-facing pose with and without
  the backpack - the plain one was made from the other by
  Tools/strip_backpack.py - so subtracting them leaves the bag and nothing
  else. That is the straps over his shoulders, the bag's top edge showing
  past his right shoulder, and the body of it at his side.

  It is split in two by HIS OWN SILHOUETTE in the plain drawing: what lies
  inside it (the straps) is drawn over him, what lies outside (the bag) is
  drawn behind him. Composited as one layer the bag would sit on top of the
  arm hanging in front of it, which is a bag worn on the chest.

HIS FOREARM IS ACROSS THE RIGHT STRAP IN THE SOURCE, which is the one thing
the subtraction cannot give. In talking_uniform_with_bag his right hand is up
holding that strap and his forearm crosses it diagonally, so what comes out
of the difference is the strap above his arm, the bag below it, and a hole
between them where neither was visible. In the pose this file builds his arms
are DOWN, and the hole would be a strap that stops at his chest.

So that stretch is drawn, the same way the forearm is: a cross-section of the
strap - one row of the drawing, 24 pixels of it, taken from above his arm -
laid down the path the strap takes. The path is not invented either. His arm
covers the strap's RIGHT side, never its left, so its left edge is measured
all the way down; only the width is carried, and the width of a backpack
strap does not change. The drawing's own pixels are composited over the top
afterwards, so every pixel that WAS visible is the drawing's.

THE CANVAS IS WIDER THAN THE OTHERS AND HAS TO BE. 663 pixels is as wide as
the seated drawing, which is as wide as a boy with his arms at his sides;
reaching takes his hand past that edge. The frames are 723 - 30 added at EACH
side, symmetric for the same reason make_tayo_frames' padding is symmetric: a
sprite draws centred on its object, so padding one side only would slide him
sideways. Height stays 1521 and the scene scales by HEIGHT, so the wider
frames register with the tayo_* ones pixel for pixel.

HOW FAR HE REACHES. The bag's left edge is at world x 4.60 and his hand hangs
at 3.88, so the reach has to cross 0.72 world units - 118 pixels. At 52
degrees the fingertips land at 4.66, which is why that is the angle. It is
also about as far as this can go: the hand is rigid, and past about 60
degrees it stops reading as an arm and starts reading as a hand lying on its
side.

AND HIS BAG ITSELF, because the one in the project is the wrong colour.
prop_bag is the green canvas satchel drawn for Bahay Scene 4; the bag Kylo
wears is navy. Taking up a green bag and standing there in a navy one is the
kind of cut a five-year-old catches and an adult reads straight past, so the
prop is HIS bag, cut out of standing_uniform_with_bag_back - him from behind,
where it is drawn square on with nothing in front of it - at the size that
drawing has it, which is the size he wears it.

Writes  nothing. Tools/make_desk_bag.py writes the desk bag out of
        loose_bag, bag_layers and placed_bag below, which is what this file
        is kept for.

It prints the fist on every frame in Paaralan Scene 5's world units.
BuildPaaralanScene5 needs those to put the bag where the hand is, and the
hand is drawn here, so they are measured here: see KuninFist in that file.

Run from the repository root:  python Tools/make_kunin_frames.py
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

WITH_BAG = "talking_uniform_with_bag"   # the bag, and him under it
NO_BAG = "talking_uniform"              # the same pose with the bag stripped
BAG_BACK = "standing_uniform_with_bag_back"   # the same bag, seen square on

# --- the canvas ---
# tayo's, with room at each side for the arm to leave his body. Height and
# the figure's centre column are unchanged, so these frames and the tayo_*
# ones are the same boy in the same place.
PAD_X = 30
WIDTH = tayo.WIDTH + 2 * PAD_X          # 723
HEIGHT = tayo.HEIGHT                    # 1521
CROWN_ROW = tayo.SEAT_CROWN_ROW - tayo.RISE   # on his feet, where Tayo left him

# --- the bend ---
# WHERE THE ARM LEAVES HIS SIDE, as rows below his crown. The bend starts
# inside the seated drawing's own arm - 24 rows above where that drawing
# stops - so the corner it opens is under pixels that are drawn over it, and
# the angle is eased in over ELBOW_EASE rows rather than switched on, which
# is what stops the join reading as a hinge.
ELBOW_REL = tayo.SEAT_CUT_REL - 24      # 754
ELBOW_EASE = 50
# The forearm's own length, measured along itself. tayo puts the wrist 43
# rows below where the drawing stops; from the new start that is 67, and it
# is an ARC length, so the arm does not grow as it swings.
FOREARM_LEN = tayo.WRIST_REL - ELBOW_REL

REACH_1 = 26.0        # degrees from hanging - on the way out
REACH_2 = 52.0        # at the bag
CARRY_1 = 36.0        # swinging back with it
CARRY_2 = 20.0        # still coming in, the bag level with his hip
SETTLE = 8.0          # and it is on - his arm comes the last of the way down

# --- how the scene reads these back ---
# BuildPaaralanScene5 has to put the bag prop WHERE HIS HAND IS on each of
# these frames, and the hand is drawn here, so the grip point is measured
# here too and printed when this runs. The numbers below are Scene 5's own
# staging, repeated so the print can be read straight into it: the figure is
# scaled by HEIGHT from the 921-row seated canvas, and a sprite draws centred
# on its object.
SIT_X = 2.86
SIT_FEET_Y = -3.009
SEATED_HEIGHT = 5.609
UNITS_PER_PX = SEATED_HEIGHT / 921.0
# Along the hand from the wrist, as a fraction of its length: where a fist
# closes on a strap rather than where the fingertips end.
GRIP_ALONG = 0.45

# --- the strap his own forearm hides in the source ---
# All in talking_uniform_with_bag's own pixels. STRAP_ROW is a clean
# cross-section of the right strap from above his arm; the anchors are its
# left edge where the drawing shows it, which is everywhere, because his arm
# crosses the strap's right side and never its left.
STRAP_ROW = 268
STRAP_X = (203, 230)                    # that row, outline to outline
STRAP_PATH = ((271, 203), (316, 210), (344, 217), (368, 229))
STRAP_WIDTH = (27.0, 31.0)              # at the top of the gap, and at the end
STRAP_FILL = (272, 369)                 # the rows that are drawn
OUTLINE = 1                             # the drawn line each side of it, in
                                        # the source's own pixels


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")


def disk(r):
    y, x = np.ogrid[-r:r + 1, -r:r + 1]
    return x * x + y * y <= r * r


# ======================================================================
# The bag
# ======================================================================

def bag_mask(aw, ap):
    """Every pixel the backpack put into the drawing.

    The two sprites are one pose with and without it, so the difference is
    the bag - but it is also every pixel strip_backpack.py repainted when it
    took the bag out, which is a thread of re-drawn outline all down his
    arms and legs. Requiring the pixel to be NAVY as well leaves the bag,
    and keeping only the large pieces leaves the last specks behind.
    """
    changed = np.abs(aw - ap).max(2) > 24
    solid = aw[:, :, 3] > 60
    r, g, b = aw[:, :, 0], aw[:, :, 1], aw[:, :, 2]
    navy = solid & (b - r > 18) & ((r + g + b) / 3.0 < 150)

    m = ndimage.binary_closing(changed & navy, np.ones((5, 5)))
    lab, n = ndimage.label(m)
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    core = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s > 400])

    # AND THE LINE ROUND IT. The strap is drawn with the same near-black
    # outline as everything else in this art, and the outline is not navy,
    # so the colour test above drops it - leaving a band of flat blue with a
    # ragged edge where a drawn object should have a drawn edge. Dark pixels
    # that CHANGED and lie within a few of the bag are that line.
    dark = solid & ((r + g + b) / 3.0 < 110)
    core = core | (changed & dark & ndimage.binary_dilation(core, disk(5)))

    # AND THE SOFT EDGE OUTSIDE THAT LINE. The outermost ring of a drawn
    # edge is a BLEND of the line and whatever is behind it, so it is
    # neither navy nor dark and both tests drop it, which leaves the strap
    # cut out with a staircase. Every one of those pixels changed when the
    # bag was taken out, and what they are blended with - his white shirt,
    # or nothing at all - is what they are being put back onto.
    return core | (changed & solid & ndimage.binary_dilation(core, disk(2)))


def strap_infill(aw):
    """The stretch of right strap his own forearm is lying across.

    One row of the strap - 24 pixels of navy with the light down its left
    edge and the drawn line down both sides - laid along the path the strap
    takes, which is read off the drawing above and below the arm. The
    drawing's own pixels go over the top afterwards, so this only ever shows
    where there was nothing to show.
    """
    row = Image.fromarray(aw[STRAP_ROW:STRAP_ROW + 1,
                             STRAP_X[0] + OUTLINE:STRAP_X[1] - OUTLINE]
                          .astype(np.uint8), "RGBA")
    line_rgb = Image.new("RGBA", (1, 1), tuple(tayo.OUTLINE_RGB) + (255,))
    ys = [p[0] for p in STRAP_PATH]
    xs = [p[1] for p in STRAP_PATH]

    # THE LINE DOWN EACH SIDE IS REDRAWN, NOT STRETCHED. One row of the
    # drawing has one pixel of outline on each side of it; stretched to the
    # width this band wants, that pixel comes out as a grey smear, and a
    # strap with a grey edge beside a strap with a black one reads as two
    # objects. So the band is the strap's INSIDE, and the line is put back
    # at the width and colour every other limb in this project has.
    sub = tayo.SUBPIXEL
    wide = Image.new("RGBA", (aw.shape[1] * sub, aw.shape[0]), (0, 0, 0, 0))
    y0, y1 = STRAP_FILL
    for y in range(y0, y1):
        left = int(round(np.interp(y, ys, xs) * sub))
        t = (y - y0) / float(y1 - 1 - y0)
        w = int(round((STRAP_WIDTH[0] +
                       (STRAP_WIDTH[1] - STRAP_WIDTH[0]) * t) * sub))
        edge = OUTLINE * sub
        wide.alpha_composite(line_rgb.resize((w, 1)), (left, y))
        wide.alpha_composite(row.resize((w - 2 * edge, 1), Image.LANCZOS),
                             (left + edge, y))
    return wide.resize((aw.shape[1], aw.shape[0]), Image.LANCZOS)


def bag_layers():
    """The worn backpack, as two layers: behind him, and over him.

    Returns them in talking_uniform_with_bag's own pixels, to be scaled and
    placed by whatever is wearing it.
    """
    with_bag = load(WITH_BAG)
    aw = np.asarray(with_bag).astype(int)
    ap = np.asarray(load(NO_BAG)).astype(int)

    mask = bag_mask(aw, ap)
    whole = np.zeros(aw.shape, dtype=np.uint8)
    whole[mask] = aw[mask]
    layer = Image.fromarray(whole, "RGBA")

    filled = strap_infill(aw)
    filled.alpha_composite(layer)       # the drawing wins wherever it has pixels

    # HIS OWN OUTLINE IS WHERE THE BAG STOPS BEING BEHIND HIM. Inside the
    # plain silhouette the bag is a strap lying on his shirt; outside it, it
    # is the bag itself, which is behind his shoulder and behind the arm
    # hanging in front of it.
    body = ndimage.binary_dilation(ap[:, :, 3] > 60, disk(2))
    a = np.asarray(filled).astype(np.uint8).copy()
    front = a.copy()
    behind = a.copy()
    front[~body, 3] = 0
    behind[body, 3] = 0
    return (Image.fromarray(front, "RGBA"), Image.fromarray(behind, "RGBA"))


def loose_bag():
    """His bag on its own, cut out of the drawing of his back.

    WHY NOT prop_bag. There is a backpack prop in this project already and
    it is a GREEN canvas satchel with brass buckles, drawn for Bahay Scene 4
    and used wherever a bag has to sit on the floor. Kylo's own bag - the one
    on his back in Paaralan Scene 2 and the one this beat puts him in - is
    NAVY. Picking up a green bag and standing up wearing a navy one is the
    sort of thing a five-year-old notices and an adult reads straight past.

    standing_uniform_with_bag_back is him from behind, so the bag is drawn
    square on, whole, and nothing is in front of it: its straps, its handle,
    its zip and its pocket are all there. Finding it is one connected navy
    region - his shorts are navy too, but they are a region of their own,
    below it and not touching.

    Nothing is recoloured, redrawn or scaled here. It is his bag, lifted out
    of a drawing of his bag.
    """
    im = load(BAG_BACK)
    a = np.asarray(im).astype(int)
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    solid = al > 60
    navy = solid & (b - r > 15) & ((r + g + b) / 3.0 < 160)

    lab, n = ndimage.label(ndimage.binary_closing(navy, np.ones((3, 3))))
    sizes = ndimage.sum(navy, lab, range(1, n + 1))
    bag = lab == (int(np.argmax(sizes)) + 1)
    # Out to the drawn line round it and the soft edge outside that, which
    # are neither navy nor part of the blob - but NOT onto his arms, which
    # are behind the straps on both sides and would come away with them as a
    # fringe of skin down the edge of a bag that is meant to be on a desk.
    skin = solid & (r > 150) & (r - b > 45)
    bag = ndimage.binary_dilation(bag, disk(3)) & solid & ~skin

    out = np.zeros(a.shape, dtype=np.uint8)
    out[bag] = a[bag]
    ys, xs = np.where(bag)
    return Image.fromarray(out, "RGBA").crop((xs.min(), ys.min(),
                                              xs.max() + 1, ys.max() + 1))


def placed_bag(cap):
    """Both bag layers scaled to a head of `cap` and put on the big canvas."""
    src = load(WITH_BAG)
    c, b, cx = hair_cap(src)
    s = cap / float(b - c)
    w, h = (int(round(src.size[0] * s)), int(round(src.size[1] * s)))
    x = WIDTH // 2 - int(round(cx * s)) + tayo.LOWER_DX
    y = CROWN_ROW - int(round(c * s))

    out = []
    for layer in bag_layers():
        o = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
        o.alpha_composite(layer.resize((w, h), Image.LANCZOS), (x, y))
        out.append(o)
    return out


# ======================================================================
# The boy
# ======================================================================

def limb(slab, x_centre, w0, hand_w, theta):
    """One forearm, laid from the elbow along a path that bends by `theta`.

    Drawn into a canvas SUBPIXEL times too wide and shrunk back, because the
    taper's edge is a staircase otherwise - make_tayo_frames' trick, kept.

    The strip at each row is widened by 1/cos of the angle there: a limb
    crossed by a horizontal line is wider than the limb is, and drawing it
    at its own width would make it thin as it swings.

    IT IS DRAWN PAST THE WRIST AND THEN CUT OFF SQUARE TO ITSELF. A limb
    made of horizontal strips ends on a horizontal line, and the hand that
    goes on the end of it has been turned, so its top edge is not
    horizontal: between the two is a wedge of nothing on the outside of the
    bend, and it was the first thing this file got wrong. So the strips run
    on past the wrist and everything beyond the line ACROSS the arm there is
    erased. What is left meets the hand along the hand's own top edge.
    """
    sub = tayo.SUBPIXEL
    top = CROWN_ROW + ELBOW_REL
    wide = Image.new("RGBA", (WIDTH * sub, HEIGHT), (0, 0, 0, 0))

    x = float(x_centre)
    y = 0
    travelled = 0.0
    wrist = None

    over = int(round(hand_w * math.tan(math.radians(abs(theta))))) + 2
    while True:
        a = math.radians(theta * min(1.0, y / float(ELBOW_EASE)))
        t = min(1.0, travelled / float(FOREARM_LEN))
        w = (w0 + (hand_w - w0) * t) / math.cos(a)
        line = slab.resize((max(1, int(round(w * sub))), 1), Image.LANCZOS)
        wide.alpha_composite(line, (int(round(x * sub - line.size[0] / 2.0)),
                                    top + y))
        if wrist is None and travelled >= FOREARM_LEN:
            wrist = (x, top + y, math.degrees(a))
        elif wrist is not None and top + y - wrist[1] >= over:
            break
        x += math.tan(a)
        travelled += 1.0 / math.cos(a)
        y += 1

    wx, wy, wa = wrist
    px, py = math.sin(math.radians(wa)), math.cos(math.radians(wa))
    gy, gx = np.mgrid[0:HEIGHT, 0:WIDTH * sub]
    beyond = ((gx / float(sub) - wx) * px + (gy - wy) * py) > 0
    a = np.asarray(wide).copy()
    a[beyond, 3] = 0
    return Image.fromarray(a, "RGBA"), wrist


def put_hand(out, hand, hand_cx, x, y, angle):
    """The drawn hand, turned rigidly to point along the arm it ends.

    RIGIDLY is the whole of it, and Tools/make_bati_front_frames.py has the
    long version: a hand is all outline and no flat area, so any cut through
    one shows. Turned whole, every pixel of it is the pixel that was drawn.
    """
    pad = Image.new("RGBA", (hand.size[0] * 3, hand.size[1] * 3), (0, 0, 0, 0))
    cx, cy = pad.size[0] // 2, pad.size[1] // 2
    pad.alpha_composite(hand, (cx - int(round(hand_cx)), cy))
    pad = pad.rotate(angle, Image.BICUBIC, center=(cx, cy))
    out.alpha_composite(pad, (int(round(x)) - cx, int(round(y)) - cy))


def forearms(cap, theta_right):
    """Both forearms, the right one swung out by `theta_right` degrees."""
    seat = load(tayo.BASE)
    out = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    hands = []

    for (x0, x1), flip, theta in ((tayo.LEFT_ARM, False, 0.0),
                                  (tayo.RIGHT_ARM, True, theta_right)):
        hand, hand_cx, hand_w = tayo.hanging_hand(cap, flip)
        slab = seat.crop((x0, tayo.ARM_SRC_ROW - 1, x1, tayo.ARM_SRC_ROW))
        centre = PAD_X + (x0 + x1) / 2.0
        wide, wrist = limb(slab, centre, x1 - x0, hand_w, theta)
        out.alpha_composite(wide.resize((WIDTH, HEIGHT), Image.LANCZOS))
        hands.append((hand, hand_cx, wrist))

    for hand, hand_cx, (x, y, a) in hands:
        put_hand(out, hand, hand_cx, x, y, a)
    return out


def grip(cap, theta):
    """Where his fist closes, in Scene 5's world units, at this angle.

    The arm is drawn by this file, so the only honest place to measure the
    hand is here. The point is taken part of the way DOWN the hand from the
    wrist - a strap is held in a fist, not pinched between fingertips - and
    is turned with the hand, so it follows the arm round.
    """
    hand, hand_cx, hand_w = tayo.hanging_hand(cap, True)
    drawn = np.where(np.asarray(hand)[:, :, 3].max(1) > 60)[0]
    _, (x, y, a) = limb(seat_slab(), PAD_X + sum(tayo.RIGHT_ARM) / 2.0,
                        tayo.RIGHT_ARM[1] - tayo.RIGHT_ARM[0], hand_w, theta)
    along = GRIP_ALONG * float(drawn.max())
    x += along * math.sin(math.radians(a))
    y += along * math.cos(math.radians(a))
    return (SIT_X + (x - WIDTH / 2.0) * UNITS_PER_PX,
            SIT_FEET_Y + SEATED_HEIGHT * 0.5 - (y - HEIGHT / 2.0) * UNITS_PER_PX)


def seat_slab():
    seat = load(tayo.BASE)
    return seat.crop((tayo.RIGHT_ARM[0], tayo.ARM_SRC_ROW - 1,
                      tayo.RIGHT_ARM[1], tayo.ARM_SRC_ROW))


def figure(theta_right, wear):
    """The whole boy, standing, one arm out by `theta_right`, bag or not."""
    seat = load(tayo.BASE)
    s_crown, s_bottom, s_cx = hair_cap(seat)
    cap = s_bottom - s_crown
    front, behind = placed_bag(cap) if wear else (None, None)

    arms = forearms(cap, theta_right)

    out = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    if wear:
        out.alpha_composite(behind)
    out.alpha_composite(tayo.lower_body(CROWN_ROW, cap, tayo.shirt_white(seat)),
                        (PAD_X, 0))
    out.alpha_composite(arms)
    out.alpha_composite(seat, (WIDTH // 2 - s_cx, CROWN_ROW - s_crown))
    if wear:
        # THE STRAPS GO OVER HIS SHIRT AND UNDER HIS HANDS. Over the shirt
        # because that is where a strap is, and the seated drawing has just
        # been laid over everything; under his hands because the strap ends
        # at his hip and his arm hangs in front of it. So the forearms are
        # composited a second time, cropped to the rows BELOW the seated
        # drawing - above that row they are already covered by its own arms
        # and compositing them again would only thicken his elbow.
        out.alpha_composite(front)
        cut = CROWN_ROW + tayo.SEAT_CUT_REL
        out.alpha_composite(arms.crop((0, cut, WIDTH, HEIGHT)), (0, cut))
    return out


def write(im, name):
    assert im.size == (WIDTH, HEIGHT), (name, im.size)
    im.save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, tayo.BASE)


FRAMES = (("kunin_reach_1_uniform", REACH_1, False),
          ("kunin_reach_2_uniform", REACH_2, False),
          ("kunin_carry_1_uniform", CARRY_1, False),
          ("kunin_carry_2_uniform", CARRY_2, False),
          ("kunin_settle_uniform", SETTLE, True),
          ("kunin_stand_uniform", 0.0, True))


def main():
    # IT DOES NOT WRITE ANY MORE. See the note at the top of this file: the
    # frames below are the version whose reach is a wrist bend, and
    # Tools/make_uwian_canva_frames.py has replaced every one of them. Writing them
    # from here would quietly undo that, and the symptom - a bag that slides
    # off a desk instead of being lifted off it - is one that reads as a
    # timing problem rather than as a file having been re-run.
    print("make_kunin_frames: superseded - run Tools/make_uwian_canva_frames.py "
          "instead, which writes the Kunin, Paalam and Alis frames. This file "
          "is kept for the bag it works out (loose_bag, bag_layers, "
          "placed_bag), which Tools/make_desk_bag.py imports.")
    return


def _old_main():
    seat = load(tayo.BASE)
    s_crown, s_bottom, _ = hair_cap(seat)
    cap = s_bottom - s_crown

    print("Kunin - he takes his bag off the desk and puts it on.")
    print("  canvas %dx%d, crown on row %d, elbow %d rows below it"
          % (WIDTH, HEIGHT, CROWN_ROW, ELBOW_REL))

    bag = loose_bag()
    bag.save(os.path.join(SPRITES, "prop_bag_uniform.png"))
    meta_for("prop_bag_uniform", BAG_BACK)
    # His own height in that drawing is 745 rows for the 5.609 world units
    # this desk draws him at, so the bag comes out at the size he wears it.
    bag_h = bag.size[1] * SEATED_HEIGHT / 745.0
    print("  %-26s %dx%d  -> %.2f x %.2f world units"
          % ("prop_bag_uniform.png", bag.size[0], bag.size[1],
             bag_h * bag.size[0] / bag.size[1], bag_h))

    for name, theta, wear in FRAMES:
        write(figure(theta, wear), name)
        gx, gy = grip(cap, theta)
        print("  %-26s %4.0f deg   fist at (%5.2f, %5.2f)%s"
              % (name + ".png", theta, gx, gy,
                 "   wearing it" if wear else ""))

    print("\nWire them in BuildPaaralanScene5 as the 'Kunin' PoseSequence:")
    print("  frames      tayo_stand, kunin_reach_1, kunin_reach_2,")
    print("              kunin_carry_1, kunin_carry_2, kunin_settle")
    print("  destination kunin_stand_uniform")
    print("  the carry   one waypoint per frame from kunin_reach_2 on,")
    print("              at the fists printed above")


if __name__ == "__main__":
    main()
