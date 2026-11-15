#!/usr/bin/env python3
"""Render a Markdown subset to a ReportLab PDF, in the Group 33 house style.

Supports: # / ## / ### headings, paragraphs, - bullets, 1. numbered lists,
pipe tables, fenced code blocks, --- rules, and inline **bold** / *italic* /
`code`.

Usage:  build_report_pdf.py <input.md> <output.pdf> ["Activity label"]

The label goes in the cover line, the running footer and the PDF metadata. Left
out, it is taken from the document's own `# ` title - never from whichever
activity this script was last used for.
"""
import re
import sys

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase.pdfmetrics import stringWidth
from reportlab.platypus import (BaseDocTemplate, Flowable, Frame, HRFlowable,
                                KeepTogether, PageBreak, PageTemplate, Paragraph,
                                Spacer, Table, TableStyle)

INK = colors.HexColor("#1a1a1a")
ACCENT = colors.HexColor("#2f6b70")
ACCENT_BG = colors.HexColor("#f4f8f8")
MUTED = colors.HexColor("#5a5a5a")
RULE = colors.HexColor("#b9b9b9")
HEAD_BG = colors.HexColor("#ececec")
ZEBRA = colors.HexColor("#f7f7f7")

PAGE = A4
LMARGIN = RMARGIN = 18 * mm
TMARGIN = 20 * mm
BMARGIN = 18 * mm
AVAIL = PAGE[0] - LMARGIN - RMARGIN

# Characters outside cp1252 render as black boxes in the built-in fonts.
FOLD = {"\u2192": "->", "\u2190": "<-", "\u2713": "x", "\u2022": "-",
        "\u2026": "...", "\u00a0": " ", "\u2264": "<=", "\u2265": ">=", "\u2212": "-",
        # Vertical flow diagrams and file trees. Without these, every arrow in a
        # process diagram and every branch of a directory listing renders as a
        # black box, which is how the Activity 8 report first came out.
        # A lone "v" on its own line reads as a stray letter, not an arrow -
        # especially above a line that starts with a word. A bar reads as flow.
        "\u2193": "|", "\u2191": "|",
        "\u2500": "-", "\u2502": "|", "\u251c": "+", "\u2514": "+",
        "\u252c": "+", "\u2534": "+", "\u253c": "+", "\u2510": "+",
        "\u250c": "+", "\u2518": "+"}


def fold(text):
    for bad, good in FOLD.items():
        text = text.replace(bad, good)
    return "".join(c if c.encode("cp1252", "ignore") else "?" for c in text)


def inline(text):
    """Markdown inline markup -> ReportLab markup."""
    text = fold(text)
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    # Lift code spans out before emphasis runs, so a `*.csproj` inside one
    # cannot be mistaken for an italic delimiter.
    spans = []

    def stash(m):
        spans.append(m.group(1))
        return "\x00%d\x00" % (len(spans) - 1)

    text = re.sub(r"`([^`]+)`", stash, text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"(?<![*\w])\*([^*\n]+)\*(?!\w)", r"<i>\1</i>", text)
    for k, code in enumerate(spans):
        text = text.replace(
            "\x00%d\x00" % k,
            '<font face="Courier" size="-0.6">%s</font>' % code)
    return text


