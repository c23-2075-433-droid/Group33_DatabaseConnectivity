"""Put Scene 4's uniform and socks on the bed, and make its arrows point.

The same change as the one in Assets/Editor/BuildBahayScene4.cs, applied
straight to the saved scene. Both exist because they are for different
moments: the builder is what a rebuilt scene comes out as, and this is what
the scene in the repository already is. Running the builder rewrites the
whole scene from scratch, and Chapter1_Level4_Damit.unity has drifted from
it - the saved scene still teaches Kunin where the builder now teaches
Salamin - so a rebuild is a bigger change than this one, and not one to make
on someone's behalf while they are playing the scene.

What it changes, and nothing else:

  Prop_Damit    the folded-uniform sprite, on the bed
  Prop_Medyas   the socks, beside it on the bed
  the five word prompts, lifted to PromptLift above the top of what they name
  their five arrows, swapped to ui_arrow_point, turned to point DOWN at the
                object, and given an ArrowBob so they jab rather than sit

The world positions come from the painted bed in bedroom_background.png: it
imports at 169 pixels per unit and is drawn at 1.9458 from the centre of the
screen, so one background pixel is 0.01151 world units. See the constants in
BuildBahayScene4.cs, which are the same numbers.

Unity reads a scene by fileID and not by the order blocks appear in, so the
new ArrowBob components are written in beside the Image they belong to. It
is safe to run twice: everything it writes is checked for first, and it says
what it skipped.

Run from the repository root:  python3 Tools/place_uniform_on_bed.py
"""
from PIL import Image
import io, re, sys

SCENE = "Assets/Scenes/Chapter1_Level4_Damit.unity"
FOLDED_META = "Assets/Sprites/prop_damit_folded.png.meta"
FOLDED_PNG = "Assets/Sprites/prop_damit_folded.png"
MEDYAS_PNG = "Assets/Sprites/prop_medyas.png"

ARROW_POINT_GUID = "0d4391b2493d474a99ed476ba313b781"   # ui_arrow_point.png
ARROWBOB_GUID = "d6955ecfe9e654af191fd7b44a9a0047"      # ArrowBob.cs
IMAGE_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"         # UnityEngine.UI.Image
PPU = 100.0                                             # what these props import at

# Where the clothes go, and how big. Same values as BuildBahayScene4.
DAMIT = (-6.217, -1.105, 1.05)
MEDYAS = (-5.05, -1.432, 0.75)   # grown about their bottom edge, which stays at -1.807

PROMPT_LIFT = 110.0
ARROW_SIZE = 76.0
ARROW_REACH = 16.0
ARROW_CLEAR = 18.0
ARROW_Y = -(PROMPT_LIFT - ARROW_REACH - ARROW_CLEAR) + ARROW_SIZE / 2.0

# Each prompt, and the top edge of the thing it points at, in world units.
# Bag and Kunin both point at the backpack - Bag names it, Kunin takes it.
FLOOR_Y = -3.32
PROMPTS = {
    "Prompt_Damit":   (DAMIT[0], DAMIT[1] + DAMIT[2] / 2.0),
    "Prompt_Medyas":  (MEDYAS[0], MEDYAS[1] + MEDYAS[2] / 2.0),
    "Prompt_Sapatos": (-2.0, FLOOR_Y + 0.95),
    "Prompt_Bag":     (0.2, FLOOR_Y + 2.10),
    "Prompt_Kunin":   (0.2, FLOOR_Y + 2.10),
}

# Stored the way Scene keeps every other block: without the leading "--- ",
# which text() puts back when the file is written out.
ARROWBOB_BLOCK = """!u!114 &%(fid)s
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %(go)s}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: %(script)s, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::ArrowBob
  direction: {x: 0, y: -1}
  distance: %(reach)g
  rate: 1.4
  stretch: 0.12
"""


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


