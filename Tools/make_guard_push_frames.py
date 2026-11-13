"""Cut the six frames of the guard pushing the school gate open.

WHY THESE EXIST. "Bukas" swings the gate - Tools/make_gate_frames.py makes
the in-between backgrounds - but until these existed nothing touched it. The
gate opened by itself, which the animation brief rules out as plainly as it
rules out replacing the shut gate with an open one: "make the hand-to-gate
interaction believable".

WHY THE GUARD AND NOT KYLO. The brief says both, in different places - the
step-by-step has Kylo reaching for the handle, the section headings say
"Guard Gate-opening animation". Either would stage: Kylo stops at x 0.00 and
the gate's centre is +0.43, so he ends up directly in front of it, and the
guard stands at +3.10, right beside the right-hand panel. The guard was
chosen, so his is the shorter and more natural reach and every word Kylo
speaks stays something Kylo does himself.

WHY SIX AND NOT THREE. The first strip had three, and they were badly
spread. Frame 1 was arms-at-side and frame 2 was the arm ALREADY fully
extended at shoulder height - no turn, no weight shift, no reach, so the
single biggest movement in the action happened in one jump, which is the
stiff, robotic transition the brief rules out by name. Frames 2 and 3 then
held the same extended arm: what differed between them was the stance, not
the push, so there was no push-through at all. The gate beside him swung
through five eased stages while the man moved twice.

Six gives the brief's own sequence a frame each: rest, turn and prepare,
reach, contact, push with the weight committed, settle back. The push frame
is the one that was missing most - torso out over a bent front knee, back
leg braced, shoulder rotated forward past the chest - and it is what makes
him look like he is moving the gate rather than resting a hand on it.

HOW THE GATE KNOWS WHEN TO MOVE. These frames are deliberately drawn with an
OPEN HAND REACHING INTO EMPTY SPACE and no gate in them, so the gate can be
the separate thing it already is. The builder starts the gate's own sequence
from this one's fireOnFrame, on the frame where his palm arrives - so the
panels begin to swing as he touches them rather than before or after. With
six frames that is frame index 3, guard_push_4.

AND HE HOLDS ON WHILE IT SWINGS. The gate takes about half a second to open
and the push is faster than that, so BuildPaaralanScene1 holds guard_push_5
on loopFrames until the panels have finished before playing the settle. The
first version had no hold, and with three near-identical frames nobody saw
it; with a real settle frame he would let go halfway and the gate would
finish on its own, which is the bug this whole file exists to prevent.

TWO ROWS, NOT ONE. Six figures in a single row leaves each one too narrow
for an extended arm and they run together, which the column splitter cannot
cut. The generator lays six out as two rows of three instead, so
fit_guard_strip is given count=6 and finds the bands before the figures -
see split_figures in make_guard_frames.

MATCHING HIS OTHER FRAMES. Generated from guard_frame_1, his resting pose,
rather than from the original char_guard_idle - the wave frames replaced
that sprite in this scene, and the push has to match the guard who is
actually on screen. Fitted by the same fit_guard_strip as the wave, onto
char_guard's own 551x1425 canvas shape, because CreateStanding scales him by
bounds.size.y and a different canvas would change his height mid-push.

WHERE THE ART COMES FROM. Art/guard_push_raw.png is one Canva generation, so
all six poses are drawn in one pass and match each other.

AND HOW IT GETS HERE WITHOUT A MANUAL DOWNLOAD. The Canva API hands back only
a ~199px thumbnail of a generation - Tools/check_pose_strip.py refuses one and
says so - and the thumbnail URL is signed for those dimensions, so asking it
for a bigger one returns "Signature invalid". The full-size pixels are not
reachable from the generation at all.

They ARE reachable from a DESIGN, because export-design returns a real
download URL. So the generation is routed through one:

  1. resize-design the "SALINLAHI Art Export" holder to the generation's own
     pixel size. It makes a NEW design and leaves the holder alone. The size
     has to match or the image is letterboxed and exports soft.
  2. update_fill its rect with the generated media id, then position, resize
     and crop_media it to the full page, so the page IS the image.
  3. commit, then export-design png at the same width and height, and fetch
     the URL it returns.

The holder has to be a FIXED page. A Canva doc is a responsive page, and both
insert_fill and Magic Resize refuse those outright - "Unsupported element
container". That is what makes the 1920x1080 presentation the right starting
point rather than a blank doc.

Writes  Assets/Sprites/guard_push_1..6.png

Run from the repository root:  python Tools/make_guard_push_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_guard_frames import fit_guard_strip                     # noqa: E402

RAW = "Art/guard_push_raw.png"
OUT = "Assets/Sprites/guard_push_{}.png"

# The brief's six beats: rest, prepare, reach, contact, push, settle.
POSES = 6

if __name__ == "__main__":
    fit_guard_strip(RAW, OUT, count=POSES, uniform_scale=True)
