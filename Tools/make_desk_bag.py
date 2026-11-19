"""Draw Kylo's bag as it STANDS ON A DESK, rather than as a cut-out of it.

WHAT WAS WRONG. Tools/make_kunin_frames.loose_bag lifts his backpack out of
standing_uniform_with_bag_back - him from behind, so the bag is drawn square
on, whole, and nothing is in front of it. That is the right drawing to CARRY,
because a bag swinging on his fist is seen from wherever the fist is and square
on is the honest average. It is the wrong drawing to STAND on a desk, and the
revision notes say so in the plainest words anyone has used about this scene:
it looks like a cropped sticker pasted onto the wood.

It does, and here is why, in the order the eye notices them:

  NOTHING UNDER IT.    The desk is lit from the window on the right and
                       everything else on it - the notebook, the textbook,
                       the pencil - was drawn with its own shadow. The bag
                       arrived with none, so it reads as floating a hair
                       above the surface however carefully it is placed.
  NO CONTACT.          A bag set down squashes a little where it meets the
                       wood and the wood goes dark in the crack. Both of
                       those are a few pixels and both of them are what the
                       eye uses to decide an object is RESTING rather than
                       drawn on top.
  SQUARE ON.           klasrum_interior_background looks at this desk from
                       slightly above and a little to the left; its top is a
                       shallow band rather than a plane. A bag standing bolt
                       upright and dead level is the one thing in the shot
                       with no perspective in it at all.
  FLAT LIGHT.          The drawing it came out of is lit evenly, because he
                       was standing in the middle of a character sheet. This
                       room has a window in it.
  A PALE FRINGE.       The cut takes the drawn line round the bag and the
                       soft edge outside it, and that soft edge was drawn
                       against a light background. Against this one it is a
                       thread of white down the left side.

WHAT THIS DOES NOT DO. It does not redraw the bag. Every navy pixel, the
zip, the pocket, the handle and both straps are the ones
standing_uniform_with_bag_back has; the design and the colours are its own.
What is added is the light this room would put on it and the shadow it would
cast, and what is taken away is the fringe. Docs/AnimationStandard's rule
about reusing drawn art rather than inventing it is the whole reason the bag
on the desk and the bag on his back are the same bag, and that survives this:
they are still one drawing with one lighting pass between them.

WHY THE SHADOW IS IN THE SPRITE. It could be a second prop under the first,
and for a thing that moves it would have to be. This one does not move: it
stands on the desk until his fist closes on it and then it is replaced by the
carried bag, which has no shadow because it is in the air. One sprite with
one base line is one number for BuildPaaralanScene5 to get right instead of
three, and the builder reads the numbers this prints.

Writes  Assets/Sprites/prop_bag_desk_uniform.png   the bag, standing on wood
        Assets/Sprites/prop_bag_uniform.png        the carried bag, de-fringed

Run from the repository root:  python Tools/make_desk_bag.py
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_down import SPRITES, meta_for                      # noqa: E402
import make_kunin_frames as kunin                            # noqa: E402

# --- the room ---
# The window is on the RIGHT of klasrum_interior_background and the light
# comes down across the lockers from that side, which is why every shadow in
# the shot falls to the left and why the right edge of anything standing up
# catches a warm line. Sampled off the desk's own top: the lit wood averages
# (196, 150, 121) and the shadow under its lip (120, 86, 70).
LIGHT_FROM_RIGHT = True
SHADOW_RGB = (74, 48, 38)          # the wood in shadow, which is what a cast
                                   # shadow on this desk is made of
RIM_RGB = (255, 226, 186)          # the window's warm edge

# --- how it sits ---
# A BAG SET DOWN IS NOT A BAG HELD UP. Three things change and none of them
# is large: it leans a few degrees off true, it spreads where it takes its own
# weight, and the base goes dark where the wood meets it.
#
# LEAN is anticlockwise, so it tips its top towards the middle of the desk and
# away from the edge of frame - a bag dropped on a desk leans INTO the desk,
# because that is the way the child's arm was going when he let go.
LEAN_DEG = 4.0
# HOW MUCH NARROWER THE TOP IS THAN THE BOTTOM, as a fraction of its width.
# This is the perspective, and it is the second thing after the shadow that
# decides whether the bag is standing in the room or drawn on the glass. The
# camera looks DOWN on this desk - its top is a band rather than a line, so
# the lens is above it - and a box standing on a surface below the eye has its
# far, upper end going away from us. Three per cent is as much as a bag this
# size can take before it reads as a wedge; it is also about what the
# textbook beside it was drawn with, which is the number that matters.
TAPER = 0.032
# How much wider the bottom eighth gets, as a fraction. Soft canvas resting on
# a hard surface spreads; a tenth of its width is what that looks like at this
# size and more than that is a beanbag.
SPREAD = 0.055
SPREAD_ROWS = 0.13                 # the fraction of its height that spreads

# --- the cast shadow ---
# WIDE, SHORT AND SOFT, because the desk's top is seen almost edge on: a
# shadow lying on it is compressed to a sliver, which is exactly what makes
# it read as lying on the surface rather than standing behind the bag.
SHADOW_W = 1.26                    # of the bag's own width
SHADOW_H = 0.132                   # of the bag's own width, not its height
SHADOW_DX = -0.17                  # of its width, away from the window
SHADOW_DY = 0.012
SHADOW_ALPHA = 132
SHADOW_BLUR = 11.0

# The close dark where the bag meets the wood - a second, tighter, darker
# ellipse inside the first. This is the one that does the work: a soft blob
# alone reads as a bag floating over a smudge.
CONTACT_W = 0.92
CONTACT_H = 0.046
CONTACT_DX = -0.045
CONTACT_ALPHA = 168
CONTACT_BLUR = 4.0

# How far below the bag's own base the sprite goes, as a fraction of the bag's
# height, so there is room for the shadow to spread. BuildPaaralanScene5 reads
# this back out of what the run prints.
BELOW = 0.085
SIDE = 0.20                        # and the room each side, for the same reason

# --- the light on the bag itself ---
SHADE_SIDE = 0.14                  # how dark the far side goes, at its darkest
SHADE_FOOT = 0.16                  # and the bottom, where it sits in its own
                                   # occlusion
RIM_PX = 3                         # the lit edge, in pixels of the bag's own
RIM_ALPHA = 110


def clean_fringe(im, keep=12):
    """The pale rind round its edge, taken off, twice over.

    TWO DIFFERENT PALE THINGS ARE STUCK TO THIS CUT-OUT and they come away
    by different means.

    The first is SHIRT. make_kunin_frames.loose_bag finds the bag as one navy
    region and then grows it by three pixels to pick up the drawn line round
    it - and three pixels past the line, on a drawing of a boy wearing the
    bag, is his white T-shirt. It is fully opaque, so no amount of alpha
    tidying touches it; against pale lockers nobody saw it, and against the
    dark wood of this desk it is a white rind down one side and over both
    straps. It is found here as pale, neutral, opaque pixels WITHIN A FEW
    PIXELS OF THE SILHOUETTE'S EDGE - which is what keeps the zip, which is
    the same grey and is in the middle of the bag, exactly where it is.

    The second is the ordinary fringe of a cut-out: part-transparent pixels
    whose COLOUR is the background the drawing was made against. The alpha is
    right and only the colour is wrong, so the colour is replaced with the
    nearest solid pixel's and the alpha is left alone: the edge stays as soft
    as it was drawn and stops being a different colour from the thing it is
    the edge of.
    """
    a = np.asarray(im).astype(np.uint8).copy()
    al = a[:, :, 3]

    # --- the shirt ---
    # WHAT THE BAG ITSELF IS MADE OF: navy cloth, and the black line drawn
    # round every shape in it. Fill the holes in that and you have everything
    # inside the bag's own outline - the zip, the pocket's highlight, the
    # buckle - and nothing outside it. The shirt is outside it by definition,
    # because the outline is where the bag stops.
    solid = al > 200
    lum = a[:, :, :3].astype(int).mean(axis=2)
    navy = solid & (a[:, :, 2].astype(int) - a[:, :, 0] > 12) & (lum < 170)
    ink = solid & (lum < 110)
    #
    # AND THE FILL IS TRIMMED BACK TO THE DRAWING. Closing the navy and the
    # line together is what bridges the gaps in a drawn outline, so the fill
    # has something to fill - but a closed shape is a few pixels FATTER than
    # the shape that was closed, and those few pixels are shirt again. So the
    # fill is kept only where it is within a pixel of something actually
    # drawn, or deep enough inside the bag to be the bag. Skip the trim and
    # the halo comes straight back, one closing-radius wide.
    raw = navy | ink
    filled = ndimage.binary_fill_holes(
        ndimage.binary_closing(raw, np.ones((5, 5))))
    core = filled & (ndimage.binary_dilation(raw, np.ones((3, 3)))
                     | ndimage.binary_erosion(filled, np.ones((7, 7))))
    # THE HANDLE HAS A HOLE IN IT and the fill does not know that. The loop at
    # the top of the bag is a ring of navy with his shirt showing through the
    # middle, and filling the ring keeps that shirt - a white lozenge where
    # the daylight should be. It is the ONE pale, neutral, opaque thing left
    # inside the outline once the rind is off: seventy-five pixels of
    # (236, 236, 235), and the run asserts that nothing else joins it. The
    # zip and the pocket's pull are grey but not that pale, so this rule does
    # not reach them and they stay drawn.
    lo = a[:, :, :3].min(axis=2).astype(int)
    hi = a[:, :, :3].max(axis=2).astype(int)
    core &= ~((lo > 150) & (hi - lo < 60))
    a[solid & ~core] = 0
    al = a[:, :, 3]

    # --- the fringe ---
    solid = al > 200
    fringe = (al > keep) & ~solid
    if solid.any() and fringe.any():
        _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
        a[fringe, :3] = a[iy[fringe], ix[fringe], :3]
    a[al <= keep] = 0
    return Image.fromarray(a, "RGBA")


def spread_foot(im):
    """The bottom of it widened and flattened, the way weight does.

    Row by row over the last `SPREAD_ROWS` of its height, each row is stretched
    about its own centre by up to `SPREAD`, easing in from nothing so there is
    no step where the stretch starts.

    NOTHING IS MOVED DOWN THE CANVAS HERE, and the first version of this did.
    It pushed the last rows a pixel or two lower, to settle the drawn line at
    the base into the surface - and a row moved down by one leaves the row it
    came from EMPTY. A one-pixel transparent slot across the whole width of
    the bag, with the desk showing through it, is a bright tan hairline
    exactly where the eye is already looking for the contact. The settling is
    the contact shadow's job and this only widens.
    """
    w, h = im.size
    a = np.asarray(im)
    band = int(round(h * SPREAD_ROWS))
    if band < 2:
        return im
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.alpha_composite(im.crop((0, 0, w, h - band)), (0, 0))
    for i in range(band):
        y = h - band + i
        t = (i + 1) / float(band)
        k = 1.0 + SPREAD * (t ** 1.6)
        row = Image.fromarray(a[y:y + 1], "RGBA")
        wide = row.resize((max(1, int(round(w * k))), 1), Image.LANCZOS)
        out.alpha_composite(wide, (-(wide.size[0] - w) // 2, y))
    return out


def taper(im):
    """The top drawn a little narrower than the base: one-point perspective.

    Row by row about the line down its middle, from `TAPER` narrower at the
    crown to nothing at the base, so the base - the part that is touching the
    desk and must not move - comes out exactly as wide as it was drawn. The
    rows are resampled rather than sheared, so the zip stays down the middle
    and nothing inside the bag is stretched against anything else.
    """
    w, h = im.size
    a = np.asarray(im)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        k = 1.0 - TAPER * (1.0 - y / float(h - 1))
        row = Image.fromarray(a[y:y + 1], "RGBA")
        nw = max(1, int(round(w * k)))
        out.alpha_composite(row.resize((nw, 1), Image.LANCZOS), ((w - nw) // 2, y))
    return out


def shade(im):
    """The window's light put on it: dark away from it, a warm line towards it.

    Two multiplies and an add, all of them masked to the bag's own alpha so
    nothing spills past its edge. The far side goes down by SHADE_SIDE over
    the outer third, the foot by SHADE_FOOT over the bottom fifth - an object
    sitting on a surface occludes its own base - and the near edge gets RIM_PX
    of the window.
    """
    a = np.asarray(im).astype(float)
    w, h = im.size
    al = a[:, :, 3]
    inside = al > 8

    xs = np.linspace(0.0, 1.0, w)[None, :]
    ys = np.linspace(0.0, 1.0, h)[:, None]
    # 1 at the lit edge, 0 at the far one.
    towards = xs if LIGHT_FROM_RIGHT else (1.0 - xs)
    side = 1.0 - SHADE_SIDE * np.clip((0.56 - towards) / 0.56, 0, 1) ** 1.5
    foot = 1.0 - SHADE_FOOT * np.clip((ys - 0.88) / 0.12, 0, 1) ** 1.4
    k = (side * foot)[:, :, None]
    a[:, :, :3] *= k

    # The rim: the lit edge of the silhouette, found as the pixels the mask
    # loses when it is pushed away from the window.
    mask = inside
    shifted = np.zeros_like(mask)
    if LIGHT_FROM_RIGHT:
        shifted[:, :-RIM_PX] = mask[:, RIM_PX:]
    else:
        shifted[:, RIM_PX:] = mask[:, :-RIM_PX]
    rim = mask & ~shifted
    rim = ndimage.binary_dilation(rim, np.ones((3, 1)))
    glow = np.zeros((h, w), float)
    glow[rim] = 1.0
    glow = ndimage.gaussian_filter(glow, 1.2) * (RIM_ALPHA / 255.0)
    glow *= inside
    for c in range(3):
        a[:, :, c] += (RIM_RGB[c] - a[:, :, c]) * glow

    a[~inside] = 0
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def ellipse(size, cx, cy, rw, rh, alpha, blur, rgb):
    """One soft flattened blob of shadow on the wood."""
    w, h = size
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).ellipse((cx - rw, cy - rh, cx + rw, cy + rh), fill=alpha)
    m = m.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", (w, h), rgb + (0,))
    out.putalpha(m)
    return out


def main():
    print("The bag Kunin takes off the desk, drawn standing on it.")

    bag = clean_fringe(kunin.loose_bag())
    bag.save(os.path.join(SPRITES, "prop_bag_uniform.png"))
    meta_for("prop_bag_uniform", kunin.BAG_BACK)
    bw, bh = bag.size
    print("  %-28s %dx%d   the carried bag, fringe off"
          % ("prop_bag_uniform.png", bw, bh))

    lit = shade(spread_foot(taper(bag)))

    # The canvas: the bag, room each side for the shadow to spread, and room
    # below for it to lie in. Its own base is the row the desk's top is at.
    # WIDE ENOUGH FOR THE SHADOW TO LIE IN. The first version took the
    # margin from the bag alone and the shadow was cut off square by the left
    # edge of the canvas - a soft blob with one hard side, which is worse than
    # no shadow at all, because a hard side is a thing the eye reads as an
    # object. The margin is whatever the shadow actually needs.
    reach = (SHADOW_W / 2.0 + abs(SHADOW_DX)) * bw - bw / 2.0 + 3.0 * SHADOW_BLUR
    pad_x = int(round(max(bw * SIDE, reach)))
    below = int(round(bh * BELOW))
    W, H = bw + 2 * pad_x, bh + below
    base_row = bh                       # where the wood is, on this canvas
    cx = W / 2.0

    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.alpha_composite(ellipse((W, H), cx + SHADOW_DX * bw,
                                base_row + SHADOW_DY * bw,
                                SHADOW_W * bw / 2.0, SHADOW_H * bw / 2.0,
                                SHADOW_ALPHA, SHADOW_BLUR, SHADOW_RGB))
    out.alpha_composite(ellipse((W, H), cx + CONTACT_DX * bw, base_row,
                                CONTACT_W * bw / 2.0, CONTACT_H * bw / 2.0,
                                CONTACT_ALPHA, CONTACT_BLUR, SHADOW_RGB))

    # The bag itself, leaned. It turns about the middle of its own base, which
    # is the point that cannot move: that is where it is touching the desk.
    stand = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    stand.alpha_composite(lit, (pad_x, 0))
    stand = stand.rotate(LEAN_DEG, resample=Image.BICUBIC,
                         center=(cx, base_row), fillcolor=(0, 0, 0, 0))
    out.alpha_composite(stand)

    name = "prop_bag_desk_uniform"
    out.save(os.path.join(SPRITES, name + ".png"))
    meta_for(name, kunin.BAG_BACK)

    print("  %-28s %dx%d" % (name + ".png", W, H))
    print("     lean %.1f deg, foot spread %.0f%%, shadow %.0f%% of its width"
          % (LEAN_DEG, SPREAD * 100, SHADOW_W * 100))
    print("\n  FOR BuildPaaralanScene5, where BagHeight is the BAG's height:")
    print("     sprite height   = BagHeight * %.5f" % (H / float(bh)))
    print("     sprite base Y   = BagBaseY  - BagHeight * %.5f" % (below / float(bh)))
    print("     its width is %.5f of its height" % (W / float(H)))


if __name__ == "__main__":
    main()
