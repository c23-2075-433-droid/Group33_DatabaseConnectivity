"""Read and write a Unity .unity scene as its YAML blocks.

Unity writes a scene as a flat list of blocks, each one headed by its class
and its fileID, and it reads them back BY fileID rather than by the order
they appear in. So a scene can be edited by finding blocks and rewriting
lines inside them, which is what the scene tools in this folder do, and new
blocks can simply be inserted next to the ones they belong with.

This is deliberately not a YAML parser. Unity's dialect has tags a general
parser chokes on, and a real parse-and-dump would reformat the whole file
and bury a two-line change in a ten-thousand-line diff. Everything here
works on the text of one block at a time, and every edit asserts that it
matched exactly once - a scene that has moved on is a failure, not a silent
no-op.

Used by Tools/place_uniform_on_bed.py and Tools/put_bag_beside_kylo.py.
"""
import io
import re
import sys


def num(v):
    """Unity writes floats without a trailing zero, so 20 not 20.0."""
    t = ("%.6f" % v).rstrip("0").rstrip(".")
    return "0" if t in ("", "-0") else t


class Scene:
    """The scene as its YAML blocks, addressable by fileID."""

    def __init__(self, path):
        text = io.open(path, encoding="utf-8").read()
        parts = re.split(r"^--- ", text, flags=re.M)
        self.head = parts[0]
        self.order = []
        self.block = {}
        for b in parts[1:]:
            fid = re.match(r"!u!\d+ &(\d+)", b).group(1)
            self.order.append(fid)
            self.block[fid] = b
        self.names = {f: m.group(1) for f, b in self.block.items()
                      for m in [re.search(r"^  m_Name: (.*)$", b, re.M)] if m}

    def text(self):
        return self.head + "".join("--- " + self.block[f] for f in self.order)

    def by_name(self, name):
        found = [f for f, n in self.names.items() if n == name]
        if len(found) != 1:
            sys.exit("%s: expected one '%s', found %d" % (SCENE, name, len(found)))
        return found[0]

    def components(self, go):
        return re.findall(r"component: \{fileID: (\d+)\}", self.block[go])

    def component(self, go, prefix, containing=None):
        """The one component of this type - and, where several share a type,
        the one whose block mentions `containing`.

        An Arrow has two MonoBehaviours once this script has run: the Image
        and the ArrowBob it added. Taking the first !u!114 found the ArrowBob
        on a second run and tried to set a sprite on it, so the script was
        only safe to run once. The script guid tells them apart.
        """
        found = [c for c in self.components(go)
                 if self.block[c].startswith(prefix)
                 and (containing is None or containing in self.block[c])]
        if len(found) != 1:
            sys.exit("%s: %s has %d of %s%s" % (
                SCENE, self.names.get(go, go), len(found), prefix,
                "" if containing is None else " matching " + containing))
        return found[0]

    def children(self, rect):
        kids = re.search(r"m_Children:\n((?:  - \{fileID: \d+\}\n)*)", self.block[rect])
        return re.findall(r"\{fileID: (\d+)\}", kids.group(1)) if kids else []

    def child_named(self, rect, name):
        for k in self.children(rect):
            go = re.search(r"m_GameObject: \{fileID: (\d+)\}", self.block[k]).group(1)
            if self.names.get(go) == name:
                return go, k
        return None, None

    def set(self, fid, pattern, value, label):
        b, n = re.subn(pattern, value, self.block[fid], count=1, flags=re.M)
        if n != 1:
            sys.exit("%s: could not set %s" % (SCENE, label))
        self.block[fid] = b

    def free_fileid(self, seed):
        fid = seed
        while str(fid) in self.block:
            fid += 1
        return str(fid)

    def insert_after(self, fid, block):
        new = re.match(r"!u!\d+ &(\d+)", block).group(1)
        self.order.insert(self.order.index(fid) + 1, new)
        self.block[new] = block


