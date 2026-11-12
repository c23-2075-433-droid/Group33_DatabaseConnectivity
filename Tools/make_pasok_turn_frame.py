"""Cut the three-quarter turn frame that bridges "Pasok".

WHAT IT IS, and why only one frame. "Pasok" switched Kylo from his side
profile straight to the back-facing walk - one frame, a quarter turn. This is
the view in between: shoulders angled away, most of the backpack showing, a
sliver of cheek and the rim of his glasses past the side of his head.

The strip it comes from has three figures - side, three-quarter, back - but
only the MIDDLE one is used. The outer two already exist as
standing_uniform_with_bag and back_walk_1_uniform_with_bag, and they were
asked for so the generation could be judged: if the generator held the
character steady across a rotation whose endpoints we can check, the middle
frame can be trusted. It did.

That also makes this the opposite trade from the guard, where three generated
frames beat two pops. Here the endpoints are real Kylo and only the bridge is
new.

THE ALIGNMENT, which is the fiddly part. BuildPaaralanScene1 places the
player sprite by the CENTRE of its canvas, so where his feet sit inside that
canvas decides where he stands. Measured across the existing sprites, they do
not agree:

    standing_uniform_with_bag      feet 28.5px right of centre
    back_walk_1_uniform_with_bag   feet roughly centred
    walk_frame_1..4                feet roughly centred

Because the offset in world units is (feet_px - width/2) * PlayerScale /
SpritePPU, only that pixel difference matters, not the canvas width. So
standing stands about 0.12 world units right of where the back-facing frames
put him, and Kylo ALREADY hops sideways at the moment "Pasok" switches him.
That is not introduced here; it is what the single-frame switch has always
done.

This frame is fitted to match STANDING, so the new join - profile to
three-quarter - is clean. The old hop survives, moved one frame later to the
three-quarter-to-back change, where he has started moving and the walk covers
it better than a standing turn would.

Writes  Assets/Sprites/pasok_turn_uniform_with_bag.png

Run from the repository root:  python Tools/make_pasok_turn_frame.py
"""
import os
import sys

from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, split_columns          # noqa: E402

RAW = "Art/pasok_turn_raw.png"
REF = "Assets/Sprites/standing_uniform_with_bag.png"
OUT = "Assets/Sprites/pasok_turn_uniform_with_bag.png"

TARGET_HEIGHT = 745      # every standing sprite in the project
WHICH = 1                # the middle figure of three, counting from 0


def feet_offset(im):
    """How far right of the canvas centre his feet sit, in pixels."""
    a = np.asarray(im.convert("RGBA"))
    op = a[:, :, 3] > 60
    band = op[int(im.size[1] * 0.92):]
    cols = np.where(band.any(axis=0))[0]
    if len(cols) == 0:
        return 0.0
    return (cols[0] + cols[-1]) / 2.0 - im.size[0] / 2.0


def main():
    ref = Image.open(REF).convert("RGBA")
    want = feet_offset(ref)
    print("reference %s: %s, feet %+.1fpx from centre"
          % (os.path.basename(REF), ref.size, want))

    src = Image.open(RAW).convert("RGB")
    spans = split_columns(src)
    if len(spans) < WHICH + 1:
        sys.exit("expected at least %d figures in %s, found %d"
                 % (WHICH + 1, RAW, len(spans)))

    x0, x1 = spans[WHICH]
    crop = src.crop((max(0, x0 - 30), 0, min(src.size[0], x1 + 31), src.size[1]))
    padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
    padded.paste(crop, (0, 30))
    tmp = RAW + ".__turn.png"
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

    scale = float(TARGET_HEIGHT) / content.size[1]
    w = max(1, int(round(content.size[0] * scale)))
    scaled = content.resize((w, TARGET_HEIGHT), Image.LANCZOS)

    # Pad so his feet end up the same distance right of centre as the
    # standing pose's, which is what makes the switch into this frame
    # seamless. Padding on one side moves the centre by half of it, so the
    # pad needed is twice the correction.
    here = feet_offset(scaled)
    shift = want - here
    pad = int(round(abs(shift) * 2))
    canvas_w = w + pad
    out = Image.new("RGBA", (canvas_w, TARGET_HEIGHT), (0, 0, 0, 0))
    out.alpha_composite(scaled, (pad if shift > 0 else 0, 0))
    out.save(OUT)

    print("%s  %dx%d  feet %+.1fpx from centre (wanted %+.1f)"
          % (os.path.basename(OUT), canvas_w, TARGET_HEIGHT,
             feet_offset(out), want))


if __name__ == "__main__":
    main()
