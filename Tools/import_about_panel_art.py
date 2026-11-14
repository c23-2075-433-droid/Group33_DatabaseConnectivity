"""Cut the About popup's Canva sheet into the one sprite it is.

One page at 2000x1125, like the other UI sheets - but unlike them it arrives
already keyed, with real transparency around the board instead of the solid
white the result and settings pages came on. So there is no background to
find here and no edge to key: the whole import is a crop and a bleed.

    crop   to the drawing's own bounds. The page is mostly empty, and a
           sprite that carried that emptiness would be a rectangle the
           builder has to position by eye - the Image's rect would be the
           PAGE, not the board. Cropping to the alpha bounds makes the
           sprite and the board the same thing, which is what lets the
           builder anchor the X and the version as plain fractions of it.
           Kylo's raised fists stick out past the frame on the left; they
           are part of the drawing, so they are part of the crop.

    bleed  the transparent pixels come through BLACK, and a resampler mixes
           them into the edge like any other neighbour. The board is drawn
           at about 85% of the screen height and never at 1:1, so it is
           always resized - without this it picks up a dark rim. Same
           treatment, same reason, as fix_ui_sprite_alpha.py gives the
           badge and the arrow.

The result is 794x941: PORTRAIT, and taller than the canvas is at 16:9. That
is a fact about the artwork the builder has to deal with, not a problem to
fix here by squashing it - see SettingsAboutDialogBuilder.BuildAbout, which
fits the board to whatever screen it lands on.

Run from the repository root:  python3 Tools/import_about_panel_art.py
Reads  Art/about_panel_raw.png
Writes Assets/Sprites/ui_about_panel.png
"""
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import bleed_edges  # noqa: E402

PAGE = os.path.join('Art', 'about_panel_raw.png')
OUT = os.path.join('Assets', 'Sprites', 'ui_about_panel.png')

# Deeper than the 6 the character art uses. This sprite is drawn small and
# the filter reaches further in sprite pixels than it does in screen ones.
BLEED_ROUNDS = 16


def main():
    page = Image.open(PAGE).convert('RGBA')
    box = page.split()[3].getbbox()
    if box is None:
        sys.exit('%s: the page is entirely transparent' % PAGE)

    board = bleed_edges(page.crop(box), rounds=BLEED_ROUNDS)
    board.save(OUT, optimize=True)
    print('%s  %dx%d  (cropped from %s at %s)'
          % (OUT, board.width, board.height, PAGE, box))


if __name__ == '__main__':
    main()
