"""Take the fringe off the sprites this redesign added.

Every .meta these importers write is copied from menu_bg.png.meta, and
menu_bg is a full-bleed background: it has no transparent pixel anywhere, so
its alphaIsTransparency: 0 never mattered. Copied onto cut-out art it matters
a great deal. With that flag off, Unity does not bleed the edge colour
outward into the transparent texels, and the moment a sprite is drawn at any
size but 1:1 the bilinear filter mixes real pixels with whatever colour is
stored under the transparency. A rim of it shows all the way round.

So this does two things, and both are needed - the flag alone still leaves
the bad colour in the file for a compressor to find, and the bleed alone
still leaves Unity free to do its own thing:

  1. BLEED. Every transparent pixel within a few of the artwork takes the
     colour of its nearest drawn neighbour. Alpha is not touched, so nothing
     looks different - it only changes what a filter finds when it reaches
     past the edge. This matters most for map_bahay_modern, which was keyed
     out of the mockup and so has the board's plank sitting under its
     transparency, ready to be smeared around the cottage.

  2. THE FLAG. alphaIsTransparency: 1 in the .meta, which is what every
     hand-made sprite in the project already has.

Safe to run again: bleeding an already-bled sprite changes nothing.

Run it LAST. The importers write their sprites fresh, bleed and all, so
re-running one of them puts the fringe back - this has to follow.

Run from the repository root:  python3 Tools/clean_sprite_edges.py
"""
from PIL import Image
import numpy as np
import io, os, re

SPRITES = 'Assets/Sprites'
DEPTH = 8          # how far the colour is carried into the transparency

# Everything the main-menu redesign added or recut. Grouped by the script
# that writes it, so a new piece lands next to its siblings.
NAMES = [
    # import_main_menu_art.py
    'menu_classroom_bg', 'btn_adventure', 'btn_minigames', 'btn_settings', 'btn_about',
    'menu_classroom_bg_blur', 'menu_title_blur', 'btn_adventure_blur',
    'btn_minigames_blur', 'btn_settings_blur', 'btn_about_blur',
    # import_nickname_dialog_art.py
    'ui_dialog_panel', 'btn_okay', 'btn_cancel',
    # import_adventure_art.py
    'ui_place_home', 'ui_place_school', 'ui_place_park',
    'ui_place_school_locked', 'ui_place_park_locked', 'ui_levels_board',
    'ui_title_adventure', 'ui_title_home', 'ui_title_school', 'ui_title_park',
    'btn_back_arrow', 'btn_levels_menu',
    # cut_modern_house.py
    'map_bahay_modern', 'map_bahay_modern_locked',
]


def bleed(im):
    """Carry the edge colour outward under the transparency.

    Returns the image and how many pixels were repainted, so a sprite that
    needed nothing can say so.
    """
    a = np.asarray(im).astype(np.int16)
    rgb, alpha = a[..., :3].copy(), a[..., 3]
    known = alpha > 8
    if not known.any() or known.all():
        return im, 0

    painted = 0
    for _ in range(DEPTH):
        # The four-neighbour sum of the colours already settled, and how many
        # of those neighbours there were.
        total = np.zeros_like(rgb)
        count = np.zeros(known.shape, np.int16)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            shifted_rgb = np.roll(rgb, (dy, dx), (0, 1))
            shifted_known = np.roll(known, (dy, dx), (0, 1))
            # np.roll wraps; a sprite's own edge must not seed the far side.
            if dy:
                shifted_known[0 if dy > 0 else -1, :] = False
            if dx:
                shifted_known[:, 0 if dx > 0 else -1] = False
            total += shifted_rgb * shifted_known[..., None]
            count += shifted_known

        fill = (~known) & (count > 0)
        if not fill.any():
            break
        rgb[fill] = (total[fill] // count[fill][..., None])
        known |= fill
        painted += int(fill.sum())

    return Image.fromarray(np.dstack([rgb, alpha]).astype(np.uint8)), painted


def set_alpha_flag(name):
    """alphaIsTransparency: 1, as every hand-made sprite here already has."""
    path = os.path.join(SPRITES, name + '.png.meta')
    if not os.path.exists(path):
        return False
    text = io.open(path, encoding='utf-8').read()
    fixed = re.sub(r'^(\s*alphaIsTransparency:) 0$', r'\1 1', text, flags=re.M)
    if fixed == text:
        return False
    io.open(path, 'w', encoding='utf-8').write(fixed)
    return True


if __name__ == '__main__':
    for name in NAMES:
        path = os.path.join(SPRITES, name + '.png')
        if not os.path.exists(path):
            print('  skip %-26s (not here)' % name)
            continue
        cleaned, painted = bleed(Image.open(path).convert('RGBA'))
        if painted:
            cleaned.save(path)
        flagged = set_alpha_flag(name)
        print('  %-26s %s%s' % (
            name,
            ('bled %d px' % painted) if painted else 'nothing to bleed',
            '  + alphaIsTransparency' if flagged else ''))
