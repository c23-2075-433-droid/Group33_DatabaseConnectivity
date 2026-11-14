#!/usr/bin/env python3
"""Build one Activity 9 PDF per group member.

Same arrangement as Activity 8: this is a group-developed activity with
individual submission, so Parts I to VII are the same for everyone and Part
VIII is each member's own. The shared body lives in Docs/Activity9_Security.md
with a {{SUBMITTER}} placeholder on the cover, each member's Part VIII lives in
Docs/security/part8_<key>.md, and this assembles them rather than letting three
copies of a 900-line report drift apart.

Usage:  build_activity9_reports.py            # all three
        build_activity9_reports.py manzanero  # just one
"""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_report_pdf import build

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BODY = os.path.join(ROOT, "Docs/Activity9_Security.md")
PART8 = os.path.join(ROOT, "Docs/security")
OUT = os.path.join(ROOT, "Docs")
LABEL = "Activity 9: Software Security"

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
        part8_path = os.path.join(PART8, "part8_%s.md" % key)
        if not os.path.exists(part8_path):
            sys.exit("Missing %s - every member needs their own Part VIII." % part8_path)

        part8 = io.open(part8_path, encoding="utf-8").read()
        md = body.replace("{{SUBMITTER}}", fullname).rstrip() + "\n\n" + part8

        # The assembled markdown is written out too, so what produced each PDF
        # can be read and reviewed rather than only inferred from the PDF.
        md_out = os.path.join(OUT, "Activity9_%s.md" % lastname)
        io.open(md_out, "w", encoding="utf-8").write(md)

        pdf_out = os.path.join(OUT, "%s_Group33_SoftwareSecurity.pdf" % lastname)
        build(md_out, pdf_out, LABEL)
        print("built %s" % os.path.relpath(pdf_out, ROOT))


if __name__ == "__main__":
    main(sys.argv[1:] or sorted(MEMBERS))
