"""Cut the three "Bangon" in-between frames out of one Canva strip.

WHY THEY EXIST. The same reason Tools/make_sit_frames.py gives for the "Upo"
frames, and the same reason PoseSettle's comment gives before it: a pose
switched on while another is switched off is two pictures, and easing the
transform between them does not make it a movement. WakeUpSequence had taken
a transform as far as it goes - the sleeping pose dissolves into the sitting
one while the sitting one swings up about his seat - and it still read as a
drawing being moved, because nothing INSIDE the drawing ever changed. These
are the frames where it changes.

WHAT TO GENERATE IN CANVA. One design, three poses in a row, exactly the way
the "Upo" frames were made: three in one generation rather than three
generations, which is three times less cooldown and - the part that matters -
three poses drawn in one pass, so they match each other. 1680x944 is the size
the other strips in this project use and it is plenty.

Use lying_down_barefoot and sitting_up_barefoot as the design reference, and
ask for the three stages BETWEEN them:

    Same boy, same cartoon style, side view facing right, plain white
    background, three poses evenly spaced in a row, full body, nothing
    cropped, same size in every pose. Black spiky hair, dark green
    t-shirt, navy shorts, barefoot. He is waking up in bed, seen from the
    side, and these are three stages of him sitting up:

      1. Still mostly lying on his side, but propped on one elbow, head
         lifted off the pillow, eyes still shut.
      2. Halfway up, pushing off the mattress with one hand behind him,
         back curved, eyes squinting open, knees starting to draw up.
      3. Almost upright, knees drawn up, one fist rubbing an eye,
         wearing his glasses, mouth open in a yawn.

Glasses on the LAST pose only. He has none asleep and a pair in
sitting_up_barefoot, so that is where they go on; the dissolve into the
sitting pose covers the rest of the change of view.

Save the download as Art/wake_frames_raw.png.

WHAT THIS SCRIPT DOES, which is make_sit_frames' job with two differences.

  1. Splits the strip on the empty columns between the figures.
  2. Cuts each background with the shared routine in fit_seated_pose, which
     knows not to eat a pale shirt.
  3. Normalises each frame so its HAIR CAP matches the sleeping pose's. The
     generator never draws three poses at quite the same scale, and a head
     that changes size between frames is the one thing that gives an
     animation away.
  4. Places each by the BOTTOM of the figure rather than by its crown, which
     is where make_sit_frames and this part company. Those frames were a
     seated body descending, so the crown was the thing to control. This is a
     body pivoting up off a mattress: what stays put is where he touches the
     bed, and the crown is the thing that moves most. The feet line steps from
     the sleeping pose's -0.785 to the sitting pose's -1.215, which is not a
     drop into the floor - it is the 0.43 that PlayerMovement sinks him by on
     "Bangon" so that what rests on the mattress is his backside and not his
     toes.
  5. Writes a frame 0 as well, which needs no Canva: the sleeping pose
     itself, re-canvassed onto the same frame size. That is what lets
     WakeUpSequence play the whole thing from one renderer at one transform,
     with no geometry left in the C# at all.

Everything lands on ONE canvas whose centre is the sitting pose's own centre,
so a frame shown at the transform "Bangon" leaves him at is already in the
right place.

Writes  Assets/Resources/Wake/wake_frame_0..3.png and their .meta files.

Resources, not Assets/Sprites, and the reason is worth recording: nothing in a
saved scene refers to WakeUpSequence - it is attached at run time, like the
HUD and the music - so it cannot hold an Inspector reference to a sprite.
Resources.Load is the only way a run-time object gets at art. The .meta files
are copied from the sleeping pose's own importer settings, because
SpriteImportFixer only watches Assets/Sprites: a PNG that arrives here as a
plain Texture rather than a Sprite loads as null, and a null sprite is the
white-box bug that fixer exists to prevent.

Run from the repository root:

    python3 Tools/import_wakeup_frames.py

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy and Pillow live.
"""
import hashlib
import os
import re
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, hair_cap          # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "Art", "wake_frames_raw.png")
SLEEPING = os.path.join(ROOT, "Assets", "Sprites", "lying_down_barefoot.png")
OUTDIR = os.path.join(ROOT, "Assets", "Resources", "Wake")
OUT = os.path.join(OUTDIR, "wake_frame_{}.png")

# The character art is 120 pixels to the unit and Level 1 scales him by 0.830,
# so this is what one pixel of a frame is worth on screen.
PPU = 120.0
CHARACTER_SCALE = 0.8300743
UNIT = CHARACTER_SCALE / PPU

# Measured off the scene and the art - see the commit that added the seat
# pivot. The sleeping pose is centred at y 0.27 and stands 2.110 units tall,
# so it rests on -0.785; "Bangon" leaves the sitting pose centred at 0.860,
# 4.150 tall, with its feet at -1.215.
SLEEPING_CENTRE_Y = 0.27
SEATED_CENTRE_Y = 0.860
MATTRESS_Y = -0.785
SEATED_FEET_Y = -1.215

# Where the bottom of each in-between sits, stepping between those two.
FEET = [-0.8925, -1.0, -1.1075]

# One canvas for every frame, centred on the sitting pose's centre. Wide
# enough for him lying down (6.1 units of art), tall enough for him sitting
# up, with a little margin so a generated pose that reaches further than
# expected is not clipped.
CANVAS = (820, 760)


