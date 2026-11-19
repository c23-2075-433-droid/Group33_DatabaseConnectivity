"""Cut Paaralan Scene 5's Kunin, Paalam and Alis frames out of Canva strips.

WHAT THIS REPLACES. Tools/make_uwian_frames.py built all three actions by
cutting the front-on standing drawing at its shoulder and elbow and turning
the pieces, and by painting the backpack's straps on in code. Played back it
looked exactly like that: an arm pasted on at a seam, strap ends sticking up
past his shoulders like wings, and a "walk" that was two legs swinging under a
boy who never turned round. Rotating pieces of one drawing cannot put a bag
on a back or turn a body, so this does not try. Every pose here is a DRAWING.

WHERE THE DRAWINGS COME FROM.

  Art/kunin_strip_1_raw.png   one Canva generation, four poses: looking at
                              the bag, reaching for it, holding it at his
                              hip, holding it in front of his chest. The
                              first two are used.
  Art/kunin_strip_2_raw.png   one Canva generation from the first: lifting
                              it at his side, swinging it over his shoulder,
                              both straps on and adjusting them, standing in
                              it. All four are used.
  Art/paalam_strip_raw.png    one generation from kunin_strip_2's last pose:
                              the arm coming up, the hand raised, waving out,
                              waving in.
  Art/alis_strip_raw.png      one generation from the same and from the
                              walk below: facing us, turning three-quarters
                              to the left, in profile facing left, and the
                              first step left. The last three are used.
  Art/alis_walk_strip_raw.png one generation from alis_strip: a four-frame
                              walk cycle in profile facing left. Paaralan
                              Scene 2's walk was tried first, mirrored, and
                              is a different generation - a bigger head and
                              an open mouth - so the cut from his first step
                              into it showed.

All of them were generated from the Paaralan sprites (bati_front with the
bag, standing_uniform_with_bag_back) as the design reference, so he comes back
as the boy the other Paaralan scenes draw. Each strip is ONE generation,
because four poses drawn in one pass match each other in a way four separate
generations do not - Docs/AnimationStandard.md has the measurements.

HOW A STRIP REACHES THE GAME. The Canva API returns only a ~200px thumbnail
of a generation, and its URL is signed for that size. The full-size pixels
come out of a DESIGN instead: a copy of "SALINLAHI Art Export" resized to the
generation's own size, its picture swapped for the generation, saved and
exported as a PNG. Tools/make_guard_push_frames.py explains the route. The
raws in Art/ are those exports, byte for byte.

THE CANVAS IS THE ONE THE SCENE ALREADY USES. BuildPaaralanScene5 has a single
SpriteRenderer for Kylo, sized once from sitting_front_uniform, and swaps
sprites on it - so every frame has to put his crown the same distance above
its own centre as the tayo_* frames do, at the same pixels per unit, or he
jumps when the word plays. That is CANVAS_W x CANVAS_H with the crown of the
standing pose on CROWN_ROW, exactly where make_tayo_frames and the frames this
replaces put it.

SIZE. Every figure in a strip is scaled by ONE factor, so a boy leaning out
to reach is shorter than a boy standing, which is what the drawing says. The
first strip's factor puts his head - the hair cap Tools/fit_seated_pose.py
measures everything by - at the tayo_* frames' HAIR_PX, so it is the size it
was the moment before Kunin; every other strip is matched to that one by
body height. See strip_scales.

WHERE HE STANDS. Every pose is stood on one floor row by its soles, and put
on the canvas's centre column by the middle of his WAISTBAND - see
waist_centre for why the waist and not the feet. A sprite draws centred on
its object, so registering by the canvas or by the ink box would slide him
sideways whenever an arm went out.

HIS OWN HEAD AND SKIN. Canva got the movement right and him slightly wrong -
a different hair shape, a rounder face, paler skin. Every frame has its
generated head taken off and Kylo's own put on at the neck, and its skin
recoloured to his: Tools/kylo_own_head.py. Only the body is Canva's.

THE BAG ON THE DESK STAYS. prop_bag_desk_uniform is the same backpack as the
generator's - front zip, front pocket, top handle - and Ligpit has just
packed the lesson into it, so it is not replaced. It is smaller than the one
the generator drew, so the lift frame, which is where one bag hands over to
the other, draws his bag at LIFT_BAG_UNITS: see shrink_held_bag. The world
numbers printed at the end are where his reaching hand arrives.

Writes  Assets/Sprites/kunin_look_canva.png ... (see FRAMES)

Run from the repository root:  python Tools/make_uwian_canva_frames.py
"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, hair_cap, split_columns  # noqa: E402
from look_down import SPRITES, meta_for                              # noqa: E402
from make_guard_frames import sharpen_linework                       # noqa: E402
from kylo_own_head import own_head                                   # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Art")

# The tayo_* / kunin_* canvas, widened. 1063 was enough for an arm pasted on
# at the shoulder; a drawn reach and a bag held out to the side go further.
# Widened SYMMETRICALLY, because a sprite draws centred on its object.
CANVAS_W, CANVAS_H = 1263, 1761
CROWN_ROW = 416          # the standing crown, as in every other frame here
HAIR_PX = 278            # tayo_stand_uniform's hair cap, crown to hair bottom

# Seated sprite: 921 rows for 5.609 world units. Every frame shares it.
UNITS_PER_PX = 5.609 / 921.0
SIT_X, SIT_FEET_Y = 2.86, -3.009
CENTRE_Y = SIT_FEET_Y + 5.609 * 0.5   # the world y of every canvas's centre

META_FROM = "tayo_stand_uniform"      # import settings to copy

# (strip, pose index from the left, output sprite, mirror, which of his own
# heads goes on it - see Tools/kylo_own_head.py)
FRAMES = [
    ("kunin_strip_1", 0, "kunin_look_canva", False, "front"),
    ("kunin_strip_1", 1, "kunin_reach_canva", False, "front"),
    ("kunin_strip_2", 0, "kunin_lift_canva", False, "front"),
    ("kunin_strip_2", 1, "kunin_shoulder_canva", False, "front"),
    ("kunin_strip_2", 2, "kunin_straps_canva", False, "front"),
    ("kunin_strip_2", 3, "kunin_wear_canva", False, "front"),
    ("paalam_strip", 0, "paalam_raise_canva", False, "front"),
    ("paalam_strip", 1, "paalam_up_canva", False, "front"),
    ("paalam_strip", 2, "paalam_out_canva", False, "front"),
    ("paalam_strip", 3, "paalam_in_canva", False, "front"),
    ("alis_strip", 1, "alis_turn_canva", False, "turn"),
    ("alis_strip", 2, "alis_side_canva", False, "side"),
    ("alis_strip", 3, "alis_step_canva", False, "side"),
    ("alis_walk_strip", 0, "alis_walk_1_canva", False, "side"),
    ("alis_walk_strip", 1, "alis_walk_2_canva", False, "side"),
    ("alis_walk_strip", 2, "alis_walk_3_canva", False, "side"),
    ("alis_walk_strip", 3, "alis_walk_4_canva", False, "side"),
]

# The two poses whose head is not upright: where the generated head really
# joins the body (row, column on the canvas, measured on the skin runs under
# the chin) and how far to tip his own head to follow it. Canva turned his
# head toward the bag on the look; his own head cannot turn, so it tips.
HEAD_JOIN = {
    "kunin_look_canva": ((854, 639), -5.0),
    "kunin_reach_canva": ((905, 703), None),
}

FEET_ROWS = 0.10         # the bottom tenth of a figure: shoes and socks, never his hands

# The bag on the desk is prop_bag_desk_uniform, 1.66 units tall (BagHeight in
# BuildPaaralanScene5). The lift frame draws his bag this tall, between the
# desk bag and the generator's own size on his shoulder a frame later.
LIFT_BAG_UNITS = 2.0        # the bottom this fraction of a figure is his soles


def ink_rows(alpha):
    rows = np.where((alpha > 60).any(axis=1))[0]
    return rows[0], rows[-1]


def feet_centre(rgba):
    """The midpoint of his feet: the ink in the lowest rows of the figure."""
    a = np.asarray(rgba)
    top, bottom = ink_rows(a[:, :, 3])
    band = a[max(top, bottom - int((bottom - top) * FEET_ROWS)):bottom + 1, :, 3] > 60
    cols = np.where(band.any(axis=0))[0]
    return (cols[0] + cols[-1]) / 2.0, bottom


def waist_centre(rgba):
    """The middle of his waistband: the top of the navy shorts.

    In this shot his feet are behind the desk and only the top of his
    shorts and everything above it is seen, so that is what has to hold
    still. Anchored by the feet, the reach - which leans his whole body out
    over planted feet - slid his torso a fifth of a unit sideways and put his
    hand past the bag. Anchored here, he stays put and leans.

    The shorts are told apart from the other navy things - the bag in his
    hand, its straps, his shoes - by being the navy piece with the most of
    itself in the band where shorts are on a standing boy.
    """
    from scipy import ndimage
    a = np.asarray(rgba).astype(int)
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    navy = (al > 120) & (b > r + 15) & (r < 90)
    top, bottom = ink_rows(a[:, :, 3])
    h = bottom - top
    band = slice(top + int(h * 0.60), top + int(h * 0.72))
    lab, n = ndimage.label(navy)
    counts = np.bincount(lab[band].ravel(), minlength=n + 1)
    counts[0] = 0
    shorts = lab == counts.argmax()
    ys = np.where(shorts.any(axis=1))[0]
    waist = shorts[ys[0]:ys[0] + max(4, (ys[-1] - ys[0]) // 6)]
    cols = np.where(waist.any(axis=0))[0]
    return (cols[0] + cols[-1]) / 2.0


def cut_figures(strip, count=4):
    """The strip's figures, each alpha-cut, left to right."""
    path = os.path.join(ART, strip + "_raw.png")
    src = Image.open(path).convert("RGB")
    spans = split_columns(src)
    if len(spans) != count:
        sys.exit("%s: expected %d figures, found %d: %s" % (strip, count, len(spans), spans))
    figures = []
    for i, (x0, x1) in enumerate(spans):
        crop = src.crop((max(0, x0 - 24), 0, min(src.size[0], x1 + 25), src.size[1]))
        padded = Image.new("RGB", (crop.size[0] + 60, crop.size[1] + 60), (255, 255, 255))
        padded.paste(crop, (30, 30))
        tmp = os.path.join(ART, ".__%s_%d.png" % (strip, i))
        padded.save(tmp)
        try:
            fig = cut_background(tmp)
            if strip in POCKET_STRIPS:
                fig = clear_pockets(fig)
            figures.append(drop_crumbs(fig))
        finally:
            os.remove(tmp)
    return figures


