"""Draw what the sleeping half of Level 1 needs: a breathing mouth and a Z.

Until now the only thing that said Kylo was asleep while the child hunted for
the word was the snore. The picture was a still, and a still picture of a boy
with his eyes shut is also a picture of a boy who has stopped working. These
are the two things that fix that, and both are made here rather than
generated, for the reason Tools/make_ui_click.py gives about the click: they
are small, they are mostly geometry, and a dial beats whatever comes back
from a generator.

THE MOUTH. Three whole copies of the sleeping pose, identical except for how
far his mouth is open, so WakeUpSequence can breathe him by swapping the
sprite and nothing else. Whole copies rather than a little mouth sprite laid
over the top, which was the first idea and is worse: an overlay has to be
positioned in world units against a pose that is already scaled and turned
23 degrees about Y, and it would drift the moment anything about his
transform changed. A frame cannot drift.

His mouth is at (176, 202) in the sleeping art, 24 px across and 19 down,
filled a dark maroon with a black outline - all measured off the drawing
rather than guessed. The new mouth is drawn over the old one and is never
smaller than it, so the old one never peeks out from behind.

THE Z. One letter, in Luckiest Guy, which is the game's own display face -
the vocabulary words are set in it, so a Z that drifts up off a sleeping
child already belongs to this game rather than to a clip-art pack. White
with a heavy black outline, the way a cartoon draws one. WakeUpSequence
sends one up per breath and varies its size, which is where the
"zzzZzzzZZzz" shape of the thing comes from: it is not one drawn string but
a letter per snore, rising and fading.

Writes  Assets/Resources/Wake/sleep_0..2.png  and  Assets/Resources/Wake/zzz.png

Resources, and their .meta files copied from the sleeping pose's importer,
for the reasons import_wakeup_frames.py sets out: a run-time component cannot
hold a scene reference, and a PNG that arrives outside Assets/Sprites is not
reimported as a Sprite by SpriteImportFixer, so Resources.Load would hand
back null.

Run from the repository root:

    python3 Tools/make_sleep_art.py

Plain python3, not `python3 -I`: that flag drops the user site directory,
where numpy and Pillow live.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from import_wakeup_frames import write_meta                   # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SLEEPING = os.path.join(ROOT, "Assets", "Sprites", "lying_down_barefoot.png")
FONT = os.path.join(ROOT, "Assets", "Fonts", "Vocabulary", "LuckiestGuy-Regular.ttf")
OUTDIR = os.path.join(ROOT, "Assets", "Resources", "Wake")

# Measured off lying_down_barefoot.png: the open mouth is a maroon blob of
# 215 px centred here, 24 across and 19 down, mean colour (132, 58, 34).
MOUTH = (176, 202)
MOUTH_FILL = (126, 54, 32)
MOUTH_LINE = (26, 16, 14)
MOUTH_TONGUE = (170, 92, 86)

# How far open, in pixels across and down. The first is a shade bigger than
# the mouth already drawn there, so it covers it; the last is a proper snore.
OPENINGS = [(26, 21), (30, 26), (34, 33)]

# The Z, drawn big and scaled down in the scene rather than the other way
# round, so it stays crisp if anyone wants it larger.
Z_SIZE = 150
Z_OUTLINE = 9


def main():
    os.makedirs(OUTDIR, exist_ok=True)
    mouths()
    zed()
    print("\n  now let Unity reimport. WakeUpSequence picks these up on its own.")


def mouths():
    base = Image.open(SLEEPING).convert("RGBA")

    for i, (w, h) in enumerate(OPENINGS):
        frame = base.copy()
        d = ImageDraw.Draw(frame)
        x, y = MOUTH

        box = [x - w / 2, y - h / 2, x + w / 2, y + h / 2]
        d.ellipse(box, fill=MOUTH_FILL, outline=MOUTH_LINE, width=3)

        # A tongue down in the bottom of it once he is properly open, so a
        # wide mouth reads as a mouth and not as a hole punched in his face.
        if h >= 26:
            t = [x - w * 0.26, y + h * 0.08, x + w * 0.26, y + h * 0.34]
            d.ellipse(t, fill=MOUTH_TONGUE)

        path = os.path.join(OUTDIR, "sleep_%d.png" % i)
        frame.save(path)
        write_meta(path)
        print("  -> %-42s mouth %d x %d px" % (os.path.relpath(path, ROOT), w, h))


def zed():
    font = ImageFont.truetype(FONT, Z_SIZE)

    pad = Z_OUTLINE * 2 + 8
    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    box = probe.textbbox((0, 0), "Z", font=font, stroke_width=Z_OUTLINE)

    img = Image.new("RGBA", (box[2] - box[0] + pad * 2, box[3] - box[1] + pad * 2), (0, 0, 0, 0))
    ImageDraw.Draw(img).text((pad - box[0], pad - box[1]), "Z", font=font,
                             fill=(255, 255, 255, 255),
                             stroke_width=Z_OUTLINE, stroke_fill=(26, 16, 14, 255))

    path = os.path.join(OUTDIR, "zzz.png")
    img.save(path)
    write_meta(path)
    print("  -> %-42s %d x %d px" % (os.path.relpath(path, ROOT), *img.size))


if __name__ == "__main__":
    main()
