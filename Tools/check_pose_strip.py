"""Measure a generated pose strip against the real Kylo, before using it.

WHY THIS EXISTS. The first Canva strip generated for "Sulat" passed every
check anyone was actually doing - it cut cleanly, it fitted the canvas, and at
thumbnail size it looked like him. It was only after it was composited into
the scene at full size, next to the real art, that it was obviously a
different boy: rounder face, wider jaw, and a torso about 8% short, which is
what made his arms float above the desk.

That is a slow and unreliable way to find out, and there are something like
eighty more generations to come - see Docs/AnimationStandard.md. So this
measures the things that actually went wrong, against the base sprite, and
says plainly whether a strip is usable.

WHAT IT MEASURES, and why each one.

  hair           The hair cap height. Not a pass/fail on its own - the
                 pipeline rescales every frame to match it - but a figure
                 drawn at a wildly different size is a warning.
  reach          (ink bottom - crown) / hair: how far the drawing extends
                 below the crown, measured in HEADS. This is the one that
                 caught Sulat.

                 It is a MINIMUM, not a target, and getting that wrong is
                 worth recording. Checked as a tolerance band it rejected
                 sit_frames_raw, lapis_frames_raw and kuha_frames_raw - three
                 strips whose frames are in the game - because they are drawn
                 down past the knees and so "overshoot" by 20-70%. That is
                 harmless: fit_strip scales by hair and composites at the
                 crown, so anything below the canvas bottom is simply clipped,
                 and the canvas bottom is the desk edge.

                 Too SHORT is the real failure, and it has no good outcome.
                 The base reaches 2.79 heads and exactly fills the canvas.
                 Sulat's strip reached 2.59, so fitting by hair left the arms
                 floating above the desk, and fitting to the desk made the
                 head 7-8% too big. There is no third option.
  head width     The head's width at eye level, in hair-heights. A rounder,
                 wider face is the most visible kind of character drift and
                 the hardest to see in a thumbnail.
  colours        Median hair, skin and shirt colour. Catches a restyle that
                 keeps the proportions - a greyer shirt, a warmer skin.

Thresholds are deliberately tight. A frame that only just passes still gets
composited into the scene and looked at; this narrows what is worth looking
at, it does not replace looking.

Usage, from the repository root:

    python Tools/check_pose_strip.py Art/some_strip_raw.png
"""
import os
import sys

from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, hair_cap, split_columns  # noqa: E402

BASE = "Assets/Sprites/sitting_front_uniform.png"

# How far off the base a figure may be before it is called a reject.
# One-sided: a figure may be drawn far below the desk (it gets clipped), but
# must not stop short of it. 0.99 leaves room for rounding, nothing more.
REACH_MINIMUM = 0.99          # as a fraction of the base's reach
WIDTH_TOLERANCE = 0.08        # +/-8% on head width
COLOUR_TOLERANCE = 26         # max channel distance on the medians

# Below this the measurements are noise, not evidence. Do not lower it.
#
# The Canva API returns a ~199x112 thumbnail of every generation, and running
# this on one is tempting because it costs nothing and would save a manual
# download. It does not work. On a thumbnail of a strip whose frames are IN
# THE GAME, hair_cap found 17px, 32px and 31px of hair in three figures drawn
# at the same size, head width came out -27% to +58%, and the colour medians
# were 150+ away from the base. Every figure was rejected. The real strips
# measure 180-290px of hair.
#
# So a thumbnail is refused outright rather than judged, because a false
# reject here throws away good art and costs a whole cooldown to rediscover.
MIN_HAIR_PX = 120


