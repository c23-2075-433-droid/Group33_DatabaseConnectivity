#!/usr/bin/env python3
"""Build one Activity 8 PDF per group member.

This is a group-developed activity with individual submission: Parts I to VIII
are the same for everyone, and Part IX is each member's own. Rather than keep
three copies of a 550-line report and let them drift, the shared body lives in
Docs/Activity8_BuildRelease.md with a {{SUBMITTER}} placeholder on the cover,
and each member's Part IX lives in its own file. This assembles them.

Usage:  build_activity8_reports.py            # all three
        build_activity8_reports.py daquis     # just one
"""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_report_pdf import build

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BODY = os.path.join(ROOT, "Docs/Activity8_BuildRelease.md")
PART9 = os.path.join(ROOT, "Docs/release/v0.1.0")
OUT = os.path.join(ROOT, "Docs")
LABEL = "Activity 8: Build and Release Management"

# key -> (lastname for the filename, full name for the cover)
MEMBERS = {
    "daquis": ("Daquis", "Daquis, Jhesza Mhei G."),
    "lacida": ("Lacida", "Lacida, Kylo Bryan"),
    "manzanero": ("Manzanero", "Manzanero, Kyla Samantha"),
}


def main(keys):
    body = io.open(BODY, encoding="utf-8").read()
    if "{{SUBMITTER}}" not in body:
        sys.exit("No {{SUBMITTER}} placeholder in %s - the cover would name the "
                 "wrong student on two of the three PDFs." % BODY)

    for key in keys:
        if key not in MEMBERS:
            sys.exit("Unknown member %r. Known: %s" % (key, ", ".join(sorted(MEMBERS))))
        lastname, fullname = MEMBERS[key]
        part9_path = os.path.join(PART9, "part9_%s.md" % key)
        if not os.path.exists(part9_path):
            sys.exit("Missing %s - every member needs their own Part IX." % part9_path)

        part9 = io.open(part9_path, encoding="utf-8").read()
        md = body.replace("{{SUBMITTER}}", fullname).rstrip() + "\n\n" + part9

        # The assembled markdown is written out too, so what produced each PDF
        # can be read and reviewed rather than only inferred from the PDF.
        md_out = os.path.join(OUT, "Activity8_%s.md" % lastname)
        io.open(md_out, "w", encoding="utf-8").write(md)

        pdf_out = os.path.join(OUT, "%s_Group33_FirstRelease.pdf" % lastname)
        build(md_out, pdf_out, LABEL)
        print("built %s" % os.path.relpath(pdf_out, ROOT))


if __name__ == "__main__":
    main(sys.argv[1:] or sorted(MEMBERS))