# The strips with background shut inside the figure: the wave (between his
# raised arm and his head) and the lift (between his arm and the bag he holds
# out). ONLY these, because the test cannot be made safe everywhere: in the
# walk strip the lit side of his sleeve is the same 253-254 as the
# background, and clearing pockets there cut holes in his shirt.
POCKET_STRIPS = {"paalam_strip", "kunin_strip_2"}


def clear_pockets(fig, min_px=150):
    """Take out background the flood fill could not reach.

    cut_background fills inward from the corners, so a patch of background
    with the figure all round it - between a raised arm and his head, or
    between his arm and the bag he is holding out - survives as a white
    shape stuck to him. On the wave it was 4791 pixels beside his ear.

    The background is (254,254,254) and his shirt (251,251,251), three levels
    apart, so the test is strict: opaque, every channel at least 253, and
    big enough to be a region rather than a highlight. Measured on the
    strips in POCKET_STRIPS, every piece that passes is background and no
    piece of shirt does. Its pale anti-aliased rim goes with it, as HALO_ROUNDS does for the
    outside edge.
    """
    from scipy import ndimage
    a = np.asarray(fig).copy()
    rgb = a[:, :, :3].astype(int)
    near = (a[:, :, 3] > 200) & (rgb.min(axis=2) >= 253)
    lab, n = ndimage.label(near)
    if n == 0:
        return fig
    sizes = ndimage.sum(near, lab, range(1, n + 1))
    pocket = np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz >= min_px])
    # The rim: the grey anti-aliasing between the pocket and the outline
    # round it, measured at 191-237 and colourless. Grown only through pale
    # NEUTRAL pixels - skin has colour, so the arm and face stop it, and the
    # outline's dark core (about 100) stops it before it can reach the shirt.
    pale = ((rgb.max(axis=2) - rgb.min(axis=2)) < 14) & (rgb.min(axis=2) > 150)
    for _ in range(4):
        pocket |= ndimage.binary_dilation(pocket) & pale
    a[pocket, 3] = 0
    soft = np.asarray(Image.fromarray(a[:, :, 3]).filter(ImageFilter.GaussianBlur(0.6)))
    a[:, :, 3] = np.minimum(a[:, :, 3], np.where(pocket, 0, 255)).astype(np.uint8)
    edge = ndimage.binary_dilation(pocket, iterations=2) & ~pocket
    a[edge, 3] = soft[edge]
    return Image.fromarray(a)


