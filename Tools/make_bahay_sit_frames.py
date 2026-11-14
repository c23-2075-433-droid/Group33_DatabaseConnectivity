"""Cut Bahay Scene 5's "Upo" frames out of the single-figure generations.

WHY THIS IS NOT make_sit_frames.py. That one cuts Paaralan Scene 2's frames:
a near-portrait of a boy at a desk, no backpack, on the 663x921 canvas every
seated pose in that scene shares, placed by crown height because the desk
hides his legs. Bahay Scene 5 is a different shot - a full figure seen across
a breakfast table, with the bag still on - so it needs its own frames and its
own fitting. See Docs/AnimationStandard.md.

WHERE THE ART CAME FROM. Three single-figure square generations, each made
from talking_uniform_with_bag as the design reference:

    Art/sit_crouch_bahay_raw.png   knees bent, hips back, arms hanging
    Art/sit_seated_bahay_raw.png   lower, both hands arriving on his thighs

One figure per square image, not a strip. A strip of four on a 16:9 canvas
gave 95px of hair where 142 is wanted, and two figures in one square came
back standing at identical heights, because the generator composes for the
frame rather than for a shared ground line. Alone in a square each figure
gets 228px of hair, so these scale DOWN into the scene.

HOW THEY ARE FITTED, and why it is by the CROWN rather than the feet.

talking_uniform_with_bag is 292x745 at 120 pixels per unit with the pivot at
the canvas centre, and the drawing fills it: crown on the top edge, feet on
the bottom one.

The obvious fit is feet on the floor, letting a shorter crouching figure drop
its own crown. That would be wrong here, because the scene ALREADY carries
the descent: the builder puts Kylo_Seated 1.9 units below Kylo_Standing, and
that offset was tuned against the painting until "the table edge cuts him at
the hip and his head reaches y +2.2". Baking a second drop into the art would
count it twice and sink him through the table.

So every frame is placed crown on the top edge, like the base sprite, with
its hair centre the same distance right of the canvas centre. A sprite swap
then changes the DRAWING and nothing else, and the scene keeps the framing
somebody already checked by eye. The descent stays where it was: the 1.9 unit
step, eased by PoseSettle.

His drawn feet end up well above the canvas bottom, which does not show - the
table hides everything below y -1.97, and that is the same reason the scene
could fake sitting with a standing drawing in the first place.

Each PNG is written with a .meta cloned from talking_uniform_with_bag's, so
the frames import at 120 PPU with a centre pivot like every other character
sprite. Without that they take the project default and come out the wrong
size in the scene.

Run from the repository root:  python3 Tools/make_bahay_sit_frames.py
Then re-run Tools > SALINLAHI > Build Bahay Scene 5 in Unity.
"""
import io
import os
import sys
import uuid

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, hair_cap  # noqa: E402

SPRITES = "Assets/Sprites/"
BASE = SPRITES + "talking_uniform_with_bag.png"

# raw generation -> sprite name the scene builder asks for
FRAMES = [
    ("Art/sit_crouch_bahay_raw.png", "sit_frame_1_uniform_with_bag"),
    ("Art/sit_seated_bahay_raw.png", "sitting_uniform_with_bag"),
]


def hair_height(im):
    """Hair cap height. hair_cap returns (crown, bottom, centre COLUMN) - the
    third value is a column, and reading it as a height is how a strip that
    was far too small once looked like it cleared the threshold."""
    crown, bottom, _col = hair_cap(im)
    return bottom - crown


def geometry(im):
    """(hair height, hair centre column, crown row) for a cut figure."""
    crown, bottom, col = hair_cap(im)
    return bottom - crown, col, crown


def main():
    base = Image.open(BASE).convert("RGBA")
    bw, bh = base.size
    base_hair, base_col, base_crown = geometry(base)

    # What the scene is actually anchored on, as offsets from the pivot.
    head_dx = base_col - bw / 2.0
    print("%s: %dx%d  hair %dpx  head %+.1f from centre  crown row %d"
          % (os.path.basename(BASE), bw, bh, base_hair, head_dx, base_crown))

    meta = io.open(BASE + ".meta", encoding="utf-8").read()

    for raw, name in FRAMES:
        fig = cut_background(raw).convert("RGBA")
        hair, _col, _crown = geometry(fig)

        scale = float(base_hair) / hair
        fig = fig.resize((max(1, round(fig.size[0] * scale)),
                          max(1, round(fig.size[1] * scale))), Image.LANCZOS)

        # Crop to the ink, so a mostly empty 1024px generation does not
        # become a mostly empty 1024px sprite.
        box = fig.split()[3].getbbox()
        fig = fig.crop(box)
        crown2, _bottom2, col2 = hair_cap(fig)

        width = max(bw, fig.size[0] + 24)
        if (width - bw) % 2:
            width += 1                 # keep the widening symmetric
        height = bh

        canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        x = int(round(width / 2.0 + head_dx - col2))
        y = int(round(base_crown - crown2))
        canvas.alpha_composite(fig, (x, y))

        out = SPRITES + name + ".png"
        canvas.save(out)

        # Its own GUID, or two sprites share one and Unity drops a scene
        # reference. The sub-asset name has to follow the file name too.
        guid = uuid.uuid4().hex
        m = meta.replace("84e37c7a6289499a92ac93742e431552", guid)
        m = m.replace("talking_uniform_with_bag_0", name + "_0")
        io.open(out + ".meta", "w", encoding="utf-8").write(m)

        print("%-32s %dx%d  hair %d->%d  crown row %d (base %d)  guid %s"
              % (name, width, height, hair, hair_height(canvas),
                 hair_cap(canvas)[0], base_crown, guid[:8]))


if __name__ == "__main__":
    main()
