"""Import the main menu's Canva set into Assets/Sprites.

Unlike the rest of the interface art, this set comes back from Canva already
cut out - the logo and the wooden buttons carry a real alpha channel, so there
is no white canvas to key off. Running key_ui_sprites.py over them would be
worse than useless: the cream outline around each button is near enough to
white that the edge-connected pass eats into it.

So all these need is a trim to what is actually drawn, which matters because
every piece is exported on the same 2000x1125 page. Left untrimmed, a 500px
button would arrive as a 2000px image that is 97% empty, and the builder's rect
sizes would describe the page rather than the artwork.

The four menu buttons now arrive as one sheet rather than one file each, so
they are found by looking for bands of drawn pixels down the page and sliced in
top-to-bottom order. That order is the order they appear on the menu, which is
the only thing tying a band to a name - if the sheet is ever re-exported with
the buttons rearranged, reorder BUTTONS to match.

Run from the repository root:  python3 Tools/import_main_menu_art.py
Reads  Art/<name>_raw.png
Writes Assets/Sprites/<name>.png

Art/menu_layout_new_reference.png is the composed mockup these were cut from.
It is deliberately not in the list: it is what BuildMainMenuScene.cs was
measured against, not something the game draws.
"""
from PIL import Image
import numpy as np
import io, os

SPRITES = 'Assets/Sprites'   # where finished sprites go
ART = 'Art'                  # source illustrations, outside Unity's import path

# name -> trim to the artwork? The classroom plate is a full-bleed background
# and is opaque edge to edge, so trimming it would be a no-op on a good day and
# a crop on a bad one.
NAMES = {'menu_classroom_bg': False,
         'menu_title': True}

# Art/menu_buttons_raw.png, top band first.
BUTTON_SHEET = 'menu_buttons'
BUTTONS = ['btn_adventure', 'btn_minigames', 'btn_settings', 'btn_about']

PAD = 2              # breathing room around the trim, as in key_ui_sprites
VISIBLE = 8          # alpha at or below this is nothing

# Soft twins of every piece the menu draws, for the backdrop behind a dialog.
# MenuBlurBackdrop swaps each Image's sprite for its <name>_blur while the
# dialog is up, which is how the menu goes out of focus without a wash of
# colour over it and without a blur shader.
#
# The radius is in page pixels, and every piece here is cut from the same
# 2000x1125 page, so one number blurs them all by the same amount on screen.
BLUR_RADIUS = 10


def trim(im):
    """Crop to the drawn pixels. Returns the image unchanged if it is opaque."""
    a = np.asarray(im)
    ys, xs = np.where(a[..., 3] > VISIBLE)
    y0, y1 = max(0, ys.min()-PAD), min(a.shape[0], ys.max()+1+PAD)
    x0, x1 = max(0, xs.min()-PAD), min(a.shape[1], xs.max()+1+PAD)
    return im.crop((x0, y0, x1, y1))


def bands(im):
    """Rows of the page that hold artwork, as (top, bottom) pairs top-down.

    The buttons are laid out in a single column with clear gaps between them,
    so a row either crosses a button or crosses nothing.
    """
    drawn = (np.asarray(im)[..., 3] > VISIBLE).any(axis=1)
    out, start = [], None
    for y, hit in enumerate(drawn):
        if hit and start is None:
            start = y
        elif not hit and start is not None:
            out.append((start, y-1)); start = None
    if start is not None:
        out.append((start, len(drawn)-1))
    return out


def blur(im):
    """A soft copy, blurred on premultiplied alpha.

    Blurring the channels as they come would drag the transparent background's
    black into every edge, and the cut-out pieces would come back with a dark
    halo. Premultiplying first means only the colours that are actually there
    get averaged.
    """
    from PIL import ImageFilter
    a = np.asarray(im).astype(np.float32)
    alpha = a[..., 3:4] / 255.0
    pm = np.concatenate([a[..., :3] * alpha, a[..., 3:4]], axis=2)
    pm = Image.fromarray(pm.round().astype(np.uint8)) \
              .filter(ImageFilter.GaussianBlur(BLUR_RADIUS))
    b = np.asarray(pm).astype(np.float32)
    out = b[..., 3:4] / 255.0
    rgb = np.divide(b[..., :3], out, out=np.zeros_like(b[..., :3]), where=out > 0.004)
    return Image.fromarray(np.concatenate(
        [rgb.clip(0, 255), b[..., 3:4]], axis=2).round().astype(np.uint8))


def write_meta(name, model='menu_bg'):
    """Copy a working sprite's import settings so Unity imports this as a Sprite.

    Only ever called for art that is new to the project. The pieces that
    replace existing sprites keep their own .meta, and with it their GUID -
    otherwise every scene pointing at btn_settings would lose its button.
    """
    import re, uuid
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return False
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)
    return True


def save(name, im, note=''):
    im.save(os.path.join(SPRITES, name + '.png'))
    fresh = write_meta(name)
    print('  %-20s -> %dx%d%s%s' % (name + '.png', im.size[0], im.size[1], note,
                                    '  + new .meta' if fresh else ''))
    # Every piece the menu draws gets its soft twin at the same time, so the
    # two can never drift apart.
    if not name.endswith('_blur'):
        save(name + '_blur', blur(im))


def slice_buttons():
    """Cut Art/menu_buttons_raw.png into one sprite per button."""
    src = os.path.join(ART, BUTTON_SHEET + '_raw.png')
    if not os.path.exists(src):
        print('  skip %-14s (no %s yet)' % (BUTTON_SHEET, src))
        return 0
    sheet = Image.open(src).convert('RGBA')
    found = bands(sheet)
    if len(found) != len(BUTTONS):
        print('  !! %s holds %d band(s), expected %d (%s). Nothing sliced - '
              'check the export.' % (src, len(found), len(BUTTONS), ', '.join(BUTTONS)))
        return 0
    for name, (top, bottom) in zip(BUTTONS, found):
        save(name, trim(sheet.crop((0, top, sheet.size[0], bottom+1))),
             '  (band %d-%d of the sheet)' % (top, bottom))
    return len(BUTTONS)


if __name__ == '__main__':
    done = 0
    for n, crop in NAMES.items():
        src = os.path.join(ART, n + '_raw.png')
        if not os.path.exists(src):
            print('  skip %-14s (no %s yet)' % (n, src)); continue
        im = Image.open(src).convert('RGBA')
        was = im.size
        if crop:
            im = trim(im)
        save(n, im, '  (trimmed from %dx%d)' % was if crop else '')
        done += 1
    done += slice_buttons()
    if not done:
        print('\nNothing to do. Put the Canva exports in %s as <name>_raw.png '
              'first.' % ART)
