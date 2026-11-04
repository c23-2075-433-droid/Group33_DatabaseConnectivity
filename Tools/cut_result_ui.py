"""Cut the redrawn level-summary board, its star arc, and the Words Learned board.

Two Canva exports replace the summary screen: a sheet holding the board, the
banner, the buttons and ten stars, and a second board for a panel the game has
never had, where a child reads back the Filipino words they just used.

Most of it is plain crops. The stars are not, and that is what this file is
mostly about.

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
    Art/result_pieces_new_raw.png       board, banner, buttons, close X, both star rows
    Art/words_learned_board_raw.png     the Words Learned board

Writes:
    Assets/Sprites/ui_result_board.png
    Assets/Sprites/ui_result_title_complete.png
    Assets/Sprites/btn_result_replay.png
    Assets/Sprites/btn_result_continue.png
    Assets/Sprites/btn_result_close.png
    Assets/Sprites/star_gold_1.png  ... star_gold_5.png
    Assets/Sprites/star_grey_1.png  ... star_grey_5.png
    Assets/Sprites/ui_words_board.png
"""

import os

import numpy as np
from PIL import Image

PIECES_RAW = "Art/result_pieces_new_raw.png"
WORDS_RAW = "Art/words_learned_board_raw.png"
OUT = "Assets/Sprites"

# Boxes measured off the sheet: left, upper, right, lower, end-exclusive.
BOARD_BOX = (1094, 112, 1887, 1053)
TITLE_BOX = (141, 632, 802, 825)
REPLAY_BOX = (172, 933, 476, 1044)
CONTINUE_BOX = (550, 933, 854, 1044)
CLOSE_BOX = (925, 53, 1052, 181)
WORDS_BOX = (506, 64, 1300, 1005)

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
    for p in (PIECES_RAW, WORDS_RAW):
        if not os.path.exists(p):
            raise SystemExit(f"missing {p} - stage the Canva exports in Art/ first")
    os.makedirs(OUT, exist_ok=True)

    sheet = Image.open(PIECES_RAW).convert("RGBA")
    words = Image.open(WORDS_RAW).convert("RGBA")

    print("boards and buttons:")
    board = save(trim(sheet.crop(BOARD_BOX)), "ui_result_board",
                 "TOTAL POINTS and WORDS COMPLETED are painted in")
    save(trim(sheet.crop(TITLE_BOX)), "ui_result_title_complete", "LEVEL COMPLETE banner")
    save(trim(sheet.crop(REPLAY_BOX)), "btn_result_replay", "")
    save(trim(sheet.crop(CONTINUE_BOX)), "btn_result_continue", "")
    save(trim(sheet.crop(CLOSE_BOX)), "btn_result_close", "")
    wb = save(trim(words.crop(WORDS_BOX)), "ui_words_board", "five row plates painted in")

    print("star arc, lit:")
    gold = cut_stars(sheet, GOLD_ROW, "star_gold")
    print("star arc, unlit:")
    grey = cut_stars(sheet, GREY_ROW, "star_grey")

    print()
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
