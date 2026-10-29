"""Cut the level select's Canva sheets into sprites.

The set arrives as five pages at 2000x1125: a sheet of sixteen numbered tiles
with their stars and the page dots, a pair of page arrows, the SELECT LEVELS
banner, the board, and the board's backdrop. Everything but the backdrop is
drawn on solid white, so the same edge-connected keying key_ui_sprites.py does
applies here - white that touches the border is the page, white enclosed by
artwork is part of the drawing.

Nothing here is hand-measured. The sheet is keyed, the drawn clusters are
found, and the tiles fall out in reading order; a cell is then split at its
widest empty row, which is the gap between a tile and the stars beneath it.
Measuring sixteen boxes by hand would have to be redone the first time the
sheet is re-exported a few pixels off.

The backdrop is not imported at all. It is menu_room_bg under a brown wash -
fitting an overlay to the pair gives alpha 0.81 over (73, 40, 27) with a
residual under 6/255 - so BuildLevelSelectScene lays that colour over the
menu's own background instead of carrying a second copy of the same classroom.

Run from the repository root:  python3 Tools/import_level_select_art.py
Reads  Art/levelselect_*_raw.png, Art/ui_select_levels_raw.png
Writes Assets/Sprites/<name>.png
"""
from PIL import Image, ImageChops
import numpy as np
from scipy import ndimage
import io, os

SPRITES = 'Assets/Sprites'
ART = 'Art'

WHITE = 238          # at or above this in every channel counts as page
FEATHER = 1.0        # softens the cut edge, as in key_ui_sprites
PAD = 2

TILES_SHEET = os.path.join(ART, 'levelselect_tiles_raw.png')
ARROWS_SHEET = os.path.join(ART, 'levelselect_arrows_raw.png')
BANNER_SHEET = os.path.join(ART, 'ui_select_levels_raw.png')
BOARD_PAGE = os.path.join(ART, 'levelselect_board_raw.png')
BACKDROP_PAGE = os.path.join(ART, 'levelselect_dim_raw.png')

# Only the tiles a page can actually show. A page is one place, and the
# longest place is Bahay with six scenes, so 7 and above are left on the sheet.
TILE_LABELS = [1, 2, 3, 4, 5, 6]


def keyed(path):
    """The page as RGBA, with the white it was drawn on made transparent."""
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im).astype(float)
    rgb = a[..., :3]

    nearly_white = (rgb.min(2) >= WHITE)
    lab, n = ndimage.label(nearly_white)
    border = set(lab[0].tolist()) | set(lab[-1].tolist()) | \
             set(lab[:, 0].tolist()) | set(lab[:, -1].tolist())
    border.discard(0)
    page = np.isin(lab, list(border))

    alpha = ndimage.gaussian_filter(np.where(page, 0.0, 255.0), FEATHER)
    out = a.copy()
    out[..., 3] = np.clip(alpha, 0, 255)
    return out


def clusters(rgba, min_area=600, close=7):
    """Drawn blobs, in reading order: top row left to right, then the next."""
    drawn = rgba[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_closing(drawn, np.ones((close, close))))
    found = []
    for sl in ndimage.find_objects(lab):
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if w * h < min_area:
            continue
        found.append((sl[1].start, sl[0].start, w, h))
    # Rounding y to a band keeps a row together even when its pieces sit a few
    # pixels apart, which they do - the tiles are not perfectly aligned.
    found.sort(key=lambda b: (round(b[1] / 50.0), b[0]))
    return found


def save(rgba, box, name):
    x, y, w, h = box
    piece = rgba[max(0, y-PAD):y+h+PAD, max(0, x-PAD):x+w+PAD]
    ys, xs = np.where(piece[..., 3] > 8)
    piece = piece[ys.min():ys.max()+1, xs.min():xs.max()+1]
    Image.fromarray(piece.astype(np.uint8)).save(os.path.join(SPRITES, name + '.png'))
    write_meta(name)
    print('  %-22s %dx%d' % (name + '.png', piece.shape[1], piece.shape[0]))


