"""Import the main menu's Canva set into Assets/Sprites.

Unlike the rest of the interface art, this set comes back from Canva already
cut out - the logo and the three wooden buttons carry a real alpha channel, so
there is no white canvas to key off. Running key_ui_sprites.py over them would
be worse than useless: the cream outline around each button is near enough to
white that the edge-connected pass eats into it.

So all these need is a trim to what is actually drawn, which matters because
every piece is exported on the same 2000x1125 page. Left untrimmed, a 500px
button would arrive as a 2000px image that is 97% empty, and the builder's rect
sizes would describe the page rather than the artwork.

Run from the repository root:  python3 Tools/import_main_menu_art.py
Reads  Art/<name>_raw.png
Writes Assets/Sprites/<name>.png

Art/menu_layout_reference.png is the composed mockup these were cut from. It is
deliberately not in the list: it is what BuildMainMenuScene.cs was measured
against, not something the game draws.
"""
from PIL import Image
import numpy as np
import io, os

SPRITES = 'Assets/Sprites'   # where finished sprites go
ART = 'Art'                  # source illustrations, outside Unity's import path

# name -> trim to the artwork? The classroom plate is a full-bleed background
# and is opaque edge to edge, so trimming it would be a no-op on a good day and
# a crop on a bad one.
NAMES = {'menu_room_bg': False,
         'menu_title': True,
         'btn_play': True,
         'btn_settings': True,
         'btn_about': True}

PAD = 2              # breathing room around the trim, as in key_ui_sprites
VISIBLE = 8          # alpha at or below this is nothing


def trim(im):
    """Crop to the drawn pixels. Returns the image unchanged if it is opaque."""
    a = np.asarray(im)
    ys, xs = np.where(a[..., 3] > VISIBLE)
    y0, y1 = max(0, ys.min()-PAD), min(a.shape[0], ys.max()+1+PAD)
    x0, x1 = max(0, xs.min()-PAD), min(a.shape[1], xs.max()+1+PAD)
    return im.crop((x0, y0, x1, y1))


def write_meta(name, model='menu_bg'):
    """Copy a working sprite's import settings so Unity imports this as a Sprite.

    Only ever called for art that is new to the project. The pieces that
    replace existing sprites keep their own .meta, and with it their GUID -
    otherwise every scene pointing at btn_play would lose its button.
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
        im.save(os.path.join(SPRITES, n + '.png'))
        fresh = write_meta(n)
        print('  %-18s -> %dx%d%s%s' % (n + '.png', im.size[0], im.size[1],
              '  (trimmed from %dx%d)' % was if crop else '',
              '  + new .meta' if fresh else ''))
        done += 1
    if not done:
        print('\nNothing to do. Put the Canva exports in %s as <name>_raw.png '
              'first.' % ART)
