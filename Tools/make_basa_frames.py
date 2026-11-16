"""Draw the "Basa" reading frames - Kylo with the book up, reading it.

THE BEAT. He reaches for the book on his desk, brings it up in both hands and
reads, and he KEEPS READING until the next word is said. Docs/AnimationStandard
rule 10 lists reading among the things that repeat in life, so this is a loop
with no count on it: PoseSequence.loopCycles of 0 or less runs until Stop(),
and "Sulat" is what calls Stop.

THE BODY IS NOT REDRAWN. aklat_frame_2_uniform already has him holding the
book square in front of his chest with a hand at each bottom corner - it is
the frame he carries it to the desk in, made by Tools/make_aklat_frames.py
out of Paaralan Scene 2's place_frame_2. A child holding a book to read it
holds it exactly there. So the only thing these frames change is where he is
looking, which is the whole difference between carrying a book and reading
one.

WHAT MOVES, and why only this much. Tools/look_down.py slides his head down
on its own neck - and only his head; his eyes are left as drawn, for reasons
its docstring sets out at length. The numbers here are the beat:

  frame 1   14        settling into it. This is the last frame of the
                      lead-in as well as the first of the loop, so it is the
                      shallowest: he has only just brought the book up.
  frame 2   22        reading. The deepest of them.
  frame 3   19 / -2   a line finished, his head drifting back up a little
                      and across.
  frame 4   22 / +2   and down into the next line, leaning the other way.

Four frames run as 2, 3, 2, 4 at six a second: his head rocks gently through
about two pixels and two degrees while his eyes stay down. THAT IS THE WHOLE
GESTURE, deliberately. This loop plays under Rachel asking for the next word
and may run for half a minute if the child is slow; anything bigger becomes
the thing being watched instead of the word being learned, which is the same
reason Tools/make_kinig_frames.py keeps a nod to fourteen pixels.

NO PAGE TURN. He is holding the book with its COVER towards us - a reader
sees the pages and the room sees the back of the book - so there is no page
on screen to turn. A turning gesture here would be his hands moving over a
red rectangle, which reads as fidgeting rather than as reading.

Writes  Assets/Sprites/basa_frame_1..4_uniform.png

Run from the repository root (after Tools/make_aklat_frames.py):
    python Tools/make_basa_frames.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_down import load, look_down, save                  # noqa: E402

SRC = "aklat_open_b_uniform"

# DEEPER THAN THE FIRST PASS, because his eyes no longer help. Tools/
# look_down.py has the whole of why they do not; the short version is that
# his irises nearly fill his eyes, so sliding them left a seam across the
# top of each one that was plainly visible in the game. With the head
# carrying the gesture alone it has to be worth seeing: 22 pixels rather
# than 16, which is a chin properly down on the page and still well short of
# the 37 that a five-degree tilt moves his crown.
#
#        name                     head  tilt  what it is
FRAMES = [
    ("basa_frame_1_uniform",        14,   0.0, "settling into the read"),
    ("basa_frame_2_uniform",        22,   0.0, "reading"),
    ("basa_frame_3_uniform",        19,  -2.0, "a line finished"),
    ("basa_frame_4_uniform",        22,   2.0, "into the next line"),
]


def main():
    src = load(SRC)
    print("reading, from %s:" % SRC)
    for name, head, tilt, what in FRAMES:
        out = look_down(src, head_drop=head, tilt=tilt)
        assert out.size == src.size, (name, out.size, src.size)
        save(out, name, SRC)
        print("  %-24s head %2d px, %+0.1f deg   %s"
              % (name + ".png", head, tilt, what))

    print("\nWire them in BuildPaaralanScene3 as the 'Basa' PoseSequence:")
    print("  frames      place_frame_3, aklat_frame_2, basa_frame_1")
    print("  loopFrames  basa_frame_2, 3, 2, 4     loopCycles 0 - until Stop()")
    print("  tailFrames  basa_frame_1, aklat_frame_2, place_frame_3")
    print("  destination sitting_front_uniform")


main()
