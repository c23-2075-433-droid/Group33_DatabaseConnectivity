"""Turn a generated classroom illustration into Paaralan Scene 2's background.

The project's other backgrounds were made the same way: the picture is
generated outside the repo, downloaded into Art/ as a raw file, and a script
in here fits it to the game's frame. This is that script for the classroom.

Scene 2 currently borrows menu_bg.png, the main menu's background. That works,
but the menu and the classroom then look identical, and menu_bg is a corner
view of an empty room rather than a class in session.

Reads   Art/klasrum_raw.png   (or .jpeg / .jpg / .webp - whichever is there)
Writes  Assets/Sprites/klasrum_background.png

The camera is orthographic 5.4, so the frame is exactly 16:9. A source of any
other shape is CENTRE-CROPPED to 16:9 rather than squashed - a stretched room
reads as wrong long before anyone can say why. The crop is reported, so you
can see how much was lost and regenerate wider if it took something important.

Output is 1920x1080, matching kusina_pov_background. Larger gains nothing: the
game is played on a phone.

Run from the repository root:  python Tools/import_klasrum_background.py
"""
import os
from PIL import Image

ART = "Art"
STEM = "klasrum_raw"
DST = "Assets/Sprites/klasrum_background.png"
OUT_W, OUT_H = 1920, 1080
TARGET = OUT_W / OUT_H


def find_source():
    for ext in (".png", ".jpeg", ".jpg", ".webp"):
        path = os.path.join(ART, STEM + ext)
        if os.path.exists(path):
            return path
    return None


def main():
    src_path = find_source()
    if src_path is None:
        print("No %s.* in %s/ yet." % (STEM, ART))
        print("Generate the classroom, save it there, and run this again.")
        return

    im = Image.open(src_path).convert("RGB")
    w, h = im.size
    aspect = w / h
    print("source %s  %dx%d  (aspect %.3f)" % (src_path, w, h, aspect))

    if abs(aspect - TARGET) > 0.001:
        if aspect > TARGET:                     # too wide: trim the sides
            new_w = int(round(h * TARGET))
            left = (w - new_w) // 2
            im = im.crop((left, 0, left + new_w, h))
            print("  trimmed %d px off each side to reach 16:9" % left)
        else:                                   # too tall: trim top and bottom
            new_h = int(round(w / TARGET))
            top = (h - new_h) // 2
            im = im.crop((0, top, w, top + new_h))
            print("  trimmed %d px off the top and bottom to reach 16:9" % top)

    im = im.resize((OUT_W, OUT_H), Image.LANCZOS)
    im.save(DST)
    print("wrote %s  %dx%d" % (DST, OUT_W, OUT_H))
    print("Now re-run Tools > SALINLAHI > Build Paaralan Scene 2 in Unity.")


if __name__ == "__main__":
    main()
