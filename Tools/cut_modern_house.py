"""Lift the modern house out of the adventure mockup.

A stopgap, and it says so plainly. The bahay kubo the journey map uses for
Bahay reads as rustic next to the rest of this interface, and the mockup the
group drew the picker against already has a modern cottage sitting in the
HOME board - red roof, lit windows, smoke from the chimney. This cuts that
cottage out of the mockup so the screen has it now.

It is a key, not an export: the cottage is painted onto the board's wood, so
the wood is flood-filled away from the edges inward. Wood is recognised by
its hue rather than by one sampled colour, because the plank is grained and
knotted - but a dark roof tile is brown too, so the test also rejects
anything as red as the roof, or the fill creeps up under the tiles and
leaves holes in them.

Replace this whenever the cottage can be exported from Canva with its own
alpha. That will be cleaner than any keying, and then this file can go.

Run from the repository root:  python3 Tools/cut_modern_house.py
Reads  Art/adventure_layout_reference.png
Writes Assets/Sprites/map_bahay_modern.png
       Assets/Sprites/map_bahay_modern_locked.png
"""
from PIL import Image, ImageFilter
from collections import deque
import numpy as np
import io, os, re, uuid

SPRITES = 'Assets/Sprites'
SRC = 'Art/adventure_layout_reference.png'

# The HOME board's inner panel on the 2000x1125 mockup, pulled inside the
# board's pale rim so every edge pixel of the crop is plank.
PANEL = (125, 478, 632, 828)

TOL = 52          # how far one plank pixel may differ from its neighbour
PAD = 3


def is_wood(p):
    """Grained plank, and not the roof.

    Plank sits around (145, 78, 42): red well above green, green a little
    above blue, nothing bright. A roof tile is (190, 55, 45) - far redder -
    so capping red-minus-green keeps the fill out of the roof.
    """
    r, g, b = int(p[0]), int(p[1]), int(p[2])
    return 28 < r - g < 95 and g - b > 14 and r < 200 and g < 140


def flood_from_edges(smooth):
    """Plank reachable from the crop's border, which is all of it."""
    h, w, _ = smooth.shape
    mask = np.zeros((h, w), bool)
    queue = deque()

    def seed(y, x):
        if not mask[y, x] and is_wood(smooth[y, x]):
            mask[y, x] = True; queue.append((y, x))

    for x in range(w):
        seed(0, x); seed(h-1, x)
    for y in range(h):
        seed(y, 0); seed(y, w-1)

    while queue:
        y, x = queue.popleft()
        here = smooth[y, x]
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y+dy, x+dx
            if 0 <= ny < h and 0 <= nx < w and not mask[ny, nx]:
                there = smooth[ny, nx]
                if is_wood(there) and int(abs(there-here).sum()) < TOL:
                    mask[ny, nx] = True; queue.append((ny, nx))
    return mask


def edge_of(mask):
    """Pixels of mask that have a neighbour outside it."""
    p = np.pad(mask, 1, constant_values=False)
    return mask & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])


def erode(mask):
    p = np.pad(mask, 1, constant_values=False)
    return p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:] & mask


def dilate(mask):
    p = np.pad(mask, 1, constant_values=False)
    return p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:] | mask


def largest_island(keep):
    """The cottage itself, without the flecks of plank the fill missed."""
    h, w = keep.shape
    label = np.zeros((h, w), int)
    sizes, current = {}, 0
    for y in range(h):
        for x in range(w):
            if keep[y, x] and label[y, x] == 0:
                current += 1; count = 0
                queue = deque([(y, x)]); label[y, x] = current
                while queue:
                    cy, cx = queue.popleft(); count += 1
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        ny, nx = cy+dy, cx+dx
                        if 0 <= ny < h and 0 <= nx < w and keep[ny, nx] and label[ny, nx] == 0:
                            label[ny, nx] = current; queue.append((ny, nx))
                sizes[current] = count
    return label == max(sizes, key=sizes.get)


