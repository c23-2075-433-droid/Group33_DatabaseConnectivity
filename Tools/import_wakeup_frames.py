"""Cut the three "Bangon" in-between frames out of one Canva strip.

WHY THEY EXIST. The same reason Tools/make_sit_frames.py gives for the "Upo"
frames, and the same reason PoseSettle's comment gives before it: a pose
switched on while another is switched off is two pictures, and easing the
transform between them does not make it a movement. WakeUpSequence had taken
a transform as far as it goes - the sleeping pose dissolves into the sitting
one while the sitting one swings up about his seat - and it still read as a
drawing being moved, because nothing INSIDE the drawing ever changed. These
are the frames where it changes.

WHAT TO GENERATE IN CANVA. One design, three poses in a row, exactly the way
the "Upo" frames were made: three in one generation rather than three
generations, which is three times less cooldown and - the part that matters -
three poses drawn in one pass, so they match each other. 1680x944 is the size
the other strips in this project use and it is plenty.

Use lying_down_barefoot and sitting_up_barefoot as the design reference, and
ask for the three stages BETWEEN them:

    Same boy, same cartoon style, side view facing right, plain white
    background, three poses evenly spaced in a row, full body, nothing
    cropped, same size in every pose. Black spiky hair, dark green
    t-shirt, navy shorts, barefoot. He is waking up in bed, seen from the
    side, and these are three stages of him sitting up:

      1. Still mostly lying on his side, but propped on one elbow, head
         lifted off the pillow, eyes still shut.
      2. Halfway up, pushing off the mattress with one hand behind him,
         back curved, eyes squinting open, knees starting to draw up.
      3. Almost upright, knees drawn up, one fist rubbing an eye,
         wearing his glasses, mouth open in a yawn.

Glasses on the LAST pose only. He has none asleep and a pair in
sitting_up_barefoot, so that is where they go on; the dissolve into the
sitting pose covers the rest of the change of view.

Save the download as Art/wake_frames_raw.png.

WHAT THIS SCRIPT DOES, which is make_sit_frames' job with two differences.

  1. Splits the strip on the empty columns between the figures.
  2. Cuts the white off each pose, and does NOT use fit_seated_pose's shared
     cut_background to do it, which is worth explaining because sharing it
     was the first thing tried.

     That routine fills from the corners at a tolerance of 8, and its comment
     says exactly why it is that tight: the poses it was written for wear a
     near-white polo, and any looser a fill steps off the background and
     hollows out his shirt. Tolerance 8 cannot get into a narrow notch,
     because the anti-aliased gradient across the mouth of the notch exceeds
     it and stops the fill dead. This boy's hair is drawn as spikes, so the
     silhouette is nothing but narrow notches, and every one of them kept a
     wedge of white background trapped between the spikes - about one and a
     half per cent of the figure, sitting around six pixels deep.

     So this keys at tolerance 60 instead, which is safe here for a reason
     that does not hold for the seated poses: nothing this boy wears is
     white. The only near-white in the drawing is the whites of his eyes,
     and a fill seeded at the corners cannot reach those - they are enclosed
     by his outline. Then the boundary is softened, because a hard key on
     anti-aliased art leaves a jagged edge.

     Afterwards the edge colour is bled outward under the transparency and
     alphaIsTransparency is turned on, both for the reason
     Tools/clean_sprite_edges.py gives: with the flag off Unity does not
     bleed for you, and the moment a sprite is drawn at any size but 1:1 the
     bilinear filter mixes real pixels with whatever is stored under the
     transparent texels. Leave white there and a rim of it shows all the way
     round.
  3. Scales the frames so the two ENDS of the sequence match the real art,
     and walks between those two scales in the middle.

     make_sit_frames normalises on the hair cap and is right to: a head that
     changes size between frames is the one thing that gives an animation
     away. It does not work here, and both reasons are worth recording so
     nobody tries it again. hair_cap measures a VERTICAL run of dark pixels,
     and in the sleeping pose his head is on its side, so the number it
     returns for the pose this sequence starts from measures his head across
     rather than down - 198 px against the sitting pose's 161, which reads as
     a bigger head when it is the same head lying down. Measuring the dark
     blob in both axes instead does not rescue it either: this art is outlined
     in black, so the hair is joined to the outline of the whole body and the
     "head" comes out 538 px of a 600 px sprite.

     So the frames are anchored on what can be measured without ambiguity -
     the drawn size of the two sprites they run between. The first frame is
     scaled to the sleeping pose's width, because it is a lying pose and its
     length is what reads; the last to the sitting pose's height, because
     that is the drawing it dissolves into and a figure that changes size
     mid-dissolve is the most visible mistake available here. Anything in
     between is interpolated. The three scales come out close together, which
     is the check that the generator did draw the poses at one size.
  4. Places each by the BOTTOM of the figure rather than by its crown, which
     is where make_sit_frames and this part company. Those frames were a
     seated body descending, so the crown was the thing to control. This is a
     body pivoting up off a mattress: what stays put is where he touches the
     bed, and the crown is the thing that moves most. The feet line steps from
     the sleeping pose's -0.785 to the sitting pose's -1.215, which is not a
     drop into the floor - it is the 0.43 that PlayerMovement sinks him by on
     "Bangon" so that what rests on the mattress is his backside and not his
     toes.
  5. Writes a frame 0 as well, which needs no Canva: the sleeping pose
     itself, re-canvassed onto the same frame size. That is what lets
     WakeUpSequence play the whole thing from one renderer at one transform,
     with no geometry left in the C# at all.

Everything lands on ONE canvas whose centre is the sitting pose's own centre,
so a frame shown at the transform "Bangon" leaves him at is already in the
right place.

Writes  Assets/Resources/Wake/wake_frame_0..3.png and their .meta files.

Resources, not Assets/Sprites, and the reason is worth recording: nothing in a
saved scene refers to WakeUpSequence - it is attached at run time, like the
HUD and the music - so it cannot hold an Inspector reference to a sprite.
Resources.Load is the only way a run-time object gets at art. The .meta files
are copied from the sleeping pose's own importer settings, because
SpriteImportFixer only watches Assets/Sprites: a PNG that arrives here as a
plain Texture rather than a Sprite loads as null, and a null sprite is the
white-box bug that fixer exists to prevent.

Run from the repository root:

    python3 Tools/import_wakeup_frames.py

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy and Pillow live.
"""
import hashlib
import os
import re
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