def styles():
    ss = getSampleStyleSheet()
    s = {}
    s["body"] = ParagraphStyle("body", parent=ss["Normal"], fontName="Helvetica",
                               fontSize=9.4, leading=13.4, textColor=INK,
                               alignment=TA_JUSTIFY, spaceAfter=6)
    s["h1"] = ParagraphStyle("h1", parent=s["body"], fontName="Helvetica-Bold",
                             fontSize=15, leading=19, spaceBefore=16,
                             spaceAfter=7, alignment=0, keepWithNext=1)
    s["h2"] = ParagraphStyle("h2", parent=s["body"], fontName="Helvetica-Bold",
                             fontSize=11, leading=15, spaceBefore=12,
                             spaceAfter=4, alignment=0, keepWithNext=1)
    s["bullet"] = ParagraphStyle("bullet", parent=s["body"], leftIndent=13,
                                 bulletIndent=3, spaceAfter=3,
                                 alignment=TA_JUSTIFY)
    s["code"] = ParagraphStyle("code", parent=s["body"], fontName="Courier",
                               fontSize=7.9, leading=10.4, alignment=0,
                               textColor=INK, leftIndent=7, spaceBefore=2,
                               spaceAfter=2)
    s["th"] = ParagraphStyle("th", parent=s["body"], fontName="Helvetica-Bold",
                             fontSize=8.1, leading=10.6, alignment=0,
                             spaceAfter=0)
    s["td"] = ParagraphStyle("td", parent=s["body"], fontSize=8.1, leading=10.6,
                             alignment=0, spaceAfter=0)
    s["cover_title"] = ParagraphStyle("ct", parent=s["body"],
                                      fontName="Helvetica-Bold", fontSize=21,
                                      leading=26, alignment=TA_CENTER,
                                      spaceAfter=4)
    s["cover_sub"] = ParagraphStyle("cs", parent=s["body"], fontSize=12,
                                    leading=17, alignment=TA_CENTER,
                                    textColor=MUTED, spaceAfter=3)
    s["cover_small"] = ParagraphStyle("csm", parent=s["body"], fontSize=9.4,
                                      leading=14, alignment=TA_CENTER,
                                      textColor=MUTED)
    s["note"] = ParagraphStyle("note", parent=s["body"], fontSize=8.6,
                               leading=12.2, textColor=MUTED,
                               leftIndent=9, rightIndent=9)
    return s


