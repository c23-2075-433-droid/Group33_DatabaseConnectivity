"""Cut the adventure screens' Canva pages into Assets/Sprites.

Three pages, all 2000x1125 and all already carrying alpha, so there is
nothing to key - only cutting:

  Art/place_boards_raw.png     a 3x2 grid of place boards. Top row is the
                               open board for HOME, SCHOOL and PARK; bottom
                               row is the locked one. HOME's locked slot
                               holds a copy of its open board, because the
                               first place is never locked, so it is skipped.
  Art/levels_board_raw.png     the big empty board the level grid sits on.
  Art/adventure_titles_raw.png the back arrow, the "YOUR ADVENTURE AWAITS!"
                               banner and the notebook button across the top,
                               then one title banner per place.

Every piece is found by looking for bands of drawn pixels rather than by
hard-coded boxes, so a re-export that shifts things a little still cuts
correctly. What is hard-coded is only how many bands to expect and what they
are called, in the order they appear down and across the page.

Run from the repository root:  python3 Tools/import_adventure_art.py

Art/adventure_layout_reference.png and Art/levels_layout_reference.png are
the composed mockups these were measured against. They are deliberately not
cut: they are what BuildLevelSelectScene.cs was laid out against, not
something the game draws.
"""
from PIL import Image
import numpy as np
import io, os, re, uuid

SPRITES = 'Assets/Sprites'
ART = 'Art'

PAD = 2
VISIBLE = 8

# Page -> 1920x1080 canvas, the same 0.96 the rest of the interface uses.
SCALE = 0.96
PAGE = (2000, 1125)

# place_boards_raw: row of the grid, then column. None is skipped.
PLACE_BOARDS = [['ui_place_home', 'ui_place_school', 'ui_place_park'],
                [None, 'ui_place_school_locked', 'ui_place_park_locked']]

# adventure_titles_raw: one list per band of rows, left to right.
TITLES = [['btn_back_arrow', 'ui_title_adventure', 'btn_levels_menu'],
          ['ui_title_home'],
          ['ui_title_school'],
          ['ui_title_park']]


def bands(drawn, axis):
    """Runs along one axis that hold artwork, in order."""
    hit = drawn.any(axis=axis)
    out, start = [], None
    for i, v in enumerate(hit):
        if v and start is None:
            start = i
        elif not v and start is not None:
            out.append((start, i-1)); start = None
    if start is not None:
        out.append((start, len(hit)-1))
    return out


def box(drawn, y0, y1, x0, x1):
    """Tight box around the artwork in a region, padded but never past it."""
    ys, xs = np.where(drawn[y0:y1+1, x0:x1+1])
    return (max(x0, x0+xs.min()-PAD), max(y0, y0+ys.min()-PAD),
            min(x1+1, x0+xs.max()+1+PAD), min(y1+1, y0+ys.max()+1+PAD))


def unity(b):
    """A page box as a builder wants it: centred anchoredPosition and size."""
    x0, y0, x1, y1 = b
    return (round((((x0+x1)/2) - PAGE[0]/2) * SCALE),
            round((PAGE[1]/2 - ((y0+y1)/2)) * SCALE),
            round((x1-x0) * SCALE), round((y1-y0) * SCALE))


def write_meta(name, model='menu_bg'):
    """Copy a working sprite's import settings, with a GUID of its own."""
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return False
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)
    return True


def cut(page, name, b):
    page.crop(b).save(os.path.join(SPRITES, name + '.png'))
    fresh = write_meta(name)
    print('  %-24s page %-24s ->  pos (%d, %d), size (%d, %d)%s'
          % ((name, str(tuple(int(v) for v in b))) + unity(b)
             + ('  + new .meta' if fresh else '',)))


def cut_page(filename, layout):
    """Cut one page whose pieces sit in rows, each row read left to right."""
    src = os.path.join(ART, filename)
    if not os.path.exists(src):
        print('  skip %s (not here yet)' % src)
        return 0
    page = Image.open(src).convert('RGBA')
    drawn = np.asarray(page)[..., 3] > VISIBLE

    rows = bands(drawn, 1)
    if len(rows) != len(layout):
        print('  !! %s holds %d row(s), expected %d. Nothing cut - check the '
              'export.' % (src, len(rows), len(layout)))
        return 0

    done = 0
    for (y0, y1), names in zip(rows, layout):
        cols = bands(drawn[y0:y1+1], 0)
        if len(cols) != len(names):
            print('  !! %s rows %d-%d hold %d piece(s), expected %d (%s). Row '
                  'skipped.' % (src, y0, y1, len(cols), len(names),
                                ', '.join(n or '-' for n in names)))
            continue
        for name, (x0, x1) in zip(names, cols):
            if name is None:
                continue
            cut(page, name, box(drawn, y0, y1, x0, x1))
            done += 1
    return done


if __name__ == '__main__':
    total = cut_page('place_boards_raw.png', PLACE_BOARDS)
    total += cut_page('levels_board_raw.png', [['ui_levels_board']])
    total += cut_page('adventure_titles_raw.png', TITLES)
    if not total:
        print('\nNothing to do. Put the Canva exports in %s first.' % ART)