from PIL import ImageDraw                                     # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "Art", "wake_frames_raw.png")
SLEEPING = os.path.join(ROOT, "Assets", "Sprites", "lying_down_barefoot.png")
SEATED = os.path.join(ROOT, "Assets", "Sprites", "sitting_up_barefoot.png")
OUTDIR = os.path.join(ROOT, "Assets", "Resources", "Wake")
OUT = os.path.join(OUTDIR, "wake_frame_{}.png")

# The character art is 120 pixels to the unit and Level 1 scales him by 0.830,
# so this is what one pixel of a frame is worth on screen.
PPU = 120.0
CHARACTER_SCALE = 0.8300743
UNIT = CHARACTER_SCALE / PPU

# Measured off the scene and the art - see the commit that added the seat
# pivot. The sleeping pose is centred at y 0.27 and stands 2.110 units tall,
# so it rests on -0.785; "Bangon" leaves the sitting pose centred at 0.860,
# 4.150 tall, with its feet at -1.215.
SLEEPING_CENTRE_Y = 0.27
SEATED_CENTRE_Y = 0.860
MATTRESS_Y = -0.785
SEATED_FEET_Y = -1.215

# Where the bottom of each in-between sits, stepping between those two.
FEET = [-0.8925, -1.0, -1.1075]