class Flowchart(Flowable):
    """A vertical numbered flow diagram: boxes joined by downward arrows.

    Written because the ASCII version reads badly on a page. An arrow has to
    become a character, and every candidate is wrong: "v" on a line of its own
    looks like a stray letter, and "|" is a connector with no direction. Drawn
    as vector instead, so it is a real arrow - and being drawn here rather than
    generated elsewhere, it picks up the report's own ink and rule colours, it
    stays sharp at any zoom, and it is regenerated from the source text every
    time the report is built, so the diagram cannot drift from the process it
    documents.

    Written in markdown as a fenced block tagged `flow`, one step per line.

    A flow may end in a decision, which is what the security review needed and
    the release process did not: a sequence of steps is only half of how a
    token check works, because the point of it is that there are two outcomes
    and they are not equal. Three prefixes say so:

        ? matches?              a decision - drawn as a hexagon, not numbered
        + update the row        the "yes" outcome
        - 403, the row is untouched    the "no" outcome

    The decision and its two outcomes have to come last, and that is not a
    limitation worth lifting: a flow that branches in the middle and rejoins is
    a different picture, and it should be drawn as one rather than squeezed
    through this.
    """

    FONT, SIZE, LEAD = "Helvetica", 9.3, 11.8
    PAD_X, PAD_Y, GAP, NUM_W = 9, 6, 14, 22
    MAX_W = 150 * mm
    FORK_GAP = 10          # between the two outcome boxes
    STEM = 11              # the drop below the decision before the fork splits

    def __init__(self, steps, width):
        Flowable.__init__(self)
        self.width = width
        self.box_w = min(width, self.MAX_W)

        # Split the lines into the linear part and the decision, if there is one.
        self.steps, self.decision, self.outcomes = [], None, []
        for raw in steps:
            if raw.startswith("? "):
                self.decision = raw[2:].strip()
            elif raw.startswith("+ "):
                self.outcomes.append(("Yes", raw[2:].strip()))
            elif raw.startswith("- "):
                self.outcomes.append(("No", raw[2:].strip()))
            else:
                self.steps.append(raw)

        self._lines = [self._wrap(t) for t in self.steps]
        self._heights = [len(l) * self.LEAD + 2 * self.PAD_Y for l in self._lines]

        # The decision's text is centred, so it gets the full width to wrap in.
        self._dec_lines = (self._wrap(self.decision, numbered=False)
                           if self.decision else [])
        self._dec_h = (len(self._dec_lines) * self.LEAD + 2 * self.PAD_Y
                       if self.decision else 0)

        # Both outcome boxes are the same height, so the fork reads as a pair.
        self.out_w = (self.box_w - self.FORK_GAP) / 2.0
        self._out_lines = [self._wrap(t, numbered=False, width=self.out_w)
                           for _, t in self.outcomes]
        self._out_h = (max((len(l) for l in self._out_lines), default=0)
                       * self.LEAD + 2 * self.PAD_Y)

    def _wrap(self, text, numbered=True, width=None):
        box = width if width is not None else self.box_w
        avail = box - 2 * self.PAD_X - (self.NUM_W if numbered else 0)
        lines, cur = [], ""
        for word in fold(text).split():
            trial = (cur + " " + word).strip()
            if not cur or stringWidth(trial, self.FONT, self.SIZE) <= avail:
                cur = trial
            else:
                lines.append(cur)
                cur = word
        if cur:
            lines.append(cur)
        return lines or [""]

    def wrap(self, availWidth, availHeight):
        h = sum(self._heights) + self.GAP * (len(self.steps) - 1)
        if self.decision:
            h += self.GAP + self._dec_h + self.STEM + self.GAP + self._out_h
        return self.width, h

    def draw(self):
        c = self.canv
        x = (self.width - self.box_w) / 2.0
        y = self.wrap(0, 0)[1]
        # The last plain step keeps its arrow when a decision follows it.
        last = len(self.steps) - 1 if not self.decision else -1
        for k, (lines, h) in enumerate(zip(self._lines, self._heights)):
            top = y
            y -= h
            c.setStrokeColor(ACCENT)
            c.setLineWidth(0.7)
            c.setFillColor(ACCENT_BG)
            c.roundRect(x, y, self.box_w, h, 3, stroke=1, fill=1)

            base = top - self.PAD_Y - self.SIZE + 1.5
            c.setFillColor(ACCENT)
            c.setFont("Helvetica-Bold", self.SIZE)
            c.drawRightString(x + self.PAD_X + self.NUM_W - 7, base, "%d." % (k + 1))

            c.setFillColor(INK)
            c.setFont(self.FONT, self.SIZE)
            ty = base
            for line in lines:
                c.drawString(x + self.PAD_X + self.NUM_W, ty, line)
                ty -= self.LEAD

            if k < last or (self.decision and k == len(self.steps) - 1):
                self._arrow_down(x + self.box_w / 2.0, y, y - self.GAP)
            y -= self.GAP

        if not self.decision:
            return

        # ---- the decision, as a hexagon so it is not read as another step ----
        y -= self._dec_h
        cx = x + self.box_w / 2.0
        nick = min(16.0, self.box_w / 6.0)
        mid = y + self._dec_h / 2.0
        path = c.beginPath()
        path.moveTo(x + nick, y + self._dec_h)
        path.lineTo(x + self.box_w - nick, y + self._dec_h)
        path.lineTo(x + self.box_w, mid)
        path.lineTo(x + self.box_w - nick, y)
        path.lineTo(x + nick, y)
        path.lineTo(x, mid)
        path.close()
        c.setStrokeColor(ACCENT)
        c.setLineWidth(0.9)
        c.setFillColor(colors.white)
        c.drawPath(path, stroke=1, fill=1)

        c.setFillColor(ACCENT)
        c.setFont("Helvetica-Bold", self.SIZE)
        ty = y + self._dec_h - self.PAD_Y - self.SIZE + 1.5
        for line in self._dec_lines:
            c.drawCentredString(cx, ty, line)
            ty -= self.LEAD

        if not self.outcomes:
            return

        # ---- the fork: a stem, then one arrow into each outcome ----
        stem_bottom = y - self.STEM
        c.setStrokeColor(ACCENT)
        c.setLineWidth(1.0)
        c.line(cx, y, cx, stem_bottom)

        out_top = stem_bottom - self.GAP
        out_y = out_top - self._out_h
        centres = [x + self.out_w / 2.0,
                   x + self.out_w + self.FORK_GAP + self.out_w / 2.0]
        for (label, _), ocx in zip(self.outcomes, centres):
            c.setStrokeColor(ACCENT)
            c.setLineWidth(1.0)
            c.line(cx, stem_bottom, ocx, stem_bottom)
            self._arrow_down(ocx, stem_bottom, out_top)
            # The label sits beside the horizontal run, on the side it travels.
            c.setFont("Helvetica-Bold", self.SIZE - 1.4)
            c.setFillColor(ACCENT)
            toward_left = ocx < cx
            c.drawString(ocx + (5 if toward_left else -5 -
                                stringWidth(label, "Helvetica-Bold", self.SIZE - 1.4)),
                         stem_bottom + 3.2, label)

        for k, ((_, text), lines) in enumerate(zip(self.outcomes, self._out_lines)):
            ox = x + k * (self.out_w + self.FORK_GAP)
            c.setStrokeColor(ACCENT)
            c.setLineWidth(0.7)
            c.setFillColor(ACCENT_BG)
            c.roundRect(ox, out_y, self.out_w, self._out_h, 3, stroke=1, fill=1)
            c.setFillColor(INK)
            c.setFont(self.FONT, self.SIZE)
            ty = out_top - self.PAD_Y - self.SIZE + 1.5
            for line in lines:
                c.drawString(ox + self.PAD_X, ty, line)
                ty -= self.LEAD

    def _arrow_down(self, cx, top, bottom):
        """A vertical line ending in a real arrowhead at `bottom`."""
        c = self.canv
        head = bottom + 4.5
        c.setStrokeColor(ACCENT)
        c.setLineWidth(1.0)
        c.line(cx, top, cx, head)
        path = c.beginPath()
        path.moveTo(cx - 3.2, head)
        path.lineTo(cx + 3.2, head)
        path.lineTo(cx, bottom)
        path.close()
        c.setFillColor(ACCENT)
        c.drawPath(path, stroke=0, fill=1)


