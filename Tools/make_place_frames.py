"""Cut the three "putting the notebook down" frames out of one generated strip.

WHY THEY EXIST. "Kuha" used to end with Kylo frozen holding the notepad over
an empty desk, and the book then materialised on the desk a whole word later,
when "Lapis" was said, with nothing on screen having put it there. These are
the frames in between: he lifts the notepad clear of the bag, carries it round
in front of him, and lays it down.

They are the tail of the Kuha sequence rather than a beat of their own, so
BuildPaaralanScene2 plays them straight after reach_bag_uniform and switches
the notebook prop on from PoseSequence.onFinished - the book appears at the
moment he finishes putting it there.

WHERE THE ART CAME FROM. Art/place_frames_raw.png is one Canva generation made
from sitting_front_uniform (his design) and reach_bag_uniform (the backpack),
downloaded at full size by hand because the API only returns a thumbnail.

CROWN HEIGHTS. All three at +1.735, the same as every other seated pose in
this scene. He leans and reaches from the shoulders; his head does not rise or
fall, and moving the figure would read as him shifting in the chair.

Writes  Assets/Sprites/place_frame_1..3_uniform.png

Run from the repository root:  python Tools/make_place_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fit_seated_pose import fit_strip                        # noqa: E402

RAW = "Art/place_frames_raw.png"
OUT = "Assets/Sprites/place_frame_{}_uniform.png"
CROWNS = [1.735, 1.735, 1.735]

if __name__ == "__main__":
    fit_strip(RAW, OUT, CROWNS)