# How the white is keyed. See the note at the top for why this is six times
# looser than fit_seated_pose's 8, and why that is safe for this character.
FILL_TOLERANCE = 60
WALL = 2

# Background the fill can never reach, because it is walled in by the figure:
# the triangle between his chest and the mattress, the slot behind a bent
# knee, the gap between his face and his raised forearm. Anything near-white
# and bigger than this is one of those; anything smaller is artwork, and the
# thing it is in practice is the white of an eye.
#
# The gap between the two is wide, so this is not a delicate number: in this
# strip the largest piece of artwork it must keep is an eye at about 110 px
# and the smallest gap it must cut is about 870.
POCKET_WHITE = 205
POCKET_MIN = 250

# Bits the generator drew loose - a spare curl of toes floating beside a foot,
# outline and all. They are not something the key severed: they come away with
# a clean gap around them, because they were drawn detached. Anything this
# much smaller than the figure is one of those, and in the game it would be a
# scrap of foot hanging in the air beside him.
ISLAND_MIN_FRACTION = 0.02

# The band over which the edge fades out, as channel minimums. A pixel at or
# above SOFT_HI is background, at or below SOFT_LO is solid, and between the
# two it is the anti-aliased rim and gets a proportional alpha.
SOFT_LO = 150
SOFT_HI = 248

# How far the edge colour is carried into the transparency, in pixels.
BLEED = 8

# One canvas for every frame, centred on the sitting pose's centre. Wide
# enough for him lying down (6.1 units of art), tall enough for him sitting
# up, with a little margin so a generated pose that reaches further than
# expected is not clipped.
CANVAS = (820, 760)


def main():
    os.makedirs(OUTDIR, exist_ok=True)

    sleeping = Image.open(SLEEPING).convert("RGBA")
    seated = Image.open(SEATED).convert("RGBA")

    sleeping_width = sleeping.size[0] * UNIT
    seated_height = seated.size[1] * UNIT
    print("  anchors: sleeping pose %.3f u wide, sitting pose %.3f u tall"
          % (sleeping_width, seated_height))

    # Frame 0 is the sleeping pose itself, moved onto the shared canvas.
    place(sleeping, MATTRESS_Y, 0)

    if not os.path.exists(RAW):
        print("\n  No %s yet - frame 0 is written and the other three are not."
              % os.path.relpath(RAW, ROOT))
        print("  WakeUpSequence needs all four before it will play frames; until")
        print("  then it falls back to the swing. See the top of this file for")
        print("  what to generate in Canva.")
        return 0

    strip = Image.open(RAW).convert("RGB")
    spans = split_columns(strip)
    if len(spans) != len(FEET):
        sys.exit("expected %d figures in %s, found %d: %s"
                 % (len(FEET), os.path.relpath(RAW, ROOT), len(spans), spans))

    cuts = [trim(cut_one(strip, x0, x1, i)) for i, (x0, x1) in enumerate(spans)]

    # The two ends, measured against the real art; see the note above.
    first = sleeping_width / (cuts[0].size[0] * UNIT)
    last = seated_height / (cuts[-1].size[1] * UNIT)
    print("  scales: first frame x%.3f (to the sleeping width), "
          "last x%.3f (to the sitting height)" % (first, last))

    for i, content in enumerate(cuts):
        t = i / float(len(cuts) - 1) if len(cuts) > 1 else 1.0
        scale = first + (last - first) * t

        place(content, FEET[i], i + 1, scale)

    print("\n  now let Unity reimport. WakeUpSequence picks the frames up on its own.")
    return 0


def split_columns(img):
    """Column spans of the separate figures, found on the empty gaps.

    The same scan make_sit_frames uses, and it wants the same thing of the
    art: a plain background and a clear gap between the poses.
    """
    a = np.asarray(img.convert("RGB")).astype(int)
    ink = (a.min(axis=2) < 225).sum(axis=0)
    spans, start = [], None
    for x, v in enumerate(ink):
        if v > 2 and start is None:
            start = x
        if v <= 2 and start is not None:
            if x - start > 40:
                spans.append((start, x - 1))
            start = None
    if start is not None and len(ink) - start > 40:
        spans.append((start, len(ink) - 1))
    return spans


