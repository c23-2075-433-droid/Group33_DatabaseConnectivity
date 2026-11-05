"""Cut the "Enter Nickname" hanging sign out of its Canva page.

The dialog arrives as one 2000x1125 export, Art/nickname_dialog_raw.png: two
ropes running off the top of the page to a board with the prompt and the white
name field painted on, then four short ropes down to the OKAY and CANCEL
plates. It already carries alpha, so there is nothing to key - it only needs
cutting into the three pieces the scene moves independently.

The cut is one horizontal line. Ignore the four short rope columns and the
page is empty between row 683 and row 704: everything above is board and rope,
everything below is plate. Cutting at CUT means each rope is whole on the
board and meets its plate exactly where the plate starts, with no seam.

The white name field is painted into the board, so there is no separate slot
sprite any more - NicknameDialogBuilder lays a transparent InputField over it
and this script prints where.

Run from the repository root:  python3 Tools/import_nickname_dialog_art.py
Reads  Art/nickname_dialog_raw.png
Writes Assets/Sprites/{ui_dialog_panel,btn_okay,btn_cancel}.png

Art/nickname_layout_reference.png is the composed mockup, kept for the same
reason the menu's is: it is what the builder was measured against, not
something the game draws.
"""
from PIL import Image
import numpy as np
import os

SPRITES = 'Assets/Sprites'
SRC = 'Art/nickname_dialog_raw.png'

CUT = 705            # first row of the OKAY/CANCEL plates
VISIBLE = 8          # alpha at or below this is nothing
PAD = 2              # breathing room around a trim, as in key_ui_sprites

# Page -> 1920x1080 canvas, the same 0.96 the main menu is laid out with.
SCALE = 0.96
PAGE = (2000, 1125)


def bands(drawn, axis):
    """Runs of rows (axis=1) or columns (axis=0) that hold artwork."""
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
    """Tight box around the artwork in a region, padded but never past it.

    The padding stops at the region's own edges, which matters at the cut: the
    ropes run right up to it, so two spare pixels would put rope on the plates
    and plate on the board.
    """
    ys, xs = np.where(drawn[y0:y1+1, x0:x1+1])
    return (max(x0, x0+xs.min()-PAD), max(y0, y0+ys.min()-PAD),
            min(x1+1, x0+xs.max()+1+PAD), min(y1+1, y0+ys.max()+1+PAD))


def unity(b):
    """A page box as the builder wants it: centred anchoredPosition and size."""
    x0, y0, x1, y1 = b
    return (round((((x0+x1)/2) - PAGE[0]/2) * SCALE),
            round((PAGE[1]/2 - ((y0+y1)/2)) * SCALE),
            round((x1-x0) * SCALE), round((y1-y0) * SCALE))


def report(name, b):
    """One line a person can copy straight into NicknameDialogBuilder."""
    print('  %-16s page %-26s ->  anchoredPosition (%d, %d), sizeDelta (%d, %d)'
          % ((name, str(tuple(int(v) for v in b))) + unity(b)))


if __name__ == '__main__':
    if not os.path.exists(SRC):
        raise SystemExit('No %s yet - put the Canva export there first.' % SRC)
    sheet = Image.open(SRC).convert('RGBA')
    a = np.asarray(sheet)
    drawn = a[..., 3] > VISIBLE

    plates = bands(drawn[CUT:], 0)
    if len(plates) != 2:
        raise SystemExit('Below row %d the page holds %d plate(s), expected OKAY '
                         'and CANCEL. Check the export, or move CUT.' % (CUT, len(plates)))

    pieces = [('ui_dialog_panel', box(drawn, 0, CUT-1, 0, sheet.size[0]-1))]
    for name, (x0, x1) in zip(('btn_okay', 'btn_cancel'), plates):
        pieces.append((name, box(drawn, CUT, sheet.size[1]-1, x0, x1)))

    for name, b in pieces:
        sheet.crop(b).save(os.path.join(SPRITES, name + '.png'))
        report(name, b)

    # The white name field is painted into the board; the builder needs to know
    # where to lay the invisible InputField over it.
    white = (a[..., :3].min(axis=2) > 225) & drawn
    wide = [y for y in range(CUT) if white[y].sum() > 600]
    xs = np.where(white[wide[0]:wide[-1]+1].any(axis=0))[0]
    report('name field', (xs.min(), wide[0], xs.max()+1, wide[-1]+1))
