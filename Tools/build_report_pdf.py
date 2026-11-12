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
from reportlab.platypus import (BaseDocTemplate, Frame, HRFlowable, KeepTogether,
                                PageBreak, PageTemplate, Paragraph, Spacer, Table,
                                TableStyle)

INK = colors.HexColor("#1a1a1a")
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
            i += 1
            buf = []
            while i < n and not lines[i].strip().startswith("```"):
                buf.append(lines[i].rstrip())
                i += 1
            i += 1
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
