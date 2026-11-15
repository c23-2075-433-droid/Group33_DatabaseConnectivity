"""Draw what Paaralan Scene 4 will look like, without opening Unity.

WHY. BuildPaaralanScene4 places about twenty objects by world coordinate -
a desk at -3.009, a bin whose foot is below the bottom of the frame, a
wrapper that has to leave a drawn hand at exactly the point the drawing had
it - and every one of those numbers is wrong until something shows it is
right. Opening the editor and running the menu item is the real test, and it
is a slow one to iterate on.

This composites the same sprites at the same world coordinates through the
same camera and writes PNGs. It is NOT the game: it knows nothing about
PoseSequence, sorting layers beyond the order it draws things in, or the
voice UI. What it checks is the thing most likely to be wrong, which is
whether everything lands where the arithmetic says.

KEEP THE NUMBERS IN STEP. They are copied from BuildPaaralanScene4.cs and
there is nothing keeping them honest but this sentence: change one there and
change it here, or this draws a scene that no longer exists.

Writes PNGs into the path given as the first argument, or ./preview_p4 if
none is given.

Run from the repository root:  python Tools/preview_paaralan4.py [outdir]
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

# --- copied from BuildPaaralanScene4.cs ---
SIT_X, SIT_FEET_Y, SEATED_H = 2.86, -3.009, 5.609

NOTEBOOK = (2.86, -3.84, 0.83)         # x, baseY, height
AKLAT = (5.30, -3.84, 1.00)
PENCIL = (3.60, -3.35, 0.30)
BOTTLE = (0.52, -3.62, 1.55)
BAON = (2.86, -3.86, 1.45)
BASURA = (4.45, -3.60, 0.70)

BIN_X, BIN_FOOT_Y, BIN_H, BIN_MOUTH_UP = -1.30, -6.55, 4.10, 0.825

HAND_HIGH = (1.77, -1.09)
HAND_CHEST = (2.09, -1.59)
BAG_MOUTH = (4.01, -2.58)


def world_to_px(x, y):
    """Where a world point lands in the rendered image."""
    return (x + VIEW_W / 2) * PX, (VIEW_H / 2 - y) * PX


def load(name):
    path = os.path.join(SPRITES, name + ".png")
    if not os.path.exists(path):
        return None
    return Image.open(path).convert("RGBA")


def paste_by_height(canvas, name, cx, base_y, height):
    """A sprite scaled to `height` world units, standing with its foot at base_y."""
    im = load(name)
    if im is None:
        print("  MISSING", name)
        return
    w = height * im.size[0] / im.size[1]
    px = int(w * PX)
    py = int(height * PX)
    im = im.resize((max(1, px), max(1, py)), Image.LANCZOS)
    left, top = world_to_px(cx - w / 2, base_y + height)
    canvas.alpha_composite(im, (int(left), int(top)))


def paste_full(canvas, name):
    """A full-frame sprite - the background, or the desk cut from it."""
    im = load(name)
    if im is None:
        print("  MISSING", name)
        return
    s = max(H / im.size[1], W / im.size[0])
    im = im.resize((int(im.size[0] * s), int(im.size[1] * s)), Image.LANCZOS)
    canvas.alpha_composite(im, ((W - im.size[0]) // 2, (H - im.size[1]) // 2))


def mark(canvas, x, y, label):
    """A cross at a world point, for the hand positions the throws start at."""
    from PIL import ImageDraw
    d = ImageDraw.Draw(canvas)
    px, py = world_to_px(x, y)
    d.line([(px - 14, py), (px + 14, py)], fill=(255, 40, 40, 255), width=3)
    d.line([(px, py - 14), (px, py + 14)], fill=(255, 40, 40, 255), width=3)
    d.text((px + 18, py - 8), label, fill=(255, 40, 40, 255))


def frame(pose, on_desk, out, marks=(), flying=None):
    """One moment of the scene: a pose of Kylo's and what is on the desk."""
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    paste_full(canvas, "klasrum_interior_background")
    # The bin is beside the desk and nearer the camera than it is.
    paste_by_height(canvas, "prop_basurahan", BIN_X, BIN_FOOT_Y, BIN_H)
    paste_by_height(canvas, pose, SIT_X, SIT_FEET_Y, SEATED_H)
    paste_full(canvas, "klasrum_desk_front_near")
    for name, (x, base, height) in on_desk:
        paste_by_height(canvas, name, x, base, height)
    if flying is not None:
        name, (x, y), height = flying
        im = load(name)
        if im is not None:
            w = height * im.size[0] / im.size[1]
            left, top = world_to_px(x - w / 2, y + height / 2)
            canvas.alpha_composite(
                im.resize((int(w * PX), int(height * PX)), Image.LANCZOS),
                (int(left), int(top)))
    for x, y, label in marks:
        mark(canvas, x, y, label)
    canvas.convert("RGB").save(out)
    print("  wrote", os.path.basename(out))


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "preview_p4")
    os.makedirs(outdir, exist_ok=True)

    lesson = [("prop_notebook_open", NOTEBOOK), ("prop_aklat_closed", AKLAT),
              ("prop_lapis", PENCIL), ("prop_bote", BOTTLE)]
    bottle_only = [("prop_bote", BOTTLE)]
    bin_mouth_y = BIN_FOOT_Y + BIN_H * BIN_MOUTH_UP

    print("Paaralan Scene 4, as the numbers in the builder place it:")
    # The opening shot, with the hand positions and the bin's mouth marked.
    frame("sitting_front_uniform", lesson,
          os.path.join(outdir, "01_open.png"),
          marks=[HAND_HIGH + ("hand high",), HAND_CHEST + ("hand chest",),
                 BAG_MOUTH + ("bag",), (BIN_X, bin_mouth_y, "bin mouth")])
    # Ligpit, mid-way: the notebook is in his hands and off the desk.
    frame("place_frame_2_uniform",
          [("prop_aklat_closed", AKLAT), ("prop_lapis", PENCIL), ("prop_bote", BOTTLE)],
          os.path.join(outdir, "02_ligpit.png"))
    # Ligpit done, Kuha: the baon arrives on a cleared desk.
    frame("kuha_baon_2_uniform", bottle_only, os.path.join(outdir, "03_kuha.png"))
    frame("sitting_front_uniform", bottle_only + [("prop_baon_open", BAON)],
          os.path.join(outdir, "04_baon_on_desk.png"))
    # Kain, at his mouth.
    frame("kain_bite_1_uniform", bottle_only + [("prop_baon_empty", BAON)],
          os.path.join(outdir, "05_kain.png"))
    # Inom: the bottle is in his hand, so it is off the desk.
    frame("inom_drink_1_uniform", [("prop_baon_empty", BAON)],
          os.path.join(outdir, "06_inom.png"))
    # The wrapper on the desk, which is where Kain leaves it.
    frame("sitting_front_uniform",
          bottle_only + [("prop_baon_empty", BAON), ("prop_basura", BASURA)],
          os.path.join(outdir, "06b_wrapper_on_desk.png"))
    # Tapon: cocked, with the wrapper still drawn in his hand.
    frame("tapon_cock_uniform", bottle_only + [("prop_baon_empty", BAON)],
          os.path.join(outdir, "07_tapon_cock.png"))
    # And in flight, at the top of the arc and arriving in the bin.
    mid = ((HAND_HIGH[0] + BIN_X) / 2,
           (HAND_HIGH[1] + bin_mouth_y) / 2 + 1.25)
    frame("place_frame_3_uniform", bottle_only + [("prop_baon_empty", BAON)],
          os.path.join(outdir, "08_tapon_flight.png"),
          flying=("prop_basura", mid, 0.70))
    frame("sitting_front_uniform", bottle_only + [("prop_baon_empty", BAON)],
          os.path.join(outdir, "09_tapon_land.png"),
          flying=("prop_basura", (BIN_X, bin_mouth_y), 0.70))


main()
