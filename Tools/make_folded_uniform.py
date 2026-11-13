"""Draw the school uniform FOLDED, for Scene 4's bed.

Rachel's line in this scene is "Kylo cannot go to school in his towel. His
uniform is folded on the bed." It was not. prop_damit.png is the shirt cut
off the dresser rack in cut_bedroom_props.py - it hangs on a coat hanger -
and the scene laid it on the floorboards with the socks, the shoes and the
bag. So the one line of English in the scene pointed at a bed the child could
see was empty.

WHAT THE UNIFORM ACTUALLY IS. Not the shirt on the rack: that one is a
button-up with a collar, and nothing in this game wears it. Kylo's uniform,
in talking_uniform.png and every other dressed pose, is a plain white
crew-neck tee and navy shorts. This is drawn from those two - their colours
are sampled out of that sprite - so the folded thing on the bed is the thing
he is wearing two seconds later.

THE SILHOUETTE DOES THE WORK. Three attempts at this were a rounded white
slab with a collar drawn on it, and a rounded slab on a darker base is a
sofa before it is anything else - no amount of shading inside the shape
argues with its outline. So the outline is the shape now: the neckline is
CUT OUT of the top edge, and the folded sleeves stick out as tabs at the
sides. A notch at the top and a bump on each side is a shirt at any size,
including the ninety-odd pixels this gets on a phone.

Built as masks rather than drawings for that reason - body, two sleeves and
the neck hole combined into one silhouette, and the dark line found by
growing it. That is also the only way the line comes out even around a shape
made of four overlapping pieces.

The navy shorts under it are two shallow folded layers, not a slab. They are
there for the colour as much as the word: white cloth on a yellow blanket
needs something dark in it to be picked out at all.

Writes, overwriting:

  Assets/Sprites/prop_damit_folded.png

prop_damit.png is left alone - nothing uses it now, but it is what the
dresser rack in Scene 1 is missing, and re-cutting it is not free.

Run from the repository root:  python3 Tools/make_folded_uniform.py
"""
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage
import numpy as np
import io, os, re, uuid

SPRITES = "Assets/Sprites"
MODEL = "prop_medyas"          # the sprite whose import settings this copies
SOURCE = "talking_uniform"     # the pose the colours are sampled from
OUT = "prop_damit_folded"

# Sampled off talking_uniform.png - the white of the tee and the navy of the
# shorts he is wearing - and the dark line off prop_medyas.png, so the three
# props on the bed are drawn with one pen.
OUTLINE = (45, 33, 34)
# Warmed towards the lamp in the room: the bedroom is painted in evening
# light and a pure-white prop on that yellow blanket is a cut-out.
CLOTH = (235, 230, 222)
CLOTH_LIT = (247, 243, 235)
CLOTH_DIM = (201, 195, 189)
CREASE = (180, 174, 169)
NAVY = (41, 56, 83)
NAVY_LIT = (62, 81, 114)
NAVY_DIM = (26, 36, 56)
# Dark enough to be seen on a painted yellow blanket. At 95 it was
# invisible there and the stack looked stuck on rather than put down.
SHADOW = (46, 28, 18, 140)

SS = 3                         # supersampling, for clean curves at this size
W, H = 960, 640
LINE = 9                       # outline thickness, in final pixels

# --- The folded tee ---
BODY = (240, 120, 720, 350)
BODY_R = 26
# The folded sleeve ends. They step out of the body rather than stick out of
# it: at sprite size a tab that clears the body by its own width stops being
# a sleeve and becomes a handle, which is what the first pass drew.
# Not mirrored: one sleeve is folded a little lower and a little shorter
# than the other, because nobody folds both the same.
SLEEVE_L = (192, 168, 252, 312)
SLEEVE_R = (710, 182, 766, 318)
SLEEVE_R_RAD = 26
NECK = (408, 74, 552, 176)             # a scoop cut THROUGH the top edge
RIB = (386, 56, 574, 194)              # the ribbing around it, inside the body
FOLD_Y = 306                           # where the bottom fold shows its layers
HALF_FOLD = 236                        # the tee's own fold across the middle

# --- The folded shorts under it ---
# Offset right of the tee rather than centred under it: a stack squared up
# to the millimetre is a stack nobody made.
SHORTS = (226, 336, 756, 424)
SHORTS_R = 12
SHORTS_FOLD = 388

CX = (BODY[0] + BODY[2]) // 2


def s(v):
    if isinstance(v, (tuple, list)):
        return tuple(s(x) for x in v)
    return v * SS


