"""Cut the three "Kuha" in-between frames out of one generated strip.

WHY THEY EXIST. "Kuha" was one picture replacing another: the seated Kylo
cross-faded straight into reach_bag_uniform, already holding the notepad. The
reaching never happened on screen. These are the frames where it does - he
turns his shoulders towards the bag, reaches down, and puts his hand in it.

WHERE THE ART CAME FROM. Art/kuha_frames_raw.png is one Canva generation made
from two references: sitting_front_uniform for his design, and
reach_bag_uniform for the backpack, so the bag is in the same place and at the
same size in every frame and cannot drift away from his hand. The Canva API
returns only a ~200px thumbnail, so the full-size file is downloaded by hand
out of Canva Uploads, which is why the raw lives in Art/.

ON THE CROWN HEIGHTS. All three sit at +1.735, the same height as the seated
pose before them and reach_bag_uniform after them. His head does NOT drop
during this beat: he leans from the shoulders and the lean is drawn into the
poses, so moving the whole figure down as well would read as him sinking
through the chair. Compare Tools/make_sit_frames.py, where the crown really
does descend and the stepping-down is the point.

He is drawn kneeling rather than sitting, which does not matter: the canvas
bottom is the desk's far edge and everything below it is hidden by the desk.

Writes  Assets/Sprites/kuha_frame_1..3_uniform.png

Run from the repository root:  python Tools/make_kuha_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import fit_strip                        # noqa: E402

RAW = "Art/kuha_frames_raw.png"
OUT = "Assets/Sprites/kuha_frame_{}_uniform.png"
CROWNS = [1.735, 1.735, 1.735]

if __name__ == "__main__":
    fit_strip(RAW, OUT, CROWNS)
