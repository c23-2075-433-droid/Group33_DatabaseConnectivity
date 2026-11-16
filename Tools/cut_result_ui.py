"""Cut the level-summary board, its two banners, the star arc, and the Words
Learned board.

Three Canva exports feed the summary screen:

    result_pieces_new_raw.png      board, LEVEL COMPLETE, buttons, X, stars
    result_pieces_labels_raw.png   the same sheet, relettered, plus TRY AGAIN!
    words_learned_board_raw.png    the vocabulary board

Two things changed on the relettered sheet. The board's second plate now reads
TOTAL ATTEMPTS: where it read WORDS COMPLETED:, because that row has always
counted tries and never words. And TRY AGAIN! is drawn beside LEVEL COMPLETE
instead of living on a sheet of its own at three times the size - a different
plate, rounder and without the planking, but the same height, so the banner
slot no longer has to hold two sizes.

Why both sheets are still read

The relettered sheet reached the repository through a lossy re-encode: against
the original it is a mean of 2 levels out, with ringing up to 17 along the
high-contrast edges the art is full of. Nothing a child would see, but there is
no reason to accept it on pieces that did not change. So the lossless sheet is
still the source for the board, the LEVEL COMPLETE banner, the buttons, the X
and the stars, and the relettered one is read only where it says something
different: the two windows below, patched into the board, and the TRY AGAIN!
plate, which exists nowhere else.

The patches are feathered rather than pasted square. Everything outside the
lettering agrees to within 8 levels, so a hard edge would very nearly vanish
anyway - but the plaque's gold is a gradient, which is exactly where a square
of re-encoded pixels shows its corners.

PLAQUE_PATCH is the board's own title plate, which used to have LEVEL COMPLETE
printed into it and is now bare. In the built panel the banner covers it: it
spans 0.17 to 0.80 of the board against the lettering's 0.22 to 0.79, which is
how a board could carry the words twice for a month without anyone seeing it.
Blank is still what the art should say, and it is what a narrower banner would
need.

Most of this is plain crops. The stars are not, and that is the rest of the
file.

The row is an arc - five stars swelling towards the middle, the outer pairs
sitting lower - so the game cannot tile one sprite five times. It places five,
each at its own size and height, which is why Assets/Editor/SceneResultPanelBuilder.cs
loads star_gold_1..5 and star_grey_1..5 rather than one star and a layout rule.

The trouble is that the five are drawn touching. Their soft shadows bridge the
gaps, and raising the alpha threshold does not separate them: at alpha > 245 the
middle three are still one blob, because the big centre star genuinely overlaps
its neighbours. So they cannot be found; they have to be cut at measured
boundaries.

Four columns do the job - 181, 336, 581, 730 - each one a minimum where only
shadow crosses. Cutting there clips a little of the big star's shadow, which
sounds worse than it is: the neighbour that cast its own shadow into that column
is drawn right beside it at its designed position, so the composited row is the
sheet, pixel for pixel. Nothing is lost that is not immediately redrawn.

Every star's artwork ends on the same baseline - the arc lives in the top edges,
not the bottom - so each one is trimmed to its own bounds and the five are
bottom-aligned when placed. That is also why this script prints each star's
width, height and centre: those are the numbers the builder's slot table wants,
and measuring them by eye in the Scene view is how a row ends up subtly crooked.

Reads:
    Art/result_pieces_new_raw.png       board, LEVEL COMPLETE, buttons, X, stars
    Art/result_pieces_labels_raw.png    the relettered board, TRY AGAIN!
    Art/words_learned_board_raw.png     the Words Learned board

Writes:
    Assets/Sprites/ui_result_board.png
    Assets/Sprites/ui_result_title_complete.png
    Assets/Sprites/ui_result_title_tryagain.png
    Assets/Sprites/btn_result_replay.png
    Assets/Sprites/btn_result_continue.png
    Assets/Sprites/btn_result_close.png
    Assets/Sprites/star_gold_1.png  ... star_gold_5.png
    Assets/Sprites/star_grey_1.png  ... star_grey_5.png
    Assets/Sprites/ui_words_board.png
"""

import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

PIECES_RAW = "Art/result_pieces_new_raw.png"
LABELS_RAW = "Art/result_pieces_labels_raw.png"
WORDS_RAW = "Art/words_learned_board_raw.png"
OUT = "Assets/Sprites"

# Boxes measured off the sheets: left, upper, right, lower, end-exclusive.
BOARD_BOX = (1094, 112, 1887, 1053)
TITLE_BOX = (141, 632, 802, 825)
REPLAY_BOX = (172, 933, 476, 1044)
CONTINUE_BOX = (550, 933, 854, 1044)
CLOSE_BOX = (925, 53, 1052, 181)
WORDS_BOX = (506, 64, 1300, 1005)

# On the relettered sheet. The board sits at the same place on both, so the
# patch windows below are in the board's own pixels.
TRYAGAIN_BOX = (141, 511, 802, 704)
PLAQUE_PATCH = (140, 14, 680, 110)      # LEVEL COMPLETE printed in -> bare
LABEL_PATCH = (70, 674, 600, 740)       # WORDS COMPLETED: -> TOTAL ATTEMPTS:
PATCH_FEATHER = 4

# The two star rows, and the four columns that divide each into five.
GOLD_ROW = (71, 308)
GREY_ROW = (308, 545)
STAR_CUTS = (59, 181, 336, 581, 730, 864)


