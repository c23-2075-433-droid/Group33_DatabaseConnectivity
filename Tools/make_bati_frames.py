"""Cut the three "Bati" greeting frames out of one generated strip.

WHAT THEY ARE. Kylo raising his hand to greet the guard at the school gate,
in three stages: hand at hip, hand at chest, hand up in a wave. "Bati" in
Paaralan Scene 1 used to be a PoseSwitch - standing in one frame, the talking
pose in the next, nothing between.

A DIFFERENT CANVAS FROM THE SEATED POSES, and that is why this does not use
fit_seated_pose.fit_strip. The seated frames live on a fixed 663x921 canvas
with the crown pinned to row 142 and the bottom edge at the desk, and they
are normalised by HAIR CAP because the body is cropped and the head is the
only reliable invariant.

These are full-body standing poses. Every standing sprite in the project -
standing_uniform_with_bag, talking_uniform_with_bag, walk_frame_1..4 - is
745 pixels tall, cropped tight horizontally, with the feet on the bottom row
and the width left to whatever the pose needs. The builder sizes them by
world height, so the width does not matter and the height is what has to
match. Nothing is cropped away, so TOTAL HEIGHT is the honest invariant here:
match it and he is the same boy at the same size, standing in the same place.

The hair cap is still measured and reported, as a check rather than as the
thing being fitted. If the head comes out far from the reference's the
generation has drifted and the frames should not be used, however well the
height matches.

ON THE HAIR, because it cost a generation. The first attempt came back with
small, neat, swept hair - because the prompt carried a line over from the
seated work telling it NOT to draw a big puffy mop. That was right for
sitting_front_uniform and exactly backwards here: standing_uniform_with_bag
has a big wild spiky mop, wider than his head. The two sprites genuinely
have different hair, so the prompt has to describe the hair of the
reference being used, not the character in general.

WHERE THE ART CAME FROM. Art/bati_frames_raw.png is one Canva generation
from standing_uniform_with_bag. The API returns only a ~200px thumbnail, so
the full-size file is downloaded by hand out of Canva Uploads.

THEIR .META MUST SAY 120 PIXELS PER UNIT, and this is not a detail.

Kylo's original six sprites - standing, talking and the four walk frames -
import at 120. Every sprite generated for him since, these included, takes
Unity's default of 100. The art is the same 745px tall either way, so the
only thing that changes is how many world units Unity thinks that is:

    745 / 120 = 6.21 units          745 / 100 = 7.45 units

which is TWENTY PERCENT. "Bati" swaps him from standing to bati_frame_1 and
back again, so at 100 he swelled by a fifth the moment he waved and snapped
back when he finished - reported from the game as "nalaki ng sobra".

Unity writes the .meta on first import and does not rewrite it afterwards,
so the fix lives there rather than here: these three are set to 120 by hand.
Re-running this script keeps them. DELETING a .meta does not - Unity makes a
fresh one at the default 100 and the swell comes back.

Writes  Assets/Sprites/bati_frame_1..3_uniform_with_bag.png

Run from the repository root:  python Tools/make_bati_frames.py
"""
import os
import sys

from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import cut_background, hair_cap, split_columns  # noqa: E402

RAW = "Art/bati_frames_raw.png"
REF = "Assets/Sprites/standing_uniform_with_bag.png"
OUT = "Assets/Sprites/bati_frame_{}_uniform_with_bag.png"

# Every standing sprite in the project is this tall.
TARGET_HEIGHT = 745


def main():
    ref = Image.open(REF).convert("RGBA")
    ra = np.asarray(ref)
    rrows = np.where((ra[:, :, 3] > 60).any(axis=1))[0]
    ref_h = rrows[-1] - rrows[0] + 1
    r_crown, r_bottom, _ = hair_cap(ref)
    ref_hair = r_bottom - r_crown
    print("reference %s: %dpx tall, hair cap %dpx (%.1f%% of height)"
          % (os.path.basename(REF), ref_h, ref_hair, 100.0 * ref_hair / ref_h))
    print()

    src = Image.open(RAW).convert("RGB")
    spans = split_columns(src)
    if len(spans) != 3:
        sys.exit("expected 3 figures in %s, found %d" % (RAW, len(spans)))

    for i, (x0, x1) in enumerate(spans, start=1):
        # Pad with white so cut_background's border wall has background to
        # bite on rather than the figure itself.
        crop = src.crop((max(0, x0 - 30), 0, min(src.size[0], x1 + 31), src.size[1]))
        padded = Image.new("RGB", (crop.size[0], crop.size[1] + 60), (255, 255, 255))
        padded.paste(crop, (0, 30))
        tmp = "%s.__bati%d.png" % (RAW, i)
        padded.save(tmp)
        try:
            cut = cut_background(tmp)
        finally:
            os.remove(tmp)

        a = np.asarray(cut)
        opaque = a[:, :, 3] > 60
        rows = np.where(opaque.any(axis=1))[0]
        cols = np.where(opaque.any(axis=0))[0]
        content = cut.crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))

        scale = float(TARGET_HEIGHT) / content.size[1]
        w = max(1, int(round(content.size[0] * scale)))
        scaled = content.resize((w, TARGET_HEIGHT), Image.LANCZOS)

        path = OUT.format(i)
        scaled.save(path)

        c, b, _ = hair_cap(scaled)
        hair = b - c
        drift = 100.0 * (hair - ref_hair) / ref_hair
        flag = "" if abs(drift) <= 8 else "   <-- HEAD OFF, check before using"
        print("%s  %dx%d  hair cap %dpx (%+.1f%% vs reference)%s"
              % (os.path.basename(path), w, TARGET_HEIGHT, hair, drift, flag))


if __name__ == "__main__":
    main()