class Panels(Flowable):
    """Two labelled panels side by side, with an arrow from one to the other.

    For the picture a flow cannot draw: not a sequence of steps but two places
    and the small thing that passes between them. The security review needed
    exactly one of these, and it is the diagram the whole report rests on -
    what stays on the device against what reaches the cloud - so it is worth
    being a drawing rather than a wall of pipe characters.

    Written in markdown as a fenced block tagged `panels`:

        # A phone in a classroom      <- a panel title, two of them
        the save file                 <- a heading inside the current panel
        - nickname, points and stars  <- a line belonging to the heading above
        * the submission token        <- marked out in the accent ink
        > four values                 <- the label on the arrow between them

    The heading and its indented lines are what make the picture readable:
    without them a panel is a list of eight equal things, and the whole point
    of this diagram is which of them sit inside which.

    The arrow is drawn left to right, because that is the direction the data
    travels and a reader should not have to be told so in a caption.
    """

    FONT, SIZE, LEAD = "Helvetica", 8.7, 11.2
    TITLE_SIZE = 9.4
    PAD_X, PAD_Y = 9, 7
    SUB_X = 9                # how far a line sits inside its heading
    MID = 30                 # the channel the arrow runs through
    MAX_W = 165 * mm

    def __init__(self, spec, width):
        Flowable.__init__(self)
        self.width = width
        self.total_w = min(width, self.MAX_W)
        self.panel_w = (self.total_w - self.MID) / 2.0

        self.titles, self.bodies, self.arrow = ["", ""], [[], []], ""
        which = -1
        for raw in spec:
            line = raw.strip()
            if not line:
                continue
            if line.startswith("# "):
                which += 1
                if which < 2:
                    self.titles[which] = line[2:].strip()
            elif line.startswith("> "):
                self.arrow = line[2:].strip()
            elif 0 <= which < 2:
                # kind: "head" stands on its own, "sub" belongs to the head
                # above it, "mark" is the one line worth the accent ink.
                if line.startswith("* "):
                    self.bodies[which].append(("mark", line[2:].strip()))
                elif line.startswith("- "):
                    self.bodies[which].append(("sub", line[2:].strip()))
                else:
                    self.bodies[which].append(("head", line))

        # Wrap every line, then make both panels as tall as the taller one so
        # the pair reads as two things being compared rather than two lists.
        self._wrapped = []
        for which in (0, 1):
            rows = []
            for kind, text in self.bodies[which]:
                indent = self.SUB_X if kind == "sub" else 0
                for k, line in enumerate(self._wrap(text, indent)):
                    rows.append((kind, line, k == 0, indent))
            self._wrapped.append(rows)

        tallest = max(len(r) for r in self._wrapped) if self._wrapped else 0
        self.panel_h = (self.PAD_Y * 2 + self.TITLE_SIZE + 7
                        + tallest * self.LEAD)

    def _wrap(self, text, indent=0):
        avail = self.panel_w - 2 * self.PAD_X - indent
        lines, cur = [], ""
        for word in fold(text).split():
            trial = (cur + " " + word).strip()
            if not cur or stringWidth(trial, self.FONT, self.SIZE) <= avail:
                cur = trial
            else:
                lines.append(cur)
                cur = word
        if cur:
            lines.append(cur)
        return lines or [""]

    def wrap(self, availWidth, availHeight):
        return self.width, self.panel_h

    def draw(self):
        c = self.canv
        x0 = (self.width - self.total_w) / 2.0
        for which in (0, 1):
            x = x0 + which * (self.panel_w + self.MID)
            c.setStrokeColor(RULE)
            c.setLineWidth(0.7)
            c.setFillColor(colors.white)
            c.roundRect(x, 0, self.panel_w, self.panel_h, 3, stroke=1, fill=1)

            # The title sits on a tinted strip, so the panels have a top edge
            # to read from rather than starting mid-sentence.
            strip_h = self.PAD_Y + self.TITLE_SIZE + 3
            c.setFillColor(ACCENT_BG)
            c.rect(x + 0.4, self.panel_h - strip_h, self.panel_w - 0.8,
                   strip_h - 0.4, stroke=0, fill=1)
            c.setFillColor(ACCENT)
            c.setFont("Helvetica-Bold", self.TITLE_SIZE)
            c.drawString(x + self.PAD_X,
                         self.panel_h - self.PAD_Y - self.TITLE_SIZE + 1.5,
                         fold(self.titles[which]))

            ty = self.panel_h - strip_h - 6 - self.SIZE
            for kind, line, first, indent in self._wrapped[which]:
                if kind == "mark":
                    c.setFillColor(ACCENT)
                    c.setFont("Helvetica-Bold", self.SIZE)
                elif kind == "head":
                    c.setFillColor(INK)
                    c.setFont("Helvetica-Bold", self.SIZE)
                else:
                    c.setFillColor(MUTED)
                    c.setFont(self.FONT, self.SIZE)
                # A wrapped line is indented past its own first line, so a
                # sentence that runs on is never mistaken for a new item.
                c.drawString(x + self.PAD_X + indent + (0 if first else 7),
                             ty, line)
                ty -= self.LEAD

        # ---- the arrow through the channel, left to right ----
        y = self.panel_h / 2.0
        ax, bx = x0 + self.panel_w, x0 + self.panel_w + self.MID
        c.setStrokeColor(ACCENT)
        c.setLineWidth(1.1)
        c.line(ax + 2, y, bx - 4.5, y)
        path = c.beginPath()
        path.moveTo(bx - 4.5, y - 3.2)
        path.lineTo(bx - 4.5, y + 3.2)
        path.lineTo(bx - 1, y)
        path.close()
        c.setFillColor(ACCENT)
        c.drawPath(path, stroke=0, fill=1)

        if self.arrow:
            label = fold(self.arrow)
            c.setFont("Helvetica-Bold", self.SIZE - 1.2)
            w = stringWidth(label, "Helvetica-Bold", self.SIZE - 1.2)
            c.setFillColor(colors.white)
            c.rect((ax + bx) / 2.0 - w / 2.0 - 2, y + 4, w + 4,
                   self.SIZE - 0.5, stroke=0, fill=1)
            c.setFillColor(ACCENT)
            c.drawCentredString((ax + bx) / 2.0, y + 5.5, label)