def place_prop(scene, name, x, y, world_height, sprite_png, sprite_guid=None):
    go = scene.by_name(name)
    sr = scene.component(go, "!u!212 ")
    tr = scene.component(go, "!u!4 ")

    w, h = Image.open(sprite_png).size
    scale = world_height / (h / PPU)

    if sprite_guid:
        scene.set(sr, r"^  m_Sprite: \{fileID: 21300000, guid: [0-9a-f]{32}, type: 3\}$",
                  "  m_Sprite: {fileID: 21300000, guid: %s, type: 3}" % sprite_guid,
                  name + " sprite")
        scene.set(sr, r"^  m_Size: \{.*\}$",
                  "  m_Size: {x: %s, y: %s}" % (num(w / PPU), num(h / PPU)),
                  name + " size")

    scene.set(tr, r"^  m_LocalPosition: \{.*\}$",
              "  m_LocalPosition: {x: %s, y: %s, z: 0}" % (num(x), num(y)),
              name + " position")
    scene.set(tr, r"^  m_LocalScale: \{.*\}$",
              "  m_LocalScale: {x: %s, y: %s, z: 1}" % (num(scale), num(scale)),
              name + " scale")
    print("  %-14s at (%s, %s), %.2f units tall" % (name, num(x), num(y), world_height))


def point_arrow(scene, prompt_name, x, top_y):
    prompt_go = scene.by_name(prompt_name)
    prompt_rect = scene.component(prompt_go, "!u!224 ")

    scene.set(prompt_rect, r"^  m_AnchoredPosition: \{.*\}$",
              "  m_AnchoredPosition: {x: %s, y: %s}"
              % (num(x * 100.0), num(top_y * 100.0 + PROMPT_LIFT)),
              prompt_name + " position")

    arrow_go, arrow_rect = scene.child_named(prompt_rect, "Arrow")
    if arrow_go is None:
        sys.exit("%s: %s has no Arrow" % (SCENE, prompt_name))

    # Pointing DOWN at the object, not up at the ceiling: a quarter turn
    # clockwise, because ui_arrow_point is drawn pointing right.
    scene.set(arrow_rect, r"^  m_LocalRotation: \{.*\}$",
              "  m_LocalRotation: {x: 0, y: 0, z: -0.7071068, w: 0.7071068}",
              prompt_name + " arrow rotation")
    scene.set(arrow_rect, r"^  m_AnchoredPosition: \{.*\}$",
              "  m_AnchoredPosition: {x: 0, y: %s}" % num(ARROW_Y),
              prompt_name + " arrow position")
    scene.set(arrow_rect, r"^  m_SizeDelta: \{.*\}$",
              "  m_SizeDelta: {x: %s, y: %s}" % (num(ARROW_SIZE), num(ARROW_SIZE)),
              prompt_name + " arrow size")

    image = scene.component(arrow_go, "!u!114 ", IMAGE_GUID)
    scene.set(image, r"^  m_Sprite: \{fileID: 21300000, guid: [0-9a-f]{32}, type: 3\}$",
              "  m_Sprite: {fileID: 21300000, guid: %s, type: 3}" % ARROW_POINT_GUID,
              prompt_name + " arrow sprite")
    scene.set(image, r"^  m_PreserveAspect: \d$", "  m_PreserveAspect: 1",
              prompt_name + " arrow aspect")

    if any(ARROWBOB_GUID in scene.block[c] for c in scene.components(arrow_go)):
        print("  %-14s arrow already jabs" % prompt_name)
        return

    fid = scene.free_fileid(int(arrow_go) + 1)
    scene.set(arrow_go, r"^(  m_Component:\n)",
              r"\1  - component: {fileID: %s}\n" % fid,
              prompt_name + " arrow component list")
    scene.insert_after(image, ARROWBOB_BLOCK % {
        "fid": fid, "go": arrow_go, "script": ARROWBOB_GUID, "reach": ARROW_REACH})
    print("  %-14s arrow points down and jabs" % prompt_name)


def main():
    guid = re.search(r"^guid: ([0-9a-f]{32})$",
                     io.open(FOLDED_META, encoding="utf-8").read(), re.M).group(1)

    scene = Scene(SCENE)
    place_prop(scene, "Prop_Damit", DAMIT[0], DAMIT[1], DAMIT[2], FOLDED_PNG, guid)
    place_prop(scene, "Prop_Medyas", MEDYAS[0], MEDYAS[1], MEDYAS[2], MEDYAS_PNG)
    for name, (x, top) in PROMPTS.items():
        point_arrow(scene, name, x, top)

    io.open(SCENE, "w", encoding="utf-8").write(scene.text())
    print("  wrote %s" % SCENE)


main()
