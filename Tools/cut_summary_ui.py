"""Cut the group's level-summary mock-up into the sprites Unity needs.

The mock-up is one flat 1024x1024 picture: title, panel, plaque, stars, three
icon medallions, their labels, and three buttons, all baked together on a dark
background. A game cannot use it like that - the figures have to change every
run - so this splits it into parts and, crucially, WIPES the three label-and-
value blocks off the panel. Unity then draws live text in the holes.

The labels are replaced rather than kept because none of the originals belong
to this game:

  TOTAL SCORE     ->  WORDS SAID        how many Filipino words landed
  TIME            ->  VOICE TRIES       how many attempts that took
  GEMS COLLECTED  ->  WORDS PRACTISED   which words they were

The icons are left exactly as drawn, because the group asked for their UI
unchanged apart from the wording.

Wiping is done by interpolating across each row between clean panel wood just
left of the text and just right of it. The interior is a slow horizontal
gradient, so a straight blend reads as untouched board; copying a patch from
elsewhere left a visible seam where the gradient did not line up.

Reads   Art/level_summary_raw.jpg   (the mock-up, copied in by this script's caller)
Writes  Assets/Sprites/
          ui_summary_panel.png      the board, icons kept, label text gone
          ui_summary_title.png      "LEVEL COMPLETE"
          ui_summary_plaque.png     the square plaque that holds the level number
          ui_summary_star_on.png    a lit star
          ui_summary_star_off.png   the same star, dimmed
          btn_summary_replay.png    replay arrow
          btn_summary_home.png      house
          btn_summary_next.png      "Next Level" arrow

Run from the repository root:  python Tools/cut_summary_ui.py
"""
import os
from PIL import Image, ImageDraw, ImageFilter

SRC = "Art/level_summary_raw.jpg"
OUT = "Assets/Sprites/"

# Measured off the mock-up at 1024x1024.
PANEL = (110, 208, 932, 706)
TITLE = (168, 44, 868, 172)
PLAQUE = (222, 286, 470, 500)
STAR = (196, 522, 296, 618)          # the left-hand star, used for both states
BUTTONS = {
    "btn_summary_replay": (116, 744, 356, 876),
    "btn_summary_home": (396, 744, 630, 876),
    "btn_summary_next": (660, 730, 926, 868),
}

# Everything baked into the board that has to become live text, in source
# coordinates. The three label+value blocks, and the "7" on the plaque - a
# painted level number would be wrong in a game that has three levels.
TEXT_BLOCKS = [(648, 292, 872, 386),      # TOTAL SCORE / 24,500
               (648, 410, 872, 500),      # TIME / 2:34
               (648, 524, 872, 614),      # GEMS COLLECTED / 15/15
               (288, 318, 412, 474),      # the "7"
               (188, 514, 514, 626)]      # the three stars, all lit in the art
SAMPLE = 9                                # columns averaged either side of a block

# The three round medallions, as centre and radius on the mock-up. They sit on
# the board like the stars do, but a brightness test cannot lift them either:
# the disc is darker than the wood while the symbol inside it is lighter. The
# shape is a known circle, so it is masked as one.
ICONS = {
    # Radius 45 rather than 50: the medallion is drawn with a pale ring around
    # its rim, which reads as a white border once it is on the board.
    "ui_summary_icon_chest": (585, 335, 45),
    "ui_summary_icon_clock": (585, 448, 45),
    "ui_summary_icon_gem": (585, 562, 45),
}
FEATHER = 10                              # rows/columns the wipe fades out over


def wipe_text(img):
    """
    Blend clean wood across each baked block, left edge to right edge.

    The patch is composited through a mask that fades out over FEATHER pixels
    on every side. A hard-edged patch left faint horizontal bands where the
    replaced gradient met the untouched board - the blocks are padded past the
    lettering, so the pixels being blended in the border are wood either way.
    """
    for block in TEXT_BLOCKS:
        wipe_block(img, block)
    return img


def wipe_block(img, block):
    """Blend clean wood across one rectangle, fading out at its border."""
    if True:
        x0, y0, x1, y1 = block
        patch = Image.new("RGB", (x1 - x0, y1 - y0))
        ppx = patch.load()
        spx = img.load()

        for y in range(y0, y1):
            left = [0.0, 0.0, 0.0]
            right = [0.0, 0.0, 0.0]
            for i in range(SAMPLE):
                lp = spx[x0 - 1 - i, y]
                rp = spx[x1 + i, y]
                for c in range(3):
                    left[c] += lp[c]
                    right[c] += rp[c]
            left = [v / SAMPLE for v in left]
            right = [v / SAMPLE for v in right]

            span = x1 - x0
            for x in range(x0, x1):
                t = (x - x0) / span
                ppx[x - x0, y - y0] = tuple(
                    int(left[c] + (right[c] - left[c]) * t) for c in range(3))

        w, h = patch.size
        mask = Image.new("L", (w, h), 255)
        mpx = mask.load()
        for y in range(h):
            fy = min(1.0, min(y, h - 1 - y) / float(FEATHER))
            for x in range(w):
                fx = min(1.0, min(x, w - 1 - x) / float(FEATHER))
                mpx[x, y] = int(255 * min(fx, fy))

        img.paste(patch, (x0, y0), mask)
    return img