def make_table(rows, s):
    """Lay a pipe table out so no column is narrower than its longest word."""
    from reportlab.pdfbase.pdfmetrics import stringWidth

    ncol = max(len(r) for r in rows)
    rows = [r + [""] * (ncol - len(r)) for r in rows]
    # A table written with an empty header row is a key/value list, not a
    # column table: drop the blank row and emphasise the key column instead.
    keyed = not any(c.strip() for c in rows[0])
    if keyed:
        rows = rows[1:]
    pad = 2 * 4.5 + 1.5

    def plain(cell):
        return re.sub(r"[`*]", "", fold(cell))

    mins, prefs = [], []
    for c in range(ncol):
        widest_word = 0.0
        volume = 0
        for r in range(len(rows)):
            txt = plain(rows[r][c])
            volume = max(volume, len(txt))
            font = ("Helvetica-Bold"
                    if (r == 0 and not keyed) or (keyed and c == 0)
                    else "Helvetica")
            for word in txt.split():
                widest_word = max(widest_word,
                                  stringWidth(word, font, 8.1))
        mins.append(min(widest_word + pad, AVAIL / 2.0))
        prefs.append(max(volume, 6))

    slack = AVAIL - sum(mins)
    if slack <= 0:                      # pathological: share out evenly
        widths = [AVAIL * m / sum(mins) for m in mins]
    else:
        # Give each column its minimum, then split the slack by text volume,
        # capped so one wordy column cannot swallow the table.
        total_pref = float(sum(prefs))
        widths = [mins[c] + slack * prefs[c] / total_pref for c in range(ncol)]

    def cell_style(r, c):
        if keyed:
            return s["th"] if c == 0 else s["td"]
        return s["th"] if r == 0 else s["td"]

    data = [[Paragraph(inline(c), cell_style(r, i)) for i, c in enumerate(row)]
            for r, row in enumerate(rows)]
    t = Table(data, colWidths=widths, repeatRows=0 if keyed else 1,
              hAlign="LEFT")
    style = [("BACKGROUND", (0, 0), (-1, 0),
              ZEBRA if keyed else HEAD_BG),
             ("VALIGN", (0, 0), (-1, -1), "TOP"),
             ("GRID", (0, 0), (-1, -1), 0.4, RULE),
             ("LEFTPADDING", (0, 0), (-1, -1), 4.5),
             ("RIGHTPADDING", (0, 0), (-1, -1), 4.5),
             ("TOPPADDING", (0, 0), (-1, -1), 3.6),
             ("BOTTOMPADDING", (0, 0), (-1, -1), 3.6)]
    if keyed:
        style.append(("BACKGROUND", (0, 0), (0, -1), ZEBRA))
    else:
        for r in range(2, len(rows), 2):
            style.append(("BACKGROUND", (0, r), (-1, r), ZEBRA))
    t.setStyle(TableStyle(style))
    return t