def write_meta(name, model='menu_bg'):
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


def grey(im):
    """The locked twin, to match map_*_locked on the other boards."""
    a = np.asarray(im).astype(float)
    lum = (a[..., 0]*0.299 + a[..., 1]*0.587 + a[..., 2]*0.114) * 0.88
    return Image.fromarray(np.dstack([lum, lum, lum, a[..., 3]]).round().astype(np.uint8))


if __name__ == '__main__':
    if not os.path.exists(SRC):
        raise SystemExit('No %s - put the mockup there first.' % SRC)

    panel = Image.open(SRC).convert('RGB').crop(PANEL)
    colour = np.asarray(panel).astype(int)
    smooth = np.asarray(panel.filter(ImageFilter.GaussianBlur(1.0))).astype(int)
    h, w, _ = colour.shape

    keep = largest_island(~flood_from_edges(smooth))

    # A dark knot in the plank sits inside the cottage's ground shadow, so it
    # is walled off from the border and survives the fill. It is too dark for
    # is_wood, which is tuned to the lit plank around it, so it is caught by
    # being brown and dark in the one corner it can be in. A bush there is
    # green - g above r - so this cannot bite into one.
    ys, xs = np.mgrid[0:h, 0:w]
    corner = (xs < 0.22*w) & (ys > 0.78*h)
    r, g, b = smooth[..., 0], smooth[..., 1], smooth[..., 2]
    lum = r*0.299 + g*0.587 + b*0.114
    knot = (r > g) & (g >= b) & (lum < 120)
    keep &= ~(corner & knot)

    # The cottage was drawn standing on a soft white ellipse. On the mockup's
    # own board that reads as ground; cut out and laid on this one it reads as
    # a white rim left over from a bad cut, which is worse than no ground at
    # all. It is the only near-white thing along the bottom - the chimney
    # smoke is near-white too, but that is all in the top fifth, and the front
    # steps are a good deal greyer than either.
    ground = (smooth.min(axis=2) > 205) & \
             ((smooth.max(axis=2) - smooth.min(axis=2)) < 26) & \
             (ys > 0.84*h)
    keep &= ~ground

    # Taking the body of the ellipse away leaves its antialiased rim, which is
    # the same white only mixed with the plank, so it is too dark and too warm
    # for the test above and survives as a hairline. It is peeled off instead:
    # three passes, each dropping pale pixels that have become edge pixels.
    # Peeling stops of its own accord at the bushes and at the steps' shading,
    # which are nowhere near this pale, and it only runs along the bottom, so
    # the chimney smoke is never touched.
    pale = (smooth.min(axis=2) > 178) & \
           ((smooth.max(axis=2) - smooth.min(axis=2)) < 32) & \
           (ys > 0.84*h)

    # The front steps are pale too, and peeling by colour alone hollows them
    # out. What tells them apart from a hairline is thickness, not colour: a
    # shape that still has something left after being eroded three deep is a
    # shape, so it is put back out of reach. A rim one or two pixels wide has
    # no core and is left to the peel.
    solid = pale.copy()
    for _ in range(3):
        solid = erode(solid)
    for _ in range(5):
        solid = dilate(solid)

    for _ in range(3):
        keep &= ~(pale & ~solid & edge_of(keep))

    keep = largest_island(keep)

    cut = Image.fromarray(np.dstack([colour.astype(np.uint8),
                                     np.where(keep, 255, 0).astype(np.uint8)]))
    ys, xs = np.where(keep)
    cut = cut.crop((max(0, xs.min()-PAD), max(0, ys.min()-PAD),
                    min(w, xs.max()+1+PAD), min(h, ys.max()+1+PAD)))

    for name, image in (('map_bahay_modern', cut), ('map_bahay_modern_locked', grey(cut))):
        image.save(os.path.join(SPRITES, name + '.png'))
        print('  %-26s -> %dx%d%s' % (name + '.png', image.size[0], image.size[1],
                                      '  + new .meta' if write_meta(name) else ''))
