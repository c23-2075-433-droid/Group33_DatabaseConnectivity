"""Import the player-profile Canva set into Assets/Sprites.

One 2000x1125 page, Art/profile_ui_raw.png, holding everything the profile
menu and the two boards are drawn from:

  ui_profile_board      the "WHO ARE YOU?" board - title, four slots and the
                        RENAME / DELETE / OKAY / CANCEL plates are all painted
                        into it, so the scene lays live widgets over them
                        rather than drawing its own
  ui_leaderboard_board  the "LEADERBOARDS" board, six slots, same idea
  ui_profile_menu       what hangs under the nickname holder when it is
                        tapped: the two ropes and the CHANGE PROFILE and
                        LEADERBOARDS signs, in one piece because the ropes run
                        between them
  ui_profile_gem        the clasp that sits on the holder's bottom edge. On
                        the page it is drawn point-up; on the holder it hangs
                        point-down, so it is turned over here rather than by a
                        rotation on the RectTransform
  btn_rename, btn_delete, btn_okay_plate, btn_cancel_plate
                        the same four plates again, loose. They are laid over
                        the ones painted into the board so that a press tints
                        the plate instead of the whole board

Each piece is cut from its own window of the page and the white inside that
window is keyed off, edge-connected only, so the shine down the middle of a
gold plate survives. Nothing here carries a white sticker outline, unlike the
main menu's logo and books - see Tools/import_main_menu_art.py.

Run from the repository root:  python3 Tools/import_profile_art.py
Reads  Art/profile_ui_raw.png
Writes Assets/Sprites/<name>.png
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import io, os

SPRITES = 'Assets/Sprites'
ART = 'Art'
PAGE = 'profile_ui'

WHITE = 240
FEATHER = 1.0
PAD = 2

# White that the page shows through but that the edge-connected pass cannot
# reach, because the artwork closes a box around it: between the two signs,
# the ropes down each side and the sign above and below make a pocket of bare
# page. Left alone it comes out as a white slab hanging between the signs.
#
# Only ui_profile_menu has one, and saying so by name is safer than a rule
# that applies everywhere - the white letters painted on RENAME and OKAY are
# enclosed by their plates and are flat white too, and a general rule would
# punch them out. The area floor is the second guard: the pocket is some nine
# thousand pixels, a letter a few hundred.
HOLES = {'ui_profile_menu'}
MIN_HOLE = 2000

# name -> (left, top, right, bottom) window on the page, and whether the piece
# is drawn upside down relative to how it is used.
#
# The windows are generous but never reach a neighbour: the four loose plates
# sit in a 2x2 block only 7px apart vertically, which is why their windows are
# written out rather than found by looking for bands.
PIECES = {
    'ui_profile_board':     ((  30,  210,  700, 1050), False),
    'ui_leaderboard_board': (( 700,  210, 1365, 1050), False),
    'ui_profile_menu':      ((1535,  217, 1840,  495), False),
    'ui_profile_gem':       ((1205,  100, 1300,  182), True),
    'btn_rename':           ((1374,  757, 1654,  866), False),
    'btn_delete':           ((1652,  757, 1932,  866), False),
    'btn_okay_plate':       ((1374,  866, 1654,  974), False),
    'btn_cancel_plate':     ((1652,  866, 1932,  974), False),
}


def key(window, holes=False):
    """Cut the white page away from one window, edge-connected white only."""
    rgb = np.asarray(window.convert('RGB')).astype(float)
    nearly_white = rgb.min(2) >= WHITE
    lab, n = ndimage.label(nearly_white)
    border = set(lab[0].tolist()) | set(lab[-1].tolist()) | \
             set(lab[:, 0].tolist()) | set(lab[:, -1].tolist())
    border.discard(0)
    bg = np.isin(lab, list(border))

    if holes:
        # A large pocket of flat white that the drawing has closed around is
        # still the page. Shaded white belongs to the artwork and varies far
        # more than this; dead-flat white does not.
        for i in range(1, n + 1):
            if i in border:
                continue
            sel = lab == i
            if sel.sum() < MIN_HOLE:
                continue
            patch = rgb[sel]
            if patch.mean() >= 250 and patch.std() < 4.0:
                bg |= sel

    alpha = ndimage.gaussian_filter(np.where(bg, 0.0, 255.0), FEATHER)
    return Image.fromarray(np.concatenate(
        [rgb, np.clip(alpha, 0, 255)[..., None]], axis=2).round().astype(np.uint8))


def trim(im):
    a = np.asarray(im)
    ys, xs = np.where(a[..., 3] > 8)
    y0, y1 = max(0, ys.min()-PAD), min(a.shape[0], ys.max()+1+PAD)
    x0, x1 = max(0, xs.min()-PAD), min(a.shape[1], xs.max()+1+PAD)
    return im.crop((x0, y0, x1, y1))


def write_meta(name, model='menu_bg'):
    """Copy a working sprite's import settings so Unity imports this as a Sprite."""
    import re, uuid
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return False
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    # See the same note in import_main_menu_art.py: menu_bg is a full-bleed
    # background, and its alphaIsTransparency: 0 fringes every cut-out edge.
    t = re.sub(r'^(\s*alphaIsTransparency:) 0$', r'\1 1', t, flags=re.M)
    io.open(out, 'w', encoding='utf-8').write(t)
    return True


if __name__ == '__main__':
    src = os.path.join(ART, PAGE + '_raw.png')
    if not os.path.exists(src):
        print('Nothing to do: put the Canva export in %s first.' % src)
        raise SystemExit

    page = Image.open(src).convert('RGB')
    for name, (box, flip) in PIECES.items():
        piece = trim(key(page.crop(box), name in HOLES))
        if flip:
            piece = piece.transpose(Image.ROTATE_180)
        piece.save(os.path.join(SPRITES, name + '.png'))
        fresh = write_meta(name)
        print('  %-22s -> %4dx%-4d  (window %d,%d)%s'
              % (name + '.png', piece.size[0], piece.size[1], box[0], box[1],
                 '  + new .meta' if fresh else ''))
