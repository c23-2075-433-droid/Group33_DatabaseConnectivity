"""Import the main menu's Canva set into Assets/Sprites.

The set is one 2000x1125 page, Art/menu_pieces_raw.png, holding every piece
the menu draws: the logo, the nickname holder, the four wooden buttons and the
stack of books. Art/menu_classroom_bg_raw.png is the classroom behind them and
Art/menu_layout_new_reference.png is the composed mockup, which is what
BuildMainMenuScene.cs was measured against rather than anything the game draws.

Two things about this page are worth knowing before changing anything here.

It arrives on white rather than with an alpha channel, so the background has to
be keyed off. Only white that reaches the edge of the page is removed, exactly
as Tools/key_ui_sprites.py does it, so the white highlights inside the gold
plates survive.

The logo and the books are drawn with a white sticker outline, which on a white
page is invisible - keying alone would quietly shave it off. Those two are
given their outline back as a ring of white around the keyed shape. The width
is the one measured off the mockup, where the pieces sit on the classroom and
the outline can actually be seen: the drawn extent there is 16px wider and
16px taller than the same piece on this page, for both of them.

Run from the repository root:  python3 Tools/import_main_menu_art.py
Reads  Art/<name>_raw.png
Writes Assets/Sprites/<name>.png
"""
from PIL import Image, ImageFilter
import numpy as np
from scipy import ndimage
import io, os

SPRITES = 'Assets/Sprites'   # where finished sprites go
ART = 'Art'                  # source illustrations, outside Unity's import path

# The classroom plate is a full-bleed background: opaque edge to edge, so it is
# copied across untouched rather than keyed and trimmed.
BACKGROUND = 'menu_classroom_bg'

# Everything on Art/menu_pieces_raw.png. The page is keyed, then split into
# connected pieces, and each piece is named by where it sits - see classify().
PIECE_SHEET = 'menu_pieces'

WHITE = 240          # at or above this in every channel counts as page, not art
FEATHER = 1.0        # softens the cut edge
PAD = 2              # breathing room around the trim, as in key_ui_sprites

# Pieces drawn with a white sticker outline, and how wide it is in page pixels.
# See the note at the top: measured against the mockup, not guessed.
OUTLINE = {'menu_title': 8, 'menu_books': 8}

# Soft twins of every piece the menu draws, for the backdrop behind a dialog.
# MenuBlurBackdrop swaps each Image's sprite for its <name>_blur while the
# dialog is up, which is how the menu goes out of focus without a wash of
# colour over it and without a blur shader.
#
# The radius is in page pixels, and every piece here is cut from the same
# 2000x1125 page, so one number blurs them all by the same amount on screen.
BLUR_RADIUS = 10


def page_alpha(rgb):
    """Alpha for art drawn on a white page: 0 where the page shows through.

    Only white connected to the edge of the page is background. White enclosed
    by the drawing - the shine down the middle of a gold plate - is part of it.
    """
    nearly_white = rgb.min(2) >= WHITE
    lab, n = ndimage.label(nearly_white)
    border = set(lab[0].tolist()) | set(lab[-1].tolist()) | \
             set(lab[:, 0].tolist()) | set(lab[:, -1].tolist())
    border.discard(0)
    bg = np.isin(lab, list(border))
    return np.where(bg, 0.0, 255.0)


def add_outline(im, width):
    """Give a piece back the white sticker outline the white page swallowed."""
    a = np.asarray(im).astype(float)
    solid = a[..., 3] > 128
    grown = ndimage.binary_dilation(solid, ndimage.generate_binary_structure(2, 2),
                                    iterations=int(width))
    ring = grown & ~solid

    out = a.copy()
    out[..., :3][ring] = 255.0            # the outline is white
    out[..., 3] = np.maximum(out[..., 3], np.where(grown, 255.0, 0.0))
    # The ring is drawn from a hard mask, so soften its outer edge the same way
    # the keyed edge was softened - otherwise the outline alone looks cut out.
    out[..., 3] = np.clip(ndimage.gaussian_filter(out[..., 3], FEATHER), 0, 255)
    return Image.fromarray(out.round().astype(np.uint8))


def trim(im):
    """Crop to the drawn pixels, with a little padding."""
    a = np.asarray(im)
    ys, xs = np.where(a[..., 3] > 8)
    y0, y1 = max(0, ys.min()-PAD), min(a.shape[0], ys.max()+1+PAD)
    x0, x1 = max(0, xs.min()-PAD), min(a.shape[1], xs.max()+1+PAD)
    return im.crop((x0, y0, x1, y1))


