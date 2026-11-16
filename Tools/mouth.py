"""Give Kylo a mouth that moves, on the one drawing of his face that exists.

WHAT THIS IS FOR. Paaralan Scene 4 is recess: he eats and he drinks. The
frames for it were built by putting food in his fist and rotating it until
the far end of it landed on his lips - and his lips, in every drawing this
project has of him, are a closed sixty-pixel smile. So what played was a boy
holding a sandwich against a shut mouth and waving it, nine times a minute,
for as long as the child took to say the next word. Rule 4 of
Docs/AnimationStandard.md asks for "natural body movement - arms, hands,
head, facial expression where it helps", and this is the one beat in the game
where the face IS the action: eating that does not open a mouth is not
eating.

WHY HIS MOUTH IS REDRAWN RATHER THAN SWAPPED. Tools/look_down.py is the
project's usual answer to "the drawing does not do that" - move the pixels
that are already there. It cannot answer this one. There is no frame of Kylo
with his mouth open in a school uniform, so there are no pixels of an open
mouth to move, and sliding or stretching a closed smile gives a longer closed
smile. The nearest thing the project owns is eat_bite_bahay, which Bahay
Scene 5 generated for its own camera, and it is a different canvas, a
different scale and a different head position.

So the mouth is DRAWN, and eat_bite_bahay is used as the reference for what
it should look like rather than as pixels to copy. Every colour below is
sampled out of it - the near-black lip line, the white upper teeth, the dark
throat behind them, the warm red tongue - so a mouth drawn here and a mouth
drawn there are the same mouth, which is rule 7.

THE THREE MEASUREMENTS EVERYTHING TURNS ON, all read off lapis_frame_3:

    the drawn smile     x 302..365, y 505..512
    the base of his nose    y 482
    the line of his jaw     y 545 at the centre

Sixty-three pixels of width to work in and sixty-three of height between the
nose and the jaw. A mouth drawn to the proportion eat_bite_bahay uses - 30%
of the face's width, 42% of nose-to-chin - comes out at about 68 by 44, which
fits with ten pixels clear at the top and bottom. That is the whole reason
the open mouths here are the size they are: they are not a style choice, they
are what the gap allows.

A JAW OPENS DOWNWARDS, which is why none of these are centred on the old
smile. The upper lip of a face belongs to the skull and does not move; the
lower jaw does all of it. So every open state here keeps its TOP EDGE within
a few pixels of where the closed smile's corners were and grows towards the
chin. Centring them on the smile instead - which the first pass did - raises
the upper lip towards the nose as the mouth opens, and what that reads as is
not a boy opening his mouth but a boy whose whole face is stretching.

WHAT THE CHEEK BULGE AND THE CHIN DROP ARE. Two of these states warp his
face rather than paint on it:

  chewing     a mouthful is in one cheek, so that cheek is further out than
              the other. The jaw line itself has to move for that to read,
              and no amount of shading on top of a jaw line drawn in the
              wrong place will do it. So the pixels are RESAMPLED through a
              smooth sideways displacement centred on the cheek - the
              outline bends out with everything else, because it is being
              carried rather than redrawn.
  wide open   the chin comes down with the jaw. Four pixels, and the reason
              it is only four is measured: his collar is at y 558 and the
              bottom of his chin at 545, so anything past about six closes
              the gap and the chin sits on his own shirt.

Both are the same `warp` below, which is a bilinear resample through a
displacement field. It is the one operation in this file that is not a
drawing.

WHAT THIS FILE DELIBERATELY DOES NOT DO.

  No teeth in the small states. At fourteen pixels of opening a row of teeth
  is two pixels of white with a dark line through it, and after the sprite is
  scaled into the scene it is a grey smear across his lip. The parted and
  pursed states are a dark gap and a lip, which is what that much opening
  actually looks like.

  No eyes. Tools/look_down.py has the long account of why his eyes cannot be
  moved - the irises nearly fill the eye and anything vacated has to be
  filled with something that is not there. A squint while chewing would be
  worth having and is not available.

  No head turn. Same limit the rest of the project works under: there is no
  three-quarter view of this face.

This is a module, not a tool. Tools/make_recess_frames.py imports `apply`.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# Where the drawn smile is looked for. Wide enough to hold it in every
# seated pose the project has - they differ by two or three pixels, because
# they were cut from different strips - and tight enough that his nose above
# and his jaw below are outside it. Both of those are darker than skin and
# would otherwise be taken for part of the mouth.
SMILE_BAND = (492, 532, 290, 380)          # y0, y1, x0, x1

# Anything this much darker than the skin around it, inside the band, is
# ink. His skin there runs 254,188,118; the smile stroke runs about
# 150,96,74. Halfway between the two luminances, which is nowhere near
# either.
INK = 162

# The mouth, sampled out of Assets/Sprites/eat_bite_bahay.png - the one
# drawing in the project of this boy with his mouth open. See the note above
# on why it is sampled rather than copied.
LIP_LINE = (26, 22, 22)                     # the dark edge, as on every outline
THROAT = (96, 32, 24)                       # the back of the mouth, behind the tongue
THROAT_DEEP = (62, 18, 14)
TONGUE = (232, 106, 90)
TONGUE_LIGHT = (246, 138, 120)
TONGUE_CREASE = (196, 72, 60)
TEETH = (250, 249, 246)
TEETH_SHADE = (214, 206, 198)
LIP_WARM = (206, 120, 96)                   # the roll of the lower lip


# ------------------------------------------------------------ finding it

def find_smile(arr, band=SMILE_BAND):
    """The drawn smile's bounding box, as (x0, x1, y0, y1).

    FOUND RATHER THAN HARD-CODED, for the reason Tools/look_down.py gives
    about his eyes: the seated poses came off more than one strip and his
    face is a few pixels across between them. A box measured on one pose and
    applied to another puts the new mouth half a lip to the side of the old
    one, and the old one is still there beside it.
    """
    y0, y1, x0, x1 = band
    sub = arr[y0:y1, x0:x1]
    lum = sub[:, :, :3].mean(axis=2)
    ink = (lum < INK) & (sub[:, :, 3] > 100)
    ys, xs = np.where(ink)
    if len(ys) < 20:
        raise ValueError("no mouth found in %r - is this a face?" % (band,))
    return (int(xs.min()) + x0, int(xs.max()) + x0,
            int(ys.min()) + y0, int(ys.max()) + y0)


# ------------------------------------------------------- painting it out

def erase(img, smile, pad=6):
    """Paint the drawn smile out, with the skin that is on either side of it.

    A ROW AT A TIME, FROM BOTH ENDS. The cheek this sits on is flat colour -
    there is no texture to reconstruct - but it is not CONSTANT: it warms by
    about four points of green from the nose down to the jaw. Filling the
    patch with one sampled colour leaves a rectangle of yesterday's skin on
    his chin, which is visible the moment the sprite moves. Taking the skin
    from eight pixels outside each end of the stroke, on the row being
    filled, and running one into the other across the gap, follows the
    gradient exactly and costs nothing.
    """
    x0, x1, y0, y1 = smile
    a = np.asarray(img).astype(float).copy()
    lx, rx = x0 - 10, x1 + 10
    for y in range(y0 - pad, y1 + pad + 1):
        left, right = a[y, lx, :3], a[y, rx, :3]
        span = rx - lx
        for x in range(lx + 1, rx):
            t = (x - lx) / span
            a[y, x, :3] = left + (right - left) * t
            a[y, x, 3] = max(a[y, x, 3], 255)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


# ------------------------------------------------------------- the warps

def warp(img, dy, dx):
    """`img` resampled through a displacement field, bilinearly.

    Positive dx moves what is drawn to the RIGHT, positive dy moves it DOWN,
    which is the opposite of how the sampling reads - the field is
    subtracted from the sample position, not added to it. Getting that
    backwards is the first thing to check if a cheek caves in instead of
    bulging out.
    """
    a = np.asarray(img).astype(float)
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    sy, sx = yy - dy, xx - dx
    sy = np.clip(sy, 0, h - 1.001)
    sx = np.clip(sx, 0, w - 1.001)
    y0 = sy.astype(int)
    x0 = sx.astype(int)
    fy = (sy - y0)[:, :, None]
    fx = (sx - x0)[:, :, None]
    out = (a[y0, x0] * (1 - fy) * (1 - fx)
           + a[y0 + 1, x0] * fy * (1 - fx)
           + a[y0, x0 + 1] * (1 - fy) * fx
           + a[y0 + 1, x0 + 1] * fy * fx)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")


def _bump(shape, cy, cx, ry, rx):
    """A smooth 0..1 hill, for a displacement to be shaped by.

    A cosine rather than a gaussian, because it reaches exactly zero at the
    edge of its own box. A gaussian does not, and the tail of one centred on
    his cheek is still a fifth of a pixel out at his ear - which over a hard
    black outline is a visible softening of the hair.
    """
    yy, xx = np.mgrid[0:shape[0], 0:shape[1]].astype(float)
    r = np.sqrt(((yy - cy) / ry) ** 2 + ((xx - cx) / rx) ** 2)
    return np.where(r < 1.0, 0.5 + 0.5 * np.cos(np.pi * np.clip(r, 0, 1)), 0.0)


def cheek_bulge(img, cx, cy, side, amount):
    """One cheek pushed out, as a boy chewing has.

    The hill is centred BESIDE the mouth, not on it, and it is wider than it
    is tall: a mouthful sits in the pouch between the jaw and the teeth, and
    that pouch is a long low swelling along the jaw rather than a ball.
    """
    shape = np.asarray(img).shape[:2]
    at = cx + side * 62
    hill = _bump(shape, cy + 16, at, 58.0, 74.0)
    return warp(img, np.zeros(shape), hill * amount * side)


def chin_drop(img, cx, chin_y, amount):
    """The chin carried down, for the frames where the jaw is open.

    FOUR OR FIVE PIXELS, AND THE REASON IS HIS COLLAR. The bottom of his jaw
    is drawn at y 545 and the neck of his shirt at 558, so thirteen pixels
    is the whole budget; past six of them the chin reaches the collar and
    what the frame shows is a boy with no neck. The hill reaches zero before
    the collar on purpose, so the shirt is never resampled at all.
    """
    shape = np.asarray(img).shape[:2]
    hill = _bump(shape, chin_y - 6, cx, 60.0, 104.0)
    return warp(img, hill * amount, np.zeros(shape))


# ------------------------------------------------------- drawing a mouth

SS = 4          # supersample. Every edge here is a curve; see _blit.


def _blit(img, box, draw_fn):
    """Run `draw_fn` on a patch four times the size, then lay it back down.

    Everything in this file is ellipse arcs and three-pixel strokes, and
    both alias into a staircase at the size the sprite is kept at. Drawing
    the patch large and reducing it is one line of code against
    anti-aliasing a dozen shapes by hand, and the patch is small enough that
    the cost is nothing.
    """
    x0, y0, x1, y1 = box
    w, h = (x1 - x0) * SS, (y1 - y0) * SS
    patch = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_fn(patch, SS)
    out = img.copy()
    out.alpha_composite(patch.resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))
    return out


def _lips_path(cx, cy, rx, up, down, n=120):
    """The outline of an open mouth: flat along the top, deep at the bottom.

    NOT AN ELLIPSE, and the difference is the whole expression. An ellipse
    is as round above as below, and a mouth drawn as one reads as a shout -
    the upper lip arching up towards the nose. A real open mouth is the
    upper lip staying nearly where it was while the jaw swings away from it,
    so the top of the shape is shallow and the bottom is not. Two half
    ellipses with different depths, joined at the corners.
    """
    pts = []
    for i in range(n + 1):                       # the top, left to right
        t = -np.pi + np.pi * i / n
        pts.append((cx + rx * np.cos(t), cy + up * np.sin(t)))
    for i in range(n + 1):                       # the bottom, right to left
        t = np.pi * i / n
        pts.append((cx + rx * np.cos(t), cy + down * np.sin(t)))
    return pts


def _draw_open(patch, S, w, h, teeth, tongue):
    """An open mouth, filling the patch it is handed."""
    pw, ph = patch.size
    d = ImageDraw.Draw(patch)
    cx, cy = pw / 2.0, ph * 0.30
    rx = w * S / 2.0
    up, down = h * S * 0.30, h * S * 0.70
    line = max(2.0, 2.6 * S)

    shape = _lips_path(cx, cy, rx, up, down)
    d.polygon(shape, fill=THROAT)

    # The throat, darkest at the back - which from straight on is the middle
    # and the top, because that is where it is furthest away.
    deep = Image.new("RGBA", patch.size, (0, 0, 0, 0))
    ImageDraw.Draw(deep).ellipse(
        [cx - rx * 0.74, cy - up * 0.10, cx + rx * 0.74, cy + down * 0.72],
        fill=THROAT_DEEP + (255,))
    deep = deep.filter(ImageFilter.GaussianBlur(3.0 * S))
    inside = Image.new("L", patch.size, 0)
    ImageDraw.Draw(inside).polygon(shape, fill=255)
    patch.paste(deep, (0, 0), Image.composite(
        deep.split()[3], Image.new("L", patch.size, 0), inside))

    if tongue:
        tn = Image.new("RGBA", patch.size, (0, 0, 0, 0))
        td = ImageDraw.Draw(tn)
        ty = cy + down * 0.56
        td.ellipse([cx - rx * 0.80, ty - down * 0.34,
                    cx + rx * 0.80, ty + down * 0.90], fill=TONGUE)
        td.ellipse([cx - rx * 0.52, ty - down * 0.14,
                    cx + rx * 0.52, ty + down * 0.66], fill=TONGUE_LIGHT)
        td.line([(cx, ty - down * 0.16), (cx, ty + down * 0.46)],
                fill=TONGUE_CREASE, width=int(1.3 * S))
        tn = tn.filter(ImageFilter.GaussianBlur(0.8 * S))
        patch.paste(tn, (0, 0), Image.composite(
            tn.split()[3], Image.new("L", patch.size, 0), inside))

    if teeth:
        # The upper row only. The lower teeth of a mouth this open are
        # behind the tongue and below the light, and eat_bite_bahay does not
        # draw them either.
        th = Image.new("RGBA", patch.size, (0, 0, 0, 0))
        hd = ImageDraw.Draw(th)
        # THE DEPTH IS NEGATIVE, AND THAT IS THE SHAPE. The lower edge of
        # the row is an arc bulging DOWNWARDS from the upper lip, so the
        # teeth are deepest in the middle of the mouth and close to nothing
        # at the two corners - which is what a row of upper teeth seen from
        # the front does. Passing a positive depth here, which the first
        # pass did, fills most of the mouth with white and what plays is a
        # boy with a bowl of milk for a face.
        deep_t = h * S * 0.24 - up
        hd.polygon(_lips_path(cx, cy, rx * 0.96, up * 0.96, deep_t), fill=TEETH)
        hd.line([(cx - rx * 0.62, cy + deep_t - 1.0 * S),
                 (cx + rx * 0.62, cy + deep_t - 1.0 * S)],
                fill=TEETH_SHADE, width=int(1.1 * S))
        th = th.filter(ImageFilter.GaussianBlur(0.6 * S))
        patch.paste(th, (0, 0), Image.composite(
            th.split()[3], Image.new("L", patch.size, 0), inside))

    # The lower lip: a warm roll just inside the bottom edge, so the mouth
    # has a rim and not just a hole.
    lp = Image.new("RGBA", patch.size, (0, 0, 0, 0))
    ImageDraw.Draw(lp).line(
        [(x, y) for x, y in _lips_path(cx, cy, rx * 0.93, -down * 0.74, down * 0.90)
         if y > cy + down * 0.3],
        fill=LIP_WARM + (170,), width=int(2.2 * S))
    lp = lp.filter(ImageFilter.GaussianBlur(1.6 * S))
    patch.alpha_composite(lp)

    d.line(shape + [shape[0]], fill=LIP_LINE, width=int(line), joint="curve")


def _draw_slit(patch, S, w, h):
    """Lips parted a little: a dark gap with a lip under it, and no teeth.

    See the note at the top on why there are no teeth in here.
    """
    pw, ph = patch.size
    d = ImageDraw.Draw(patch)
    cx, cy = pw / 2.0, ph * 0.40
    rx = w * S / 2.0
    up, down = h * S * 0.34, h * S * 0.66
    shape = _lips_path(cx, cy, rx, up, down)
    d.polygon(shape, fill=THROAT)
    d.polygon(_lips_path(cx, cy + down * 0.52, rx * 0.86, 0.0, down * 0.55),
              fill=TONGUE)
    d.line(shape + [shape[0]], fill=LIP_LINE, width=max(2, int(2.4 * S)),
           joint="curve")


def _draw_purse(patch, S, w, h):
    """Lips rounded onto something - a bottle, in this scene.

    SMALLER AND ROUNDER THAN ANY OF THE OPEN STATES, which is the whole
    point of having it. A boy drinking with his mouth open is a boy being
    poured on. What a mouth does on the lip of a bottle is close to a circle
    with a thick soft edge, and most of what is seen of it in these frames
    is the bottom of that edge anyway, because the bottle covers the rest.
    """
    pw, ph = patch.size
    d = ImageDraw.Draw(patch)
    cx, cy = pw / 2.0, ph * 0.42
    rx, ry = w * S / 2.0, h * S / 2.0

    # The pushed-forward lip, as a soft warm ring a little bigger than the
    # hole it goes round.
    ring = Image.new("RGBA", patch.size, (0, 0, 0, 0))
    ImageDraw.Draw(ring).ellipse(
        [cx - rx * 1.34, cy - ry * 1.06, cx + rx * 1.34, cy + ry * 1.14],
        fill=LIP_WARM + (130,))
    ring = ring.filter(ImageFilter.GaussianBlur(3.4 * S))
    patch.alpha_composite(ring)

    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=THROAT)
    d.ellipse([cx - rx * 0.58, cy + ry * 0.06, cx + rx * 0.58, cy + ry * 0.92],
              fill=TONGUE)
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], outline=LIP_LINE,
              width=max(2, int(2.6 * S)))


def _draw_closed(patch, S, w, lift, press):
    """Lips shut: the drawn smile again, but this file's own.

    `lift` raises one corner and drops the other, which is what one side of
    a jaw working does to a mouth, and `press` flattens the curve - a mouth
    with food behind it is a straighter line than a smile.
    """
    pw, ph = patch.size
    d = ImageDraw.Draw(patch)
    cx, cy = pw / 2.0, ph * 0.5
    rx = w * S / 2.0
    pts = []
    n = 48
    for i in range(n + 1):
        t = i / n
        u = 2 * t - 1
        x = cx + rx * u
        y = cy + (1 - u * u) * (6.0 * S * (1.0 - press)) + lift * S * u
        pts.append((x, y))
    d.line(pts, fill=LIP_LINE, width=max(2, int(2.2 * S)), joint="curve")
    # A hint of lower lip under the middle of it, which is what stops a
    # closed mouth reading as a pencil line.
    soft = Image.new("RGBA", patch.size, (0, 0, 0, 0))
    ImageDraw.Draw(soft).line([(x, y + 3.0 * S) for x, y in pts],
                              fill=LIP_WARM + (120,), width=int(2.4 * S),
                              joint="curve")
    patch.alpha_composite(soft.filter(ImageFilter.GaussianBlur(1.4 * S)))


# ------------------------------------------------------------- the states
#
# Every number is in source pixels on the 663 x 921 uniform canvas, and all
# of them are measured off lapis_frame_3 - see the note at the top.
#
#   name        what it is                       w    h   top   extras
STATES = {
    "smile":   None,                                     # leave him as drawn
    "part":   dict(kind="slit",  w=54, h=12, top=-1),
    "open":   dict(kind="open",  w=64, h=33, top=-4, teeth=True, tongue=True),
    "wide":   dict(kind="open",  w=70, h=43, top=-6, teeth=True, tongue=True,
                   chin=4),
    "purse":  dict(kind="purse", w=34, h=27, top=-1),
    "chew_l": dict(kind="closed", w=48, top=1, lift=-2.5, press=0.35,
                   cheek=-1, bulge=13.0),
    "chew_r": dict(kind="closed", w=48, top=0, lift=2.5, press=0.35,
                   cheek=1, bulge=13.0),
    "swallow": dict(kind="closed", w=44, top=1, lift=0.0, press=0.85,
                    throat=True),
}


def _throat_swallow(img, cx, chin_y):
    """The lump of a swallow, under his chin.

    HALF SHADING AND HALF A WARP, because either on its own fails. A drawn
    shadow on a neck that has not changed shape is a smudge; a warp with no
    shading moves the outline and leaves the inside of the throat flat. The
    two together put a swelling on the front of his neck, which is the one
    part of drinking that happens after the bottle has come down and is the
    reason this state exists at all.
    """
    shape = np.asarray(img).shape[:2]
    hill = _bump(shape, chin_y + 15, cx, 15.0, 32.0)
    out = warp(img, hill * 4.0, np.zeros(shape))

    shade = Image.new("RGBA", out.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    # The lump itself: a short bright arc with its own shadow under it. One
    # line on its own reads as a crease in his neck, which is the opposite
    # of a swallow; the pair reads as something passing down it.
    sd.arc([cx - 24, chin_y + 2, cx + 24, chin_y + 26], 200, 340,
           fill=(255, 214, 160, 150), width=4)
    sd.arc([cx - 22, chin_y + 8, cx + 22, chin_y + 30], 20, 160,
           fill=(196, 128, 74, 140), width=4)
    out.alpha_composite(shade.filter(ImageFilter.GaussianBlur(2.2)))
    return out


def apply(img, state):
    """`img` with the named mouth on it. The one entry point of this file.

    The order is the method and it does not commute: the old mouth comes off
    first, the new one is painted onto flat skin, and the face is warped LAST
    so that whatever was painted is carried by the warp instead of being
    laid over a face that has already moved. Painting after the warp puts a
    straight-edged mouth on a bulged cheek, which is a sticker.
    """
    if state in (None, "smile"):
        return img
    spec = STATES[state]
    if spec is None:
        return img

    arr = np.asarray(img).astype(int)
    x0, x1, y0, y1 = find_smile(arr)
    cx = (x0 + x1) // 2
    chin_y = 545                       # measured; see the note at the top

    out = erase(img, (x0, x1, y0, y1))

    # The TOP of every open state sits where the closed mouth's corners sat,
    # give or take the state's own `top`, because a jaw opens downwards.
    top = y0 + spec["top"]
    w = spec["w"]

    if spec["kind"] == "closed":
        pad = 14
        box = (cx - w // 2 - pad, top - pad, cx + w // 2 + pad, top + 20 + pad)
        out = _blit(out, box, lambda p, S: _draw_closed(
            p, S, w, spec["lift"], spec["press"]))
    else:
        h = spec["h"]
        pad = 12
        box = (cx - w // 2 - pad, top - pad, cx + w // 2 + pad, top + h + pad)
        if spec["kind"] == "open":
            out = _blit(out, box, lambda p, S: _draw_open(
                p, S, w, h, spec.get("teeth", False), spec.get("tongue", False)))
        elif spec["kind"] == "slit":
            out = _blit(out, box, lambda p, S: _draw_slit(p, S, w, h))
        else:
            out = _blit(out, box, lambda p, S: _draw_purse(p, S, w, h))

    if spec.get("cheek"):
        out = cheek_bulge(out, cx, (y0 + y1) // 2, spec["cheek"], spec["bulge"])
    if spec.get("chin"):
        out = chin_drop(out, cx, chin_y, spec["chin"])
    if spec.get("throat"):
        out = _throat_swallow(out, cx, chin_y)
    return out
