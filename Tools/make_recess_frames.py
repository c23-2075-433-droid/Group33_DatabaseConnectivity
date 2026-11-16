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
  and a hotdog, a bottle or a crumpled wrapper drawn in its place. The
  erase is what make_aklat_frames never needed and is described below.

AND, FOR THE TWO WORDS THAT GO TO HIS MOUTH, A THIRD THING: the mouth
itself. Tools/mouth.py paints the drawn smile out and puts an open mouth, a
bite, a chew, pursed lips or a swallow there instead, and that module's
docstring is the account of why it had to be drawn rather than moved. It
matters here only for the ORDER things are composited in, which swap_object
sets out: the mouth is painted before the food, so the food is in front of
it, which is the difference between eating something and holding it near
your face.

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
gesture is carried by the OBJECT and by the FACE: the bottle tips further
between inom_drink_1 and _2, which is what a drink looks like anyway, and
the mouth opens, closes on the bun and works from one cheek to the other
while the hotdog goes up and comes down. Only the frames with both hands
down on the desk - the hold frames, built on lapis_frame_1 - move his head,
and there the object is three hundred pixels below the cut.

The cheek bulge and the chin drop in Tools/mouth.py are warps of the face
and do not run into this at all, because they are small, they are local to
the jaw, and they happen BEFORE the object is composited rather than after.

Writes, into Assets/Sprites/:

  kuha_baon_bag_uniform      the lunchbox gripped in the bag
  kuha_baon_2_uniform        carried square in front of him
  kain_hold_uniform          the hotdog picked up off the desk
  kain_lift_uniform          raised towards his face, lips parting
  kain_open_uniform          mouth wide, the food just below it
  kain_bite_1_uniform        the bun in his mouth, closing on it
  kain_chew_1/2_uniform      chewing, hand down, the mouthful crossing
                             from one cheek to the other
  kain_open_2_uniform        the same open mouth, with the bitten hotdog
  kain_bite_2_uniform        and the next bite out of it
  kain_last_uniform          a stub of it, on the way down
  inom_hold_uniform          the bottle picked up off the desk
  inom_lift_uniform          raised, lips coming apart
  inom_drink_1/2_uniform     tipped to his mouth, lips round the cap
  inom_swallow_uniform       bottle down an inch, throat working
  tapon_hold_uniform         the wrapper picked up
  tapon_lift_uniform         raised
  tapon_cock_uniform         back by his head, about to go

Run from the repository root (after Tools/make_recess_props.py):
    python Tools/make_recess_frames.py
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mouth                                                        # noqa: E402
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


def pencil_mask(a, box, reach=70, pad=9):
    """The drawn pencil, inside `box` only: a capsule fitted to its barrel.

    FOUND BY COLOUR AND THEN BY GEOMETRY, with no flood fill anywhere in
    it, and the second half of that sentence is the whole of this function's
    history.

    The barrel is the one part of a pencil that no part of Kylo shares. The
    wooden cone is within four points of his skin on every channel, the
    graphite is the same black his hair and every outline is drawn with,
    the ferrule is the same grey as his glasses and the eraser is the same
    pink as his lip. So the barrel is found by colour - reliably, it is the
    only saturated yellow in the frame - and the rest of the pencil has to
    be found some other way.

    THE OTHER WAY WAS A FLOOD, AND IT DOES NOT WORK. Three versions of this
    spread outwards from the barrel through "anything that is not skin, not
    fingernail, not shirt and not navy", on the reasoning that the pencil is
    one connected object and the spread would walk the length of it and
    stop at the first pixel of him it met.

    It does not stop, because the drawn lines of his own hand are not skin,
    fingernail, shirt or navy either. The barrel crosses two knuckles. The
    spread steps onto the first crease it touches and then walks every line
    in the fist - the finger separations, the knuckle loop, the outer
    edge - and settles at nineteen thousand pixels, which is the size of
    his hand and not the size of a pencil. Excluding dark pixels as well
    does not fix it: the creases are mid-brown, lighter than the outline
    test and darker than the skin test, and the spread goes straight down
    them.

    What that cost was not subtle. The capsule below is fitted to whatever
    the mask contains, so a mask the shape of his fist gives a capsule the
    size of his fist; the erase then takes his whole hand away, and
    fill_holes paints the hole with the average of what surrounded it -
    which, the surroundings being outlines, is black. Every earlier version
    of these frames had a smeared, edgeless mitten where his fist should
    be, and that is where it came from.

    A PENCIL IS STRAIGHT, so none of that is needed. The barrel's own
    pixels give an axis and a width by least squares, and the whole object
    is the capsule round that line: `reach` pixels past each end of the
    barrel, `pad` wider than it. Seventy covers the longest cone-and-point
    in these frames twice over, and over-reaching is harmless by
    construction - his hand is put back on top afterwards, and whatever is
    erased past the ends of the pencil and not covered by the new object is
    painted back from its own neighbourhood.

    A tighter capsule - a smaller `reach` and `pad` - is the pencil as
    DRAWN rather than the region it is safe to erase, and swap_object asks
    for both. See the note there.
    """
    on, keep, c, d, perp, along, half = barrel_fit(a, box)

    yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    rel = np.stack([yy - c[0], xx - c[1]], axis=-1)
    s = rel @ d
    t = rel @ perp
    capsule = ((np.abs(t) <= half + pad)
               & (s >= along.min() - reach) & (s <= along.max() + reach))
    return capsule & keep & on