def cut(img, box, feather=True, trim=True, threshold=46):
    """
    Crop a piece and clear the dark background behind it.

    trim=False keeps the crop box exactly. The panel needs that: the Unity
    builder places everything on it by the coordinates measured off this
    mock-up, and trimming to the drawn pixels would shift that origin by a few
    pixels and drag every row with it.
    """
    piece = img.crop(box).convert("RGBA")
    w, h = piece.size

    # The backdrop is near-black; every drawn element is far brighter. A flood
    # from the corners would also eat the dark brown outlines, so this keeps
    # to a luminance cut instead.
    mask = Image.new("L", (w, h), 0)
    mpx = mask.load()
    ppx = piece.load()
    for y in range(h):
        for x in range(w):
            r, g, b, _ = ppx[x, y]
            luma = 0.299 * r + 0.587 * g + 0.114 * b
            mpx[x, y] = 255 if luma > threshold else 0

    if feather:
        mask = mask.filter(ImageFilter.MaxFilter(3))      # close pinholes in the outline
        mask = mask.filter(ImageFilter.GaussianBlur(0.8))

    piece.putalpha(mask)
    return piece.crop(piece.getbbox()) if trim else piece


def cut_star(img):
    """
    Lift a star off the BOARD rather than off the backdrop.

    Everything else in this mock-up sits on the dark background, so a
    luminance cut separates it. The stars do not - they sit on the tan board,
    whose corners read brighter than some of the star, so that test keeps the
    whole crop and the dimmed copy came out as a dark rectangle.

    Blue is what actually separates them: the star is pale yellow (blue around
    128) against wood (blue around 64). The glow keeps a soft edge, which is
    what it should have.
    """
    piece = img.crop(STAR).convert("RGBA")
    w, h = piece.size
    mask = Image.new("L", (w, h), 0)
    mpx, ppx = mask.load(), piece.load()
    for y in range(h):
        for x in range(w):
            b = ppx[x, y][2]
            mpx[x, y] = 0 if b <= 78 else (255 if b >= 108 else int((b - 78) * 255 / 30))
    mask = mask.filter(ImageFilter.GaussianBlur(0.7))
    piece.putalpha(mask)
    return piece


def cut_plaque(img):
    """
    The plaque on its own, taken from the board AFTER the painted "7" has been
    wiped off it. Like the stars and the medallions it sits on wood, not on the
    backdrop, so it is masked by its own rounded-square outline rather than by
    brightness.
    """
    piece = img.crop(PLAQUE).convert("RGBA")
    mask = Image.new("L", piece.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [0, 0, piece.size[0] - 1, piece.size[1] - 1], radius=36, fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(0.7))
    piece.putalpha(mask)
    return piece


def cut_icon(img, cx, cy, r):
    """One medallion, masked as the circle it is."""
    pad = r + 3
    piece = img.crop((cx - pad, cy - pad, cx + pad, cy + pad)).convert("RGBA")
    size = piece.size[0]
    mask = Image.new("L", (size, size), 0)
    mpx = mask.load()
    centre = size / 2.0
    for y in range(size):
        for x in range(size):
            dist = ((x + 0.5 - centre) ** 2 + (y + 0.5 - centre) ** 2) ** 0.5
            if dist <= r - 1:
                mpx[x, y] = 255
            elif dist < r + 1:
                mpx[x, y] = int(255 * (r + 1 - dist) / 2.0)
    piece.putalpha(mask)
    return piece


def wipe_icons(img):
    """
    Blend board over the medallions, for a bare frame with nothing on it.

    Goes through the same feathered blend the lettering uses. A hard-edged
    patch left visible streaks here, because a medallion is far wider than a
    line of text and the mismatch at its edge had further to travel.
    """
    for cx, cy, r in ICONS.values():
        wipe_block(img, (cx - r - 4, cy - r - 4, cx + r + 4, cy + r + 4))
    return img


def main():
    """
    Produce ONLY the three medallions.

    This script used to cut a whole interface out of the composed mock-up.
    Tools/cut_summary_kit.py replaced all of that from the proper sprite kit,
    which needs none of the wiping and guessing this one does. What the kit
    does not have is the symbols already mounted on their dark discs - its
    glyphs are drawn for a light background - so those three are still taken
    from here.

    Everything else it used to write is gone deliberately. It shared file
    names with the kit cutter (ui_summary_board, btn_summary_next and others),
    so running this one afterwards quietly replaced kit sprites with the old
    composite versions, and the board came back with the streaks left where
    its medallions had been wiped out.
    """
    if not os.path.exists(SRC):
        print("No %s - copy the mock-up there first." % SRC)
        return

    src = Image.open(SRC).convert("RGB")
    if src.size != (1024, 1024):
        src = src.resize((1024, 1024), Image.LANCZOS)

    for name, (cx, cy, r) in ICONS.items():
        piece = cut_icon(src, cx, cy, r)
        piece.save(OUT + name + ".png")
        print("%-24s %s" % (name, piece.size))


if __name__ == "__main__":
    main()
