"""Tiny SVG drawing helpers for the SALINLAHI Chapter 3 figures.
Every shape is placed by explicit coordinates so the layout is exact and
editable: change a number, re-run build_figures.py."""
from xml.sax.saxutils import escape
import math, re, sys
try:
    from PIL import ImageFont
    _FONTS = {}
    def measure(text, size, bold=False):
        """Rendered width of one line in Arial (Helvetica's metric twin), in figure units."""
        key = (round(size * 4), bold)
        if key not in _FONTS:
            path = "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf"
            _FONTS[key] = ImageFont.truetype(path, key[0])
        return _FONTS[key].getlength(text) / 4.0
except Exception:   # no Pillow: skip the fit checks
    def measure(text, size, bold=False): return 0.0

WARNINGS = []
def _fit(lines, size, avail, where):
    """Record a warning for every line wider than the room it has."""
    if isinstance(lines, str): lines = lines.split("\n")
    for i, l in enumerate(lines):
        bold = l.startswith("**")
        w = measure(l.replace("**", ""), size, bold)
        room = avail(i, len(lines)) if callable(avail) else avail
        if w > room:
            WARNINGS.append(f"{where}: '{l}' is {w:.0f} wide, room {room:.0f}")

FONT = "Helvetica, Arial, sans-serif"
INK = "#1f2937"; LINE = "#374151"; MUTED = "#6b7280"
FILL = {"process": "#eef3fa", "decision": "#fdf6e3", "start": "#e3edf9",
        "good": "#e6f4ea", "bad": "#fde8e8", "warn": "#fff4dc", "note": "#f7f7f5",
        "device": "#f4f8f4", "net": "#eef3fb", "maybe": "#fff7ea", "white": "#ffffff"}
STROKE = {"good": "#2f7d4a", "bad": "#b42318", "warn": "#b7791f", "process": "#4a6a8a",
          "decision": "#8a6d2b", "start": "#4a6a8a", "note": "#9ca3af", "device": "#2f7d4a",
          "net": "#3b5b8c", "maybe": "#b7791f", "white": "#4a6a8a"}