# The pose in each strip that has both hands below his head, which is the one
# a strip is measured by. A raised hand is the first ink from the top, and
# hair_cap's crown is the first ink from the top - measured on the wave, the
# fingertips passed for hair and the strip came out 4.6% too big.
REST_POSE = {"kunin_strip_1": 0, "kunin_strip_2": 3, "paalam_strip": 0, "alis_strip": 0}


def body_px(fig):
    """Crown to sole, in the figure's own pixels."""
    return float(np.subtract(*ink_rows(np.asarray(fig)[:, :, 3])[::-1]))


def strip_scales(figures):
    """One factor per strip, so his height never changes between them.

    The first strip is sized by its head: its rest pose's hair cap goes to
    HAIR_PX, the tayo_* frames' head, so he is the size he was before Kunin.
    Every later strip is sized by its rest pose's whole BODY to that one's,
    because a body is what a viewer compares across a cut - and the four
    strips are separate generations, so their heads and their legs are not
    in quite the same proportion.
    """
    first = figures["kunin_strip_1"][REST_POSE["kunin_strip_1"]]
    crown, bottom, _ = hair_cap(np.asarray(first))
    s1 = HAIR_PX / float(bottom - crown)
    target = body_px(first) * s1
    scales = {strip: (s1 if strip == "kunin_strip_1" else target / body_px(figs[REST_POSE[strip]]))
              for strip, figs in figures.items() if strip in REST_POSE}

    # The walk has no pose standing still to measure - every frame is mid
    # stride - and it was generated FROM alis_strip, at that strip's size, so
    # it takes that strip's factor. Sized by its own heads instead it came
    # out 4% taller, and his head rose as he set off.
    if "alis_walk_strip" in figures and "alis_strip" in scales:
        scales["alis_walk_strip"] = scales["alis_strip"]
    return scales


