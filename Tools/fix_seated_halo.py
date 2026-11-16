"""Take the white ring off the seated poses.

THE BUG THIS FIXES, reported from a build as "remove the excess white colors".

Each seated pose was cut out of a white Canva generation. Cutting sets alpha,
not colour, so every part-transparent pixel around the figure still holds the
WHITE it was before - and when Unity scales the sprite, its filter samples
those pixels and the white comes back as a pale ring round his whole body. On
the classroom background that ring is what reads as a sticker pasted on rather
than a boy sitting in the room.

MEASURED, mean luminance of the part-transparent rim against the solid artwork
just inside it:

    standing_uniform_with_bag     rim  14 vs inside  68     -54   his outline
    talking_uniform_with_bag      rim  17 vs inside  85     -68   his outline
    sitting_front_uniform         rim 147 vs inside  12    +135   a white ring
    pencil_ready_uniform          rim 164                         a white ring

His standing sprites come out DARKER at the edge, which is right: that edge is
the black outline the artist drew. Every seated pose comes out a hundred and
thirty brighter. That is not an outline, it is the background that was never
cleaned off.

WHY fit_seated_pose.bleed_edges DID NOT CATCH IT. That one treats any pixel
with alpha above zero as known and fills outwards from it, so it cleans the
fully transparent margin beyond the figure but never touches the rim itself -
and the rim is where the white actually is. This fills from alpha 250 instead,
so the soft edge is repainted from the solid artwork beside it. Same idea as
Tools/make_guard_frames.bleed_edges, which was written for the same ring on the
guard.

WHAT IT CHANGES AND WHAT IT DOES NOT. Alpha is never written. The silhouette,
the shape, the proportions and every pixel you can actually see are exactly
what they were; only the colour STORED underneath the transparency changes -
the colour a filter finds when it samples past the edge. Nothing is redesigned,
repainted or moved.

IDEMPOTENT: a second run finds the rim already carrying the figure's own colour
and has nothing left to spread.

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

Run from the repository root:  python Tools/fix_seated_halo.py
"""
import os

import numpy as np
from PIL import Image

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

# Only a pixel this opaque is trusted to be the artwork rather than the cut.
SOLID = 250

# How far the colour is pushed out. The rim is two or three pixels; sixteen
# also covers the margin a resampler can reach into when the sprite is scaled
# up, which on this canvas it is.
DEPTH = 16


def rim_luminance(a):
    """Mean brightness of the part-transparent edge, for the before and after."""
    alpha = a[:, :, 3]
    rim = (alpha > 20) & (alpha < SOLID - 15)
    if not rim.any():
        return float("nan")
    return float(a[:, :, :3].astype(float).mean(axis=2)[rim].mean())


def bleed(path):
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im).astype(np.float32)
    before = rim_luminance(a)

    rgb = a[:, :, :3].copy()
    alpha = a[:, :, 3]
    known = alpha >= SOLID
    if not known.any():
        return before, before

    for _ in range(DEPTH):
        if known.all():
            break
        total = np.zeros_like(rgb)
        count = np.zeros(known.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nb = np.roll(known, (dy, dx), (0, 1))
            total += np.roll(rgb, (dy, dx), (0, 1)) * nb[:, :, None]
            count += nb
        fill = (~known) & (count > 0)
        if not fill.any():
            break
        rgb[fill] = total[fill] / count[fill][:, None]
        known = known | fill

    out = np.dstack([rgb, alpha]).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(path)
    return before, rim_luminance(out.astype(np.float32))


def main():
    print("%-26s %8s %8s" % ("sprite", "rim was", "rim now"))
    for name in POSES:
        path = os.path.join(SPRITES, name + ".png")
        if not os.path.exists(path):
            print("  %-24s not here, skipped" % name)
            continue
        before, after = bleed(path)
        print("  %-24s %8.1f %8.1f" % (name, before, after))
    print("\nAlpha untouched: the silhouette is identical, so nothing has to be "
          "rebuilt - Unity re-importing these is enough.")


main()
