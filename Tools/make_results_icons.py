"""Draw the icons for the level-complete panel's three statistics.

The panel used to show nothing but two lines of plain text. The reference
mock-up the group liked shows each figure on its own row, behind a round dark
medallion - but its figures (total score, time, gems collected) belong to a
different kind of game. SALINLAHI has no gems, no timer, and no points: what
it knows at the end of a level is how many Filipino words the child said
correctly, how many tries that took, and which words they were.

So the medallions here carry those three:

  ui_stat_words.png     a speech bubble   - words said correctly
  ui_stat_tries.png     a microphone      - voice attempts
  ui_stat_learned.png   a sparkle         - the words practised

plus a star pair for the rating row, earned on accuracy rather than on speed:

  ui_star_on.png
  ui_star_off.png

Drawn rather than generated, because these are flat symbols a few hundred
pixels wide. The palette is read off ui_results_panel.png so they sit on the
bamboo board rather than on top of it.

Run from the repository root:  python Tools/make_results_icons.py
"""
import math
from PIL import Image, ImageDraw, ImageFilter

OUT = "Assets/Sprites/"
SIZE = 256                      # medallions
STAR = 192

# Sampled from ui_results_panel.png.
DARK = (74, 42, 22)             # the medallion disc, and the panel's deep shadow
DARK_RIM = (104, 62, 34)
CREAM = (247, 231, 196)         # the glyph, matching the panel's light wood
GOLD = (236, 180, 62)
GOLD_DEEP = (198, 140, 32)
STAR_OFF = (96, 62, 38)
STAR_OFF_RIM = (124, 84, 52)


def disc():
    """A medallion with its glyph area empty, ready to be drawn into."""
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([0, 0, SIZE - 1, SIZE - 1], fill=DARK_RIM)
    d.ellipse([7, 7, SIZE - 8, SIZE - 8], fill=DARK)
    return img, d


def speech_bubble():
    """Words said correctly."""
    img, d = disc()
    d.rounded_rectangle([58, 68, 198, 162], radius=28, fill=CREAM)
    d.polygon([(88, 158), (88, 205), (128, 162)], fill=CREAM)
    # Three dots, so it reads as speech rather than as a blank card.
    for cx in (98, 128, 158):
        d.ellipse([cx - 10, 105, cx + 10, 125], fill=DARK)
    img.save(OUT + "ui_stat_words.png")


def microphone():
    """Voice attempts."""
    img, d = disc()
    d.rounded_rectangle([104, 52, 152, 142], radius=24, fill=CREAM)      # capsule
    # The cradle: an arc, thick enough to read at panel size.
    d.arc([76, 82, 180, 170], start=0, end=180, fill=CREAM, width=14)
    d.rectangle([122, 168, 134, 196], fill=CREAM)                        # stem
    d.rounded_rectangle([92, 196, 164, 212], radius=8, fill=CREAM)       # base
    img.save(OUT + "ui_stat_tries.png")


def sparkle():
    """The words practised this level."""
    img, d = disc()

    def four_point(cx, cy, r, waist):
        return [(cx, cy - r), (cx + waist, cy - waist), (cx + r, cy),
                (cx + waist, cy + waist), (cx, cy + r), (cx - waist, cy + waist),
                (cx - r, cy), (cx - waist, cy - waist)]

    d.polygon(four_point(120, 118, 74, 20), fill=CREAM)
    d.polygon(four_point(186, 176, 34, 9), fill=CREAM)
    img.save(OUT + "ui_stat_learned.png")


def star_points(cx, cy, outer, inner):
    pts = []
    for i in range(10):
        r = outer if i % 2 == 0 else inner
        a = -math.pi / 2 + i * math.pi / 5
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def stars():
    for name, fill, rim in (("ui_star_on", GOLD, GOLD_DEEP),
                             ("ui_star_off", STAR_OFF, STAR_OFF_RIM)):
        img = Image.new("RGBA", (STAR, STAR), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        c = STAR / 2
        d.polygon(star_points(c, c + 4, c - 8, (c - 8) * 0.46), fill=rim)
        d.polygon(star_points(c, c, c - 14, (c - 14) * 0.46), fill=fill)
        if name == "ui_star_on":
            # A soft glow, so an earned star reads as earned at a glance.
            glow = Image.new("RGBA", (STAR, STAR), (0, 0, 0, 0))
            ImageDraw.Draw(glow).polygon(
                star_points(c, c, c - 10, (c - 10) * 0.46), fill=(255, 226, 140, 150))
            img = Image.alpha_composite(glow.filter(ImageFilter.GaussianBlur(9)), img)
        img.save(OUT + name + ".png")


if __name__ == "__main__":
    speech_bubble()
    microphone()
    sparkle()
    stars()
    print("wrote ui_stat_words, ui_stat_tries, ui_stat_learned, ui_star_on, ui_star_off")
