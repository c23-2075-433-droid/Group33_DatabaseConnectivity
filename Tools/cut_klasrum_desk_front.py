"""Cut the near desk out of the classroom painting, to draw IN FRONT of Kylo.

THE PROBLEM THIS SOLVES. The seated poses are cut off flat at the bottom of
their canvas - fix_seated_hands.py even does it deliberately, replacing his
hands with the forearm cross-section so his arms "run down behind the desk".
That only works if something actually covers the cut. Nothing does: Kylo is
drawn at sorting order 0 over the background, so the slice lands ON TOP of
the painted desk and you can see it.

What it looks like in the game:
  "Upo"    his forearms end in flat stumps and his shirt in a straight line,
           which reads as the character being cropped.
  "Lapis"  the sliced shirt hem shows as two white slabs either side of his
           shorts, sitting on the wood - the "excess white beside his arms".

Both are the same cut seen against two different poses.

WHY NOT JUST MOVE HIM. There is nowhere to move him to. His slice sits at
world -2.967 and the desk's painted top edge runs between -2.92 and -3.00
depending where you measure - it is a receding surface, not a straight line.
Lower him and the slice is drawn further down the wood, which is worse;
raise him and bare background shows under him. He is in front of the desk
either way, and that is the whole difficulty.

WHY NOT klasrum_desk_front. That sprite exists and was used here once. It
was cut from the OLD wide classroom shot - a whole front row of desks - and
in this locker-corner view it lies across the bottom third of a picture it
does not belong to and cuts him off at the neck.

WHAT THIS DOES INSTEAD. Takes the near desk out of THIS room's own painting,
so it matches by construction: same artwork, same perspective, same light.
Written to the full 1680x944 canvas with everything above the desk
transparent, so the builder can give it the background's exact transform and
the two register pixel for pixel with no arithmetic to get wrong.

FINDING THE EDGE. Per column, the topmost wood pixel at or below row 715.
The row limit matters: the chair back is the same wood and sits higher, and
without it the cut would take the chair too and draw it over Kylo's face.

Writes  Assets/Sprites/klasrum_desk_front_near.png

Run from the repository root:  python Tools/cut_klasrum_desk_front.py
"""
from PIL import Image, ImageFilter
import numpy as np

SRC = "Assets/Sprites/klasrum_interior_background.png"
OUT = "Assets/Sprites/klasrum_desk_front_near.png"

# Below the chair back, above the desk's far edge. The desk's top runs from
# about row 727 to row 734; the chair is well above both.
SEARCH_FROM = 715


def main():
    im = Image.open(SRC).convert("RGB")
    a = np.asarray(im).astype(int)
    h, w, _ = a.shape

    wood = (a[:, :, 0] > 175) & ((a[:, :, 0] - a[:, :, 2]) > 45)

    keep = np.zeros((h, w), dtype=bool)
    cols = 0
    for x in range(w):
        ys = np.where(wood[SEARCH_FROM:, x])[0]
        if len(ys) == 0:
            continue
        top = SEARCH_FROM + ys[0]
        keep[top:, x] = True
        cols += 1

    out = np.zeros((h, w, 4), dtype=np.uint8)
    out[:, :, :3] = a
    out[:, :, 3] = np.where(keep, 255, 0)
    img = Image.fromarray(out, "RGBA")

    # A hair of feathering on the boundary, so the join reads as the painted
    # edge rather than as a mask. Any more and the cut shows through it.
    alpha = img.getchannel("A").filter(ImageFilter.GaussianBlur(0.6))
    img.putalpha(alpha)

    img.save(OUT)
    covered = int(keep.sum())
    print("%s  %dx%d" % (OUT, w, h))
    print("  desk found in %d of %d columns, covering %.1f%% of the canvas"
          % (cols, w, 100.0 * covered / (w * h)))
    rows = np.where(keep.any(axis=1))[0]
    xs = np.where(keep.any(axis=0))[0]
    PX = 87.407
    print("  spans world x %+.2f..%+.2f, y %+.2f..%+.2f"
          % ((xs[0] - 840) / PX, (xs[-1] - 840) / PX,
             (472 - rows[-1]) / PX, (472 - rows[0]) / PX))


if __name__ == "__main__":
    main()
