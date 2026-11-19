"""Draw what Paaralan Scene 5 will look like, without opening Unity.

WHY. BuildPaaralanScene5 places Kylo, his desk, his lesson and his bag by
world coordinate, and the three beats this scene ends on - Kunin, Paalam,
Alis - are the ones where a drawn hand has to meet a prop, a strap has to
stay on a shoulder and a boy has to leave the frame on his own legs. Every
one of those numbers is wrong until something shows it is right, and opening
the editor to run a menu item is a slow way to find out.

This composites the same sprites at the same world coordinates through the
same camera and writes PNGs. It is NOT the game: it knows nothing about
PoseSequence, sorting layers beyond the order it draws things in, or the
voice UI. What it checks is the thing most likely to be wrong, which is
whether everything lands where the arithmetic says.

KEEP THE NUMBERS IN STEP. They are copied from BuildPaaralanScene5.cs and
there is nothing keeping them honest but this sentence: change one there and
change it here, or this draws a scene that no longer exists.

Writes PNGs into the path given as the first argument, or ./preview_p5 if
none is given.

Run from the repository root:  python Tools/preview_paaralan5.py [outdir]
"""
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

# The camera: orthographic, size 5.4, centred on the origin.
VIEW_W, VIEW_H = 19.2, 10.8
PX = 100                       # pixels per world unit in this preview
W, H = int(VIEW_W * PX), int(VIEW_H * PX)

# --- copied from BuildPaaralanScene5.cs ---
SIT_X, SIT_FEET_Y, SEATED_H = 2.86, -3.009, 5.609

NOTEBOOK = (2.86, -3.84, 0.83)         # x, baseY, height
AKLAT = (5.30, -3.84, 1.00)
PENCIL = (2.54, -3.35, 0.30)

BAG_X, BAG_BASE_Y, BAG_H = 4.90, -3.84, 1.66
# The desk bag's sprite carries its own shadow, so it is taller than the bag
# and its base sits that much lower. Tools/make_desk_bag.py prints both.
BAG_DESK_H = BAG_H * 1.08597
BAG_DESK_BASE_Y = BAG_BASE_Y - BAG_H * 0.08597

# One waypoint per drawn frame of "Kunin", from the frame his fist closes on
# the bag. The fists are printed by Tools/make_uwian_frames.py; the third
# number is how much of the hanging offset still applies, because a bag
# swung onto a back comes UP against the hand as it goes round.
KUNIN_FIST = [(5.34, -1.92, 1.00), (4.28, -2.73, 0.95),
              (2.68, -1.97, 0.15), (2.65, -1.99, 0.00)]
GRIP_DX = BAG_X - KUNIN_FIST[0][0]
GRIP_DY = (BAG_BASE_Y + BAG_H * 0.5) - KUNIN_FIST[0][1]

# Where he walks to - BuildPaaralanScene5's AlisEndX, half a sprite width
# past the left edge of the view.
ALIS_END_X = -12.8


def world_to_px(x, y):
    return (x + VIEW_W / 2) * PX, (VIEW_H / 2 - y) * PX


def load(name):
    path = os.path.join(SPRITES, name + ".png")
    return Image.open(path).convert("RGBA") if os.path.exists(path) else None


def paste_by_height(canvas, name, cx, base_y, height):
    """A sprite scaled to `height` world units, standing with its foot at base_y."""
    im = load(name)
    if im is None:
        print("  MISSING", name)
        return
    w = height * im.size[0] / im.size[1]
    im = im.resize((max(1, int(w * PX)), max(1, int(height * PX))), Image.LANCZOS)
    left, top = world_to_px(cx - w / 2, base_y + height)
    canvas.alpha_composite(im, (int(left), int(top)))