def parse(md, s):
    """Markdown -> flowables.  Returns (cover_flowables, body_flowables)."""
    lines = md.split("\n")
    out, i, n = [], 0, len(lines)
    while i < n:
        line = lines[i]
        stripped = line.strip()

        if stripped.startswith("```"):
            lang = stripped[3:].strip().lower()
            i += 1
            buf = []
            while i < n and not lines[i].strip().startswith("```"):
                buf.append(lines[i].rstrip())
                i += 1
            i += 1
            if lang == "flow":
                steps = [l.strip() for l in buf if l.strip()]
                out.append(Spacer(1, 7))
                out.append(KeepTogether(Flowchart(steps, AVAIL)))
                out.append(Spacer(1, 9))
                continue
            if lang == "panels":
                out.append(Spacer(1, 7))
                out.append(KeepTogether(Panels(buf, AVAIL)))
                out.append(Spacer(1, 9))
                continue
            text = "<br/>".join(
                fold(l).replace("&", "&amp;").replace("<", "&lt;")
                .replace(">", "&gt;").replace(" ", "&nbsp;") or "&nbsp;"
                for l in buf)
            block = Table([[Paragraph(text, s["code"])]], colWidths=[AVAIL],
                          hAlign="LEFT")
            block.setStyle(TableStyle([
                ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#f4f4f4")),
                ("BOX", (0, 0), (-1, -1), 0.4, RULE),
                ("LEFTPADDING", (0, 0), (-1, -1), 6),
                ("RIGHTPADDING", (0, 0), (-1, -1), 6),
                ("TOPPADDING", (0, 0), (-1, -1), 5),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 5)]))
            out.append(Spacer(1, 3))
            out.append(block)
            out.append(Spacer(1, 7))
            continue

        if stripped.startswith("|") and i + 1 < n and re.match(
                r"^\|[\s:\-|]+\|$", lines[i + 1].strip()):
            rows = []
            while i < n and lines[i].strip().startswith("|"):
                raw = lines[i].strip()
                if re.match(r"^\|[\s:\-|]+\|$", raw):
                    i += 1
                    continue
                cells = [c.strip() for c in raw.strip("|").split("|")]
                rows.append(cells)
                i += 1
            out.append(Spacer(1, 3))
            out.append(make_table(rows, s))
            out.append(Spacer(1, 9))
            continue

        if stripped.startswith("### "):
            out.append(Paragraph(inline(stripped[4:]), s["h2"]))
            i += 1
            continue
        if stripped.startswith("## "):
            out.append(Paragraph(inline(stripped[3:]), s["h1"]))
            out.append(HRFlowable(width="100%", thickness=0.7, color=RULE,
                                  spaceBefore=1, spaceAfter=7))
            i += 1
            continue
        if stripped.startswith("# "):
            out.append(("TITLE", stripped[2:]))
            i += 1
            continue
        if stripped == "---":
            out.append(("RULE", None))
            i += 1
            continue

        m = re.match(r"^(\d+)\.\s+(.*)$", stripped)
        if stripped.startswith("- ") or m:
            items = []
            while i < n:
                st = lines[i].strip()
                mm_ = re.match(r"^(\d+)\.\s+(.*)$", st)
                if st.startswith("- "):
                    marker, text = "\u2013", st[2:]
                elif mm_:
                    marker, text = mm_.group(1) + ".", mm_.group(2)
                elif st and items and not st.startswith(("|", "#", "```", "-")) \
                        and lines[i].startswith(("  ", "\t")):
                    items[-1] = (items[-1][0], items[-1][1] + " " + st)
                    i += 1
                    continue
                else:
                    break
                items.append((marker, text))
                i += 1
            for marker, text in items:
                out.append(Paragraph(inline(text), s["bullet"],
                                     bulletText=fold(marker)))
            out.append(Spacer(1, 5))
            continue

        if not stripped:
            i += 1
            continue

        buf = [stripped]
        i += 1
        while i < n and lines[i].strip() and not lines[i].strip().startswith(
                ("|", "#", "```", "- ", "---")) and not re.match(
                r"^\d+\.\s", lines[i].strip()):
            buf.append(lines[i].strip())
            i += 1
        para = " ".join(buf)
        style = s["note"] if para.startswith("*(") else s["body"]
        out.append(Paragraph(inline(para), style))
    return out


