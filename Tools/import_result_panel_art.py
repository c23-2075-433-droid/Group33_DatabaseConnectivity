"""Cut the scene summary's Canva sheets into sprites.

Three pages at 2000x1125, all drawn on solid white: the board with SCORE: and
TIME: printed into it, the two banners, and the loose pieces - the close
button, the REPLAY and CONTINUE planks, and the five stars twice over, lit and
unlit. Keying is the same edge-connected pass key_ui_sprites.py uses.

The stars are the awkward part. They are a drawn arrangement, not one star
scaled five times: the middle one is the largest and they shrink outwards, and
the unlit set is drawn a touch larger than the lit one because of its heavier
outline. So each of the ten is cut separately and the builder places them from
their own sizes, rather than tiling one sprite five times and losing the arc.

Lit and unlit cannot be told apart by position - their rows overlap on the
page - so they are told apart by colour: gold has red and green well above
blue, grey has the three within a few points of each other.

Run from the repository root:  python3 Tools/import_result_panel_art.py
Reads  Art/result_board_raw.png, result_titles_raw.png, result_pieces_raw.png
Writes Assets/Sprites/<name>.png
"""
from PIL import Image
import numpy as np
from scipy import ndimage
import io, os

SPRITES = 'Assets/Sprites'
ART = 'Art'

WHITE = 238
FEATHER = 1.0
PAD = 2

BOARD_PAGE = os.path.join(ART, 'result_board_raw.png')
TITLES_PAGE = os.path.join(ART, 'result_titles_raw.png')
PIECES_PAGE = os.path.join(ART, 'result_pieces_raw.png')


def keyed(path):
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


def clusters(rgba, min_area=700, close=7):
    drawn = rgba[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_closing(drawn, np.ones((close, close))))
    found = []
    for sl in ndimage.find_objects(lab):
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if w * h < min_area:
            continue
        found.append((sl[1].start, sl[0].start, w, h))
    return found


def is_gold(rgba, box):
    """Gold reads warm - red and green well clear of blue. Grey does not."""
    x, y, w, h = box
    patch = rgba[y:y+h, x:x+w]
    solid = patch[..., 3] > 200
    if not solid.any():
        return False
    rgb = patch[solid][:, :3].mean(0)
    return (rgb[0] + rgb[1]) * 0.5 - rgb[2] > 40


def save(rgba, box, name):
    x, y, w, h = box
    piece = rgba[max(0, y-PAD):y+h+PAD, max(0, x-PAD):x+w+PAD]
    ys, xs = np.where(piece[..., 3] > 8)
    piece = piece[ys.min():ys.max()+1, xs.min():xs.max()+1]
    Image.fromarray(piece.astype(np.uint8)).save(os.path.join(SPRITES, name + '.png'))
    write_meta(name)
    print('  %-24s %dx%d' % (name + '.png', piece.shape[1], piece.shape[0]))


def write_meta(name, model='btn_back'):
    import re, uuid
    out = os.path.join(SPRITES, name + '.png.meta')
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, model + '.png.meta'), encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]{32}$', 'guid: ' + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(model + '_0', name + '_0')
    io.open(out, 'w', encoding='utf-8').write(t)


if __name__ == '__main__':
    missing = [p for p in (BOARD_PAGE, TITLES_PAGE, PIECES_PAGE) if not os.path.exists(p)]
    if missing:
        print('Nothing to do. Missing:')
        for p in missing:
            print('  ' + p)
        raise SystemExit(1)

    board = keyed(BOARD_PAGE)
    save(board, max(clusters(board), key=lambda b: b[2] * b[3]), 'ui_result_board')

    titles = keyed(TITLES_PAGE)
    complete, tryagain = sorted(clusters(titles), key=lambda b: b[1])[:2]
    save(titles, complete, 'ui_result_title_complete')
    save(titles, tryagain, 'ui_result_title_tryagain')

    pieces = keyed(PIECES_PAGE)
    found = clusters(pieces)

    # The close button and the two planks sit along the top of the page; the
    # stars are everything below them.
    top = sorted([b for b in found if b[1] + b[3] < 400], key=lambda b: b[0])
    save(pieces, top[0], 'btn_result_close')
    save(pieces, top[1], 'btn_result_replay')
    save(pieces, top[2], 'btn_result_continue')

    stars = [b for b in found if b[1] + b[3] >= 400]
    gold = sorted([b for b in stars if is_gold(pieces, b)], key=lambda b: b[0])
    grey = sorted([b for b in stars if not is_gold(pieces, b)], key=lambda b: b[0])
    if len(gold) != 5 or len(grey) != 5:
        print('  expected five of each star, found %d gold and %d grey - '
              'check the page' % (len(gold), len(grey)))
    for i, box in enumerate(gold):
        save(pieces, box, 'star_gold_%d' % (i + 1))
    for i, box in enumerate(grey):
        save(pieces, box, 'star_grey_%d' % (i + 1))