def split_cell(rgba, box):
    """A cell is a tile with its three stars under it. Cut them apart at the
    widest gap, which is the only place in the cell with no artwork at all."""
    x, y, w, h = box
    sub = rgba[y:y+h, x:x+w, 3] > 40
    rows = sub.sum(1)
    runs, start = [], None
    for i, r in enumerate(rows):
        if r == 0 and start is None:
            start = i
        elif r != 0 and start is not None:
            runs.append((start, i)); start = None
    if start is not None:
        runs.append((start, len(rows)))
    runs = [r for r in runs if r[0] > h // 3]        # below the tile, not inside it
    if not runs:
        return (x, y, w, h), None
    cut = max(runs, key=lambda r: r[1] - r[0])[0]
    return (x, y, w, cut), (x, y + cut, w, h - cut)


def first_star(rgba, stars_box):
    """The leftmost of the three stars under a tile."""
    x, y, w, h = stars_box
    sub = rgba[y:y+h, x:x+w, 3] > 40
    lab, n = ndimage.label(sub)
    best = None
    for sl in ndimage.find_objects(lab):
        bw, bh = sl[1].stop - sl[1].start, sl[0].stop - sl[0].start
        if bw * bh < 200:
            continue
        box = (x + sl[1].start, y + sl[0].start, bw, bh)
        if best is None or box[0] < best[0]:
            best = box
    return best


def write_meta(name, model='btn_back'):
    """Copy a working sprite's import settings. Existing art keeps its own
    .meta, and with it its GUID, so replacing a sprite relinks nothing."""
    import re, uuid
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)


def cut_board():
    """The board, lifted off its backdrop by what the two pages disagree about.

    The board page is the backdrop with the board drawn on it, so the board is
    exactly where they differ. Opening first drops the odd stray pixel of JPEG
    noise, and filling holes keeps the board's own dark grain, which matches
    the backdrop closely enough in places to read as a hole."""
    board = Image.open(BOARD_PAGE).convert('RGB')
    backdrop = Image.open(BACKDROP_PAGE).convert('RGB')
    diff = np.asarray(ImageChops.difference(board, backdrop)).max(2)

    m = ndimage.binary_opening(diff > 12, np.ones((3, 3)))
    m = ndimage.binary_fill_holes(ndimage.binary_closing(m, np.ones((15, 15))))
    lab, n = ndimage.label(m)
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    m = (lab == int(np.argmax(sizes)) + 1)

    out = np.dstack([np.asarray(board).astype(float),
                     ndimage.gaussian_filter(np.where(m, 255.0, 0.0), FEATHER)])
    ys, xs = np.where(out[..., 3] > 8)
    out = out[ys.min():ys.max()+1, xs.min():xs.max()+1]
    Image.fromarray(out.astype(np.uint8)).save(os.path.join(SPRITES, 'ui_level_board.png'))
    write_meta('ui_level_board')
    print('  %-22s %dx%d' % ('ui_level_board.png', out.shape[1], out.shape[0]))


if __name__ == '__main__':
    missing = [p for p in (TILES_SHEET, ARROWS_SHEET, BANNER_SHEET, BOARD_PAGE, BACKDROP_PAGE)
               if not os.path.exists(p)]
    if missing:
        print('Nothing to do. Missing:')
        for p in missing:
            print('  ' + p)
        raise SystemExit(1)

    sheet = keyed(TILES_SHEET)
    cells = [c for c in clusters(sheet) if c[3] > 100]       # tile-and-stars cells
    dots = [c for c in clusters(sheet) if c[3] <= 100 and c[1] > 850]
    print('tiles sheet: %d cells, %d page dots' % (len(cells), len(dots)))

    # Reading order gives 0..7 on the top row, 8..14 and the padlock below.
    for label in TILE_LABELS:
        tile, _ = split_cell(sheet, cells[label])
        save(sheet, tile, 'btn_step_%d' % label)
    lock_tile, lock_stars = split_cell(sheet, cells[-1])
    save(sheet, lock_tile, 'btn_step_locked')

    _, lit_stars = split_cell(sheet, cells[0])               # tile 0 has three gold
    save(sheet, first_star(sheet, lit_stars), 'ui_step_star_on')
    save(sheet, first_star(sheet, lock_stars), 'ui_step_star_off')

    dots.sort(key=lambda d: d[0])                            # the lit one is first
    save(sheet, dots[0], 'ui_page_dot_on')
    save(sheet, dots[1], 'ui_page_dot_off')

    arrows = keyed(ARROWS_SHEET)
    left, right = sorted(clusters(arrows), key=lambda b: b[0])[:2]
    save(arrows, left, 'btn_page_left')
    save(arrows, right, 'btn_page_right')

    banner = keyed(BANNER_SHEET)
    save(banner, clusters(banner)[0], 'ui_select_levels')

    cut_board()
