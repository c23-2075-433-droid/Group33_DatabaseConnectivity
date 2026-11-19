"""Put Kylo's own head and skin back on a generated drawing of him.

WHY. The Canva frames for Paaralan Scene 5 got the MOVEMENT right and the BOY
slightly wrong: the generator redrew his hair a different shape, his face a
little rounder with different eyes, and his skin a paler yellow. The group's
instruction for this was plain - the animations are good, but do not change
Kylo's hair, skin tone or face. So the body stays Canva's and everything that
makes him recognisably him is put back from the drawings he already has:

  his front head    tayo_stand_uniform, the drawing he stands up as in
                    "Tayo" a moment before Kunin. Crown to neck it is 413
                    rows, the same as the Canva front poses to two rows, and
                    the necks are both 83 wide - so it goes on at 1:1.
  his profile head  standing_uniform_with_bag, Paaralan Scene 2's side view,
                    mirrored to face left and scaled so it is the front
                    head's size: both are 215 rows crown to neck in the
                    745-row Paaralan set.
  his skin          tayo_stand_uniform's, by matching the mean and spread of
                    each channel over the skin. Shading survives because the
                    mapping is linear; outlines survive because only skin is
                    touched, weighted by how skin-coloured each pixel is.

HOW THE GENERATED HEAD COMES OFF. Not by cutting along a row - a raised hand
is above the neck, and so is a shoulder on a lean - but by the head's SHAPE:
an ellipse from his crown to his neck and across his hair's widest extent.
Skin that lies mostly inside it is head (the face behind the glasses is half
a dozen pieces of skin, which is why it cannot be found as one); every other
piece is an arm or a hand, however close to his face it comes, and is laid
back over the new head afterwards, because a hand waving beside a face is in
front of it. The bag and its straps are kept by colour, as long as they run
down past the neck - Canva's hair has blue in its highlights, and that kept
stray tufts of it the first time.

WHAT IT CANNOT DO. There is no three-quarter drawing of his head. The turn
uses the front head narrowed to 86%, which is the precedent the Paaralan 2
turn set (bati_turn_*): a front view turning by getting narrower. And his
eyes do not move - Tools/look_down.py has the two attempts at that and why
they read as broken - so when he looks at his bag his head tips toward it
instead.
"""
import math
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_down import SPRITES                                     # noqa: E402

FRONT_FROM = "tayo_stand_uniform"
SIDE_FROM = "standing_uniform_with_bag"

FRONT_NECK = (709, 332)      # tayo_stand_uniform: narrowest row, its centre
SIDE_NECK = (215, 95)        # the side view mirrored: its 40px skin run
HEAD_H = 413                 # crown to neck, front head and Canva front poses
SIDE_SCALE = HEAD_H / 215.0  # profile head to the front head's size
TURN_NARROW = 0.86
SKIRT = 18                   # rows of his own neck carried below the join


def load(name):
    return Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")


def skin_weight(a):
    """0..1: how much of each pixel is skin. Hue-tested so the white shirt,
    the navy and the outlines are 0, and ramped so anti-aliasing blends."""
    r, g, b, al = [a[:, :, i].astype(float) for i in range(4)]
    hue = (r > g + 10) & (g > b + 10) & (r - b > 45)
    return np.clip((r - 150) / 40.0, 0, 1) * hue * (al > 0)


def skin_stats(a):
    px = a[:, :, :3][skin_weight(a) > 0.99].astype(float)
    return px.mean(0), px.std(0)


TARGET_SKIN = skin_stats(np.asarray(load(FRONT_FROM)))


def recolour(im):
    """His skin, per channel, mean and spread matched to his own drawing."""
    a = np.asarray(im).astype(float)
    mu, sd = skin_stats(a.astype(np.uint8))
    tm, ts = TARGET_SKIN
    w = skin_weight(a.astype(np.uint8))[:, :, None]
    mapped = np.clip((a[:, :, :3] - mu) / sd * ts + tm, 0, 255)
    a[:, :, :3] = a[:, :, :3] * (1 - w) + mapped * w
    return Image.fromarray(a.astype(np.uint8))


def head_top_and_col(a):
    """The crown row and the column of the hair-and-glasses mass."""
    r, g, b, al = [a[:, :, i].astype(int) for i in range(4)]
    dark = (al > 120) & (r < 60) & (g < 60) & (b < 60)
    dark[900:, :] = False
    ys, xs = np.where(dark)
    return int(ys.min()), int(np.median(xs))


def neck_at(a, row, col):
    """The skin run on `row` nearest `col`: (row, centre column)."""
    xs = np.where(skin_weight(a)[row] > 0.3)[0]
    if len(xs) == 0:
        return row, col
    runs, start = [], xs[0]
    for i in range(1, len(xs)):
        if xs[i] != xs[i - 1] + 1:
            runs.append((start, xs[i - 1]))
            start = xs[i]
    runs.append((start, xs[-1]))
    l, r = min(runs, key=lambda t: 0 if t[0] <= col <= t[1]
               else min(abs(t[0] - col), abs(t[1] - col)))
    return row, (l + r) // 2


