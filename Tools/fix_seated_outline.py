"""Draw back the dark outline where the seated poses lost it.

THE BUG THIS FIXES, reported from a build as "remove the white extra outline in
lapis".

There is no white outline. There is a MISSING BLACK one, which looks the same
from the player's chair: Kylo is drawn in flat cel colour with a hard dark line
all the way round him, and where that line is thin or absent his white T-shirt
runs straight out to the edge of the sprite. Against the brown chair behind him
the shirt then ends in a pale rim - and a pale rim beside a dark one on the next
frame is exactly what "an extra white outline" looks like.

MEASURED, taking the outermost solid pixel of every row whose shirt reaches the
edge, and its mean brightness:

    sitting_front_uniform    12.9     0.0% of rows pale     a proper outline
    sit_frame_1_uniform      13.4     0.0%
    place_frame_3_uniform    11.7     0.0%
    lapis_frame_1..3         20-26    0.0%
    kuha_frame_1_uniform     36.3    11.2%
    reach_bag_uniform        44.1     6.5%
    pencil_ready_uniform     47.5     4.7%                  four times as pale

pencil_ready_uniform is the pose "Lapis" ENDS on and sits in for the rest of the
scene, which is why that beat is the one that got reported. The poses either
side of it are fine, so the frames flicker between an outlined boy and a
half-outlined one.

They came off different generations, which is where the difference comes from.
This does not re-cut or re-fit them - it finds the pale pixels on the silhouette
and paints them the colour the outline already is everywhere else on the same
sprite.

WHAT IT CHANGES AND WHAT IT DOES NOT. Alpha is never written, so the silhouette
is identical to the pixel. Only pixels that are ON the silhouette edge AND
bright AND unsaturated are touched - the white shirt at the boundary. His skin,
his hair, his glasses, the pencil and everything inside him are left alone,
because none of them is both pale and on the edge.

IDEMPOTENT: a second run finds the edge already dark and does nothing.

Overwrites in place deliberately, so the GUIDs the scene references do not move.
The originals are in git.

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

Run from the repository root:  python Tools/fix_seated_outline.py
"""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Sprites")

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

SOLID = 250

# How deep the restored outline goes. The drawn outline is three or four pixels
# on this canvas, so two is a repair rather than a redraw - enough to stop the
# shirt reaching the edge, not enough to fatten a line that is already there.
DEPTH = 2

# What counts as "the shirt rather than the outline". His outline sits under 90;
# the shirt is over 230. Anything above this on the edge is shirt that should
# have had a line drawn round it.
PALE = 150

# And it has to be colourless, so a bright edge of skin or of the pencil is
# never mistaken for shirt.
MAX_SAT = 40


def outline_colour(a):
    """The colour this sprite's own outline is, where it has one."""
    al = a[:, :, 3]
    lum = a[:, :, :3].mean(2)
    edge = (al >= SOLID) & ndimage.binary_dilation(al < SOLID, iterations=2)
    dark = edge & (lum < 90)
    if dark.sum() < 50:
        return np.array([20, 20, 20])
    return np.median(a[:, :, :3][dark], axis=0)


def repair(path):
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im).astype(int)
    al = a[:, :, 3]
    lum = a[:, :, :3].mean(2)
    sat = a[:, :, :3].max(2) - a[:, :, :3].min(2)

    # Solid pixels that touch anything not solid - the silhouette itself.
    # Measured against alpha 250 rather than against full transparency: these
    # sprites were cut with a soft rim two or three pixels deep, so "next to
    # the transparent part" starts further out than the edge you can see, and
    # looking for it there found one pixel of the hundreds that are wrong.
    edge = (al >= SOLID) & ndimage.binary_dilation(al < SOLID, iterations=DEPTH)
    pale = edge & (lum > PALE) & (sat < MAX_SAT)
    before = int(pale.sum())
    if not before:
        return 0, 0.0

    ink = outline_colour(a)
    a[:, :, :3][pale] = ink

    Image.fromarray(a.astype(np.uint8), "RGBA").save(path)
    return before, float(lum[edge].mean())


def main():
    print("%-26s %10s %12s" % ("sprite", "pale edge", "edge lum was"))
    total = 0
    for name in POSES:
        path = os.path.join(SPRITES, name + ".png")
        if not os.path.exists(path):
            print("  %-24s not here, skipped" % name)
            continue
        painted, was = repair(path)
        total += painted
        print("  %-24s %10s %12s"
              % (name, painted if painted else "-", "%.1f" % was if painted else "clean"))
    print("\n%d edge pixels given their outline back. Alpha untouched, so nothing "
          "has to be rebuilt - Unity re-importing these is enough." % total)


main()