def place(fig, scale, floor_row, mirror=False):
    """Scale a cut figure and stand it on the canvas by its feet."""
    if mirror:
        fig = fig.transpose(Image.FLIP_LEFT_RIGHT)
    big = fig.resize((int(round(fig.size[0] * scale)), int(round(fig.size[1] * scale))),
                     Image.LANCZOS)
    big = sharpen_linework(big)
    _, sole = feet_centre(big)
    fx = waist_centre(big)
    canvas = Image.new("RGBA", (CANVAS_W, CANVAS_H), (0, 0, 0, 0))
    left = int(round(CANVAS_W / 2.0 - fx))
    top = int(round(floor_row - sole))
    canvas.alpha_composite(big, (left, top)) if left >= 0 and top >= 0 else \
        paste_clipped(canvas, big, left, top)
    return canvas, (left, top)


def paste_clipped(canvas, im, left, top):
    """alpha_composite with a negative offset, which PIL refuses."""
    x0, y0 = max(0, -left), max(0, -top)
    im = im.crop((x0, y0, im.size[0], im.size[1]))
    canvas.alpha_composite(im, (max(0, left), max(0, top)))


def clipped_ink(canvas):
    """True if the figure runs off the left, right or top of the canvas."""
    a = np.asarray(canvas)[:, :, 3] > 60
    return a[:, 0].any() or a[:, -1].any() or a[0, :].any()


