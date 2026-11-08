"""Take the hands off the seated Kylo, so his arms go down behind the desk.

THE PROBLEM. sitting_front_uniform.png is drawn with both hands curled into
downward claws, hovering just above the desk line - fingers fused into blobs,
thumbs not really there. At the size he is now drawn in Paaralan Scene 2 they
are the first thing you look at, and they read as deformed rather than as a
boy resting his hands.

THE FIX, and why it is a cut rather than a redraw. The storyboard this scene
is matched to does not show his hands at all in the sitting panel: his arms
run down and vanish behind the desk, which is what a child sitting at a desk
actually looks like from the front. So the hands are not redrawn, they are
removed - everything below the wrist is replaced by the forearm cross-section
immediately above it, repeated down to the bottom of the canvas.

That bottom edge is the desk's far edge: BuildPaaralanScene2 pins the seated
canvas there. So the arms meet the desk exactly, with nothing dangling over
it and no gap under them. Nothing about his face, hair, glasses, uniform or
proportions is touched; the cut is below the sleeve.

IDEMPOTENT. Row 818 is forearm in both the original and the result, so
running this twice changes nothing the second time. The original is in git if
you need it back:  git checkout HEAD -- Assets/Sprites/sitting_front_uniform.png

Overwrites in place deliberately, rather than writing a new file: the sprite
is referenced by GUID from Chapter2_Level2_Klasrum.unity, and keeping the
same .png keeps the same GUID, so no scene reference moves and no new .meta
has to be committed alongside it.

Run from the repository root:  python Tools/fix_seated_hands.py
"""
from PIL import Image
import numpy as np

SPRITE = "Assets/Sprites/sitting_front_uniform.png"

# Just above the wrists. Measured: skin reappears below the sleeve at row 765,
# the hands widen from 830 and taper out by 885. 818 is forearm on both arms.
WRIST_ROW = 818


def main():
    im = Image.open(SPRITE).convert("RGBA")
    before = im.size
    a = np.asarray(im).copy()

    cross_section = a[WRIST_ROW].copy()
    for y in range(WRIST_ROW, a.shape[0]):
        a[y] = cross_section

    Image.fromarray(a, "RGBA").save(SPRITE)
    print(f"{SPRITE}: hands removed below row {WRIST_ROW}, canvas {before} unchanged")


if __name__ == "__main__":
    main()
