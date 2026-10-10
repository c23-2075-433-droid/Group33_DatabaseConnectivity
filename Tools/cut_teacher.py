"""Cut Teacher Rachel out of the main-menu mockup.

Paaralan Scene 2 asks the child to say "Kumusta" when Kylo walks into the
classroom. Scene 1 already made the argument for why that needs a person on
screen: greeting an empty room "teaches the word in a vacuum", which is why a
guard was put on the school gate.

There was no teacher sprite, but there was already a teacher drawn in the
project's own style: menu_background.png, the main-menu mockup, has her
standing beside Kylo with a book under one arm and the other hand raised in a
wave. A wave is exactly the gesture "Kumusta" wants.

She cannot be lifted by colour the way the bathroom items were - her cream
blouse is the same family as the warm wall behind her - so the silhouette is
traced by hand instead, as a polygon in source pixels, and the mask is then
eroded by a pixel to pull in the light halo the trace leaves along her edge.

Two things the trace deliberately gives up:

  * Her legs. The mockup crops her at the hip, so there are none to take. She
    is therefore a WAIST-UP sprite, and Scene 2 stands her BEHIND her desk,
    where a waist-up figure is all you would see anyway.
  * A sliver of hair on her left. Kylo stands against that side in the mockup
    and their hair is the same brown, with no edge between them to follow. The
    cut takes her side of the guess.

menu_background.png itself is left untouched - it is the design mockup and is
still wanted as a reference.

Writes:  Assets/Sprites/char_teacher.png

Run from the repository root:  python Tools/cut_teacher.py
"""
from PIL import Image, ImageDraw, ImageFilter

SRC = "Assets/Sprites/menu_background.png"
DST = "Assets/Sprites/char_teacher.png"

# Her silhouette in source pixels, clockwise from the top of her head.
SILHOUETTE = [
    # hair and head
    (424, 252), (452, 243), (486, 243), (516, 254), (540, 276), (558, 306),
    (570, 348), (576, 398), (574, 440), (568, 475), (564, 495),
    # up the inside of the raised arm, round the fingers, down the outside
    (572, 470), (578, 440), (584, 424), (592, 414), (600, 411), (609, 414),
    (617, 411), (626, 416), (634, 426), (642, 448), (646, 470), (640, 492),
    (628, 510), (616, 536), (604, 566), (596, 596), (590, 620),
    # shoulder and the right side of her blouse
    (606, 634), (618, 664), (624, 700),
    # cut straight across at the hip - the desk hides this edge
    (624, 706), (430, 706),
    # back up her left side, keeping clear of Kylo's hair
    (416, 660), (414, 600), (418, 540), (415, 480), (400, 440), (394, 390),
    (398, 330), (410, 276),
]


def main():
    src = Image.open(SRC).convert("RGBA")

    mask = Image.new("L", src.size, 0)
    ImageDraw.Draw(mask).polygon(SILHOUETTE, fill=255)

    # Pull the edge in by two pixels. The traced line sits just outside her
    # outline, so without this she carries a pale rim of the wall behind her,
    # which reads as a halo once she is composited onto the green blackboard.
    mask = mask.filter(ImageFilter.MinFilter(5))
    # Then soften what is left, so the edge is not a staircase.
    mask = mask.filter(ImageFilter.GaussianBlur(0.6))

    out = Image.new("RGBA", src.size, (0, 0, 0, 0))
    out.paste(src, (0, 0), mask)
    out = out.crop(out.getbbox())
    out.save(DST)
    print("char_teacher.png", out.size)


if __name__ == "__main__":
    main()