def to_world(col, row):
    return (SIT_X + (col - CANVAS_W / 2.0) * UNITS_PER_PX,
            CENTRE_Y + (CANVAS_H / 2.0 - row) * UNITS_PER_PX)


def save(im, name):
    im.save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, META_FROM)
    print("  wrote %s" % name)


def bag_mask(fig):
    """The bag in his hand: the biggest navy piece right of his body.

    Navy is the bag's colour and the shorts' colour, so the piece is chosen
    by where it is - its centre in the right part of the figure - as well as
    by size. His shoes are navy too and lie in the same columns; they lose
    on size.
    """
    from scipy import ndimage
    a = np.asarray(fig).astype(int)
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    skin = (r > 170) & (g > 110) & (b < 160) & (r > b + 40)
    navy = (al > 60) & ~skin & (b >= r) & (r < 120)
    lab, n = ndimage.label(ndimage.binary_closing(navy, iterations=2))
    w = a.shape[1]
    best, best_size = None, 0
    for i in range(1, n + 1):
        piece = lab == i
        size = piece.sum()
        if np.where(piece)[1].mean() > w * 0.58 and size > best_size:
            best, best_size = i, size
    mask = ndimage.binary_fill_holes(lab == best)
    mask = ndimage.binary_dilation(mask, iterations=3) & (al > 0) & ~skin
    return mask, skin


def shrink_held_bag(fig, factor):
    """Draw the bag in his hand smaller, hanging from the same fingers.

    The generator drew the backpack half again the size of the one standing
    on the desk, and the lift is the frame that replaces the one with the
    other - so in this frame only it is brought most of the way down to the
    desk bag's size, and the next frame, where it goes over his shoulder,
    takes it the rest of the way. Shrunk about the top of its handle, which
    is under his fingers, and his hand is laid back over it afterwards.
    """
    mask, skin = bag_mask(fig)
    a = np.asarray(fig).copy()
    ys, xs = np.where(mask)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    top_cols = xs[ys < y0 + (y1 - y0) * 0.08]
    anchor = (top_cols.mean(), float(y0))

    bag = np.zeros_like(a)
    bag[mask] = a[mask]
    bag_im = Image.fromarray(bag).crop((x0, y0, x1 + 1, y1 + 1))
    small = bag_im.resize((max(1, int(bag_im.size[0] * factor)),
                           max(1, int(bag_im.size[1] * factor))), Image.LANCZOS)

    # The hand: skin over and just above the bag, kept from the original.
    from scipy import ndimage
    hand = skin & (np.asarray(fig)[:, :, 3] > 0)
    region = np.zeros_like(hand)
    region[max(0, y0 - int((y1 - y0) * 0.2)):y0 + int((y1 - y0) * 0.25), x0:x1 + 1] = True
    hand = ndimage.binary_dilation(hand & region, iterations=3) & (np.asarray(fig)[:, :, 3] > 0)
    hand_px = np.zeros_like(a)
    hand_px[hand] = a[hand]

    a[mask & ~hand] = 0
    out = Image.fromarray(a)
    left = int(round(anchor[0] - (anchor[0] - x0) * factor))
    out.alpha_composite(small, (left, int(anchor[1])))
    out.alpha_composite(Image.fromarray(hand_px))
    return drop_crumbs(out), (y1 - y0 + 1)