def trim(img):
    """Crop to the art's own bounds - a sprite padded with nothing cannot be placed."""
    bbox = img.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    return img.crop(bbox) if bbox else img


def save(img, name, note):
    img.save(os.path.join(OUT, name + ".png"), "PNG", optimize=True)
    print(f"  {name:26s} {img.size[0]:4d} x {img.size[1]:4d}   {note}")
    return img


def patch(base, source, windows, feather=PATCH_FEATHER):
    """Blend the given windows of `source` into `base` through a soft mask."""
    mask = Image.new("L", base.size, 0)
    draw = ImageDraw.Draw(mask)
    for x0, y0, x1, y1 in windows:
        draw.rectangle((x0, y0, x1 - 1, y1 - 1), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(feather))
    return Image.composite(source, base, mask)


def label_right_edge(board):
    """Where each printed label's last pixel sits, as a fraction of the board.

    The figures the builder prints are right-aligned on the bare plate that
    follows the lettering, so how much room each one has is a measurement off
    this art and not a taste. Printing it here is what keeps the builder's
    ValueWidthFrac pair honest when the lettering is re-exported.
    """
    a = np.asarray(board.convert("RGBA"))
    w, h = board.size
    cream = (a[..., :3].min(2) > 170) & (a[..., 3] > 128)
    edges = []
    for lo, hi in ((0.55, 0.68), (0.68, 0.82)):
        band = cream[int(lo * h):int(hi * h), :int(0.9 * w)]
        xs = np.where(band.any(0))[0]
        edges.append(xs.max() / w if len(xs) else float("nan"))
    return edges


def cut_stars(sheet, row, prefix):
    """Five stars from one row, each trimmed, with its placement numbers printed."""
    y0, y1 = row
    sizes = []
    for i in range(5):
        x0, x1 = STAR_CUTS[i], STAR_CUTS[i + 1]
        star = trim(sheet.crop((x0, y0, x1, y1)))
        # Where this star sits across the row, so the builder can place it
        # without anyone measuring in the Scene view.
        centre = (x0 + x1) * 0.5 - (STAR_CUTS[0] + STAR_CUTS[5]) * 0.5
        save(star, f"{prefix}_{i + 1}", f"centre {centre:+7.1f} of the row")
        sizes.append(star.size)
    return sizes


def main():
    for p in (PIECES_RAW, LABELS_RAW, WORDS_RAW):
        if not os.path.exists(p):
            raise SystemExit(f"missing {p} - stage the Canva exports in Art/ first")
    os.makedirs(OUT, exist_ok=True)

    sheet = Image.open(PIECES_RAW).convert("RGBA")
    labels = Image.open(LABELS_RAW).convert("RGBA")
    words = Image.open(WORDS_RAW).convert("RGBA")

    print("boards and buttons:")
    board = save(trim(patch(sheet.crop(BOARD_BOX), labels.crop(BOARD_BOX),
                            (PLAQUE_PATCH, LABEL_PATCH))),
                 "ui_result_board",
                 "TOTAL POINTS and TOTAL ATTEMPTS are painted in")
    save(trim(sheet.crop(TITLE_BOX)), "ui_result_title_complete", "LEVEL COMPLETE banner")
    save(trim(labels.crop(TRYAGAIN_BOX)), "ui_result_title_tryagain",
         "TRY AGAIN! banner, the same height")
    save(trim(sheet.crop(REPLAY_BOX)), "btn_result_replay", "")
    save(trim(sheet.crop(CONTINUE_BOX)), "btn_result_continue", "")
    save(trim(sheet.crop(CLOSE_BOX)), "btn_result_close", "")
    wb = save(trim(words.crop(WORDS_BOX)), "ui_words_board", "five row plates painted in")

    print("star arc, lit:")
    gold = cut_stars(sheet, GOLD_ROW, "star_gold")
    print("star arc, unlit:")
    grey = cut_stars(sheet, GREY_ROW, "star_grey")

    points_edge, attempts_edge = label_right_edge(board)
    print()
    print(f"  the printed labels end at {points_edge:.4f} (TOTAL POINTS:) and "
          f"{attempts_edge:.4f} (TOTAL ATTEMPTS:)")
    print("  of the board; SceneResultPanelBuilder's ValueWidthFrac and")
    print("  AttemptsValueWidthFrac are the room that leaves, measured from these.")
    print(f"  boards: result {board.size[0]}x{board.size[1]}, "
          f"words {wb.size[0]}x{wb.size[1]}"
          + ("  (same aspect)" if abs(board.size[0] / board.size[1]
                                      - wb.size[0] / wb.size[1]) < 0.01 else
             "  (DIFFERENT aspect - the two panels will not match)"))
    print("  the unlit stars are drawn a touch larger than the lit ones, by design:")
    for i, (g, e) in enumerate(zip(gold, grey), 1):
        print(f"    slot {i}: lit {g[0]:3d}x{g[1]:3d}   unlit {e[0]:3d}x{e[1]:3d}")

    print("\nNow run Tools > SALINLAHI > Restyle Scene Result Panels in Unity. The")
    print("panel is baked into all fifteen chapter scenes, so new art alone changes")
    print("nothing until that has run over them.")


if __name__ == "__main__":
    main()