def cut_one(strip, x0, x1, i):
    """One figure, lifted off the background.

    Padded with white first: cut_background walls off the border, and that
    wall needs background to bite on rather than the figure itself.
    """
    crop = strip.crop((max(0, x0 - 30), 0, min(strip.size[0], x1 + 31), strip.size[1]))
    padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
    padded.paste(crop, (0, 30))

    return cut_white(padded)


def cut_white(src):
    """Lifts the figure off its white background, softly.

    The same shape as fit_seated_pose.cut_background - wall the border so a
    fill cannot run along an edge where the figure is cut, then seed from the
    corners that really are background - but at a tolerance that can get into
    the notches between his hair spikes, and with a soft edge on the end. See
    the note at the top of this file.
    """
    w, h = src.size
    work = src.copy()
    sentinel = (255, 0, 255)

    # The wall. Two pixels is enough to stop 4-connected propagation.
    ImageDraw.Draw(work).rectangle([0, 0, w - 1, h - 1], outline=(0, 0, 0), width=WALL)

    probe = np.asarray(src).astype(int)
    inside = WALL + 1
    used = 0
    for x, y in ((inside, inside), (w - 1 - inside, inside),
                 (inside, h - 1 - inside), (w - 1 - inside, h - 1 - inside)):
        # Only seed where it really is background; a corner the figure
        # reaches must not become a hole.
        if probe[y, x].min() < 225:
            continue
        ImageDraw.floodfill(work, (x, y), sentinel, thresh=FILL_TOLERANCE)
        used += 1
    if used == 0:
        sys.exit("no corner of a pose looks like background - check the strip")

    filled = np.all(np.asarray(work).astype(int) == sentinel, axis=2)

    # The wall itself is not artwork. Left opaque it is a black frame around
    # the whole pose, which trim() then reads as the figure's own extent -
    # that measured the poses at twice their height and halved their scale.
    filled[:WALL, :] = True
    filled[-WALL:, :] = True
    filled[:, :WALL] = True
    filled[:, -WALL:] = True

    # Then the pockets the fill could not get into - see POCKET_MIN.
    pockets = (~filled) & (probe.min(axis=2) >= POCKET_WHITE)
    label, count = ndimage.label(pockets)
    if count:
        sizes = ndimage.sum(pockets, label, range(1, count + 1))
        keep = [k + 1 for k in range(count) if sizes[k] >= POCKET_MIN]
        if keep:
            filled |= np.isin(label, keep)

    # Hard transparent where the fill reached; everywhere else, fade out the
    # anti-aliased rim by how close to white it is. Only pixels touching the
    # fill are faded, so pale art well inside the figure - his skin, the
    # whites of his eyes - is never thinned.
    near = ndimage.binary_dilation(filled, iterations=2) & ~filled
    whiteness = (probe.min(axis=2) - SOFT_LO) / float(SOFT_HI - SOFT_LO)

    alpha = np.where(filled, 0.0, 255.0)
    alpha[near] = 255.0 * (1.0 - np.clip(whiteness[near], 0.0, 1.0))

    alpha = drop_islands(alpha)

    out = np.dstack([probe, alpha]).astype(np.uint8)
    return bleed(Image.fromarray(out, "RGBA"))


def drop_islands(alpha):
    """Rubs out anything the generator drew loose. See ISLAND_MIN_FRACTION."""
    solid = alpha > 60
    label, count = ndimage.label(solid)
    if count <= 1:
        return alpha

    sizes = ndimage.sum(solid, label, range(1, count + 1))
    biggest = sizes.max()
    for k in range(count):
        if sizes[k] < biggest * ISLAND_MIN_FRACTION:
            alpha[label == k + 1] = 0.0
            print("     dropped a loose %d px fragment" % sizes[k])
    return alpha