class SVG:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.items = []
        self.min_font = 99
    def add(self, s): self.items.append(s)
    def text(self, x, y, lines, size=15, weight="normal", anchor="middle", color=INK, italic=False, lh=1.3):
        if isinstance(lines, str): lines = lines.split("\n")
        n = len(lines); top = y - (n - 1) * size * lh / 2
        style = ' font-style="italic"' if italic else ''
        self.min_font = min(self.min_font, size)
        for i, l in enumerate(lines):
            w = weight
            if l.startswith("**") and l.endswith("**"): l = l[2:-2]; w = "bold"
            self.add(f'<text x="{x:.1f}" y="{top + i*size*lh:.1f}" font-family="{FONT}" font-size="{size}" '
                     f'font-weight="{w}" fill="{color}" text-anchor="{anchor}" dominant-baseline="central"{style}>{escape(l)}</text>')
    def box(self, cx, cy, w, h, lines, kind="process", size=15, r=10, dash=False, title=None, sw=2):
        d = ' stroke-dasharray="7 5"' if dash else ''
        self.add(f'<rect x="{cx-w/2:.1f}" y="{cy-h/2:.1f}" width="{w}" height="{h}" rx="{r}" ry="{r}" '
                 f'fill="{FILL[kind]}" stroke="{STROKE[kind]}" stroke-width="{sw}"{d}/>')
        if lines:
            self.text(cx, cy, lines, size)
            _fit(lines, size, w - 18, "box")
    def term(self, cx, cy, w, h, lines, kind="start", size=15):
        self.box(cx, cy, w, h, lines, kind, size, r=h/2)
    def diamond(self, cx, cy, w, h, lines, size=15):
        pts = f"{cx},{cy-h/2} {cx+w/2},{cy} {cx},{cy+h/2} {cx-w/2},{cy}"
        self.add(f'<polygon points="{pts}" fill="{FILL["decision"]}" stroke="{STROKE["decision"]}" stroke-width="2"/>')
        self.text(cx, cy, lines, size)
        def room(i, n, lh=1.3):
            dy = abs((i - (n - 1) / 2) * size * lh) + size * 0.5
            return w * max(0.0, 1 - dy / (h / 2)) - 12
        _fit(lines, size, room, "diamond")
    def circle(self, cx, cy, r, label, size=15):
        self.add(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="#ffffff" stroke="{LINE}" stroke-width="2"/>')
        self.text(cx, cy, label, size, "bold")
    def path(self, pts, arrow=True, dash=False, color=LINE, sw=2):
        d = "M " + " L ".join(f"{x:.1f} {y:.1f}" for x, y in pts)
        m = ' marker-end="url(#arr)"' if arrow else ''
        da = ' stroke-dasharray="8 6"' if dash else ''
        self.add(f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{sw}"{m}{da} stroke-linejoin="round"/>')
    def label(self, x, y, lines, size=13, anchor="middle", color="#374151", bg=True, weight="bold"):
        if isinstance(lines, str): lines = lines.split("\n")
        if bg:
            wmax = max(len(l) for l in lines) * size * 0.56 + 10
            hh = len(lines) * size * 1.3 + 6
            x0 = x - wmax/2 if anchor == "middle" else (x - 5 if anchor == "start" else x - wmax + 5)
            self.add(f'<rect x="{x0:.1f}" y="{y-hh/2:.1f}" width="{wmax:.1f}" height="{hh:.1f}" fill="#ffffff" opacity="0.95"/>')
        self.text(x, y, lines, size, weight, anchor, color)
    def ellipse(self, cx, cy, rx, ry, lines, kind="process", size=15, dash=False):
        d = ' stroke-dasharray="7 5"' if dash else ''
        self.add(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{FILL[kind]}" stroke="{STROKE[kind]}" stroke-width="2"{d}/>')
        self.text(cx, cy, lines, size)
        def room(i, n, lh=1.3):
            dy = abs((i - (n - 1) / 2) * size * lh) + size * 0.5
            return 2 * rx * math.sqrt(max(0.0, 1 - (dy / ry) ** 2)) - 14
        _fit(lines, size, room, "ellipse")
    def actor(self, cx, cy, lines, size=15, above=False):
        s = LINE
        self.add(f'<circle cx="{cx}" cy="{cy-48}" r="14" fill="#fff" stroke="{s}" stroke-width="2"/>')
        self.add(f'<path d="M {cx} {cy-34} L {cx} {cy+6} M {cx-24} {cy-20} L {cx+24} {cy-20} M {cx} {cy+6} L {cx-20} {cy+38} M {cx} {cy+6} L {cx+20} {cy+38}" stroke="{s}" stroke-width="2" fill="none"/>')
        n = len(lines.split("\n")) if isinstance(lines, str) else len(lines)
        self.text(cx, (cy - 82 - (n - 1) * size * 0.65) if above else cy + 62 + (n - 1) * size * 0.65, lines, size, "bold")
    def save(self, path, display_pt=None):
        """display_pt: the width the figure is shown at in the paper, in points."""
        if display_pt:
            k = display_pt / self.w
            print(f"{path}: {self.w}x{self.h} shown at {display_pt:.0f} x {self.h*k:.0f} pt; "
                  f"smallest text {self.min_font*k:.1f} pt", file=sys.stderr)
        for w in WARNINGS: print("  OVERFLOW " + w, file=sys.stderr)
        WARNINGS.clear()
        head = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}">'
                '<defs><marker id="arr" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="9" markerHeight="9" orient="auto-start-reverse">'
                f'<path d="M0,0 L10,5 L0,10 z" fill="{LINE}"/></marker>'
                '<marker id="open" viewBox="0 0 12 12" refX="11" refY="6" markerWidth="11" markerHeight="11" orient="auto">'
                f'<path d="M0,0 L11,6 L0,12" fill="none" stroke="{LINE}" stroke-width="1.6"/></marker></defs>'
                f'<rect width="{self.w}" height="{self.h}" fill="#ffffff"/>')
        open(path, "w", encoding="utf-8").write(head + "\n".join(self.items) + "</svg>")

def panel(s, x, y, w, h, title, lines, kind="process", size=14, dash=False, tsize=14, lh=1.45):
    """A rectangle with a bold title and left-aligned lines (x, y = top-left)."""
    d = ' stroke-dasharray="8 6"' if dash else ''
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="8" fill="{FILL[kind]}" stroke="{STROKE[kind]}" stroke-width="2"{d}/>')
    s.min_font = min(s.min_font, size, tsize)
    _fit([title], tsize * 1.12, w - 32, "panel title")
    _fit(lines, size, w - 32, "panel")
    s.add(f'<text x="{x+16}" y="{y+24}" font-family="{FONT}" font-size="{tsize}" font-weight="bold" letter-spacing="1.2" fill="{STROKE[kind]}" dominant-baseline="central">{escape(title)}</text>')
    for i, l in enumerate(lines):
        # **bold** may appear anywhere in the line
        parts = l.split("**")
        spans = "".join(f'<tspan font-weight="bold">{escape(t)}</tspan>' if k % 2 else escape(t)
                        for k, t in enumerate(parts) if t)
        s.add(f'<text x="{x+16}" y="{y+52+i*size*lh:.1f}" font-family="{FONT}" font-size="{size}" fill="{INK}" dominant-baseline="central">{spans}</text>')

def dagger(s, x, y, size=20):
    """Marks an element added after the v0.1.1 release."""
    s.text(x, y, "†", size, "bold", color="#b42318")