def sag(y, x0, x1, drop, steps=28):
    """A fold edge, lowest in the middle. Cloth does not fold in a straight
    line, and a ruler-straight crease is what makes a drawn prop look like
    furniture."""
    cx, half = (x0 + x1) / 2.0, (x1 - x0) / 2.0
    pts = []
    for i in range(steps + 1):
        x = x0 + (x1 - x0) * i / steps
        t = (x - cx) / half
        pts.append((s(x), s(y + drop * (1.0 - t * t))))
    return pts


def alpha_of(draw_shape, size):
    """Rasterise a silhouette on its own, so it can be grown into an outline."""
    m = Image.new("L", size, 0)
    draw_shape(ImageDraw.Draw(m))
    return m


def with_outline(img, shape, paint):
    """One piece: its dark line, found by growing its silhouette, then its
    own shading clipped back inside it.

    The line is grown with a distance transform rather than by drawing each
    part bigger. A shirt here is a body, two sleeves and a hole, and drawing
    four bigger shapes leaves the line doubled where they overlap and missing
    where the hole cuts in.
    """
    size = img.size
    mask = alpha_of(shape, size)
    a = np.asarray(mask) > 127
    grown = ndimage.distance_transform_edt(~a) <= LINE * SS

    back = Image.new("RGBA", size, OUTLINE + (255,))
    back.putalpha(Image.fromarray((grown * 255).astype(np.uint8)))

    flat = Image.new("RGBA", size, (0, 0, 0, 0))
    soft = Image.new("RGBA", size, (0, 0, 0, 0))
    paint(ImageDraw.Draw(flat), ImageDraw.Draw(soft))
    face = Image.alpha_composite(flat, soft.filter(ImageFilter.GaussianBlur(8 * SS)))
    face.putalpha(Image.fromarray(
        (np.asarray(face.split()[3]).astype(int) * np.asarray(mask).astype(int)
         // 255).astype(np.uint8)))

    return Image.alpha_composite(img, Image.alpha_composite(back, face))


def tee_shape(d):
    d.rounded_rectangle(s(SLEEVE_L), radius=s(SLEEVE_R_RAD), fill=255)
    d.rounded_rectangle(s(SLEEVE_R), radius=s(SLEEVE_R_RAD), fill=255)
    d.rounded_rectangle(s(BODY), radius=s(BODY_R), fill=255)
    # The neckline is a scoop out of the TOP EDGE, not a hole in the middle.
    # As a closed ring it came out as the handle of a basket.
    d.ellipse(s(NECK), fill=0)


def shorts_shape(d):
    d.rounded_rectangle(s(SHORTS), radius=s(SHORTS_R), fill=255)


def gradient(d, x0, y0, x1, y1, top, bottom):
    span = max(1, y1 - y0)
    for y in range(y0, y1 + 1):
        t = (y - y0) / span
        c = tuple(int(round(a + (b - a) * t)) for a, b in zip(top, bottom))
        d.line([s(x0), s(y), s(x1), s(y)], fill=c + (255,), width=SS + 1)


def paint_tee(flat, soft):
    x0, y0, x1, y1 = BODY
    # Lit from the window behind the bed, falling away towards the viewer.
    gradient(flat, x0 - 80, y0 - 50, x1 + 80, FOLD_Y, CLOTH_LIT, CLOTH)

    # The bottom fold: the cut edge of the cloth, facing the floor rather
    # than the window, with the lit edge of the layer under it.
    flat.rectangle(s((x0 - 80, FOLD_Y, x1 + 80, y1 + 20)), fill=CLOTH_DIM + (255,))
    flat.line(sag(FOLD_Y, x0, x1, 8), fill=CREASE + (255,), width=5 * SS)
    flat.line(sag(FOLD_Y + 8, x0, x1, 8), fill=CLOTH_LIT + (230,), width=5 * SS)

    # The ribbing round the scoop, a shade down from the body, so the neck
    # does not read as a bite taken out of a white rectangle. Painted only -
    # it is not part of the silhouette, so it cannot stick up past the
    # shoulders.
    flat.ellipse(s(RIB), fill=CLOTH_DIM + (255,))
    flat.ellipse(s((RIB[0] + 16, RIB[1] + 14, RIB[2] - 16, RIB[3] - 14)),
                 fill=CLOTH_LIT + (255,))

    # Where each sleeve folds back over the body.
    for x in (BODY[0] + 44, BODY[2] - 44):
        flat.line([s(x), s(150), s(x), s(FOLD_Y - 6)], fill=CREASE + (190,), width=4 * SS)

    # The tee's own fold across the middle, soft because it is a crease in
    # cloth lying flat rather than an edge you could put a finger under.
    soft.line(sag(HALF_FOLD, x0 + 6, x1 - 6, 7), fill=CREASE + (190,), width=6 * SS)

    # The sleeves lie under the body, so the body casts onto them.
    soft.rectangle(s((SLEEVE_L[0] - 10, SLEEVE_L[1] - 10, BODY[0] + 26, SLEEVE_L[3] + 10)),
                   fill=CLOTH_DIM + (170,))
    soft.rectangle(s((BODY[2] - 26, SLEEVE_R[1] - 10, SLEEVE_R[2] + 10, SLEEVE_R[3] + 10)),
                   fill=CLOTH_DIM + (170,))
    soft.rectangle(s((x0 - 80, FOLD_Y - 28, x1 + 80, FOLD_Y)), fill=CLOTH_DIM + (130,))


def paint_shorts(flat, soft):
    x0, y0, x1, y1 = SHORTS
    flat.rectangle(s(SHORTS), fill=NAVY + (255,))
    gradient(flat, x0, y1 - 18, x1, y1, NAVY, NAVY_DIM)
    flat.line(sag(SHORTS_FOLD, x0 + 4, x1 - 4, 6), fill=NAVY_DIM + (255,), width=5 * SS)
    flat.line(sag(SHORTS_FOLD + 7, x0 + 4, x1 - 4, 6), fill=NAVY_LIT + (235,), width=4 * SS)
    # The tee sits on them, so their top edge is in its shadow.
    soft.rectangle(s((x0, y0, x1, y0 + 30)), fill=NAVY_DIM + (210,))


def handmade(img):
    """Push the whole sprite about with a smooth random field, then tip it.

    Everything above is built out of rounded rectangles, and rounded
    rectangles in a row are an appliance: at sprite size the thing read as a
    printer sitting on the bed. Cloth has no two edges the same and nothing
    lying on a bed is square to the camera, so every edge gets a few pixels
    of wander and the whole stack a couple of degrees of tilt. The field is
    smooth and low-frequency, so edges bend rather than fray, and the SAME
    field moves the paint and the dark line together.

    Seeded, so the sprite does not change every time this is run.
    """
    rng = np.random.default_rng(20261113)
    a = np.asarray(img).astype(np.float32)
    h, w = a.shape[:2]

    small = (max(4, h // 90), max(4, w // 90))
    field = []
    for _ in range(2):
        n = ndimage.gaussian_filter(rng.standard_normal(small), 1.1)
        n = ndimage.zoom(n, (h / small[0], w / small[1]), order=3)[:h, :w]
        peak = max(1e-6, np.abs(n).max())
        field.append(n / peak * (7 * SS))

    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    coords = np.stack([yy + field[0], xx + field[1]])
    out = np.stack([ndimage.map_coordinates(a[..., c], coords, order=1, mode="constant")
                    for c in range(4)], axis=-1)

    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).rotate(
        -2.4, resample=Image.BICUBIC, expand=True)


def meta_for(name):
    """Give the new sprite the same import settings as the one it sits beside."""
    out = os.path.join(SPRITES, name + ".png.meta")
    if os.path.exists(out):
        return
    base = io.open(os.path.join(SPRITES, MODEL + ".png.meta"), encoding="utf-8").read()
    t = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + uuid.uuid4().hex, base, count=1, flags=re.M)
    t = t.replace(MODEL + "_0", name + "_0")
    io.open(out, "w", encoding="utf-8").write(t)


def main():
    size = (W * SS, H * SS)
    img = Image.new("RGBA", size, (0, 0, 0, 0))

    # Contact shadow first, so the stack sits on the blanket rather than
    # floating above it.
    sh = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse(s((228, 396, 734, 470)), fill=SHADOW)
    img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(12 * SS)))

    img = with_outline(img, shorts_shape, paint_shorts)
    img = with_outline(img, tee_shape, paint_tee)

    img = handmade(img)
    img = img.resize((img.size[0] // SS, img.size[1] // SS), Image.LANCZOS)
    box = img.getbbox()
    pad = 4
    img = img.crop((max(0, box[0] - pad), max(0, box[1] - pad),
                    min(img.size[0], box[2] + pad), min(img.size[1], box[3] + pad)))

    img.save(os.path.join(SPRITES, OUT + ".png"))
    meta_for(OUT)
    print("  %s.png  %dx%d" % (OUT, img.size[0], img.size[1]))


main()