def main():
    os.makedirs(OUTDIR, exist_ok=True)

    sleeping = Image.open(SLEEPING).convert("RGBA")
    base_crown, base_bottom, _ = hair_cap(sleeping)
    base_hair = base_bottom - base_crown
    print("  sleeping pose: hair cap %d px, art %dx%d" % (base_hair, *sleeping.size))

    # Frame 0 is the sleeping pose itself, moved onto the shared canvas.
    place(sleeping, MATTRESS_Y, 0)

    if not os.path.exists(RAW):
        print("\n  No %s yet - frame 0 is written and the other three are not."
              % os.path.relpath(RAW, ROOT))
        print("  WakeUpSequence needs all four before it will play frames; until")
        print("  then it falls back to the swing. See the top of this file for")
        print("  what to generate in Canva.")
        return 0

    strip = Image.open(RAW).convert("RGB")
    spans = split_columns(strip)
    if len(spans) != len(FEET):
        sys.exit("expected %d figures in %s, found %d: %s"
                 % (len(FEET), os.path.relpath(RAW, ROOT), len(spans), spans))

    for i, (x0, x1) in enumerate(spans):
        cut = cut_one(strip, x0, x1, i)

        crown, bottom, _ = hair_cap(cut)
        scale = base_hair / float(bottom - crown)
        print("  frame %d: hair %d -> %d px (x%.3f)" % (i + 1, bottom - crown, base_hair, scale))

        place(cut, FEET[i], i + 1, scale)

    print("\n  now let Unity reimport. WakeUpSequence picks the frames up on its own.")
    return 0


def split_columns(img):
    """Column spans of the separate figures, found on the empty gaps.

    The same scan make_sit_frames uses, and it wants the same thing of the
    art: a plain background and a clear gap between the poses.
    """
    a = np.asarray(img.convert("RGB")).astype(int)
    ink = (a.min(axis=2) < 225).sum(axis=0)
    spans, start = [], None
    for x, v in enumerate(ink):
        if v > 2 and start is None:
            start = x
        if v <= 2 and start is not None:
            if x - start > 40:
                spans.append((start, x - 1))
            start = None
    if start is not None and len(ink) - start > 40:
        spans.append((start, len(ink) - 1))
    return spans


def cut_one(strip, x0, x1, i):
    """One figure, lifted off the background.

    Padded with white first: cut_background walls off the border, and that
    wall needs background to bite on rather than the figure itself.
    """
    crop = strip.crop((max(0, x0 - 30), 0, min(strip.size[0], x1 + 31), strip.size[1]))
    padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
    padded.paste(crop, (0, 30))

    tmp = os.path.join(ROOT, "Art", "_wake_tmp_%d.png" % i)
    padded.save(tmp)
    try:
        return cut_background(tmp)
    finally:
        os.remove(tmp)


def place(pose, feet_y, index, scale=1.0):
    """Puts one pose on the shared canvas, standing on a given world line.

    Centred horizontally, because he does not travel across the bed - every
    pose in the sequence is drawn over the same hips - and sat on feet_y
    vertically, because what stays still in a sit-up is the part touching the
    mattress.
    """
    content = trim(pose)
    if scale != 1.0:
        content = content.resize((max(1, int(round(content.size[0] * scale))),
                                  max(1, int(round(content.size[1] * scale)))),
                                 Image.LANCZOS)

    w, h = CANVAS
    canvas_top_y = SEATED_CENTRE_Y + (h * UNIT) / 2.0

    x = int(round(w / 2.0 - content.size[0] / 2.0))
    y = int(round((canvas_top_y - (feet_y + content.size[1] * UNIT)) / UNIT))

    if y < 0 or y + content.size[1] > h or x < 0:
        print("  ! frame %d does not fit the canvas - widen CANVAS" % index)

    out = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    out.alpha_composite(content, (max(0, x), max(0, y)))

    path = OUT.format(index)
    out.save(path)
    write_meta(path)
    print("  -> %-44s feet at %+.3f" % (os.path.relpath(path, ROOT), feet_y))


def trim(im):
    a = np.asarray(im)
    opaque = a[:, :, 3] > 60
    rows = np.where(opaque.any(axis=1))[0]
    cols = np.where(opaque.any(axis=0))[0]
    return im.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))


def write_meta(path):
    """Gives a frame the sleeping pose's own import settings, with its own id.

    Two things would otherwise go wrong. A new PNG arrives at 100 pixels to
    the unit while the character art is 120, so every frame would be a fifth
    too big; and outside Assets/Sprites nothing reimports it as a Sprite, so
    Resources.Load<Sprite> would hand back null.
    """
    template = open(SLEEPING + ".meta", encoding="utf-8").read()
    name = os.path.splitext(os.path.basename(path))[0]

    digest = hashlib.sha1(name.encode()).hexdigest()
    internal = int(digest[32:48], 16) % (2 ** 62)

    out = re.sub(r"^guid: .*$", "guid: " + digest[:32], template, count=1, flags=re.M)
    out = re.sub(r"213: -?\d+", "213: -%d" % internal, out, count=1)
    out = out.replace("lying_down_barefoot_0", name + "_0")
    out = re.sub(r"^  spriteName: .*$", "  spriteName: " + name, out, flags=re.M)

    open(path + ".meta", "w", encoding="utf-8").write(out)


if __name__ == "__main__":
    sys.exit(main())
