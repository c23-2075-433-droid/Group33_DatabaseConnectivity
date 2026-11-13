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
    # Bahay 3. The bathroom items were cut out of the painting and still
    # have the room's own white sitting under their transparency, which did
    # not matter while they were only ever drawn at the size they were cut
    # at - and started mattering the moment the towel was shrunk onto
    # Kylo's shoulder, where it arrived inside a pale box.
    'item_tuwalya', 'item_sabon', 'item_suklay', 'item_sipilyo', 'item_baso',
    'standing_towel_messy', 'ui_arrow_point',
    'sipilyo_frame_1', 'sipilyo_frame_2', 'sipilyo_frame_3',
    'sipilyo_frame_4', 'sipilyo_frame_5', 'sipilyo_frame_6',
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

# The place pictures on the picker: older art, and a different fault - see
# strip_sticker_halo(). Each maps to its greyscale locked twin, which is the
# same drawing and so gets the silhouette worked out for the colour one
# rather than having the test run again over grey, where light artwork and a
# white border are no longer told apart.
PLACE_PICTURES = {
    'map_bahay':    'map_bahay_locked',
    'map_paaralan': 'map_paaralan_locked',
    'map_parke':    'map_parke_locked',
    'map_palengke': 'map_palengke_locked',
}


def strip_sticker_halo(im):
    """Cut the white sticker border off a place picture.

    The hut, the school, the park and the market are drawn with a thick white
    outline hugging them, the way a sticker is cut with a border left on. On
    a white page nobody notices. On the picker's dark board the park is all
    border - a swing frame and a bench are mostly outline for their size -
    and it reads as a bad cut.

    This is not a matte that can be divided back out: the white is painted,
    and it fills the gaps between the swing's legs and between the bench
    slats as well as running round the outside. So it is taken away instead.

    What it is NOT allowed to take is the school, whose walls are cream and
    almost as pale as the border. Two things tell them apart. Nearly all of
    the border is soft - it is the sticker's own antialiased edge - so a pale
    pixel is only removed outright where it is fully opaque AND practically
    colourless, which the cream walls are not; everywhere else it has to be
    partly transparent as well. That leaves the walls alone (58 pure-white
    pixels in the school, against 837 in the park) while still taking the
    park's border, which is nearly all of it.

    That leaves one hairline where the white met the dark outline and the two
    blended. Those pixels are too dark to be caught as white and too light to
    belong, so three passes peel back any edge pixel clearly brighter than
    the artwork just inside it - the same guard the project's
    SpriteEdgeHaloFixer uses, which is what stops the peel eating into pale
    artwork and running away inward.
    """
    a = np.asarray(im).astype(np.float64)
    rgb, alpha = a[..., :3], a[..., 3].copy()
    h, w = alpha.shape

    low = rgb.min(axis=2)
    saturation = rgb.max(axis=2) - low
    soft_border = (low > 225) & (saturation < 18) & (alpha < 250)
    solid_border = (low > 240) & (saturation < 10)
    pale = soft_border | solid_border
    if not pale.any():
        return im, 0
    alpha[pale] = 0
    removed = int((pale & (np.asarray(im)[..., 3] > 16)).sum())

    lum = (rgb * [0.299, 0.587, 0.114]).sum(axis=2)
    for _ in range(3):
        solid = alpha > 16
        p = np.pad(solid, 1, constant_values=False)
        edge = solid & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
        inner = solid & ~edge

        total = np.zeros((h, w))
        count = np.zeros((h, w))
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            shifted, brightness = np.roll(inner, (dy, dx), (0, 1)), np.roll(lum, (dy, dx), (0, 1))
            if dy:
                shifted[0 if dy > 0 else -1, :] = False
            if dx:
                shifted[:, 0 if dx > 0 else -1] = False
            total += brightness * shifted
            count += shifted

        reference = np.divide(total, count, out=np.zeros((h, w)), where=count > 0)
        drop = edge & (count > 0) & (lum > reference + 22)
        if not drop.any():
            break
        alpha[drop] = 0
        removed += int(drop.sum())

    return Image.fromarray(np.dstack([rgb, alpha]).round().astype(np.uint8)), removed


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


def finish(name, image, note=''):
    """Bleed, save, set the flag, and say what happened."""
    bled, painted = bleed(image)
    bled.save(os.path.join(SPRITES, name + '.png'))
    flagged = set_alpha_flag(name)
    print('  %-26s %-24s %-16s%s' % (
        name, note,
        ('bled %d px' % painted) if painted else 'nothing to bleed',
        '  + alphaIsTransparency' if flagged else ''))


def load(name):
    path = os.path.join(SPRITES, name + '.png')
    return Image.open(path).convert('RGBA') if os.path.exists(path) else None


if __name__ == '__main__':
    for name in NAMES:
        image = load(name)
        if image is None:
            print('  skip %-26s (not here)' % name)
            continue
        finish(name, image)

    for name, twin in PLACE_PICTURES.items():
        image = load(name)
        if image is None:
            print('  skip %-26s (not here)' % name)
            continue
        stripped, removed = strip_sticker_halo(image)
        finish(name, stripped, 'halo: %d px off' % removed)

        locked = load(twin)
        if locked is None:
            continue
        if locked.size != stripped.size:
            print('  !! %s is %s but %s is %s - the twin is left alone, since '
                  'the silhouette cannot be shared.' % (twin, locked.size, name, stripped.size))
            continue
        shared = np.dstack([np.asarray(locked)[..., :3],
                            np.asarray(stripped)[..., 3]]).astype(np.uint8)
        finish(twin, Image.fromarray(shared), 'halo: silhouette shared')
