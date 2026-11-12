"""Cut the three guard wave frames out of one generated strip.

WHAT THEY ARE. The school guard at the Paaralan gate raising his hand to wave
back when the child says "Bati": arms down, half raised, waving. "Bati" used
to flip him from char_guard_idle to char_guard in a single frame, so Kylo's
wave was drawn while the man he waved at popped.

WHY ALL THREE ARE GENERATED, rather than two existing sprites with one
in-between. char_guard_idle and char_guard look like the same drawing with
the arm moved, and they are not. Searching every offset for the best
alignment of their legs and torso tops out at 0.66 overlap - they are two
separate generations, so there is no arm to cut out and swing the way
Tools/make_gate_frames.py swings the gate panels.

That left a choice. Keeping the two existing sprites as the endpoints and
using one generated frame between them gives original -> generated ->
original, which pops twice; using all three generated frames gives one
consistent drawing that animates cleanly, at the cost of the guard being a
slightly flatter version of himself. The guard appears in this scene and
nowhere else, so the second was chosen deliberately.

MATCHING THE CANVAS, which is what keeps him from changing size.
CreateStanding in BuildPaaralanScene1 scales a sprite by
worldHeight / bounds.size.y - the CANVAS height, not the ink - and places it
centred, so its feet land on FloorY only if the ink fills the canvas the way
char_guard's does. So these are written at 551x1425 with the figure occupying
rows 3..1420, exactly like the sprites they replace.

WHERE THE ART CAME FROM. Art/guard_frames_raw.png is one Canva generation
from char_guard_idle. His own appearance was described from his own
reference - cap, moustache, epaulettes, belt - rather than carried over from
any other prompt, which is the mistake that cost a generation on Kylo's hair.

Writes  Assets/Sprites/guard_frame_1..3.png

Run from the repository root:  python Tools/make_guard_frames.py
"""
import os
import sys

from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, split_columns          # noqa: E402

RAW = "Art/guard_frames_raw.png"
REF = "Assets/Sprites/char_guard_idle.png"
OUT = "Assets/Sprites/guard_frame_{}.png"

CANVAS_H = 1425
INK_TOP = 3          # where the figure starts, matching char_guard
INK_H = 1418         # and how tall it is


def fit_guard_strip(raw, out_pattern):
    """Cut a three-figure strip and write it on char_guard's canvas shape.

    Shared by the wave frames and the gate-push frames, because the thing
    that matters is the same for both: CreateStanding scales the guard by
    bounds.size.y, so every frame has to sit on a canvas the same height as
    the sprites it stands beside or he changes height mid-action.
    """
    ref = Image.open(REF).convert("RGBA")
    ra = np.asarray(ref)
    rrows = np.where((ra[:, :, 3] > 60).any(axis=1))[0]
    print("reference %s: canvas %s, ink rows %d..%d"
          % (os.path.basename(REF), ref.size, rrows[0], rrows[-1]))
    print()

    src = Image.open(raw).convert("RGB")
    spans = split_columns(src)
    if len(spans) != 3:
        sys.exit("expected 3 figures in %s, found %d" % (raw, len(spans)))

    # PASS ONE: cut and scale every figure, and find where its FEET are.
    #
    # The feet matter because CreateStanding places a sprite by the centre of
    # its canvas. Crop each frame to its own ink and the canvas centre moves
    # as the arm extends - measured on the first attempt, his feet slid from
    # x +3.26 to +2.75 across the push, over half a world unit, so he walked
    # sideways while reaching. His feet do not move in the drawing; only the
    # canvas around them did.
    figures = []
    for i, (x0, x1) in enumerate(spans, start=1):
        crop = src.crop((max(0, x0 - 30), 0, min(src.size[0], x1 + 31), src.size[1]))
        padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
        padded.paste(crop, (0, 30))
        tmp = "%s.__guard%d.png" % (raw, i)
        padded.save(tmp)
        try:
            cut = cut_background(tmp)
        finally:
            os.remove(tmp)

        a = np.asarray(cut)
        opaque = a[:, :, 3] > 60
        rows = np.where(opaque.any(axis=1))[0]
        cols = np.where(opaque.any(axis=0))[0]
        content = cut.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))

        scale = float(INK_H) / content.size[1]
        w = max(1, int(round(content.size[0] * scale)))
        scaled = content.resize((w, INK_H), Image.LANCZOS)

        # His feet: the ink in the bottom twelfth, which is shoes and nothing
        # else in any of these poses.
        sa = np.asarray(scaled)
        sop = sa[:, :, 3] > 60
        foot = sop[int(INK_H * 0.92):]
        fcols = np.where(foot.any(axis=0))[0]
        feet_cx = (fcols[0] + fcols[-1]) / 2.0 if len(fcols) else w / 2.0
        figures.append((i, scaled, feet_cx))

    # PASS TWO: one canvas wide enough for every pose, SYMMETRIC about the
    # feet, so the canvas centre is the feet in all of them. The builder
    # centres the canvas on his spot, so his feet land on that spot in every
    # frame and the arm grows out to the side instead of dragging him along.
    half = 0
    for _, scaled, feet_cx in figures:
        half = max(half, feet_cx, scaled.size[0] - feet_cx)
    canvas_w = int(round(half * 2))

    for i, scaled, feet_cx in figures:
        out = Image.new("RGBA", (canvas_w, CANVAS_H), (0, 0, 0, 0))
        out.alpha_composite(scaled, (int(round(half - feet_cx)), INK_TOP))
        path = out_pattern.format(i)
        out.save(path)
        print("%s  %dx%d  ink rows %d..%d  feet on the canvas centre"
              % (os.path.basename(path), canvas_w, CANVAS_H,
                 INK_TOP, INK_TOP + INK_H - 1))


if __name__ == "__main__":
    fit_guard_strip(RAW, OUT)
