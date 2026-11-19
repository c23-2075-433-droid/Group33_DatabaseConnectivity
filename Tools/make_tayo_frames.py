"""Draw the "Tayo" frames - Kylo stands up out of his chair to go home.

THE BEAT. Paaralan Scene 5's second word. He has just put the afternoon
lesson away ("Ligpit") and is sitting at his desk with his hands flat on it;
the child says "Tayo" and he gets to his feet. Nothing else happens. He is
not answering a question and he is not greeting anyone - he stands up, and
from that moment to the end of the level he is a boy standing at his desk.

WHY THIS FILE EXISTS, when Scene 5 already had a rise. The scene borrowed
Scene 3's "Sagot" frames, which end on sagot_stand_uniform - and
sagot_stand_uniform is talking_uniform entire, which is a boy with both
hands up by his chest and his mouth open. That is right for answering a
question and wrong for going home: he stood up and then held the pose of
someone mid-sentence for the three words that follow. What standing up
should leave him in is his IDLE - arms down at his sides, mouth closed -
facing the camera.

THERE IS NO SUCH DRAWING. Counted across the project, Kylo has exactly one
front-facing standing figure (talking_uniform, hands up, mouth open) and
several side-on ones (standing_uniform and the walk cycle, arms down, in
profile). The pose this beat wants - front on, arms down, calm - exists
only from the waist up, in sitting_front_uniform, the drawing this whole
desk is built around.

SO THE FIGURE IS BUILT, and the three pieces are chosen so that nothing has
to be invented:

  sitting_front_uniform   everything from his crown to mid-forearm. It is
                          already front on, his arms already hang at his
                          sides, and his mouth is already closed in the
                          small calm smile this beat wants. Seated and
                          standing differ below the waist; his head, his
                          shoulders and his upper arms do not know which he
                          is doing.
  talking_uniform         the shirt hem, the waistband, the shorts and the
                          legs - taken from BELOW ITS ROW 368, where that
                          drawing's own arms have ended, so not one pixel of
                          its raised hands comes across with the shirt.
  standing_uniform        one hand, hanging. Mirrored for the other side.

WHY THE GRAFT WORKS HERE WHEN IT FAILED FOR "SAGOT".
Tools/make_sagot_frames.py has a long note saying A SEATED DRAWING AND A
STANDING ONE DO NOT JOIN, and it is right about the join it tried: at the
DESK LINE the seated figure is 407 pixels across because his ARMS are in
that number, and talking_uniform's hips are 264. Easing one width into the
other gave him a white sash and arms that ended in mid-air.

The join here is not at the desk line. It is at the SHIRT, 745 rows below
his crown, and measured there the two drawings are the same boy:

    crown-relative row   seated shirt        talking shirt
    700                  -128 .. +127        -126 .. +129
    740                  -129 .. +128        -131 .. +134
    770                  -129 .. +128        -136 .. +128

That is the rule the Sagot note was reaching for and did not quite state:
graft at a narrow joint where both drawings are THE SAME PART OF HIM. His
shirt is his shirt whether he is sitting or standing. His arms are not.

So the arms are not grafted. They are the seated drawing's own forearms
carried straight down - they are vertical and a constant 67 pixels wide for
the last hundred rows of that sprite, so continuing them is copying a row -
narrowed into a wrist, with a drawn hand put on the end.

THE HAND IS THE SIDE-ON ONE AND THAT IS NOT A COMPROMISE. standing_uniform
is a profile, but a hand hanging loose at a side is drawn broad-face-on even
there: fingers down and slightly apart, the back of it towards the camera.
At hair-cap scale its breadth comes out 78 pixels against the front-facing
forearm's 67, which is about the ratio a hand and a forearm actually have
seen from the front. Scaling it to match the forearm's WIDTH instead would
have been wrong by half again - the profile forearm is narrow because it is
seen edge-on, and the hand in the same drawing is not.

HIS FINGERTIPS GO BEHIND THE DESK. Standing, the desk crosses him 910 rows
below his crown; his wrist lands at 821 and his fingertips at 945. So the
hand is three-quarters visible and its last third is behind the desk edge,
which is where a boy standing at his own desk has his hands. Nothing had to
be drawn for the part that does not show.

HOW FAR HE RISES. 146 rows, 0.89 world units - Scene 3's number, kept
exactly, because BuildPaaralanScene5 measures the "Paalam" gleam off it and
the narration plaque owns the shot from about +2.79 up.

THE POSE NEVER CHANGES DURING THE RISE, which is the other thing this buys
and the reason it reads as standing up rather than as a cut. Scene 3 had to
swap drawings on the first frame of its rise - a seated figure, then a
different standing one - and hid the swap inside the fastest moment of the
movement. Here every frame of the rise is the SAME figure at a different
height, so there is nothing to hide: he comes up and more of him appears
from behind the desk, which is what rising actually looks like.

THE SPACING IS EASED AND IT OVERSHOOTS. A body getting out of a chair
accelerates, decelerates and settles; it does not travel at one speed and
stop dead. The heights are 0.22, 0.56, 0.86 and 1.05 of the rise, and the
sequence then rests on the 1.00 one - so he goes a little past where he
ends up and sinks back onto his feet.

CANVAS. 663x1521, the seated canvas with 300 rows added at EACH end, which
is make_sagot_frames' convention and has to stay symmetric: a sprite draws
centred on its object, so padding one end only would slide him sideways.

Writes  Assets/Sprites/tayo_rise_1..3_uniform.png   up out of the chair
        Assets/Sprites/tayo_over_uniform.png        a little past it
        Assets/Sprites/tayo_stand_uniform.png       his idle, standing

Run from the repository root:  python Tools/make_tayo_frames.py
"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import hair_cap                         # noqa: E402
from look_down import SPRITES, meta_for                      # noqa: E402

BASE = "sitting_front_uniform"   # the canvas, the head size, and him from the waist up
LOWER = "talking_uniform"        # his shirt hem, waistband, shorts and legs
HAND = "standing_uniform"        # one hand, hanging

PAD = 300
WIDTH = 663
HEIGHT = 921 + 2 * PAD           # 1521

SEAT_CROWN_ROW = 142 + PAD       # where the seated crown sits on this canvas
RISE = 146                       # Scene 3's rise, kept exactly - see above

# --- the graft ---
# Below this row, measured from his crown, the figure is talking_uniform. It
# is below that drawing's own arms - they end 722 rows below its crown once
# scaled - and well above its shirt hem at 850, so what comes across is
# shirt, waistband, shorts and legs and nothing else.
JOIN_REL = 745
LOWER_ARMS_END_REL = 722
# talking_uniform's shirt sits a few pixels left of the seated one's at the
# join. Split the difference rather than leave a step in his side.
LOWER_DX = 3

# --- his forearms, carried down out of the seated drawing ---
# The source row is the seated sprite's last, where both forearms have been
# vertical and unchanging for a hundred rows. The spans are the arm plus
# both of its outlines, stopping short of the shirt behind it.
ARM_SRC_ROW = 920
SEAT_CUT_REL = ARM_SRC_ROW - 142   # 778, the drawn arm's last row below his crown
LEFT_ARM = (127, 202)
RIGHT_ARM = (461, 537)
WRIST_REL = 821                    # where the hand's wrist lands below his crown
SUBPIXEL = 4                       # the taper is drawn this many times too wide
                                   # and shrunk back, or its edge is a staircase

# --- the hand ---
# A window on standing_uniform, IN THAT SPRITE'S OWN PIXELS - rows below its
# own crown, columns in its own 214-wide canvas - which is why every one of
# them is multiplied by that drawing's scale before it is used. They are not
# in the seated canvas's units the way everything else here is, and reading
# them as if they were crops his shirt instead of his hand.
HAND_TOP_REL = 424                 # just above the wrist: the seam lands in forearm
HAND_BOT_REL = 508                 # past his fingertips
HAND_X = (92, 142)
# A hand seen from the front is broader than the same hand seen edge-on, and
# standing_uniform is a profile. This is the only number here that is a
# judgement rather than a measurement: it is set so the hand comes out a
# little wider than the forearm, which is the proportion a front view has.
HAND_GAIN = 1.20
# The line round every limb in sitting_front_uniform, measured off the
# forearm's own edge at row 900: three pixels of (11, 11, 9).
OUTLINE_RGB = (12, 12, 10)
OUTLINE_PX = 3


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")


def scaled_to_base(name, base_cap):
    """A sprite resized so its hair cap matches the seated drawing's.

    The same normalisation every generated strip in this project goes
    through: a head cannot change size between two drawings of one boy.
    Returns the image, its crown row, its centre column and the scale.
    """
    im = load(name)
    c, b, cx = hair_cap(im)
    s = base_cap / float(b - c)
    im = im.resize((int(round(im.size[0] * s)), int(round(im.size[1] * s))),
                   Image.LANCZOS)
    return im, c * s, cx * s, s


def match_shirt_white(im, target):
    """Put talking_uniform's shirt on the seated drawing's white.

    THE TWO DRAWINGS ARE NOT LIT THE SAME. Measured over the shirt, the
    seated one averages (254, 254, 254) and talking_uniform (231, 232, 236)
    - darker, and faintly blue. Grafted untouched, the graft shows up as a
    horizontal tone step across his waist even though the silhouettes line
    up to within a few pixels, and that step was the whole of what was
    visible in the first version of this file.

    The correction is a per-channel GAIN rather than an offset, so the hem's
    own shading survives as shading instead of being flattened, and it is
    applied only to pixels that are pale and neutral - his shorts are
    neither, and brightening navy would be a different bug.
    """
    a = np.array(im).astype(float)
    rgb = a[:, :, :3]
    lo, hi = rgb.min(axis=2), rgb.max(axis=2)
    shirt = (a[:, :, 3] > 60) & (lo > 120) & (hi - lo < 45)
    if not shirt.any():
        return im
    gain = np.array(target, dtype=float) / rgb[shirt].mean(axis=0)
    rgb[shirt] = np.clip(rgb[shirt] * gain, 0, 255)
    a[:, :, :3] = rgb
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def lower_body(crown_row, base_cap, white):
    """talking_uniform from the shirt down, head-matched, lit and placed."""
    im, crown, cx, _ = scaled_to_base(LOWER, base_cap)
    cut = int(round(crown + JOIN_REL))
    im = match_shirt_white(im.crop((0, cut, im.size[0], im.size[1])), white)

    out = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    out.alpha_composite(im, (WIDTH // 2 - int(round(cx)) + LOWER_DX,
                             crown_row + JOIN_REL))
    return out


def cut_hand(im):
    """Take the hand out of the window and leave the shorts and shirt behind.

    THE OUTLINE IS NOT FOUND BY DARKNESS. His hand hangs against his navy
    shorts, and navy is dark: (36,52,80) is a luminance of about 50 against
    the outline's 20, so any threshold that keeps the line round his fingers
    keeps the shorts with it. What separates them is HUE - the outline is
    neutral and the shorts are blue - so the test is "dark AND not blue",
    and it is only applied within a few pixels of skin, which is the only
    place an outline of his hand can be.

    The holes are then filled, because the creases between his fingers are
    drawn in that same line colour and are nowhere near the outside edge;
    taking them out would leave him with four separate fingers.
    """
    a = np.array(im).astype(int)
    r, g, b, al = a[:, :, 0], a[:, :, 1], a[:, :, 2], a[:, :, 3]
    solid = al > 60

    skin = solid & (r > 150) & (r - b > 45)
    neutral_dark = solid & (np.maximum(np.maximum(r, g), b) < 95) & (b - r < 22)

    near = Image.fromarray((skin * 255).astype(np.uint8)).filter(
        ImageFilter.MaxFilter(9))
    mask = skin | (neutral_dark & (np.array(near) > 0))

    # Fill the enclosed creases: anything the window's background cannot
    # reach from the outside is inside his hand.
    outside = ~mask
    seen = np.zeros_like(mask)
    seen[0, :] = outside[0, :]
    seen[-1, :] = outside[-1, :]
    seen[:, 0] = outside[:, 0]
    seen[:, -1] = outside[:, -1]
    while True:
        grown = seen.copy()
        grown[1:, :] |= seen[:-1, :]
        grown[:-1, :] |= seen[1:, :]
        grown[:, 1:] |= seen[:, :-1]
        grown[:, :-1] |= seen[:, 1:]
        grown &= outside
        if grown.sum() == seen.sum():
            break
        seen = grown
    filled = ~seen

    # REDRAW THE OUTSIDE LINE RATHER THAN RESCUE IT. Whatever is kept of the
    # drawn outline where it met the shorts has been thinned by the cut and
    # by the blend between a near-black line and a dark navy behind it, and
    # the forearm it joins has a clean 3-pixel line down both sides. A hand
    # with a thin brown edge beside an arm with a black one reads as two
    # drawings. So the silhouette is stroked at the forearm's own line
    # colour and width - the creases between his fingers are untouched,
    # being nowhere near the edge, and neither is the top row, where a line
    # would be a bracelet.
    # The top row is where the forearm continues, so the mask is padded
    # upwards before it is eroded: the erosion then never sees that edge and
    # the line runs down both sides of the wrist without closing across it.
    pad = np.vstack([np.repeat(filled[:1], OUTLINE_PX * 2, axis=0), filled])
    inner = np.array(Image.fromarray((pad * 255).astype(np.uint8)).filter(
        ImageFilter.MinFilter(OUTLINE_PX * 2 + 1)))[OUTLINE_PX * 2:] > 0
    a[:, :, :3][filled & ~inner] = OUTLINE_RGB

    # A one-pixel feather, so the edge is as soft as every other drawn edge
    # in the sprite rather than a staircase.
    soft = Image.fromarray((filled * 255).astype(np.uint8)).filter(
        ImageFilter.GaussianBlur(0.8))
    a[:, :, 3] = np.minimum(np.maximum(al, filled * 255), np.array(soft).astype(int))
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def hanging_hand(base_cap, flip):
    """standing_uniform's hanging hand, cut out, head-matched and widened.

    Returns the hand, the column its wrist is centred on and how wide that
    wrist is - the forearm above it is tapered to meet both, so the join is
    a continuation of one arm rather than a hand parked at the end of one.
    """
    src = load(HAND)
    c, b, _ = hair_cap(src)
    s = base_cap / float(b - c) * HAND_GAIN
    im = src.resize((int(round(src.size[0] * s)), int(round(src.size[1] * s))),
                    Image.LANCZOS)
    crown = c * s
    im = cut_hand(im.crop(
        (int(round(HAND_X[0] * s)), int(round(crown + HAND_TOP_REL * s)),
         int(round(HAND_X[1] * s)), int(round(crown + HAND_BOT_REL * s)))))
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)

    xs = np.where(np.array(im)[0, :, 3] > 60)[0]
    return im, (xs.min() + xs.max()) / 2.0, float(xs.max() - xs.min() + 1)


def forearms(crown_row, base_cap):
    """Both forearms carried down out of the seated drawing, into hands.

    The seated sprite stops flat at the desk's far edge, which is right while
    he is sitting - the desk covers the cut - and leaves him with no hands
    the moment he stands. The forearm is a constant 67 pixels wide and
    vertical for the last hundred rows of that drawing, so it is extended by
    copying one row, narrowed into a wrist, and finished with a drawn hand.
    """
    seat = load(BASE)
    wide = Image.new("RGBA", (WIDTH * SUBPIXEL, HEIGHT), (0, 0, 0, 0))

    top = crown_row + SEAT_CUT_REL               # where the drawn arm stops
    wrist = crown_row + WRIST_REL
    rows = wrist - top

    hands = []
    for (x0, x1), flip in ((LEFT_ARM, False), (RIGHT_ARM, True)):
        hand, hand_cx, hand_w = hanging_hand(base_cap, flip)
        hands.append((hand, hand_cx, (x0 + x1) / 2.0))

        slab = seat.crop((x0, ARM_SRC_ROW - 1, x1, ARM_SRC_ROW))
        cx = (x0 + x1) / 2.0 * SUBPIXEL
        for i in range(rows):
            t = i / float(max(1, rows - 1))
            w = ((x1 - x0) + (hand_w - (x1 - x0)) * t) * SUBPIXEL
            line = slab.resize((max(1, int(round(w))), 1), Image.LANCZOS)
            wide.alpha_composite(line, (int(round(cx - line.size[0] / 2.0)),
                                        top + i))

    out = wide.resize((WIDTH, HEIGHT), Image.LANCZOS)
    for hand, hand_cx, cx in hands:
        out.alpha_composite(hand, (int(round(cx - hand_cx)), wrist))
    return out


def figure(crown_row):
    """The whole standing boy, crown at `crown_row` on the 663x1521 canvas."""
    seat = load(BASE)
    s_crown, s_bottom, s_cx = hair_cap(seat)
    cap = s_bottom - s_crown

    out = lower_body(crown_row, cap, shirt_white(seat))
    out.alpha_composite(forearms(crown_row, cap))
    out.alpha_composite(seat, (WIDTH // 2 - s_cx, crown_row - s_crown))
    return out


def shirt_white(seat):
    """The seated drawing's own shirt colour, which the graft is lit to."""
    a = np.array(seat).astype(float)
    return a[880:915, 250:420, :3].reshape(-1, 3).mean(axis=0)