def measure(im):
    """Hair height, body length in heads, head width in heads, colours."""
    crown, hair_bottom, cx = hair_cap(im)
    hair = hair_bottom - crown

    a = np.asarray(im.convert("RGBA")).astype(int)
    opaque = a[:, :, 3] > 60
    rows = np.where(opaque.any(axis=1))[0]

    reach = (rows[-1] - crown) / float(hair)

    # Head width at eye level - a third of the way down the hair cap is about
    # the brow, which is the widest part of the skull in this art style.
    eye_row = int(crown + hair * 0.75)
    eye_row = min(max(eye_row, 0), a.shape[0] - 1)
    cols = np.where(opaque[eye_row])[0]
    width = (cols[-1] - cols[0]) / float(hair) if len(cols) else float("nan")

    # Colours, sampled where each thing reliably is.
    def median(r0, r1, c0, c1):
        sel = opaque[r0:r1, c0:c1]
        px = a[r0:r1, c0:c1, :3][sel]
        return np.median(px, axis=0) if len(px) else np.array([0, 0, 0])

    hair_c = median(crown + int(hair * 0.1), crown + int(hair * 0.4),
                    cx - int(hair * 0.3), cx + int(hair * 0.3))
    skin_c = median(crown + int(hair * 1.15), crown + int(hair * 1.35),
                    cx - int(hair * 0.15), cx + int(hair * 0.15))
    # The shirt, sampled across the whole torso band with the MIDDLE LEFT OUT.
    # Taking the chest centre read the white notebook Kylo holds up in
    # place_frame_1 as his shirt and called a frame that shipped a reject;
    # anything he holds is held centrally, so excluding the middle third is
    # what makes this robust. One median over everything that is left, rather
    # than one per side, so a side that happens to be empty cannot skew it.
    r0 = min(max(crown + int(hair * 1.6), 0), a.shape[0] - 1)
    r1 = min(max(crown + int(hair * 2.1), r0 + 1), a.shape[0])
    band = np.zeros(a.shape[:2], dtype=bool)
    band[r0:r1, :] = True
    band &= opaque
    gap = int(hair * 0.30)
    band[:, max(0, cx - gap):cx + gap] = False
    px = a[:, :, :3][band]
    shirt_c = np.median(px, axis=0) if len(px) else np.array([0, 0, 0])
    return hair, reach, width, hair_c, skin_c, shirt_c


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__.strip().splitlines()[-1])
    raw = sys.argv[1]

    base = measure(Image.open(BASE).convert("RGBA"))
    b_hair, b_reach, b_width, b_hairc, b_skin, b_shirt = base
    print("base %s" % os.path.basename(BASE))
    print("  hair %dpx   reach %.3f heads (the minimum)   head width %.3f heads"
          % (b_hair, b_reach, b_width))
    print("  hair rgb %s  skin rgb %s  shirt rgb %s\n"
          % (b_hairc.astype(int), b_skin.astype(int), b_shirt.astype(int)))

    src = Image.open(raw).convert("RGB")
    spans = split_columns(src)
    print("%s: %d figures\n" % (raw, len(spans)))

    verdicts = []
    for i, (x0, x1) in enumerate(spans):
        crop = src.crop((max(0, x0 - 30), 0, min(src.size[0], x1 + 31), src.size[1]))
        padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
        padded.paste(crop, (0, 30))
        tmp = "%s.__chk%d.png" % (raw, i)
        padded.save(tmp)
        try:
            cut = cut_background(tmp)
        finally:
            os.remove(tmp)

        hair, reach, width, hairc, skin, shirt = measure(cut)
        if hair < MIN_HAIR_PX:
            sys.exit(
                "\n  figure %d has only %dpx of hair - this looks like the "
                "Canva API thumbnail,\n  not the full-size download. Nothing "
                "measured below %dpx is trustworthy.\n\n  Download the "
                "full-resolution PNG from Canva Uploads and run this on that."
                % (i + 1, hair, MIN_HAIR_PX))
        d_reach = (reach - b_reach) / b_reach
        d_width = (width - b_width) / b_width
        d_col = max(np.abs(hairc - b_hairc).max(),
                    np.abs(skin - b_skin).max(),
                    np.abs(shirt - b_shirt).max())

        bad = []
        if reach < b_reach * REACH_MINIMUM:
            bad.append("reaches only %.2f heads, needs %.2f - he will float "
                       "above the desk" % (reach, b_reach))
        if abs(d_width) > WIDTH_TOLERANCE:
            bad.append("head width %+.1f%%" % (100 * d_width))
        if d_col > COLOUR_TOLERANCE:
            bad.append("colour off by %d" % d_col)

        ok = not bad
        verdicts.append(ok)
        print("  figure %d  hair %dpx  reach %.3f (%+.1f%%)  width %.3f (%+.1f%%)  colour %d"
              % (i + 1, hair, reach, 100 * d_reach, width, 100 * d_width, d_col))
        print("            %s" % ("OK" if ok else "REJECT - " + ", ".join(bad)))

    print()
    if all(verdicts):
        print("USABLE - all %d figures within tolerance. Composite it into the "
              "scene and look at it before committing." % len(verdicts))
    else:
        n = sum(1 for v in verdicts if not v)
        print("DO NOT USE - %d of %d figures are off model. Regenerate; see "
              "Tools/make_sulat_frames.py for what to pin in the prompt."
              % (n, len(verdicts)))
        sys.exit(1)


if __name__ == "__main__":
    main()
