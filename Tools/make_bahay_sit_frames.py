"""Refit Paaralan Scene 2's "Upo" frames for Bahay Scene 5's table.

WHY REFIT RATHER THAN DRAW NEW ONES. The breakfast scene faked sitting by
moving the standing drawing down behind the table, and generated crouches
made for it did not read as sitting. The classroom already has the pose:
sit_frame_1..3_uniform and sitting_front_uniform, four front-facing drawings
of this same boy lowering himself onto a chair, upright and relaxed with his
hands in his lap.

They were passed over once for being "a near-portrait for the close desk
shot". That was the wrong reason. The two scenes differ in how near the
camera is, and nothing else about the drawing cares: scaled so his HEAD
matches, a classroom frame covers the breakfast scene's visible band almost
to the pixel. The camera distance was never the obstacle.

    the drawing must reach 395px below the crown to meet the table edge
    sitting_front_uniform, scaled to match the head, reaches 396

The real cost is the backpack: the classroom Kylo has his bag off at his
desk. So he sets it down to eat, and all four seated frames are bagless.
Standing still wears it, which is the one visible seam, and it is the seam
the player is told about - he is sitting down to breakfast.

HOW THEY ARE FITTED.

Scale: by hair cap, to talking_uniform_with_bag's 142px, so his head is the
same size as the standing drawing it replaces. That is x0.513, a reduction,
so nothing is upscaled.

Horizontally: his hair's centre column the same distance right of the canvas
centre as the standing sprite's, so his head does not shift sideways.

Vertically: all four share one ink BOTTOM row, which is what puts his feet on
one floor and lets the fold do the rest. As he lowers, each drawing reaches
less far below his crown - 466, 444, 421, 396 once scaled - so pinning the
bottom makes his crown fall 70px across the run on its own. That is 0.74
world units of head drop, drawn rather than faked.

The bottom row is chosen so the LAST frame's crown lands exactly where the
seated pose's crown lands today, at world y +2.21 - the height Bahay Scene 5
was tuned to, where the table edge cuts him at the hip. The scene's own 1.9
unit step down to Kylo_Seated is untouched and still carries the rest of the
descent; this only distributes the last stretch of it across the drawings.

That forces the canvas height: frame 1's crown has to fit above the others,
which needs 885px. The width is whatever the widest frame needs.

Each PNG is written with a .meta cloned from talking_uniform_with_bag's, so
they import at 120 PPU with a centre pivot like every other character
sprite. Without that they take the project default and come out wrong.

Run from the repository root:  python3 Tools/make_bahay_sit_frames.py
Then re-run Tools > SALINLAHI > Build Bahay Scene 5 in Unity.
"""
import io
import os
import sys
import uuid

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import hair_cap  # noqa: E402

SPRITES = "Assets/Sprites/"
BASE = SPRITES + "talking_uniform_with_bag.png"

# classroom source -> the name Bahay Scene 5's builder asks for
FRAMES = [
    ("sit_frame_1_uniform", "sit_frame_1_bahay"),
    ("sit_frame_2_uniform", "sit_frame_2_bahay"),
    ("sit_frame_3_uniform", "sit_frame_3_bahay"),
    ("sitting_front_uniform", "sitting_front_bahay"),
]

CANVAS_H = 885          # forced by frame 1's crown; see the note above
PPU, SCALE = 120.0, 1.27
SIT_PIVOT_Y = -1.73     # BuildBahayScene5.SitPivotY
SEATED_CROWN_Y = 2.213  # where the seated crown lands today, and must still


def measure(im):
    """(hair height, hair centre column, crown row, lowest ink row)."""
    crown, bottom, col = hair_cap(im)
    ink = im.convert("RGBA").split()[3].getbbox()
    return bottom - crown, col, crown, ink[3] - 1


def main():
    base = Image.open(BASE).convert("RGBA")
    bw = base.size[0]
    base_hair, base_col, _crown, _feet = measure(base)
    head_dx = base_col - bw / 2.0

    # Load and scale every frame first: the shared bottom row cannot be
    # chosen until the tallest one is known.
    prepared = []
    for src, name in FRAMES:
        im = Image.open(SPRITES + src + ".png").convert("RGBA")
        hair, _col, _cr, _ft = measure(im)
        s = float(base_hair) / hair
        im = im.resize((round(im.size[0] * s), round(im.size[1] * s)), Image.LANCZOS)
        box = im.split()[3].getbbox()
        im = im.crop(box)
        hair, col, crown, feet = measure(im)
        prepared.append((name, im, col, crown, feet, feet - crown))

    # The last frame's crown has to land where the seated crown lands now.
    dest_reach = prepared[-1][5]
    dest_crown_row = CANVAS_H / 2.0 - (SEATED_CROWN_Y - SIT_PIVOT_Y) / SCALE * PPU
    bottom_row = dest_crown_row + dest_reach
    width = max(im.size[0] for _n, im, _c, _cr, _f, _r in prepared) + 24

    print("%s: hair %dpx, head %+.1f from canvas centre" % (
        os.path.basename(BASE), base_hair, head_dx))
    print("canvas %dx%d, every frame's feet on row %.0f\n" % (width, CANVAS_H, bottom_row))

    meta = io.open(BASE + ".meta", encoding="utf-8").read()

    for name, im, col, crown, feet, reach in prepared:
        canvas = Image.new("RGBA", (width, CANVAS_H), (0, 0, 0, 0))
        x = int(round(width / 2.0 + head_dx - col))
        y = int(round(bottom_row - feet))
        canvas.alpha_composite(im, (x, y))

        out = SPRITES + name + ".png"
        canvas.save(out)

        guid = uuid.uuid4().hex
        m = meta.replace("84e37c7a6289499a92ac93742e431552", guid)
        m = m.replace("talking_uniform_with_bag_0", name + "_0")
        io.open(out + ".meta", "w", encoding="utf-8").write(m)

        crown_row = y + crown
        crown_world = SIT_PIVOT_Y + (CANVAS_H / 2.0 - crown_row) / PPU * SCALE
        print("%-22s reach %3d  crown row %3d  crown at world y %+.2f  guid %s"
              % (name, reach, crown_row, crown_world, guid[:8]))


if __name__ == "__main__":
    main()