def bleed(im):
    """Carries the edge colour out under the transparency.

    Alpha is untouched, so nothing looks different today - it only changes
    what Unity's bilinear filter finds when it reaches past the edge at any
    scale but 1:1. Without it the white that was keyed out is still sitting
    there in the file, ready to be smeared back around him.
    """
    a = np.asarray(im).astype(np.uint8)
    rgb, al = a[..., :3].copy(), a[..., 3]

    solid = al > 8
    if not solid.any():
        return im

    # Nearest drawn neighbour for every transparent pixel, which is exactly
    # what a distance transform hands back for free.
    _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
    reach = ndimage.distance_transform_edt(~solid) <= BLEED

    rgb[reach] = rgb[iy[reach], ix[reach]]
    return Image.fromarray(np.dstack([rgb, al]).astype(np.uint8), "RGBA")


def place(pose, feet_y, index, scale=1.0):
    """Puts one pose on the shared canvas, standing on a given world line.

    Centred horizontally, because he does not travel across the bed - every
    pose in the sequence is drawn over the same hips - and sat on feet_y
    vertically, because what stays still in a sit-up is the part touching the
    mattress.
    """
    content = trim(pose)
    if scale != 1.0:
        content = content.resize((max(1, int(round(content.size[0] * scale))),
                                  max(1, int(round(content.size[1] * scale)))),
                                 Image.LANCZOS)

    w, h = CANVAS
    canvas_top_y = SEATED_CENTRE_Y + (h * UNIT) / 2.0

    x = int(round(w / 2.0 - content.size[0] / 2.0))
    y = int(round((canvas_top_y - (feet_y + content.size[1] * UNIT)) / UNIT))

    if y < 0 or y + content.size[1] > h or x < 0:
        print("  ! frame %d does not fit the canvas - widen CANVAS" % index)

    out = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    out.alpha_composite(content, (max(0, x), max(0, y)))

    path = OUT.format(index)
    out.save(path)
    write_meta(path)
    print("  -> %-44s feet at %+.3f" % (os.path.relpath(path, ROOT), feet_y))


def trim(im):
    a = np.asarray(im)
    opaque = a[:, :, 3] > 60
    rows = np.where(opaque.any(axis=1))[0]
    cols = np.where(opaque.any(axis=0))[0]
    return im.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))


def write_meta(path):
    """Gives a frame the sleeping pose's own import settings, with its own id.

    Two things would otherwise go wrong. A new PNG arrives at 100 pixels to
    the unit while the character art is 120, so every frame would be a fifth
    too big; and outside Assets/Sprites nothing reimports it as a Sprite, so
    Resources.Load<Sprite> would hand back null.
    """
    template = open(SLEEPING + ".meta", encoding="utf-8").read()
    name = os.path.splitext(os.path.basename(path))[0]

    digest = hashlib.sha1(name.encode()).hexdigest()
    internal = int(digest[32:48], 16) % (2 ** 62)

    out = re.sub(r"^guid: .*$", "guid: " + digest[:32], template, count=1, flags=re.M)
    out = re.sub(r"213: -?\d+", "213: -%d" % internal, out, count=1)
    out = out.replace("lying_down_barefoot_0", name + "_0")
    out = re.sub(r"^  spriteName: .*$", "  spriteName: " + name, out, flags=re.M)

    # Unity bleeds the edge colour outward for you only with this on, and the
    # sleeping pose this template comes from has it off. See bleed() above.
    out = re.sub(r"^  alphaIsTransparency: 0$", "  alphaIsTransparency: 1", out, flags=re.M)

    open(path + ".meta", "w", encoding="utf-8").write(out)


if __name__ == "__main__":
    sys.exit(main())