def paste_full(canvas, name):
    im = load(name)
    if im is None:
        print("  MISSING", name)
        return
    s = max(H / im.size[1], W / im.size[0])
    im = im.resize((int(im.size[0] * s), int(im.size[1] * s)), Image.LANCZOS)
    canvas.alpha_composite(im, ((W - im.size[0]) // 2, (H - im.size[1]) // 2))


def paste_kylo(canvas, name, x):
    """One of Kylo's poses, drawn the way the scene draws them.

    THE SCALE IS THE SEATED SPRITE'S AND NOTHING ELSE CHANGES IT. The figure's
    GameObject is sized once, from sitting_front_uniform's 921 rows standing
    for 5.609 world units, and PoseSequence then swaps the sprite on it - so a
    taller or wider frame draws taller or wider, centred on the same point.
    That is what lets the tayo_*, kunin_*, paalam_* and alis_* frames be
    different sizes and still register: every one of them has his crown the
    same distance above its own centre.
    """
    im = load(name)
    if im is None:
        print("  MISSING", name)
        return
    units_per_px = SEATED_H / 921.0
    w, h = im.size[0] * units_per_px, im.size[1] * units_per_px
    im = im.resize((max(1, int(w * PX)), max(1, int(h * PX))), Image.LANCZOS)
    cy = SIT_FEET_Y + SEATED_H * 0.5
    left, top = world_to_px(x - w / 2, cy + h / 2)
    canvas.alpha_composite(im, (int(left), int(top)))


def frame(pose, on_desk, out, x=SIT_X, carried=None, shrink=1.0, fade=0.0,
          black=0.0):
    """One moment of the scene: a pose of Kylo's and what is on the desk."""
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    paste_full(canvas, "klasrum_interior_background")
    if pose is not None:
        if fade > 0:
            layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            paste_kylo(layer, pose, x)
            canvas.alpha_composite(Image.blend(
                Image.new("RGBA", (W, H), (0, 0, 0, 0)), layer, 1.0 - fade))
        else:
            paste_kylo(canvas, pose, x)
    paste_full(canvas, "klasrum_desk_front_near")
    for name, (dx, base, height) in on_desk:
        paste_by_height(canvas, name, dx, base, height)
    if carried is not None:
        name, (cx, cy), height = carried
        im = load(name)
        if im is not None:
            w = height * im.size[0] / im.size[1]
            im = im.resize((max(1, int(w * PX * shrink)),
                            max(1, int(height * PX * shrink))), Image.LANCZOS)
            px, py = world_to_px(cx, cy)
            canvas.alpha_composite(im, (int(px - im.size[0] / 2),
                                        int(py - im.size[1] / 2)))
    if black > 0:
        canvas = Image.alpha_composite(
            canvas, Image.new("RGBA", (W, H), (0, 0, 0, int(255 * black))))
    canvas.convert("RGB").save(out)
    print("  wrote", os.path.basename(out))


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "preview_p5")
    os.makedirs(outdir, exist_ok=True)
    lesson = [("prop_notebook_open", NOTEBOOK), ("prop_aklat_closed", AKLAT),
              ("prop_lapis", PENCIL)]
    bag_on_desk = [("prop_bag_desk_uniform",
                    (BAG_X, BAG_DESK_BASE_Y, BAG_DESK_H))]

    print("Paaralan Scene 5, as the numbers in the builder place it:")
    frame("sitting_front_uniform", lesson, os.path.join(outdir, "01_open.png"))
    frame("tayo_stand_uniform", [], os.path.join(outdir, "02_tayo.png"))

    # --- Kunin, frame by frame: the bag from the desk onto his back ---
    # The beat most worth previewing, because it is the one where a prop has
    # to meet a drawing. If the bag is not on his fist in any of these, the
    # waypoint for that frame is wrong.
    frame("tayo_stand_uniform", bag_on_desk,
          os.path.join(outdir, "03a_kunin_bag_on_desk.png"))
    for n in ("kunin_reach_1_uniform", "kunin_reach_2_uniform",
              "kunin_grip_uniform"):
        frame(n, bag_on_desk, os.path.join(outdir, "03b_%s.png" % n.split("_", 1)[1]))
    # From the grip on, the bag is in his hand. The last of them, settle, is
    # the hand-over: the straps are drawn on his shoulders and PropCarry fades
    # the bag in his hand out over that one frame, so both are on screen
    # together and neither of them is the whole of it.
    held = ("kunin_lift_uniform", "kunin_carry_1_uniform",
            "kunin_carry_2_uniform", "kunin_settle_uniform")
    for i, (name, fist) in enumerate(zip(held, KUNIN_FIST)):
        frame(name, [], os.path.join(outdir, "03%s_%s.png"
                                     % ("defg"[i], name.split("_", 1)[1])),
              carried=("prop_bag_uniform",
                       (fist[0] + GRIP_DX, fist[1] + GRIP_DY * fist[2]), BAG_H),
              shrink=1.0 if i < 3 else 0.88)
    for i, n in enumerate(("kunin_strap_1_uniform", "kunin_strap_2_uniform",
                           "kunin_stand_uniform")):
        frame(n, [], os.path.join(outdir, "03%s_%s.png"
                                  % ("hij"[i], n.split("_", 1)[1])))

    # --- Paalam: the wave, with the bag on ---
    for name in ("paalam_lift_1_uniform", "paalam_lift_2_uniform",
                 "paalam_lift_3_uniform",
                 "paalam_wave_in_uniform", "paalam_wave_mid_uniform",
                 "paalam_wave_out_uniform", "paalam_down_uniform"):
        frame(name, [], os.path.join(outdir, "04_%s.png" % name.split("_", 1)[1]))

    # --- Alis: out of the left of the frame, and the room goes dark ---
    # The turn, then four places along the walk, then the empty room going
    # black. The one worth looking hardest at is the third: it is the first
    # frame in which the desk is no longer in front of his legs, and it is
    # where a figure cut off at mid-shin would show it.
    for i, (name, x, black) in enumerate((
            ("alis_turn_uniform", SIT_X, 0.0),
            ("alis_walk_1_uniform", SIT_X - 0.9, 0.0),
            ("alis_walk_3_uniform", -1.6, 0.0),
            ("alis_walk_2_uniform", -5.4, 0.0),
            ("alis_walk_4_uniform", -9.4, 0.0),
            (None, ALIS_END_X, 0.9))):
        frame(name, [], os.path.join(outdir, "05%s_alis.png" % "abcdef"[i]),
              x=x, black=black)


main()
