"""Draw the poses Paaralan Scene 4 ("Recess") needs, by changing what is in
Kylo's hands rather than by drawing Kylo again.

WHAT THE SCENE IS. He is at the Scene 3 desk when the bell goes. He packs the
lesson away (Ligpit), takes his baon out of his bag (Kuha), eats it (Kain),
drinks (Inom) and throws the wrapper in the bin (Tapon).

HOW MUCH OF IT IS ALREADY DRAWN, which is the reason this file is short.
Paaralan 2 and 3 between them have a boy at this desk taking a flat object
out of a bag and putting it down - kuha_frame_1..3, reach_bag,
place_frame_1..3 - and a boy holding a pencil at three heights,
lapis_frame_1..3. Packing away is that first movement RUN BACKWARDS, and the
builder does that by listing the frames in the other order; nothing here is
needed for it at all. Eating and drinking are the second movement with
something else in the fist.

So this file makes two kinds of frame:

  AN OBJECT SWAPPED FOR AN OBJECT. The lunchbox over the notepad's own
  footprint, his fingers put back on top - exactly Tools/make_aklat_frames.py,
  whose docstring is the long explanation and whose classify/grow are
  imported here rather than copied.

  AN OBJECT PUT IN AN EMPTY GRIP. The pencil erased out of lapis_frame_1..3
  and a sandwich, a bottle or a crumpled wrapper drawn in its place. The
  erase is what make_aklat_frames never needed and is described below.

THE ONE MEASUREMENT EVERYTHING HERE TURNS ON. His drawn fist is about 175
pixels to the SIDE of his drawn mouth, in every pose the project has. Nothing
can move it there: Docs/AnimationStandard is explicit that a hand cannot go
somewhere the existing hand does not reach, and the forearm rotation that
file describes moves the hand sideways and DOWN, not up and across. So the
hand is left where it is and THE OBJECT SPANS THE GAP - the sandwich and the
bottle are each drawn long, lying along their length, and rotated so that the
end of them lands on his mouth while the other end is in his fist.

That is not a dodge. It is how a hand holds a sandwich or a bottle: the fist
is at one end and the mouth is at the other. Tools/make_recess_props.py draws
both of them horizontally for this reason and says so.

ERASING THE PENCIL, and why it is safe here. lapis_frame_1..3 all have a
pencil drawn in the fist, and a sandwich pasted over it would leave yellow
sticking out past both ends. The barrel, the wooden cone, the graphite, the
ferrule and the eraser are five flat colours that nothing else on him shares
- his skin is orange at (240,167,102) and the barrel is (232,178,58), which
differ by sixty in blue and by fifty-five in green-minus-blue - so they come
off by colour, inside a per-frame box that keeps the test away from his face.

What is left behind is a hole, and the hole is filled in two passes:

  1. the object is drawn over the grip, which covers everything the fist was
     closed round;
  2. anything still transparent that was opaque before is painted from the
     nearest surviving pixel on its own row.

Pass 2 is exact rather than approximate, because every pixel the pencil
crossed had flat shirt white, flat skin or nothing on both sides of it. There
is no texture here to reconstruct.

HIS HEAD DOES NOT MOVE IN THE RAISED-HAND FRAMES, and that is a limit found
here rather than a choice. Tools/look_down.py animates a head by cutting the
drawing at the neck, moving the piece, and laying it back over the body - the
cut is invisible because the neck is the narrowest part of him.

It is invisible because nothing else crosses it. In these frames something
does: he is holding a bottle, or a sandwich, up at his mouth, and the object
runs diagonally ACROSS the neck line. Moving the head takes the top half of
the bottle with it and leaves the bottom half where it was, so the bottle
arrives at his lips with a step in the middle of it. At seven pixels it is a
visible seam; at fourteen the bottle is plainly in two pieces.

Doing the head move FIRST and compositing the object onto the moved drawing
fixes the object and breaks the hand instead, because in lapis_frame_3 his
fist is up beside his jaw - above the neck - and would travel with the head
while his forearm stayed put.

So the head is left alone in every frame where a hand is raised, and the
gesture is carried by the OBJECT: the bottle tips further between
inom_drink_1 and _2, which is what a drink looks like anyway, and the
sandwich rises and falls between the bite and the chew. Only the frames with
both hands down on the desk - the hold frames, built on lapis_frame_1 - move
his head, and there the object is three hundred pixels below the cut.

Writes, into Assets/Sprites/:

  kuha_baon_bag_uniform      the lunchbox gripped in the bag
  kuha_baon_2_uniform        carried square in front of him
  kain_hold_uniform          the sandwich picked up off the desk
  kain_lift_uniform          raised towards his face
  kain_bite_1/2_uniform      at his mouth, and chewing
  kain_last_uniform          the last bite, on the way down
  inom_hold_uniform          the bottle picked up off the desk
  inom_lift_uniform          raised
  inom_drink_1/2_uniform     tipped to his mouth, head back
  tapon_hold_uniform         the wrapper picked up
  tapon_lift_uniform         raised
  tapon_cock_uniform         back by his head, about to go

Run from the repository root (after Tools/make_recess_props.py):
    python Tools/make_recess_frames.py
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_down import head_moved, load, save                        # noqa: E402

SPRITES = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "Assets", "Sprites")

# How far a dark pixel may be from skin and still count as a finger's own
# outline. Same six pixels as make_aklat_frames, and for the same reason: the
# art is outlined at about four, so six reaches the far side of the line and
# no further.
OUTLINE_REACH = 6


# ------------------------------------------------------- masks and helpers

def classify(a):
    """Skin, pale nail, dark outline and navy, as boolean masks.

    THE SKIN TEST IS TIGHTER THAN make_aklat_frames'. That file's skin test
    accepts anything warm, which is right when the only warm things in the
    frame are his arms - and wrong here, because the pencil barrel at
    (232,178,58) passes it on every clause. The first run of this file put
    the pencil back on top of the sandwich as though it were a finger, in all
    twelve frames.

    What separates them is GREEN MINUS BLUE: his skin runs 64..76 across its
    four shades and the barrel runs 102..120, because yellow paint has almost
    no blue in it and skin has plenty. Bounding that, rather than asking only
    that it be positive, splits the two cleanly with twenty either side.
    """
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    on = al > 60
    rg, gb = r - g, g - b
    skin = on & (r > 205) & (rg >= 46) & (rg <= 95) & (gb >= 50) & (gb <= 92)
    nail = on & (r > 225) & (g > 190) & (b > 165) & (r - b > 22) & (r - b < 70)
    dark = on & (r < 95) & (g < 95) & (b < 95)
    navy = on & (b > 70) & (b - r > 18) & (r < 130)
    return skin, nail, dark, navy


def pencil_mask(a, box):
    """The drawn pencil, inside `box` only.

    SEEDED ON ONE COLOUR AND FLOODED ALONG THE OBJECT. The barrel is the only
    part of a pencil that no part of Kylo shares - the wooden cone is within
    four points of his skin on every channel, the graphite is the same black
    his hair and every outline in the project is drawn with, and the ferrule
    is the same grey as his glasses. Testing for those by colour ate the
    outlines round his own fingers, which is what the first run did; growing
    a fixed ten pixels off the barrel instead left the graphite point and the
    eraser behind as two black specks hanging in the air, which is what the
    second did.

    So the barrel is found by colour and then the mask SPREADS from it, one
    pixel at a time, through anything that is not skin, not fingernail, not
    shirt and not the navy of his shorts. The pencil is one connected object,
    so the spread walks the whole of it - cone, point, ferrule, eraser and
    the dark line round all of them - and stops at the first pixel of him it
    meets. The box keeps the whole operation away from his face and hair,
    which the spread would otherwise be free to wander into if the pencil
    ever touched them.
    """
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    on = al > 40
    rg, gb = r - g, g - b

    barrel = on & (gb > 95) & (rg > 35) & (b < 150) & (r > 150)

    keep = np.zeros_like(barrel)
    x0, y0, x1, y1 = box
    keep[y0:y1, x0:x1] = True

    skin, nail, _, navy = classify(a)
    shirt = on & (r > 228) & (g > 228) & (b > 222)
    allowed = keep & on & ~skin & ~nail & ~shirt & ~navy

    mask = barrel & allowed
    # Sixty rounds reaches three hundred pixels along the object, which is
    # more than twice the longest pencil in any of these frames. It settles
    # long before that and the loop stops when it does.
    for _ in range(60):
        grown = grow(mask, 1) & allowed
        if grown.sum() == mask.sum():
            break
        mask = grown

    # THE WOODEN CONE STOPS THE SPREAD, so the two ends are taken by
    # geometry instead.
    #
    # The sharpened cone of a pencil is drawn at (248,195,137) and his skin
    # at (250,193,123): two points apart in green and fourteen in blue, which
    # no threshold is going to separate without taking a bite out of his
    # fingers. So the spread walks up the barrel, meets the cone, decides it
    # is an arm and stops - leaving the graphite point above it as a black
    # speck floating beside his ear, which is what it looked like.
    #
    # A pencil is straight, so where the rest of it is is not a guess. The
    # barrel's own pixels give the axis, and the mask is extended seventy
    # pixels past each end of it along that line, at the barrel's own width
    # plus a little. Seventy covers the longest cone-and-point in these
    # frames twice over.
    #
    # Over-reaching is harmless by construction and that is why the number
    # can be generous: his hands are put back on top afterwards, and whatever
    # is erased past the ends of the pencil and not covered by the new object
    # is painted back from its own row. Both of those are below.
    pts = np.argwhere(mask)
    if len(pts) > 20:
        c = pts.mean(axis=0)
        d = np.linalg.svd(pts - c)[2][0]              # the pencil's own axis
        along = (pts - c) @ d
        half_w = np.abs((pts - c) @ np.array([-d[1], d[0]])).max() + 8

        yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
        rel = np.stack([yy - c[0], xx - c[1]], axis=-1)
        s = rel @ d
        t = rel @ np.array([-d[1], d[0]])
        capsule = (np.abs(t) <= half_w) & (s >= along.min() - 70) & (s <= along.max() + 70)
        mask = mask | (capsule & keep & on)
    return mask


def grow(mask, radius):
    """Binary dilation by a square of the given radius, in numpy."""
    out = mask.copy()
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            out |= np.roll(np.roll(mask, dy, axis=0), dx, axis=1)
    return out


def enclosed(holes, known):
    """The holes with something of him on all four sides of them.

    Most of the erased pencil is the part sticking up out of his fist, and
    there is nothing behind that but the classroom - those holes are supposed
    to STAY holes. The ones that are not are the slivers BETWEEN his fingers,
    where the barrel showed through a closed fist, and those have knuckle on
    every side of them.

    Four half-scans rather than a flood, because that is all the distinction
    needs and it costs nothing.
    """
    left = np.cumsum(known, axis=1) > 0
    right = np.cumsum(known[:, ::-1], axis=1)[:, ::-1] > 0
    up = np.cumsum(known, axis=0) > 0
    down = np.cumsum(known[::-1], axis=0)[::-1] > 0
    return holes & left & right & up & down


def fill_holes(arr, holes):
    """Grow the surviving colour into the holes that are inside him.

    A SPREAD, NOT A NEAREST-NEIGHBOUR COPY. The first version of this took
    each hole's nearest surviving pixel along its own row, which for a sliver
    between two fingers is the black line drawn round one of them - so every
    gap the barrel had shown through came out as a black streak running
    across the fist, and the hand looked clawed.

    Averaging the known neighbours and letting that creep inwards a pixel at
    a time gives the gaps the colour of what surrounds them, which is the
    skin of a closed hand, and it cannot pick up an outline unless the hole
    is genuinely against one.
    """
    out = arr.astype(float)
    known = (~holes) & (arr[:, :, 3] > 40)
    todo = enclosed(holes, known)
    if not todo.any():
        return arr

    for _ in range(40):
        rest = todo & ~known
        if not rest.any():
            break
        acc = np.zeros_like(out)
        cnt = np.zeros(arr.shape[:2])
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy == 0 and dx == 0:
                    continue
                k = np.roll(np.roll(known, dy, 0), dx, 1)
                v = np.roll(np.roll(out, dy, 0), dx, 1)
                acc += v * k[:, :, None]
                cnt += k
        newly = rest & (cnt > 0)
        if not newly.any():
            break
        out[newly] = acc[newly] / cnt[newly][:, None]
        known = known | newly

    return np.clip(out, 0, 255).astype(np.uint8)


def place(obj, size, cx, cy, w, h, degrees):
    """`obj` scaled, turned and centred at (cx, cy) on a transparent canvas."""
    piece = obj.resize((max(1, int(w)), max(1, int(h))), Image.LANCZOS)
    if degrees:
        # expand=True so the corners are not clipped by the old box; the
        # result is pasted by its own centre, which a rotation does not move.
        piece = piece.rotate(degrees, resample=Image.BICUBIC, expand=True)
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    layer.alpha_composite(piece, (int(cx - piece.size[0] / 2),
                                  int(cy - piece.size[1] / 2)))
    return layer


def spread(seed, allowed, rounds):
    """Everything `allowed` that can be reached from `seed` without leaving it."""
    m = seed & allowed
    for _ in range(rounds):
        grown = grow(m, 1) & allowed
        if grown.sum() == m.sum():
            break
        m = grown
    return m


def grip_of(src_arr, pencil, box):
    """Only the hand that is holding something, and its own arm.

    THE WHOLE FIGURE CANNOT GO BACK ON TOP HERE, which is the one way this
    file differs from Tools/make_aklat_frames.py. That one restores every
    skin pixel in the frame, and it is right to: a textbook carried at chest
    height touches his hands and nothing else, so painting all of him back
    over it only ever puts his fingers in front of the cover.

    A sandwich on its way to his mouth is drawn ACROSS HIS JAW. Restore every
    skin pixel and his chin, his cheek and his ear are painted back over the
    food, and what comes out is a boy with a translucent sandwich behind his
    face. That is what the first frames out of this file looked like.

    So only the hand that is actually holding the thing is restored - found
    by spreading through skin from the pencil's own footprint, which reaches
    the fingers closed round it and the forearm behind them and stops at the
    sleeve. His face is a separate island of skin with a shirt between, so it
    is not reached, and the food passes in front of it the way food does.
    """
    skin, nail, dark, _ = classify(src_arr)
    # SEEDED OUTSIDE THE PENCIL, which is what keeps the sharpened cone out
    # of the hand one last time. The cone is skin-coloured and sits inside
    # the pencil's own footprint, so seeding on "skin near the pencil" makes
    # it a seed in its own right and it survives as a tan chip floating above
    # his fist. Seeded on skin near the pencil but NOT IN it, the only way in
    # is through a skin pixel, and the cone has a black outline all round it.
    #
    # AND BOXED, which is the other half of it. "Skin you can reach from the
    # pencil" is only his hand if his hand touches nothing else made of
    # skin - and in lapis_frame_2 and _3 he is holding the pencil up BESIDE
    # HIS FACE, with his knuckles against his jaw and the point beside his
    # ear. The spread crossed at the ear, took the whole head, and the head
    # was then painted back ON TOP of whatever he was supposed to be
    # holding: the bottle lost its neck and cap to his cheek and what was
    # left read as a juice carton.
    #
    # Measured: in lapis_frame_3 the mask settles at 2,476 pixels after
    # twenty rounds of spreading and is still growing at 27,213 after two
    # hundred. A hand does not have twenty-seven thousand pixels in it.
    #
    # The box is per pose and holds the fist and the fingers and nothing
    # above them. The forearm below is left out on purpose: nothing he
    # carries is ever drawn over his forearm, so there is nothing there to
    # put back.
    keep = np.zeros(skin.shape, dtype=bool)
    x0, y0, x1, y1 = box
    keep[y0:y1, x0:x1] = True
    hand = spread(skin & grow(pencil, 4) & ~pencil & keep, skin & keep, 200)
    near = grow(hand, OUTLINE_REACH)
    # `& ~pencil` on the outline clause, and it is not a detail. The pencil's
    # own dark edge runs the length of the barrel and every pixel of it is
    # within six of a finger closed round it, so without this the outline is
    # kept and restored - a black bar standing in the fist where the pencil
    # was. The fingers' own outlines where they cross the barrel go with it;
    # fill_holes puts skin back in their place, which is a softer knuckle
    # than the drawing had and the only thing lost here.
    return hand | (nail & near & ~pencil) | (dark & near & ~pencil)


def hands_of(src_arr, keep_bag=False):
    """Every finger in the frame, for a pose where the object stays low.

    This is make_aklat_frames' own mask and it is used on the three lunchbox
    frames, where the box is carried at chest height and touches nothing but
    his hands. The three words that lift something to his MOUTH use grip_of
    above instead; the note there says why.

    `keep_bag` adds the body of the backpack, for the one frame where the bag
    is drawn in front of what he is holding. Eroded then grown, so only the
    bag ITSELF comes back and not the zip pull and stitching - make_aklat_
    frames found that out the hard way and its comment says what that looked
    like.
    """
    skin, nail, dark, navy = classify(src_arr)
    hand = skin | nail
    hand = hand | (grow(hand, OUTLINE_REACH) & dark)
    if keep_bag:
        hand = hand | (grow(~grow(~navy, 3), 4) & navy)
    return hand


def swap_object(src_name, out_name, obj_name, cx, cy, w, h, degrees,
                keep_bag=False, erase_box=None, grip_box=None,
                head_drop=0, head_tilt=0.0):
    """One frame: his pose, a different thing in his hands.

    The order is the whole of the method and it does not commute.

      1  the pencil comes off, if this pose has one (erase_box);
      2  the new object is laid over the grip;
      3  his fingers go back on top, or he carries it like a tray;
      4  anything the pencil left bare and the object did not cover is
         painted in from its own row;
      5  his head moves last, so the piece that is cut at the neck carries
         everything already composited on it.

    Step 5 being last matters: move the head first and the object is pasted
    at coordinates that no longer mean what they meant.
    """
    src = load(src_name)
    obj = Image.open(os.path.join(SPRITES, obj_name + ".png")).convert("RGBA")

    src_arr = np.asarray(src).astype(int)

    # The pencil first, because the hand mask has to be told about it - see
    # hands_of. Doing these the other way round is the bug that made three
    # passes of this file look like they had changed nothing.
    if erase_box is not None:
        # Two pixels wider than the colour test finds it. The barrel's edge
        # is anti-aliased into the background over about that distance, and
        # the half-transparent fringe left behind is a grey ghost of a pencil
        # standing beside his ear.
        pencil = grow(pencil_mask(src_arr, erase_box), 2) & (src_arr[:, :, 3] > 8)
        hand = grip_of(src_arr, pencil, grip_box)
    else:
        pencil = np.zeros(src_arr.shape[:2], dtype=bool)
        hand = hands_of(src_arr, keep_bag)

    base = np.asarray(src).copy()
    # Whatever of the pencil his own fingers are drawn over stays: a finger
    # closed on the barrel is a finger, and the object going in its place
    # passes behind it anyway.
    holes = pencil & ~hand
    if holes.any():
        base[holes] = (0, 0, 0, 0)
        # Closed BEFORE the object goes on, so what the object does not cover
        # is a hand with nothing in it rather than a hand with a slot in it.
        base = fill_holes(base, holes)

    out = Image.fromarray(base, "RGBA")
    out.alpha_composite(place(obj, src.size, cx, cy, w, h, degrees))

    arr = np.asarray(out).copy()
    arr[hand] = np.asarray(src)[hand]
    out = Image.fromarray(arr, "RGBA")

    if head_drop or head_tilt:
        out = head_moved(out, degrees=head_tilt, drop=head_drop)

    assert out.size == src.size, (out_name, out.size, src.size)
    save(out, out_name, src_name)
    print("  %-26s %-18s %3dx%-3d at (%3d,%3d) %+5.1f deg%s"
          % (out_name + ".png", obj_name, w, h, cx, cy, degrees,
             "  head %+d/%+0.1f" % (head_drop, head_tilt)
             if (head_drop or head_tilt) else ""))


# -------------------------------------------------------------- the frames
#
# Every number below is read off the drawing it applies to, not fitted. The
# three lapis poses put his fist at:
#
#   lapis_frame_1   (265, 872)   down on the desk
#   lapis_frame_2   (205, 688)   up at his chest
#   lapis_frame_3   (152, 606)   up beside his face
#
# and his mouth is at about (330, 505) in all of them, because every seated
# pose shares a head. The angles are whatever points an object from the first
# of those at the second.

# The box the pencil is looked for in, per pose. Kept well clear of his face
# and of the bag: a colour test loose enough to catch a wooden pencil cone is
# loose enough to catch an ear.
#
# WIDE ENOUGH FOR THE POINT, which is further out than the barrel. The box
# clips the capsule as well as the spread, so one drawn tight round the
# barrel's own bounding box cuts the graphite off and leaves it on screen -
# which is exactly what the first set of these did. Each one here reaches
# about fifty pixels past both ends of the pencil and stops short of his
# face, his ear and the bag.
PENCIL_BOX = {
    "lapis_frame_1_uniform": (165, 806, 330, 921),
    "lapis_frame_2_uniform": (128, 495, 244, 775),
    "lapis_frame_3_uniform": (100, 408, 186, 690),
}

# Where the FIST is in each pose, for the mask that puts his fingers back on
# top of what he is holding. See grip_of for why "skin you can reach from the
# pencil" is not enough on its own. Each box holds the hand and stops short
# of his jaw, his ear and his shoulder.
GRIP_BOX = {
    "lapis_frame_1_uniform": (100, 740, 330, 921),
    "lapis_frame_2_uniform": (95, 585, 272, 850),
    "lapis_frame_3_uniform": (80, 545, 238, 762),
}

BAON = [
    # "Kuha": the lunchbox comes out of the bag. Three frames, swapped onto
    # Paaralan 2's own take-it-out movement - the same hands, the same lean,
    # a different thing in them. The footprints are the notepad's, measured in
    # make_aklat_frames: 212..458 x 608..892 in place_frame_2.
    #
    # NO FRAME FOR place_frame_1, AND THAT IS A LIMIT RATHER THAN AN
    # OVERSIGHT. That pose holds the notepad UP BESIDE HIS SHOULDER, clear of
    # the bag, and the notepad there is 190 wide by 296 tall - a tall thin
    # rectangle. A lunchbox is nearly square, so one scaled until it is 296
    # tall is 367 across, which is wider than his head: what came out was a
    # boy holding a green slab with one thumb showing past the edge of it.
    # Anything smaller leaves the white corner of the pad sticking out behind
    # it, which is the rim make_aklat_frames warns about.
    #
    # Aklat gets away with this frame because a TEXTBOOK is a tall thin
    # rectangle too. The baon is not, so "Kuha" in Paaralan 4 goes straight
    # from the bag to his chest, one step shorter than the same movement in
    # Paaralan 3. Erasing the pad instead would work - it is against the
    # background there - but it is white on a white shirt and that is a
    # harder cut than this scene needs.
    #
    #   src                      out                     obj      cx   cy   w    h  deg  bag
    ("reach_bag_uniform",     "kuha_baon_bag_uniform", "prop_baon", 524, 818, 230, 242, -12.0, True),
    ("place_frame_2_uniform", "kuha_baon_2_uniform",   "prop_baon", 335, 748, 320, 338,   0.0, False),
]

# "Kain", "Inom" and "Tapon" all use the same arm at the same three heights,
# because it is the only arm in the project that holds anything at face
# height. What changes is the object and where his head is.
#
#  src                     out                    obj              cx   cy   w    h   deg  head tilt
HELD = [
    # --- Kain: the sandwich goes up to his mouth ---
    # Flat on the desk, just picked up. No lift and no head movement yet:
    # this frame is the join with place_frame_3, where his hands were.
    ("lapis_frame_1_uniform", "kain_hold_uniform",  "prop_sandwich", 272, 866, 206, 149,   5.0,  0,  0.0),
    ("lapis_frame_2_uniform", "kain_lift_uniform",  "prop_sandwich", 250, 660, 222, 160,  24.0,  0,  0.0),
    # AT the mouth, and sized to END there rather than to cross his face.
    # From the fist at (152,606) to the mouth at (330,505) is 204 pixels at
    # thirty degrees up; the drawing inside prop_sandwich is 83% of its box
    # wide and 77% tall, so a 240-wide box puts 200 pixels of bread between
    # the two.
    #
    # LOWER AND SHALLOWER THAN THE FIRST PASS, which had it at 250 x 151
    # centred on (242,556). Rotated thirty degrees, the far corner of a
    # sandwich that deep rises to y 444 - and his EYES are at 440. On screen
    # he was holding it across his glasses. Dropping the centre eighteen
    # pixels and taking twelve off the height puts the top corner at 465,
    # which is below the frames and above his chin, where a sandwich at
    # someone's mouth actually is.
    ("lapis_frame_3_uniform", "kain_bite_1_uniform", "prop_sandwich", 248, 566, 240, 173,  28.0,  0,  0.0),
    # Chewing: the sandwich comes down a little and his head comes back up,
    # so the loop is a bite and a chew rather than a hand shaking at his face.
    ("lapis_frame_3_uniform", "kain_bite_2_uniform", "prop_sandwich", 240, 586, 240, 173,  20.0,  0,  0.0),
    # The last of it, for the frame the eating ends on. Same grip, same
    # angle, half the bread: he has eaten most of it, so the sandwich that
    # goes down with his hand is not the one that came up. Without this the
    # loop ends on a whole sandwich and the next frame has empty hands, and
    # a whole sandwich vanishing is the thing rule 5 is about.
    ("lapis_frame_2_uniform", "kain_last_uniform",  "prop_sandwich", 224, 672, 130, 94,   24.0,  0,  0.0),

    # --- Inom: the bottle, tipped ---
    ("lapis_frame_1_uniform", "inom_hold_uniform",  "prop_bote_held", 276, 856, 250, 98,   6.0,  0,  0.0),
    ("lapis_frame_2_uniform", "inom_lift_uniform",  "prop_bote_held", 256, 640, 270, 106,  30.0,  0,  0.0),
    # The cap end lands on his mouth and his head goes BACK, which is the
    # negative drop - the one place in this project that uses look_down to
    # look up. The bottle is drawn cap-right, so turning it up 34 degrees
    # from a fist at (152,606) walks the cap to about (330,500): his mouth.
    ("lapis_frame_3_uniform", "inom_drink_1_uniform", "prop_bote_held", 244, 558, 290, 114,  34.0,  0,  0.0),
    ("lapis_frame_3_uniform", "inom_drink_2_uniform", "prop_bote_held", 248, 548, 290, 114,  40.0,  0,  0.0),

    # --- Tapon: the wrapper, picked up and cocked ---
    # The wrapper is the one held object that does NOT span the gap to his
    # mouth, because it is not going there. It stays a ball in his fist and
    # is drawn small for the reason make_recess_props gives: at fist size it
    # reads as a snowball.
    ("lapis_frame_1_uniform", "tapon_hold_uniform", "prop_basura", 268, 856, 142, 130,   0.0,  5,  0.0),
    ("lapis_frame_2_uniform", "tapon_lift_uniform", "prop_basura", 200, 650, 148, 136,   0.0,  0,  0.0),
    # Back by his head, about to go. His head tips towards the bin, which the
    # scene stages on his right - that is the -4 degrees, and it is the whole
    # of "he looks at the bin" that a front-facing drawing can honestly give.
    ("lapis_frame_3_uniform", "tapon_cock_uniform", "prop_basura", 146, 596, 155, 142,   0.0,  0,  0.0),
]


def main():
    for p in ("prop_baon", "prop_sandwich", "prop_bote_held", "prop_basura"):
        if not os.path.exists(os.path.join(SPRITES, p + ".png")):
            raise SystemExit("run Tools/make_recess_props.py first - "
                             + p + ".png is not there")

    print("the notepad becomes his baon:")
    for src, out, obj, cx, cy, w, h, deg, bag in BAON:
        swap_object(src, out, obj, cx, cy, w, h, deg, keep_bag=bag)

    print("\nthe pencil becomes his lunch:")
    for src, out, obj, cx, cy, w, h, deg, drop, tilt in HELD:
        swap_object(src, out, obj, cx, cy, w, h, deg,
                    erase_box=PENCIL_BOX[src], grip_box=GRIP_BOX[src],
                    head_drop=drop, head_tilt=tilt)


main()
