"""Carry the seated poses down to the bottom of their canvas, so the desk hides
the cut instead of leaving it hanging in the air.

THE BUG THIS FIXES, reported from a build as "his shirt on the lower part has
no color" and "remove the excess white colors".

Every seated pose is drawn on one 663x921 canvas whose BOTTOM EDGE is the far
edge of the desk - BuildPaaralanScene2 pins it there, and
Tools/cut_klasrum_desk_front.py lays the desk's own painted front over the top
of him, which is what hides the flat cut his arms end in.

That only works if he actually reaches the bottom of the canvas.
sitting_front_uniform does not. Its arms do - fix_seated_hands.py repeats the
forearm cross-section down to row 920 - but his TORSO stops at row 890, thirty
rows short, and the two sit frames before it stop shorter still. Thirty rows is
0.19 world units, and the desk's painted top edge is only 0.07 below where the
canvas bottom lands, so the shirt ended ABOVE the desk: a white rectangle with
no hem and no outline, floating, with the classroom visible underneath it and
his forearms apparently amputated either side.

It was always there. It is newly obvious because "Kinig" holds the idle seated
pose on screen for two or three seconds while he listens, where before the
scene only passed through it between one action and the next.

THE FIX is the one fix_seated_hands already uses on the arms: every column of
him that ends inside the cut band carries its last solid row down to the bottom
of the canvas. The added rows are BEHIND THE DESK and are never seen - they
exist so that there is something for the desk to cover.

A redrawn hem is not the answer here, and would be worse. The shirt does not
end at row 890 because a hem is drawn there, it ends because the generation was
cropped; drawing a hem would put a shirt hem halfway up his lap, above a desk
he is sitting at. What belongs there is more shirt.

IDEMPOTENT. A column that already reaches the bottom row is left alone, so this
can be re-run after any of the make_*_frames tools without compounding.

Overwrites in place deliberately: these sprites are referenced by GUID from
Chapter2_Level2_Klasrum.unity, and keeping the same .png keeps the same GUID.
The originals are in git -
    git checkout HEAD -- Assets/Sprites/sitting_front_uniform.png

RUN ORDER. These three repair the same sprites and the order is not free:

    python Tools/fix_seated_hem.py       carry him down behind the desk
    python Tools/fix_seated_outline.py   give the silhouette its dark line back
    python Tools/fix_seated_halo.py      push that line out under the soft rim
    python Tools/make_kinig_frames.py    derive the listening frames from the
                                         repaired sitting_front_uniform

The halo bleed spreads whatever colour the solid edge holds into the soft rim
outside it. Run it BEFORE the outline tool and it spreads the white shirt; run
it after and it spreads the outline. All three are idempotent, so running the
whole list again proves it.

Run from the repository root:  python Tools/fix_seated_hem.py
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

# Every pose that shares the seated canvas. Listed rather than globbed, so a
# sprite that happens to be 663x921 for some other reason is not touched.
POSES = [
    "sitting_front_uniform",
    "sit_frame_1_uniform", "sit_frame_2_uniform", "sit_frame_3_uniform",
    "kinig_frame_1_uniform", "kinig_frame_2_uniform",
    "kinig_frame_3_uniform", "kinig_frame_4_uniform",
    "kuha_frame_1_uniform", "kuha_frame_2_uniform", "kuha_frame_3_uniform",
    "reach_bag_uniform",
    "place_frame_1_uniform", "place_frame_2_uniform", "place_frame_3_uniform",
    "lapis_frame_1_uniform", "lapis_frame_2_uniform", "lapis_frame_3_uniform",
    "pencil_ready_uniform",
]

# How far up the canvas a column may end and still count as part of the cut
# rather than as something that genuinely stops there. His lap and forearms end
# between 876 and 920; the pencil he holds ends far higher and must not be
# dragged down the frame.
CUT_BAND = 840

# What counts as solid. The cut-out leaves a feathered rim a pixel or two deep,
# and smearing a half-transparent pixel down the canvas would draw a pale streak
# under the desk rather than more shirt.
SOLID = 250


def extend(path):
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im).copy()
    h, w = a.shape[:2]
    last = h - 1

    filled = 0
    for x in range(w):
        solid = np.where(a[:, x, 3] >= SOLID)[0]
        if len(solid) == 0:
            continue
        bottom = solid.max()
        if bottom < CUT_BAND or bottom >= last:
            continue
        # Carry the last solid pixel down, overwriting the feathered rim below
        # it as it goes.
        a[bottom + 1:, x] = a[bottom, x]
        filled += last - bottom

    if not filled:
        return 0
    Image.fromarray(a).save(path)
    return filled


def main():
    total = 0
    for name in POSES:
        path = os.path.join(SPRITES, name + ".png")
        if not os.path.exists(path):
            print("  %-26s not here, skipped" % name)
            continue
        added = extend(path)
        total += added
        print("  %-26s %s" % (name, ("%d px carried down" % added) if added
                              else "already reaches the desk"))
    print("\n%d pixels added, all of them behind the desk." % total)
    if total:
        print("Rebuild the scene only if sprite SIZES changed - they did not, "
              "so Unity re-importing these is enough.")


main()