def barrel_fit(a, box):
    """The least-squares axis of the yellow barrel inside `box`.

    Split out of pencil_mask because the ACROSS direction is needed twice
    and the fit is the only place that knows it. pencil_mask builds a
    capsule round the axis; inside_him asks whether a hole has him on both
    sides of it, and "both sides" of a slot cut by a pencil means across
    the pencil. See the note there.

    Returns the frame's own opaque and in-box masks along with the fit, so
    that neither caller repeats the colour test.
    """
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    on = al > 40
    rg, gb = r - g, g - b

    keep = np.zeros(on.shape, dtype=bool)
    x0, y0, x1, y1 = box
    keep[y0:y1, x0:x1] = True

    barrel = on & (gb > 95) & (rg > 35) & (b < 150) & (r > 150) & keep
    pts = np.argwhere(barrel)
    if len(pts) < 20:
        raise ValueError("no pencil barrel found in %r" % (box,))

    c = pts.mean(axis=0)
    d = np.linalg.svd(pts - c)[2][0]                 # the pencil's own axis
    perp = np.array([-d[1], d[0]])
    along = (pts - c) @ d
    half = np.abs((pts - c) @ perp).max()
    return on, keep, c, d, perp, along, half


def grow(mask, radius):
    """Binary dilation by a square of the given radius, in numpy.

    DONE IN TWO PASSES, down the rows and then across the columns, which
    gives the identical result to one pass over the whole square and does
    it in a fraction of the work: a square is a line dilated by a line, so
    2(2r+1) shifts rather than (2r+1) squared. At the eighteen-pixel radius
    the hand mask's closing needs, that is seventy-four shifts instead of
    thirteen hundred, and this function is called often enough that the
    difference is the difference between a three-minute run and a ten-
    second one.
    """
    out = mask.copy()
    for dy in range(-radius, radius + 1):
        if dy:
            out |= np.roll(mask, dy, axis=0)
    rows = out.copy()
    for dx in range(-radius, radius + 1):
        if dx:
            out |= np.roll(rows, dx, axis=1)
    return out


def inside_him(holes, arr, perp=None, span=46):
    """The holes that are genuinely INSIDE the drawing, as against open air.

    Most of what the pencil erase takes is the part of the barrel sticking
    up out of his fist, and behind that there is nothing but the classroom:
    those holes are supposed to STAY holes. The ones that are not are the
    part that crossed his shirt and the slivers between his fingers, and
    those have to be painted back or he has a slot cut through him.

    BY DISTANCE FROM REAL TRANSPARENCY was the second answer here, and it
    is the one that put a skin-coloured spike in the air above his fist.
    The rule was "a hole eight pixels in from the nearest genuinely
    transparent pixel is inside him", and the trouble with it is that the
    erase capsule is FIFTY PIXELS WIDE - nine wider than a barrel that is
    already twenty-eight across. So the middle of the channel above his
    fist is twenty-five pixels from the nearest real background, passes a
    test set at eight, and gets painted from its own neighbourhood. What
    is in the neighbourhood is the top of his fist, so what was drawn was
    a tan ribbon standing up out of his knuckles exactly where the pencil
    had been, with the fill's pale fringe beside it. The three frames that
    hold the food at face height all had one.

    Widening the eight does not fix it, because the slot through the FIST
    is the same fifty pixels wide and has to stay fillable: by distance
    alone the two holes are indistinguishable, which is the whole problem
    with measuring a slot's depth instead of looking at what is either
    side of it.

    ACROSS THE PENCIL, THEN, AND BOTH SIDES. A slot cut by a pencil has
    two sides by definition - the ones the barrel was between - so the
    honest question is whether the drawing is still there on both of them.
    Inside his fist it is: skin to the left of the slot and skin to the
    right. Above his fist it is not: his head is somewhere off to the
    right and the classroom is on the left, so the channel is a trench
    open to the sky rather than a slot through anything, and nothing is
    painted into it.

    `span` is how far to look, and it only has to clear the capsule's own
    half-width - twenty-five - with enough margin to find ink past the
    anti-aliased edge of it. Forty-six is that, and it is short enough
    that his head at sixty pixels off the barrel is not mistaken for the
    far side of a slot.

    Called without an axis - which nothing does now - it falls back to the
    old eight-pixel rule, so the lunchbox frames that never erase anything
    are unaffected either way.
    """
    real_bg = (arr[:, :, 3] <= 40) & ~holes
    if perp is None:
        return holes & ~grow(real_bg, 8)

    # Still drawn: anything opaque that the erase did not take. A hole is
    # inside him if this is reachable from it on BOTH sides, walking out
    # along the across-the-pencil direction a pixel at a time.
    solid = (arr[:, :, 3] > 40) & ~holes
    near, far = np.zeros_like(holes), np.zeros_like(holes)
    for k in range(1, span + 1):
        dy, dx = int(round(k * perp[0])), int(round(k * perp[1]))
        near |= np.roll(np.roll(solid, -dy, 0), -dx, 1)
        far |= np.roll(np.roll(solid, dy, 0), dx, 1)
    return holes & near & far