def drop_crumbs(fig):
    """Remove the loose flecks a removed bag leaves behind.

    The bag's mask is dilated to take its outline with it, and along the
    side of his shirt that leaves a few anti-aliased pixels with nothing
    joined to them. Anything not connected to the figure, the bag or his
    hand - under one percent of the biggest piece - goes.
    """
    from scipy import ndimage
    a = np.asarray(fig).copy()
    lab, n = ndimage.label(a[:, :, 3] > 30)
    if n <= 1:
        return fig
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz >= sizes.max() * 0.01])
    a[~keep & (a[:, :, 3] > 0)] = 0
    return Image.fromarray(a)


def main():
    figures = {}
    for strip in sorted({f[0] for f in FRAMES}):
        if not os.path.exists(os.path.join(ART, strip + "_raw.png")):
            print("  SKIP %s: no raw yet" % strip)
            continue
        figures[strip] = cut_figures(strip)
    scales = strip_scales(figures)
    for strip in sorted(scales):
        print("%s: scale %.3f" % (strip, scales[strip]))

    # One floor for everything: where the soles of the standing reference -
    # the strip-1 pose he starts Kunin in - land when his crown is on
    # CROWN_ROW. Any figure stood on it then has his crown where it belongs.
    ref = figures["kunin_strip_1"][0]
    rs = scales["kunin_strip_1"]
    rcrown = hair_cap(np.asarray(ref))[0]
    rsole = ink_rows(np.asarray(ref)[:, :, 3])[1]
    floor_row = CROWN_ROW + (rsole - rcrown) * rs
    print("floor row %.1f (world y %.3f)" % (floor_row, to_world(0, floor_row)[1]))

    placed = {}
    for strip, idx, name, mirror, head in FRAMES:
        if strip not in figures:
            continue
        fig = figures[strip][idx]
        if name == "kunin_lift_canva":
            _, bag_px = shrink_held_bag(fig, 1.0)
            drawn = bag_px * scales[strip] * UNITS_PER_PX
            fig, _ = shrink_held_bag(fig, LIFT_BAG_UNITS / drawn)
            print("  bag in his hand: drawn %.2f units, shown %.2f" % (drawn, LIFT_BAG_UNITS))
        canvas, _ = place(fig, scales[strip], floor_row, mirror)
        join, tilt = HEAD_JOIN.get(name, (None, None))
        canvas, (ny, nx, deg) = own_head(canvas, head, neck=join, tilt=tilt)
        print("  %-22s his own %s head at the neck (%d, %d), tipped %.1f"
              % (name, head, nx, ny, deg))
        if clipped_ink(canvas):
            print("  WARNING %s runs off the canvas" % name)
        save(canvas, name)
        placed[name] = canvas

    # Where his reaching hand is, for standing the desk bag under it.
    if "kunin_reach_canva" in placed:
        a = np.asarray(placed["kunin_reach_canva"]).astype(int)
        r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
        skin = (al > 60) & (r > 180) & (g > 120) & (b < 150) & (r > b + 50)
        cols = np.where(skin.any(axis=0))[0]
        right = cols[-1]
        ys = np.where(skin[:, right - 40:right + 1].any(axis=1))[0]
        hand = (right - 25, ys.mean())
        wx, wy = to_world(*hand)
        print("reaching fingertips: canvas (%d, %d) -> world (%.3f, %.3f)"
              % (int(hand[0]), int(hand[1]), wx, wy))
    if "kunin_lift_canva" in placed:
        a = np.asarray(placed["kunin_lift_canva"]).astype(int)
        r, b, al = a[:, :, 0], a[:, :, 2], a[:, :, 3]
        navy = (al > 60) & (b >= r) & (r < 120)
        navy[:, :CANVAS_W // 2 + 60] = False
        ys, xs = np.where(navy)
        cx, cy = to_world(xs.mean(), ys.mean())
        print("bag in his hand, kunin_lift: centre world (%.3f, %.3f), bottom y %.3f"
              % (cx, cy, to_world(0, ys.max())[1]))


if __name__ == "__main__":
    main()
