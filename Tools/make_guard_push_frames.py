"""Cut the three frames of the guard pushing the school gate open.

WHY THESE EXIST. "Bukas" swings the gate - Tools/make_gate_frames.py makes
the in-between backgrounds - but until now nothing touched it. The gate
opened by itself, which the animation brief rules out as plainly as it rules
out replacing the shut gate with an open one: "make the hand-to-gate
interaction believable".

WHY THE GUARD AND NOT KYLO. The brief says both, in different places - the
step-by-step has Kylo reaching for the handle, the section headings say
"Guard Gate-opening animation". Either would stage: Kylo stops at x 0.00 and
the gate's centre is +0.43, so he ends up directly in front of it, and the
guard stands at +3.10, right beside the right-hand panel. The guard was
chosen, so his is the shorter and more natural reach and every word Kylo
speaks stays something Kylo does himself.

HOW THE GATE KNOWS WHEN TO MOVE. These frames are deliberately drawn with an
OPEN HAND REACHING INTO EMPTY SPACE and no gate in them, so the gate can be
the separate thing it already is. The builder starts the gate's own sequence
from this one's fireOnFrame, on the frame where his palm arrives - so the
panels begin to swing as he touches them rather than before or after.

MATCHING HIS OTHER FRAMES. Generated from guard_frame_1, his resting pose,
rather than from the original char_guard_idle - the wave frames replaced
that sprite in this scene, and the push has to match the guard who is
actually on screen. Fitted by the same fit_guard_strip as the wave, onto
char_guard's own 551x1425 canvas shape, because CreateStanding scales him by
bounds.size.y and a different canvas would change his height mid-push.

Writes  Assets/Sprites/guard_push_1..3.png

Run from the repository root:  python Tools/make_guard_push_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_guard_frames import fit_guard_strip                     # noqa: E402

RAW = "Art/guard_push_raw.png"
OUT = "Assets/Sprites/guard_push_{}.png"

if __name__ == "__main__":
    fit_guard_strip(RAW, OUT)