def head_ellipse(a, neck_row, top):
    """The generated head's outline: crown to neck, across his hair."""
    r, g, b, al = [a[:, :, i].astype(int) for i in range(4)]
    dark = (al > 120) & (r < 70) & (g < 70) & (b < 80)
    dark[neck_row:, :] = False
    lab, n = ndimage.label(dark)
    sizes = ndimage.sum(dark, lab, range(1, n + 1))
    xs = np.where(lab == int(np.argmax(sizes)) + 1)[1]
    cx, rx = (xs.min() + xs.max()) / 2.0, (xs.max() - xs.min()) / 2.0 + 10
    cy, ry = (top + neck_row) / 2.0, (neck_row - top) / 2.0 + 6
    yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    return ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1.0


def take_head_off(a, neck_row, top):
    """The figure without its generated head, and what goes back over the new
    one: the arms and hands that were up beside it."""
    r, g, b, al = [a[:, :, i].astype(int) for i in range(4)]
    head = head_ellipse(a, neck_row, top)
    region = ndimage.binary_dilation(head, iterations=14)
    skin = skin_weight(a) > 0.3
    lab, n = ndimage.label(skin)
    keep = np.zeros(al.shape, bool)
    for i in range(1, n + 1):
        piece = lab == i
        if (piece & head).sum() < 0.5 * piece.sum():
            keep |= piece
    keep = ndimage.binary_dilation(keep, iterations=4) & (al > 0)
    navy = (b > r + 15) & (r < 90) & (al > 60)
    nlab, nn = ndimage.label(navy)
    for i in range(1, nn + 1):
        piece = nlab == i
        if np.where(piece)[0].max() > neck_row + 30:
            keep |= ndimage.binary_dilation(piece, iterations=2) & (al > 0)
    out = a.copy()
    out[region & ~keep, 3] = 0
    front = np.zeros_like(a)
    sel = keep & region & ~navy & ~head
    front[sel] = a[sel]
    return out, front


def head_piece(src, neck):
    """A head cut SKIRT rows below its neck, and where that neck is."""
    a = np.asarray(src).copy()
    a[neck[0] + SKIRT:, :, 3] = 0
    return Image.fromarray(a), neck


_FRONT = head_piece(load(FRONT_FROM), FRONT_NECK)
_SIDE = head_piece(load(SIDE_FROM).transpose(Image.FLIP_LEFT_RIGHT), SIDE_NECK)


def drop_specks(im, frac=0.002):
    """Hair tips the generator drew outside its own head outline, left
    floating once the head is gone."""
    a = np.asarray(im).copy()
    lab, n = ndimage.label(a[:, :, 3] > 30)
    if n <= 1:
        return im
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    small = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s < sizes.max() * frac])
    a[small] = 0
    return Image.fromarray(a)


def own_head(im, kind, neck=None, tilt=None):
    """`im` with Kylo's own head and skin.

    kind   "front", "turn" (front head narrowed) or "side" (profile, facing
           left).
    neck   (row, col) where the generated head joins the body, if it is not
           HEAD_H below the crown - a head looking down or leaning out.
    tilt   degrees to tip his head, + anticlockwise; None measures the lean
           of the generated head from its neck to its hair.
    """
    im = recolour(im)
    a = np.asarray(im)
    top, col = head_top_and_col(a)
    if neck is None:
        h = HEAD_H if kind in ("front", "turn") else int(round(215 * SIDE_SCALE + 18))
        neck = neck_at(a, top + h, col)
    ny, nx = neck

    if tilt is None:
        r, g, b, al = [a[:, :, i].astype(int) for i in range(4)]
        ys, xs = np.where((al > 120) & (np.maximum(np.maximum(r, g), b) < 60)
                          & (np.arange(a.shape[0])[:, None] < ny))
        tilt = -math.degrees(math.atan2(xs.mean() - nx, ny - ys.mean()))
        if abs(tilt) < 2.0 or kind != "front":
            tilt = 0.0

    body, front = take_head_off(a, ny, top)
    piece, pneck = _SIDE if kind == "side" else _FRONT
    sx = sy = 1.0
    if kind == "side":
        sx = sy = SIDE_SCALE
    elif kind == "turn":
        sx = TURN_NARROW
    if sx != 1.0 or sy != 1.0:
        piece = piece.resize((int(piece.size[0] * sx), int(piece.size[1] * sy)), Image.LANCZOS)
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    layer.alpha_composite(piece, (int(round(nx - pneck[1] * sx)), int(round(ny - pneck[0] * sy))))
    if tilt:
        layer = layer.rotate(tilt, resample=Image.BICUBIC, center=(nx, ny + SKIRT))
    out = Image.fromarray(body)
    out.alpha_composite(layer)
    out.alpha_composite(Image.fromarray(front))
    return drop_specks(out), (ny, nx, tilt)
