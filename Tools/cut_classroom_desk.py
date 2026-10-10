"""Split the front desk off the classroom painting, so Kylo can sit behind it.

"Upo" has to read as sitting down. Kylo has no seated pose - the character art
is standing, walking and talking only - so Bahay Scene 5 solved the same
problem by splitting its one painting in two and sitting him BETWEEN the
halves: the table is drawn in front of him, hiding his lap and the chair, and
what is left above it reads as a boy sitting at a table.

menu_bg.png cannot be split on a straight line the way kusina_pov was, because
the middle of this room is open floor. A horizontal strip would put that floor
in front of Kylo and hide him while he is standing in the aisle. So only the
desk itself is lifted, as a traced polygon.

The cut line is invisible wherever it falls. This layer is the same pixels as
the background, drawn again at the same place, so the only thing the trace
decides is WHAT occludes Kylo - never how the room looks.

The sprite is written at the full size of the source with everything else
transparent, NOT cropped to the desk. Scene 2 gives it the background's own
position and scale, so the two line up exactly; a cropped sprite would need an
offset and would drift the moment the background scale changed.

Writes:  Assets/Sprites/klasrum_desk_front.png

Run from the repository root:  python Tools/cut_classroom_desk.py
"""
from PIL import Image, ImageDraw

SRC = "Assets/Sprites/menu_bg.png"
DST = "Assets/Sprites/klasrum_desk_front.png"

# The scene's camera, so the trace can be written in the world units the
# builder uses rather than in pixels of this particular painting.
VIEW_W, VIEW_H = 19.2, 10.8

# The front-left desk, traced along its top surface. Everything below this
# line, out to the left edge of the frame, is the desk and the floor under it.
DESK_WORLD = [
    (-9.60, -3.30),
    (-7.00, -3.22),
    (-5.00, -3.08),
    (-3.55, -2.95),
    (-3.60, -4.20),
    (-3.95, -5.40),
    (-9.60, -5.40),
]


def main():
    src = Image.open(SRC).convert("RGBA")
    W, H = src.size

    def to_px(p):
        wx, wy = p
        return ((wx + VIEW_W / 2) / VIEW_W * W, (VIEW_H / 2 - wy) / VIEW_H * H)

    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).polygon([to_px(p) for p in DESK_WORLD], fill=255)

    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(src, (0, 0), mask)
    out.save(DST)
    print("klasrum_desk_front.png", out.size, "(full frame, not cropped)")


if __name__ == "__main__":
    main()