def fill_holes(arr, holes, perp=None):
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

    WHAT IT IS NO LONGER ASKED TO DO is the slot through his fist. A
    spread creeping in from both sides of a fifty-pixel hole meets in the
    middle, and the two sides of a drawn fist are not the same colour -
    the lit edge, the shaded edge and the creases between his fingers all
    crept inwards and set as hard-edged wedges, so the back of his hand
    came out faceted, like paper folded. swap_object paints that slot with
    his own flat skin before calling this, and what is left here is the
    part that crossed his SHIRT, which is one flat white and spreads
    perfectly well.

    `perp` is handed straight to inside_him; see there.
    """
    out = arr.astype(float)
    known = (~holes) & (arr[:, :, 3] > 40)
    todo = inside_him(holes, arr, perp)
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


def close(mask, radius):
    """Binary closing: grow by `radius`, then shrink back by it.

    What it is for here is filling a mask IN. A hand found by its skin has
    a hole wherever the drawing put a line - every knuckle crease, every
    outline, the slot the pencil was in - and a mask with holes in it
    restores a hand with holes in it. Closing seals anything narrower than
    twice the radius while leaving the outside edge where it was.
    """
    return ~grow(~grow(mask, radius), radius)


def pencil_ink(a, capsule):
    """The pencil WHERE IT IS DRAWN, as against where the capsule sweeps.

    pencil_mask answers "what is it safe to erase from the background",
    and the answer is a band, because the cone and the graphite cannot be
    found by colour and have to be covered by geometry. This answers the
    narrower question swap_object asks about his FIST, where a band is
    much too blunt an instrument: his fingers are drawn in FRONT of the
    barrel, so most of the band through his hand is hand.

    Three materials, because that is what is left of a pencil once a fist
    closes round it - and all three are found inside the capsule only, so
    nothing anywhere else in the frame can match by accident:

      the barrel    the same saturated yellow pencil_mask fits its axis
                    to. Visible between his thumb and forefinger.
      the eraser    pink: as red as skin is but with none of skin's green
                    lead over blue, which is what separates the two. It
                    sits below his little finger, inside the fist, and
                    leaving it there put a pink nub in his palm.
      the ferrule   the grey band between them. Bounded well below white
                    so that the shirt behind his knuckles cannot match,
                    and above black so that an outline cannot.

    His own skin and fingernails are cut back out at the end, which costs
    nothing where the test is right and is the safety net where it is not:
    the eraser's pink and the shaded side of a hand are closer together
    than any threshold likes.
    """
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    rg, gb = r - g, g - b
    barrel = (gb > 95) & (rg > 35) & (b < 150) & (r > 150)
    eraser = (r > 175) & (rg > 45) & (gb > -25) & (gb < 42) & (b > 95)
    ferrule = (abs(rg) < 24) & (abs(gb) < 24) & (r > 110) & (r < 225)
    skin, nail = classify(a)[:2]
    return capsule & (al > 40) & (barrel | eraser | ferrule) & ~skin & ~nail


def closed_fist_tone(src_arr, hand, pencil):
    """The one colour his fist is mostly painted in, as an RGBA tuple.

    THE MODE, NOT THE MEAN. The art is flat-shaded: his hand is three or
    four solid tones with hard edges between them, and the commonest of
    them by a long way is the lit skin that covers the back of the fist
    and the fingers. Averaging instead would land between two of those
    tones on a colour that is nowhere in the drawing, and a patch of it
    in the middle of his hand reads as a bruise.

    Taken from the fist ITSELF - skin inside the grip mask, with the
    pencil's own footprint left out so that no part of what is being
    painted over can vote on the colour it is painted with.
    """
    skin = classify(src_arr)[0]
    pick = skin & hand & ~pencil
    if not pick.any():                      # no fist to sample: leave it alone
        return (0, 0, 0, 0)
    rgb = src_arr[:, :, :3][pick].astype(np.int64)
    packed = (rgb[:, 0] << 16) | (rgb[:, 1] << 8) | rgb[:, 2]
    vals, counts = np.unique(packed, return_counts=True)
    top = int(vals[counts.argmax()])
    return (top >> 16, (top >> 8) & 255, top & 255, 255)


def grip_of(src_arr, pencil, box):
    """The whole of the hand that is holding something, as a solid region.

    THE WHOLE FIGURE CANNOT GO BACK ON TOP HERE, which is the one way this
    file differs from Tools/make_aklat_frames.py. That one restores every
    skin pixel in the frame, and it is right to: a textbook carried at chest
    height touches his hands and nothing else, so painting all of him back
    over it only ever puts his fingers in front of the cover.

    A hotdog on its way to his mouth is drawn ACROSS HIS JAW. Restore every
    skin pixel and his chin, his cheek and his ear are painted back over the
    food, and what comes out is a boy with a translucent lunch behind his
    face. That is what the first frames out of this file looked like.

    So only the hand that is actually holding the thing is found - by
    spreading through skin from the pencil's own footprint, which reaches
    the fingers closed round it and the forearm behind them and stops at the
    sleeve. His face is a separate island of skin with a shirt between, so
    it is not reached, and the food passes in front of it the way food does.

    A REGION, NOT A LIST OF PIXELS THAT PASSED A COLOUR TEST, and this is
    the part that was wrong for a long time. The mask used to be built as
    "skin, plus fingernails and dark outlines within six pixels of it,
    minus anything that might be pencil", and three separate things fell
    through the gaps in that:

      the outline      the black edge all the way round his fist is dark
                       and near skin, so it qualified - until the pencil
                       mask was subtracted, which took the long stretch of
                       it that happened to lie under the erase. The fist
                       came out with no edge at all on that side.
      the shading      the creases between his fingers are mid-brown. Not
                       skin by the test, not dark by the test, so they were
                       simply not restored and the food showed through
                       them: a hand with slots in it.
      the knuckle      drawn as a small pale loop, which is neither.

    All three are his hand. Nothing in that region is anything else except
    the pencil, so the honest mask is the REGION ITSELF - the skin flood,
    grown to take in its own outline, closed to seal the lines inside it -
    with only the drawn pencil cut back out. Then everything his hand is
    made of comes back exactly as drawn, and the slot between his fingers
    where the barrel showed through is left for the new object to fill.
    """
    skin = classify(src_arr)[0]
    # SEEDED OUTSIDE THE PENCIL, which is what keeps the sharpened cone out
    # of the hand. The cone is skin-coloured and sits inside the pencil's
    # own footprint, so seeding on "skin near the pencil" makes it a seed in
    # its own right and it survives as a tan chip floating above his fist.
    # Seeded on skin near the pencil but NOT IN it, the only way in is
    # through a skin pixel, and the cone has a black outline all round it.
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
    # THE FLOOD IS NOT ALLOWED INTO THE PENCIL'S OWN FOOTPRINT, which is
    # what keeps the sharpened cone out of his hand. The cone is drawn at
    # (248,195,137) and his skin at (250,193,123): two points apart in
    # green and fourteen in blue, which no threshold separates without
    # taking a bite out of his fingers. Let the flood through it and it is
    # restored as part of the hand - two tan spikes standing up out of his
    # fist where the pencil used to be.
    #
    # Shutting the flood out of the capsule means it cannot reach the
    # fingers on the far side of the barrel either; the closing below is
    # what bridges those, and eighteen is set by the width of the capsule
    # it has to bridge rather than by anything about hands.
    hand = spread(skin & grow(pencil, 4) & ~pencil & keep,
                  skin & keep & ~pencil, 200)
    # Grown by the outline reach to take in his own dark edge, then closed
    # to seal the creases and the knuckle inside it. Ten is wide enough to
    # bridge the pencil's slot through the fist, which is about twenty
    # pixels - and the slot is deliberately left IN the region, because
    # swap_object fills it with his own skin before using it. See there.
    # CLOSED, AND THEN GROWN ONLY INTO INK. Two mistakes were made here
    # and they pull in opposite directions.
    #
    # Growing the skin by six and closing THAT bridges anything within
    # twenty pixels of the fattened shape - and where his knuckles are
    # against his collar, that is a piece of his shirt. The shirt is then
    # restored on top of the food: a white rectangle lying across the
    # hotdog, with the collar's black line still drawn in it.
    #
    # Not growing at all loses his own dark edge, which is drawn OUTSIDE
    # the skin by definition, and the fist comes back without an outline.
    #
    # So the solid part of the mask is the skin alone, closed, which seals
    # the creases and the pencil's slot without ever reaching the shirt;
    # and the six pixels round it contribute only what is INK - an outline
    # or a fingernail. White cloth within six pixels of his knuckles is
    # neither, so it stays behind the hotdog where it belongs.
    _skin, nail, dark, _n = classify(src_arr)
    solid = close(hand, 18) & keep
    return solid | (grow(solid, OUTLINE_REACH) & keep & (dark | nail))


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
                head_drop=0, head_tilt=0.0, mouth_state="smile"):
    """One frame: his pose, a different thing in his hands.

    The order is the whole of the method and it does not commute.

      1  the pencil comes off, if this pose has one (erase_box);
      2  anything the pencil left bare is painted in from its own row;
      3  his MOUTH is redrawn, onto a face nothing is in front of yet;
      4  the new object is laid over the grip - and over the mouth, which
         is how food gets to be inside it;
      5  his fingers go back on top, or he carries it like a tray;
      6  his head moves last, so the piece that is cut at the neck carries
         everything already composited on it.

    Step 3 BEFORE step 4 is the whole of "he is eating it" rather than "he
    is holding it near his face". The food has to be drawn over the open
    mouth, so the mouth has to exist first; swap the two and the hotdog is
    behind his teeth.

    Step 3 is also where the cheek bulge and the chin drop happen, and they
    are warps of the face rather than paint on it - so they have to land
    before anything is composited over that face, or the thing on top is
    left behind by the face moving under it.

    Step 6 being last matters: move the head first and the object is pasted
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
        # the half-transparent fringe left behind is a grey ghost of a
        # pencil standing beside his ear.
        #
        # TWO CAPSULES, FOR TWO DIFFERENT QUESTIONS.
        #
        # `pencil` is the generous one: fifty pixels past each end of the
        # barrel and nine wider than it. Nine rather than five because the
        # colour test finds the LIT side of the barrel and loses the shaded
        # one, so the fit comes out a little narrow - and what is left
        # behind at five is the pencil's own dark outline, one black line
        # hanging in the air beside his fist. That is the region it is SAFE TO
        # ERASE from the background, drawn wide because the sharpened cone
        # and the graphite point cannot be found by colour and have to be
        # covered by geometry instead.
        #
        # It is NOT the region to take his hand from, and reading it as
        # one is the mistake that made these frames. A capsule is a
        # straight band: inside his fist it sweeps his fingers, which are
        # drawn in FRONT of the barrel, and whatever is taken from it
        # there is taken from his hand. What the fist needs is the pencil
        # where it is DRAWN, which pencil_ink finds by colour; see the
        # note further down.
        #
        # `core` is what the GRIP FLOOD is not allowed into - one pixel
        # wider than the barrel, the length of the pencil - and that is
        # all it is now. grip_of walks his hand's own skin to find the
        # fist, and the sharpened cone is skin-coloured, so the flood has
        # to be fenced out of the pencil or it comes back as two tan
        # spikes standing out of his knuckles. Mean across, because a
        # fence wider than the barrel cuts his fingers in half and the
        # closing has to bridge whatever it cuts.
        #
        # Thirty past the barrel rather than ten, because ten stopped
        # short of the ERASER - in lapis_frame_2 and _3 that is inside his
        # fist, and the flood ran straight through it. Measured along the
        # barrel's own axis the pink ends 22 past it in lapis_frame_3 and
        # 16 in lapis_frame_2, so thirty clears both.
        #
        # Both are two pixels wider again than the fit finds them: the
        # barrel's edge is anti-aliased into the background over about that
        # distance, and the half-transparent fringe left behind is a grey
        # ghost of a pencil standing beside his ear.
        capsule = pencil_mask(src_arr, erase_box, reach=50)
        pencil = grow(capsule, 2) & (src_arr[:, :, 3] > 8)
        core = grow(pencil_mask(src_arr, erase_box, reach=30, pad=1), 2)             & (src_arr[:, :, 3] > 8)
        hand = grip_of(src_arr, core, grip_box)
        across = barrel_fit(src_arr, erase_box)[4]
        # THE LAST OF THE FRINGE, below the alpha the masks are cut at.
        # `pencil` stops at 8 so that open air never counts as a hole to
        # be filled, and the pencil's own outermost anti-aliased pixels
        # are under that: one and two and five out of 255, a hairline of
        # them down each side of the barrel. Individually invisible and
        # collectively a pair of grey threads hanging above his fist once
        # the pencil between them has gone. Clearing a pixel that is
        # already 97% transparent cannot fatten an edge or eat one, so
        # this is simply swept rather than reasoned about.
        ghost = grow(capsule, 3) & (src_arr[:, :, 3] <= 8)
    else:
        pencil = np.zeros(src_arr.shape[:2], dtype=bool)
        core = pencil
        hand = hands_of(src_arr, keep_bag)
        across = None
        ghost = pencil

    # The whole pencil comes off the background copy, and what it leaves
    # behind is painted back in two different ways, because the slot it
    # cuts through his FIST and the slot it cuts through his SHIRT are not
    # the same kind of hole.
    base = np.asarray(src).copy()
    if pencil.any():
        base[pencil | ghost] = (0, 0, 0, 0)
        # HIS FIST, FLAT. A closed hand with the pencil taken out of it is
        # a closed hand: skin, one tone, no gap. Letting fill_holes spread
        # into a hole this wide instead gave him a folded-paper palm - the
        # note there says why - and the tone it should be is not a guess,
        # it is the one his fist is mostly painted in already.
        base[pencil & hand] = closed_fist_tone(src_arr, hand, pencil)
        # HIS SHIRT, SPREAD. One flat white, and the spread handles it.
        base = fill_holes(base, pencil & ~hand, across)

    # WHAT HIS HAND IS PUT BACK FROM: the drawing, with the pencil painted
    # out of it. Not the erased copy, and this is the correction that
    # matters most to how the fist reads.
    #
    # The capsule is GEOMETRY. It is a straight band the length of the
    # pencil and it goes through everything in its way, which inside his
    # fist is his fingers: the barrel is drawn BEHIND them, so the band
    # covering it covers them too. Counted in lapis_frame_3, the band
    # through the hand is 4,281 pixels and 3,175 of them are his own skin
    # and his own outlines - and only a hundred-odd are pencil at all,
    # because almost the whole barrel is hidden behind his fingers there.
    #
    # Taking those from the erased copy is what put a pale slab down the
    # back of his hand: a straight-sided patch of flat tone with no edge,
    # covering the creases, the knuckle and the right-hand outline of the
    # fist, in every frame that holds food at face height. It looked like
    # a plaster.
    #
    # So the pencil is painted out by WHERE IT IS DRAWN rather than by
    # where the capsule sweeps - pencil_ink finds that - and everything
    # else about his hand comes back exactly as drawn. What is left to
    # paint is the sliver of barrel between his thumb and forefinger and
    # the eraser below his little finger, which is all of the pencil there
    # is to see once his fingers are closed round it. A closed fist with
    # his own flat skin in the slot is also simply more correct than one
    # with a hotdog showing through it.
    #
    # The capsule is still right for everything OUTSIDE his hand - the
    # cone and the graphite in the air above the fist cannot be found by
    # colour, nothing of his is behind them, and `base` has them erased.
    # SPREAD INTO IT, NOT FLOODED WITH ONE TONE, which is the same choice
    # fill_holes makes and comes out the other way here because the hole
    # is a different size. One tone is right for a fifty-pixel band that
    # crosses a lit side and a shaded side and three creases, because
    # nothing local is true across all of it. The pencil's own slot is
    # twenty pixels of a closed fist, every neighbour it has is the same
    # hand, and the spread gives it whatever that hand is locally - which
    # matters at the eraser, because the eraser sits in the SHADOW under
    # his little finger. Filled with the fist's commonest tone it came
    # out a bright rectangle in a dark pocket, with a straight edge where
    # the shading should have been.
    hand_src = np.asarray(src).copy()
    if pencil.any():
        drawn = grow(pencil_ink(src_arr, pencil), 3) & pencil
        hand_src[drawn] = (0, 0, 0, 0)
        hand_src = fill_holes(hand_src, drawn, across)

    out = mouth.apply(Image.fromarray(base, "RGBA"), mouth_state)
    out.alpha_composite(place(obj, src.size, cx, cy, w, h, degrees))

    # HIS HAND IS BLENDED BACK ON, NOT STAMPED BACK ON, and the difference
    # is a halo. The drawing is anti-aliased, so the pixels round the edge
    # of his fist are half transparent - they were drawn against the
    # classroom. Copying them wholesale onto the food replaces an opaque
    # hotdog pixel with a half-transparent finger pixel, and what shows
    # through the gap is the background: a pale fringe tracing every
    # knuckle, which at game size reads as a ghost of a hand rather than a
    # hand. Compositing them over what is already there instead keeps the
    # food behind the fingers solid, and does nothing at all where the
    # source is fully opaque.
    arr = np.asarray(out).astype(float)
    s = hand_src.astype(float)
    k = (s[:, :, 3:4] / 255.0) * hand[:, :, None]
    arr[:, :, :3] = arr[:, :, :3] * (1 - k) + s[:, :, :3] * k
    arr[:, :, 3] = np.where(hand, np.maximum(arr[:, :, 3], s[:, :, 3]),
                            arr[:, :, 3])
    out = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")

    if head_drop or head_tilt:
        out = head_moved(out, degrees=head_tilt, drop=head_drop)

    assert out.size == src.size, (out_name, out.size, src.size)
    save(out, out_name, src_name)
    print("  %-24s %-22s %3dx%-3d at (%3d,%3d) %+5.1f deg  %-7s%s"
          % (out_name + ".png", obj_name, w, h, cx, cy, degrees, mouth_state,
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

# Where his drawn fist is in each of the three poses, and where his mouth
# is. Read off the drawings; see the note above the HELD table.
FIST = {
    "lapis_frame_1_uniform": (265, 872),
    "lapis_frame_2_uniform": (205, 688),
    "lapis_frame_3_uniform": (152, 606),
}

# From the fist in lapis_frame_3 to the point in his open mouth the food has
# to arrive at. 189 pixels, and every frame of the eating uses this one
# number so that the bite lands in the same place whatever is left of the
# hotdog - see place_held.
BITE_REACH = 189


def body_span(obj_name):
    """Where the drawing actually is on its canvas, as (start, length).

    Both in fractions of the canvas WIDTH, measured from the file's own
    alpha rather than from a constant in make_recess_props, so that one of
    them can be changed without silently moving the other's food through
    his hand.
    """
    im = Image.open(os.path.join(SPRITES, obj_name + ".png")).convert("RGBA")
    box = im.split()[3].getbbox()
    return box[0] / im.size[0], (box[2] - box[0]) / im.size[0]


def place_held(src_name, obj_name, w, degrees, reach=None, grip=None):
    """Where to centre the object's canvas so his fist lands ON it.

    THE PROBLEM THIS SOLVES, which hand-written centres did not. The food
    is placed by its CANVAS CENTRE and drawn left-anchored on that canvas,
    so the centre decides two things at once: how far along the object his
    fist is, and how far the far end of it reaches. Write the centre down
    as a number and both of those are implicit, and the moment the drawing
    changes length - which is the whole point of having a bitten one - they
    are both silently wrong. The first pass of the bitten frames had him
    biting forty pixels short of his own lunch.

    So one of the two is stated and the other falls out:

      reach   how far the FAR END is from his fist, in pixels along the
              object's own axis. Every frame that goes to his mouth gives
              BITE_REACH, so the whole hotdog and the bitten one arrive at
              the same point on his face and his grip slides back along the
              food instead - 25% of the way up the whole one, 15% up the
              bitten one, which is the regrip anybody makes between
              mouthfuls.
      grip    how far along the object his fist is, as a fraction. For the
              stub, which never goes near his mouth: it is too short for
              any grip to reach, and asking for one is the assertion below
              rather than a frame nobody looks at twice.
    """
    bs, bl = body_span(obj_name)
    centre_to_end = (bs + bl - 0.5) * w
    if reach is None:
        reach = (1.0 - grip) * bl * w
    g = 1.0 - reach / (bl * w)
    assert 0.0 <= g <= 0.55, (
        "%s is too short to reach %dpx from the fist in %s - his hand would "
        "be %.0f%% along it, which is off the end" % (obj_name, reach, src_name, g * 100))
    th = math.radians(degrees)
    d = (math.cos(th), -math.sin(th))
    fx, fy = FIST[src_name]
    back = centre_to_end - reach
    return (fx - back * d[0], fy - back * d[1])


# The one length the hotdog is ever drawn at, in frame pixels. Set by the
# gap it has to span: BITE_REACH of it clears his fist, and his hand holds
# the quarter behind that.
DOG_W = 262


def canvas_height(obj_name, w):
    """`w` scaled to the object's own canvas, so nothing is ever squashed."""
    im = Image.open(os.path.join(SPRITES, obj_name + ".png"))
    return round(w * im.size[1] / im.size[0])


def dog(src, out, obj, degrees, mouth, reach=BITE_REACH, grip=None, w=DOG_W):
    """One hotdog frame, with its centre worked out rather than written down."""
    cx, cy = place_held(src, obj, w, degrees, reach=reach, grip=grip)
    return (src, out, obj, round(cx), round(cy), w,
            canvas_height(obj, w), degrees, 0, mouth)


# "Kain", "Inom" and "Tapon" all use the same arm at the same three heights,
# because it is the only arm in the project that holds anything at face
# height. What changes is the object, HIS MOUTH, and where his head is.
#
# WHERE THE FOOD SITS ALONG HIS FIST, and why it is the same fraction in
# every frame. The hotdog is drawn left-anchored on its canvas (see
# make_recess_props._hotdog) and placed here by that canvas's CENTRE, so the
# distance from his fist to the centre decides how far along the object he
# is gripping. Hold it at the middle in one frame and near the end in the
# next and the food slides THROUGH his closed hand between them, which is
# rule 6. So every frame below grips it at 15% of its length, and the centre
# is therefore always 35% of the length out along the axis from the fist:
#
#   lapis_frame_1   fist (265, 872)   at  5 deg -> centre (345, 865)
#   lapis_frame_2   fist (205, 688)   at 20 deg -> centre (281, 660)
#   lapis_frame_3   fist (152, 606)   at 23 deg -> centre (226, 574)
#
# WHY 240 x 78 AND NOT THE SANDWICH'S 240 x 173. A hotdog is three times as
# long as it is deep and a sandwich is not quite twice, so the same length
# of food is less than half as tall - which is what makes this work at all.
# The old sandwich's far corner rose to y 465 with his glasses at 440 and
# the tuning note on it was about nothing else. At 78 deep the far corner of
# the hotdog is at 493, below the base of his nose at 482, so the food is
# at his MOUTH rather than across his face and the open mouth under it is
# not covered by the thing going into it.
#
#  src                     out                    obj              cx   cy   w   h  deg  head  mouth
HELD = [
    # --- Kain: the hotdog goes up to his mouth, and he eats it ----------
    #
    # Five drawings in the lead-in rather than three, because the beat they
    # have to carry is not "food near face" - it is bite, chew, swallow,
    # again. The mouth is doing as much of that as the arm: see
    # Tools/mouth.py. Only the ANGLE is written down; where the drawing goes
    # follows from his fist and from how far the food has to reach, which is
    # what place_held is for.

    # Flat on the desk, just picked up. No lift and no head movement yet:
    # this frame is the join with place_frame_3, where his hands were, and
    # his mouth is still the one the rest of the game draws.
    dog("lapis_frame_1_uniform", "kain_hold_uniform", "prop_sandwich",
        4.0, "smile"),
    # Raised, and his lips come apart on the way up. A mouth that stays shut
    # until the instant the food touches it is a mouth surprised by its own
    # hand.
    dog("lapis_frame_2_uniform", "kain_lift_uniform", "prop_sandwich",
        16.0, "part"),
    # Wide open, food just BELOW the opening and not yet in it. This is the
    # frame the whole beat needed and the scene did not have: without it he
    # goes from a shut mouth to a mouthful in one frame, which is rule 1.
    # Five degrees shallower than the bite, which walks the end of the bun
    # about twenty-five pixels down his chin - plainly on its way rather
    # than arrived.
    dog("lapis_frame_3_uniform", "kain_open_uniform", "prop_sandwich",
        13.0, "wide"),
    # The bite. At eighteen degrees the end of the bun lands across the
    # BOTTOM of the open mouth and leaves the upper lip and the teeth above
    # it showing, which is what a bite looks like from the front - the food
    # covering a mouth is the point, as long as enough of the mouth is left
    # to see that it is open. The mouth is drawn closing on it rather than
    # still gaping.
    dog("lapis_frame_3_uniform", "kain_bite_1_uniform", "prop_sandwich",
        18.1, "open"),

    # --- and the loop: chewing, with a mouthful out of the hotdog --------
    #
    # EVERY FRAME FROM HERE ON HOLDS THE BITTEN ONE. The loop runs until the
    # next word is said, which may be half a minute, and a hotdog still
    # whole on the ninth bite is the same broken promise as food that
    # vanishes - rule 5 from the other end. The lead-in above is the only
    # place the whole one is seen, which is correct: it is whole until he
    # bites it.
    #
    # They all ask for the same BITE_REACH as the whole one, so the bitten
    # hotdog arrives at the same point on his face and it is his GRIP that
    # moves back along the food. See place_held.
    dog("lapis_frame_2_uniform", "kain_chew_1_uniform", "prop_sandwich_bitten",
        16.0, "chew_l"),
    dog("lapis_frame_2_uniform", "kain_chew_2_uniform", "prop_sandwich_bitten",
        20.0, "chew_r"),
    dog("lapis_frame_3_uniform", "kain_open_2_uniform", "prop_sandwich_bitten",
        13.0, "wide"),
    dog("lapis_frame_3_uniform", "kain_bite_2_uniform", "prop_sandwich_bitten",
        18.1, "open"),
    # The last of it, for the frame the eating ends on: his hand comes down
    # with a stub and his cheek still working, so what goes back on the desk
    # is not what came off it. A grip rather than a reach - there is not
    # enough left of it to reach anything, and place_held says so out loud
    # if it is asked to.
    dog("lapis_frame_2_uniform", "kain_last_uniform", "prop_sandwich_stub",
        16.0, "chew_l", reach=None, grip=0.22),

    # --- Inom: the bottle, tipped ---------------------------------------
    ("lapis_frame_1_uniform", "inom_hold_uniform",   "prop_bote_held",
     276, 856, 250, 98, 6.0, 0, "smile"),
    ("lapis_frame_2_uniform", "inom_lift_uniform",   "prop_bote_held",
     256, 640, 270, 106, 30.0, 0, "part"),
    # The cap end lands on his mouth. The bottle is drawn cap-right, so
    # turning it up 34 degrees from a fist at (152,606) walks the cap to
    # about (330,500): his mouth.
    #
    # PURSED, NOT OPEN. A boy drinking with his mouth open is a boy being
    # poured on, and most of what is seen of these lips is the bottom of
    # them anyway, because the bottle covers the rest. What that bottom edge
    # has to say is "closed round something", which is what the purse state
    # draws.
    ("lapis_frame_3_uniform", "inom_drink_1_uniform", "prop_bote_held",
     244, 558, 290, 114, 34.0, 0, "purse"),
    ("lapis_frame_3_uniform", "inom_drink_2_uniform", "prop_bote_held",
     248, 548, 290, 114, 40.0, 0, "purse"),
    # The swallow, which is the half of drinking that happens after the
    # bottle has come down. Lips shut, throat working. Without it the loop
    # is a bottle rocking at a face; with it, it is gulps.
    ("lapis_frame_2_uniform", "inom_swallow_uniform", "prop_bote_held",
     256, 648, 270, 106, 26.0, 0, "swallow"),

    # --- Tapon: the wrapper, picked up and cocked ------------------------
    # The wrapper is the one held object that does NOT span the gap to his
    # mouth, because it is not going there. It stays a ball in his fist and
    # is drawn small for the reason make_recess_props gives: at fist size it
    # reads as a snowball.
    ("lapis_frame_1_uniform", "tapon_hold_uniform", "prop_basura",
     268, 856, 142, 130, 0.0, 5, "smile"),
    ("lapis_frame_2_uniform", "tapon_lift_uniform", "prop_basura",
     200, 650, 148, 136, 0.0, 0, "smile"),
    # Back by his head, about to go.
    ("lapis_frame_3_uniform", "tapon_cock_uniform", "prop_basura",
     146, 596, 155, 142, 0.0, 0, "smile"),
]


def main():
    for p in ("prop_baon", "prop_sandwich", "prop_sandwich_bitten",
              "prop_sandwich_stub", "prop_bote_held", "prop_basura"):
        if not os.path.exists(os.path.join(SPRITES, p + ".png")):
            raise SystemExit("run Tools/make_recess_props.py first - "
                             + p + ".png is not there")

    print("the notepad becomes his baon:")
    for src, out, obj, cx, cy, w, h, deg, bag in BAON:
        swap_object(src, out, obj, cx, cy, w, h, deg, keep_bag=bag)

    print("\nthe pencil becomes his lunch, and his mouth moves:")
    for src, out, obj, cx, cy, w, h, deg, drop, state in HELD:
        swap_object(src, out, obj, cx, cy, w, h, deg,
                    erase_box=PENCIL_BOX[src], grip_box=GRIP_BOX[src],
                    head_drop=drop, mouth_state=state)


main()