def blur(im):
    """A soft copy, blurred on premultiplied alpha.

    Blurring the channels as they come would drag the transparent background's
    black into every edge, and the cut-out pieces would come back with a dark
    halo. Premultiplying first means only the colours that are actually there
    get averaged.
    """
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
    # menu_bg is a full-bleed background and carries alphaIsTransparency: 0,
    # which on cut-out art lets Unity sample the colour under the transparency
    # and fringe every edge. See Tools/clean_sprite_edges.py.
    t = re.sub(r'^(\s*alphaIsTransparency:) 0$', r'\1 1', t, flags=re.M)
    io.open(out, 'w', encoding='utf-8').write(t)
    return True


def save(name, im, note=''):
    im.save(os.path.join(SPRITES, name + '.png'))
    fresh = write_meta(name)
    print('  %-20s -> %4dx%-4d%s%s' % (name + '.png', im.size[0], im.size[1], note,
                                       '  + new .meta' if fresh else ''))
    # Every piece the menu draws gets its soft twin at the same time, so the
    # two can never drift apart.
    if not name.endswith('_blur'):
        save(name + '_blur', blur(im))


def classify(boxes):
    """Name each piece on the page by where it sits.

    Naming them by position rather than by a hard-coded rectangle means a
    re-export that nudges something still lands on the right sprite. What it
    cannot survive is the arrangement itself changing - the logo moving to the
    right of the buttons, say - and that is deliberate: it should fail loudly
    rather than write the logo over btn_about.
    """
    named = {}
    column = sorted([b for b in boxes if b[0] > 1100 and b[3] > 150],
                    key=lambda b: b[1])
    for name, box in zip(['btn_adventure', 'btn_minigames', 'btn_settings', 'btn_about'],
                         column):
        named[name] = box
    for box in boxes:
        x, y, w, h = box
        if box in column:
            continue
        if x > 1100:
            named['ui_name_holder'] = box     # top right, above the buttons
        elif y < 600:
            named['menu_title'] = box         # the logo, over the left half
        else:
            named['menu_books'] = box         # the stack, bottom left
    return named


def slice_pieces():
    """Key Art/menu_pieces_raw.png and cut it into one sprite per piece."""
    src = os.path.join(ART, PIECE_SHEET + '_raw.png')
    if not os.path.exists(src):
        print('  skip %-14s (no %s yet)' % (PIECE_SHEET, src))
        return 0

    page = Image.open(src).convert('RGB')
    rgb = np.asarray(page).astype(float)
    alpha = ndimage.gaussian_filter(page_alpha(rgb), FEATHER)
    keyed = np.concatenate([rgb, np.clip(alpha, 0, 255)[..., None]], axis=2)

    lab, n = ndimage.label(keyed[..., 3] > 8, np.ones((3, 3)))
    boxes = []
    for sl in ndimage.find_objects(lab):
        ys, xs = sl
        if (xs.stop - xs.start) * (ys.stop - ys.start) < 2000:
            continue                       # stray speck from the keying
        boxes.append((xs.start, ys.start, xs.stop - xs.start, ys.stop - ys.start))

    named = classify(boxes)
    wanted = ['menu_title', 'ui_name_holder', 'menu_books',
              'btn_adventure', 'btn_minigames', 'btn_settings', 'btn_about']
    missing = [w for w in wanted if w not in named]
    if missing:
        print('  !! %s holds %d piece(s); could not place %s. Nothing sliced - '
              'check the export.' % (src, len(boxes), ', '.join(missing)))
        return 0

    for name in wanted:
        x, y, w, h = named[name]
        # Room for the outline to grow into. Cutting to the drawn pixels first
        # and dilating afterwards would press the ring against the edge of the
        # crop, which is exactly where it is supposed to go.
        edge = OUTLINE[name] + PAD if name in OUTLINE else 0
        x0, y0 = max(0, x - edge), max(0, y - edge)
        x1 = min(keyed.shape[1], x + w + edge)
        y1 = min(keyed.shape[0], y + h + edge)
        piece = Image.fromarray(keyed[y0:y1, x0:x1].round().astype(np.uint8))
        note = '  (page %d,%d)' % (x, y)
        if name in OUTLINE:
            piece = add_outline(piece, OUTLINE[name])
            note += ' + %dpx outline' % OUTLINE[name]
        save(name, trim(piece), note)
    return len(wanted)


if __name__ == '__main__':
    done = 0
    src = os.path.join(ART, BACKGROUND + '_raw.png')
    if os.path.exists(src):
        save(BACKGROUND, Image.open(src).convert('RGBA'))
        done += 1
    else:
        print('  skip %-14s (no %s yet)' % (BACKGROUND, src))
    done += slice_pieces()
    if not done:
        print('\nNothing to do. Put the Canva exports in %s as <name>_raw.png '
              'first.' % ART)
