"""Cut the level-summary UI kit into sprites.

This replaces Tools/cut_summary_ui.py, which had to take the same interface
apart from a finished mock-up - wiping painted text off the board, prising
stars off the wood they sat on, masking medallions by guessing they were
circles. The kit does not need any of that: every piece is already drawn on
its own, on one flat slate background, so a single colour key lifts all of
them cleanly.

The pieces are kept SEPARATE rather than composed. A button is its wooden
frame plus a glyph laid on top in Unity, so the same frame serves Replay and
Main Menu and a glyph can be swapped without recutting anything.

Reads   Art/summary_kit_raw.webp
Writes  Assets/Sprites/ui_summary_*.png and btn_summary_*.png

Run from the repository root:  python Tools/cut_summary_kit.py
"""
import os
from PIL import Image, ImageFilter

SRC = "Art/summary_kit_raw.webp"
OUT = "Assets/Sprites/"

# The sheet's flat backdrop, and how far a pixel must sit from it to count as
# artwork. The band between the two gives anti-aliased edges a soft alpha
# instead of a staircase.
BACKDROP = (49, 55, 60)
CLEAR, SOLID = 26.0, 58.0

# Measured off the sheet at 1024x1024.
PIECES = {
    # the board everything sits on, already empty
    "ui_summary_board": (28, 26, 482, 285),
    "ui_summary_plaque": (503, 40, 657, 172),
    "ui_summary_title": (36, 293, 494, 364),

    # statistic symbols, drawn flat rather than on a disc
    "ui_summary_icon_words": (36, 396, 162, 497),
    "ui_summary_icon_tries": (190, 386, 306, 499),
    "ui_summary_icon_learned": (350, 396, 470, 497),

    # button parts
    "ui_summary_btn_frame": (695, 178, 812, 274),
    "ui_summary_glyph_replay": (518, 396, 620, 494),
    "ui_summary_glyph_home": (666, 398, 782, 494),
    "btn_summary_next": (830, 406, 987, 484),

    # word art for the captions
    "ui_summary_word_replay": (43, 743, 162, 790),
    "ui_summary_word_home": (198, 743, 384, 790),
    "ui_summary_word_next": (410, 743, 587, 790),

    # a lit star and an unearned one, from the row that shows both
    "ui_summary_star_on": (190, 626, 260, 694),
    "ui_summary_star_off": (343, 626, 412, 694),
}


# Pieces whose cream outer stroke is shaved off. The kit draws a light keyline
# around each shape; against the game's own backgrounds it reads as a white
# halo rather than as part of the art. Measured at about four pixels on the
# 1024 sheet, so that is how far the alpha is pulled in. Lettering and glyphs
# are left alone - their outline is what keeps them readable on wood.
ERODE = {
    "ui_summary_board": 4,
    "ui_summary_plaque": 4,
    "ui_summary_btn_frame": 4,
    "btn_summary_next": 4,
    "ui_summary_star_on": 4,
    "ui_summary_star_off": 4,
}


def key(img, box, erode=0):
    """Lift one piece off the flat backdrop, optionally shaving its keyline."""
    piece = img.crop(box).convert("RGBA")
    w, h = piece.size
    px = piece.load()
    mask = Image.new("L", (w, h))
    mpx = mask.load()
    br, bg, bb = BACKDROP
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            dist = ((r - br) ** 2 + (g - bg) ** 2 + (b - bb) ** 2) ** 0.5
            if dist <= CLEAR:
                mpx[x, y] = 0
            elif dist >= SOLID:
                mpx[x, y] = 255
            else:
                mpx[x, y] = int(255 * (dist - CLEAR) / (SOLID - CLEAR))
    if erode > 0:
        # MinFilter(2k+1) pulls the mask in by k. Blurring after keeps the new
        # edge soft instead of leaving a cut-paper staircase.
        mask = mask.filter(ImageFilter.MinFilter(erode * 2 + 1))
        mask = mask.filter(ImageFilter.GaussianBlur(0.8))

    piece.putalpha(mask)
    bbox = piece.getbbox()
    return piece.crop(bbox) if bbox else piece


def main():
    if not os.path.exists(SRC):
        print("No %s - copy the kit there first." % SRC)
        return

    src = Image.open(SRC).convert("RGB")
    if src.size != (1024, 1024):
        src = src.resize((1024, 1024), Image.LANCZOS)

    for name, box in sorted(PIECES.items()):
        piece = key(src, box, ERODE.get(name, 0))
        piece.save(OUT + name + ".png")
        print("%-28s %s" % (name, piece.size))


if __name__ == "__main__":
    main()