def build(md_path, pdf_path, activity=None, course="Systems Fundamentals"):
    s = styles()
    with open(md_path, encoding="utf-8") as fh:
        md = fh.read()
    flows = parse(md, s)

    if not activity:
        activity = next((fold(f[1]) for f in flows
                         if isinstance(f, tuple) and f[0] == "TITLE"), "Report")

    # Split the leading block (title + metadata table) onto a cover page.
    first_part = next(k for k, f in enumerate(flows)
                      if isinstance(f, Paragraph)
                      and f.style.name == "h1"
                      and "Part I." in f.getPlainText())
    head, body = flows[:first_part], flows[first_part:]

    story = [Spacer(1, 26 * mm)]
    seen_table = False
    for f in head:
        if isinstance(f, tuple):
            if f[0] == "TITLE":
                story.append(Paragraph(inline(f[1]), s["cover_title"]))
                story.append(HRFlowable(width="48%", thickness=1.1, color=RULE,
                                        spaceBefore=8, spaceAfter=12,
                                        hAlign="CENTER"))
            continue
        if isinstance(f, Paragraph) and f.style.name == "body":
            story.append(Paragraph(
                f.text, s["cover_small"] if seen_table else s["cover_sub"]))
        elif isinstance(f, Table):
            story.append(Spacer(1, 10 * mm))
            story.append(f)
            story.append(Spacer(1, 7 * mm))
            seen_table = True
        else:
            story.append(f)
    story.append(Spacer(1, 12 * mm))
    story.append(Paragraph("%s &middot; %s" % (course, activity),
                           s["cover_small"]))
    story.append(PageBreak())

    for f in body:
        if isinstance(f, tuple):
            if f[0] == "RULE":
                story.append(Spacer(1, 2))
            continue
        story.append(f)

    doc = BaseDocTemplate(pdf_path, pagesize=PAGE, leftMargin=LMARGIN,
                          rightMargin=RMARGIN, topMargin=TMARGIN,
                          bottomMargin=BMARGIN,
                          title=activity,
                          author="Daquis, Jhesza Mhei G. - Group 33",
                          subject="SALINLAHI capstone - %s" % activity)
    frame = Frame(LMARGIN, BMARGIN, AVAIL, PAGE[1] - TMARGIN - BMARGIN,
                  id="main")

    def decorate(canvas, _doc):
        canvas.saveState()
        if canvas.getPageNumber() > 1:
            canvas.setFont("Helvetica", 7.6)
            canvas.setFillColor(MUTED)
            canvas.drawString(LMARGIN, 11 * mm,
                              "SALINLAHI - Group 33 - %s" % activity)
            canvas.drawRightString(PAGE[0] - RMARGIN, 11 * mm,
                                   "Page %d" % canvas.getPageNumber())
            canvas.setStrokeColor(RULE)
            canvas.setLineWidth(0.4)
            canvas.line(LMARGIN, 14 * mm, PAGE[0] - RMARGIN, 14 * mm)
        canvas.restoreState()

    doc.addPageTemplates([PageTemplate(id="all", frames=[frame],
                                       onPage=decorate)])
    doc.build(story)
    return pdf_path


if __name__ == "__main__":
    out = build(sys.argv[1], sys.argv[2],
                sys.argv[3] if len(sys.argv) > 3 else None)
    print("wrote", out)
