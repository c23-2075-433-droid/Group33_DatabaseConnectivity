"""Cut the three "Upo" in-between frames out of one generated strip.

WHY THEY EXIST. "Upo" used to be one picture replacing another: the standing
Kylo switched off and the seated Kylo switched on in the same frame, with
PoseSettle easing the arrival to take the worst of the edge off. PoseSettle's
own comment calls that "the cheap nine-tenths" and says the real fix is drawn
in-between frames. These are those frames.

WHERE THE ART CAME FROM. Art/sit_frames_raw.png is one Canva generation -
three poses in a row on a white background, made from sitting_front_uniform
as the design reference. One generation rather than three, which is three
times less of the four-minute cooldown and, more usefully, three poses drawn
in one pass so they match each other.

WHAT THIS SCRIPT DOES.
  1. Splits the strip on the empty columns between the figures.
  2. Cuts each background with the shared routine in fit_seated_pose, which
     knows not to eat his white shirt.
  3. Normalises each frame so its HAIR CAP matches sitting_front_uniform's.
     The generator drew the three at slightly different scales - 195, 186 and
     186 pixels of hair - and a head that changes size between frames is the
     one thing that would give the animation away.
  4. Places each so his CROWN sits at a chosen world height, stepping down
     +2.55, +2.28, +2.00 towards the seated pose's +1.735. His crown falling
     while the desk hides his legs is what reads as sitting down.

The canvas, 663x921, and its bottom edge pinned to the desk's far edge, are
the same as every other seated pose, so the frames drop straight into the
same renderer.

Writes  Assets/Sprites/sit_frame_1..3_uniform.png

Run from the repository root:  python Tools/make_sit_frames.py
"""
import os
import sys

from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, hair_cap          # noqa: E402

RAW = "Art/sit_frames_raw.png"
BASE = "Assets/Sprites/sitting_front_uniform.png"
OUT = "Assets/Sprites/sit_frame_{}_uniform.png"

# Where each frame's crown sits, in world units. The seated pose that follows
# puts its crown at +1.735, so this is a descent that hands over cleanly.
CROWNS = [2.55, 2.28, 2.00]

SEATED_HEIGHT = 5.609
SEAT_FEET_Y = -3.009


def split_columns(img):
    """Column spans of the separate figures, found on the empty gaps."""
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


def main():
    src = Image.open(RAW).convert("RGB")
    spans = split_columns(src)
    if len(spans) != len(CROWNS):
        sys.exit(f"expected {len(CROWNS)} figures in {RAW}, found {len(spans)}: {spans}")

    base = Image.open(BASE).convert("RGBA")
    b_crown, b_bottom, b_cx = hair_cap(base)
    base_hair = b_bottom - b_crown
    unit = SEATED_HEIGHT / base.size[1]
    canvas_top = SEAT_FEET_Y + SEATED_HEIGHT

    for i, (x0, x1) in enumerate(spans):
        # Pad with white: cut_background walls off the border, and that wall
        # needs background to bite on rather than the figure itself.
        crop = src.crop((max(0, x0 - 30), 0, min(src.size[0], x1 + 31), src.size[1]))
        padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
        padded.paste(crop, (0, 30))
        tmp = f"Art/_sit_tmp_{i}.png"
        padded.save(tmp)
        try:
            cut = cut_background(tmp)
        finally:
            os.remove(tmp)

        c, b, cx = hair_cap(cut)
        scale = base_hair / (b - c)

        a = np.asarray(cut)
        opaque = a[:, :, 3] > 60
        rows = np.where(opaque.any(axis=1))[0]
        cols = np.where(opaque.any(axis=0))[0]
        content = cut.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))
        scaled = content.resize((int(round(content.size[0] * scale)),
                                 int(round(content.size[1] * scale))), Image.LANCZOS)

        out = Image.new("RGBA", base.size, (0, 0, 0, 0))
        x = int(round(b_cx - (cx - cols[0]) * scale))
        y = int(round((canvas_top - CROWNS[i]) / unit))
        out.alpha_composite(scaled, (x, y))
        path = OUT.format(i + 1)
        out.save(path)
        print(f"{path}: hair {b - c} -> {base_hair}px (x{scale:.3f}), "
              f"crown at world {CROWNS[i]:+.2f} (row {y})")


if __name__ == "__main__":
    main()
