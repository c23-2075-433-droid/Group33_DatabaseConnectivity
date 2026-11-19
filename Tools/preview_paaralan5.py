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

# The rates, copied from BuildPaaralanScene5 (Pace 0.65). Used by --gif.
PACE = 0.65
KUNIN_FPS, PAALAM_FPS, PAALAM_WAVE_FPS = 9 * PACE, 8 * PACE, 9 * PACE
ALIS_FPS, ALIS_WALK = 7 * PACE, 3.2

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


KUNIN = ["tayo_stand_uniform", "kunin_look_canva", "kunin_look_canva",
         "kunin_reach_canva", "kunin_reach_canva", "kunin_lift_canva",
         "kunin_shoulder_canva", "kunin_straps_canva", "kunin_straps_canva",
         "kunin_wear_canva"]
KUNIN_BAG_UNTIL = 5      # the desk bag goes on kunin_lift
PAALAM_UP = ["kunin_wear_canva", "paalam_raise_canva", "paalam_up_canva"]
PAALAM_WAVE = ["paalam_out_canva", "paalam_up_canva", "paalam_in_canva",
               "paalam_up_canva"]
# No paalam_up at the head of this: the wave's last frame IS paalam_up, and
# repeating it here is the hitch the builder's tail no longer has.
PAALAM_DOWN = ["paalam_raise_canva", "kunin_wear_canva"]
ALIS_TURN = ["kunin_wear_canva", "alis_turn_canva", "alis_side_canva",
             "alis_step_canva"]
ALIS_CYCLE = ["alis_walk_%d_canva" % i for i in (1, 2, 3, 4)]


def gif(outdir):
    """The three beats as animated GIFs, at the builder's own rates.

    Stills show whether a frame is right; only the frames played at speed
    show whether the ACTION is - a hand that jumps, a body that slides, a
    walk whose legs and travel disagree.
    """
    tmp = os.path.join(outdir, "_gif.png")
    bag = [("prop_bag_desk_uniform", (BAG_X, BAG_DESK_BASE_Y, BAG_DESK_H))]

    def shot(pose, desk=(), x=SIT_X, black=0.0):
        frame(pose, list(desk), tmp, x=x, black=black)
        return Image.open(tmp).convert("RGB").resize((W // 2, H // 2), Image.LANCZOS)

    def write(name, shots):
        ims = [s for s, _ in shots]
        ims[0].save(os.path.join(outdir, name), save_all=True, append_images=ims[1:],
                    duration=[int(d * 1000) for _, d in shots], loop=0)
        print("  wrote", name)

    hold = 0.8
    k = [(shot(n, bag if i < KUNIN_BAG_UNTIL else ()), 1 / KUNIN_FPS)
         for i, n in enumerate(KUNIN)]
    k[0] = (k[0][0], hold)
    k[-1] = (k[-1][0], hold)
    write("kunin.gif", k)

    up = [(shot(n), 1 / PAALAM_FPS) for n in PAALAM_UP]
    up[0] = (up[0][0], hold)
    wave = [(shot(n), 1 / PAALAM_WAVE_FPS) for n in PAALAM_WAVE] * 3
    down = [(shot(n), 1 / PAALAM_FPS) for n in PAALAM_DOWN]
    down[-1] = (down[-1][0], hold)
    write("paalam.gif", up + wave + down)

    a = [(shot(n), 1 / ALIS_FPS) for n in ALIS_TURN]
    a[0] = (a[0][0], hold)
    steps = int(round(ALIS_WALK * ALIS_FPS))
    for i in range(steps + 1):
        t = i / float(steps)
        x = SIT_X + (ALIS_END_X - SIT_X) * t
        a.append((shot(ALIS_CYCLE[i % 4], x=x), 1 / ALIS_FPS))
    for b in (0.35, 0.7, 1.0):
        a.append((shot(None, black=b), 0.3))
    write("alis.gif", a)
    os.remove(tmp)


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "preview_p5")
    os.makedirs(outdir, exist_ok=True)
    if "--gif" in sys.argv:
        gif(outdir)
        return
    lesson = [("prop_notebook_open", NOTEBOOK), ("prop_aklat_closed", AKLAT),
              ("prop_lapis", PENCIL)]
    bag_on_desk = [("prop_bag_desk_uniform",
                    (BAG_X, BAG_DESK_BASE_Y, BAG_DESK_H))]

    print("Paaralan Scene 5, as the numbers in the builder place it:")
    frame("sitting_front_uniform", lesson, os.path.join(outdir, "01_open.png"))
    frame("tayo_stand_uniform", [], os.path.join(outdir, "02_tayo.png"))

    # --- Kunin: the bag stands on the desk until the lift, then is his ---
    for i, n in enumerate(dict.fromkeys(KUNIN)):
        frame(n, bag_on_desk if KUNIN.index(n) < KUNIN_BAG_UNTIL else [],
              os.path.join(outdir, "03%s_%s.png" % ("abcdefghij"[i], n)))

    # --- Paalam: the wave, with the bag on ---
    for i, n in enumerate(dict.fromkeys(PAALAM_UP + PAALAM_WAVE)):
        frame(n, [], os.path.join(outdir, "04%s_%s.png" % ("abcdef"[i], n)))

    # --- Alis: out of the left of the frame, and the room goes dark ---
    # The turn, then four places along the walk, then the empty room going
    # black. The third walking frame is the first with the desk no longer in
    # front of his legs, which is where a figure cut off at mid-shin would
    # show it.
    shots = [(n, SIT_X, 0.0) for n in ALIS_TURN[1:]] + [
        ("alis_walk_1_canva", SIT_X - 0.9, 0.0),
        ("alis_walk_3_canva", -1.6, 0.0),
        ("alis_walk_2_canva", -5.4, 0.0),
        ("alis_walk_4_canva", -9.4, 0.0),
        (None, ALIS_END_X, 0.9)]
    for i, (name, x, black) in enumerate(shots):
        frame(name, [], os.path.join(outdir, "05%s_alis.png" % "abcdefgh"[i]),
              x=x, black=black)


main()