def write(im, name):
    assert im.size == (WIDTH, HEIGHT), (name, im.size)
    im.save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, BASE)


def main():
    seat = load(BASE)
    s_crown, s_bottom, _ = hair_cap(seat)
    cap = s_bottom - s_crown

    # The one thing that would silently ruin the graft: if talking_uniform's
    # own arms reached below the join, a raised hand would come across with
    # the shirt and sit beside a boy who has his arms down.
    assert JOIN_REL > LOWER_ARMS_END_REL, "the join is inside talking_uniform's arms"

    print("Tayo - he stands up out of his chair. Rise %d rows (%0.2f world "
          "units); graft at the shirt, %d rows below his crown."
          % (RISE, RISE * 0.0060902, JOIN_REL))

    for i, part in enumerate((0.22, 0.56, 0.86), start=1):
        row = SEAT_CROWN_ROW - int(round(RISE * part))
        write(figure(row), "tayo_rise_%d_uniform" % i)
        print("  %-24s %3d rows up" % ("tayo_rise_%d_uniform.png" % i,
                                       SEAT_CROWN_ROW - row))

    over = SEAT_CROWN_ROW - int(round(RISE * 1.05))
    write(figure(over), "tayo_over_uniform")
    print("  %-24s %3d rows up - a little past it"
          % ("tayo_over_uniform.png", SEAT_CROWN_ROW - over))

    write(figure(SEAT_CROWN_ROW - RISE), "tayo_stand_uniform")
    print("  %-24s %3d rows up - on his feet, arms down"
          % ("tayo_stand_uniform.png", RISE))

    print("\nWire them in BuildPaaralanScene5 as the 'Tayo' PoseSequence:")
    print("  frames      place_frame_3, sitting_front, rise_1, rise_2, rise_3, over")
    print("  tailFrames  tayo_stand")
    print("  destination tayo_stand_uniform")


if __name__ == "__main__":
    main()
