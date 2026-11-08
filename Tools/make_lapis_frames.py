"""Cut the three "Lapis" in-between frames out of one generated strip.

WHY THEY EXIST. "Lapis" was a cross-fade: the seated Kylo dissolved into
pencil_ready_uniform with the pencil already up. These are the frames where he
picks it up and raises it - low at his waist, up past his chest, almost at his
shoulder - handing over to the pose that was already there.

WHERE THE ART CAME FROM. Art/lapis_frames_raw.png is one Canva generation made
from sitting_front_uniform (his design) and pencil_ready_uniform (the end pose
and the pencil), downloaded at full size by hand because the API only returns
a thumbnail.

CROWN HEIGHTS. All three at +1.735, the same as every other seated pose here.
Only his arm moves; moving the whole figure would read as him shifting in the
chair while he reaches for a pencil.

Writes  Assets/Sprites/lapis_frame_1..3_uniform.png

Run from the repository root:  python Tools/make_lapis_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import fit_strip                        # noqa: E402

RAW = "Art/lapis_frames_raw.png"
OUT = "Assets/Sprites/lapis_frame_{}_uniform.png"
CROWNS = [1.735, 1.735, 1.735]

if __name__ == "__main__":
    fit_strip(RAW, OUT, CROWNS)
