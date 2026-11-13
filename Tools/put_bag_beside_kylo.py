"""Move Scene 4's bag within Kylo's reach and make picking it up its own beat.

The same change as the one in Assets/Editor/BuildBahayScene4.cs, applied
straight to the saved scene, for the same reason as Tools/place_uniform_on_bed.py:
the saved scene has drifted from the builder, so rebuilding it would be a
bigger change than this one.

What it changes, and nothing else:

  Prop_Bag       moved from the middle of the floor to x 3.19, beside Kylo,
                 and given a TRIGGER collider so it can be tapped without
                 becoming something he can walk into
  Gleam_Bag      follows it
  Prompt_Bag     follows it, the same lift above it as every other badge
  BagPickup      a new object holding the sequence. Not on the bag: the bag
                 is switched off for most of the level, and a component on a
                 disabled object does not run
  the Bag word   saying it BRINGS THE BAG OUT and gleams it; it then stays
                 on the floor
  the Kunin word takes it: onCorrect runs the pickup sequence, and the bag
                 goes onto his back at the grab moment
  maxActionWait  4 -> 12 seconds

TWO WORDS, NOT A WORD AND A TAP. The bag is the one thing in this scene the
child is meant to DO something with, and saying "Bag" used to make it vanish
in the same instant - the naming and the taking were the same event. They
are two now, which is what the pair of words was always for: "Bag" names the
thing, "Kunin" takes it.

The interaction between them is SPEECH, not a tap. This is a game played
with your voice; a tap would be the one thing it never otherwise asks for,
and a child who does not realise they are meant to tap would be stuck
looking at a bag. BagPickup.Arm() is still there for anywhere a tap is
wanted. Nothing here wires it.

Safe to run twice: it checks for what it writes before writing it.

Run from the repository root:  python3 Tools/put_bag_beside_kylo.py
"""
from unity_scene import Scene, num
import io
import re
import sys

SCENE = "Assets/Scenes/Chapter1_Level4_Damit.unity"
SCRIPT_META = "Assets/Scripts/BagPickup.cs.meta"

FLOOR_Y = -3.32
BAG_X = 3.19
BAG_H = 2.10
BAG_Y = FLOOR_Y + BAG_H / 2.0
PROMPT_LIFT = 110.0

# The bag sprite's own size in units, which is what a collider on it measures
# in before the transform's scale is applied.
BAG_SPRITE = (9.29, 10.66)

MAX_ACTION_WAIT = 12            # longer than any pickup sequence

# The bag's two words, and what each one does. Anything not named here keeps
# whatever it already had.
#
# THE BAG ARRIVES ON THE ANSWER, NOT ON THE QUESTION. Putting it out when the
# word was asked meant the child saw a bag, was told to say "Bag", and said
# it - with nothing between the three. Arriving when the word lands makes the
# word do something: the room did not have a bag in it until they named one.
# Kunin still puts it out on being shown, because by then it is already there
# and that call only matters if a transition brings the scene back here.
BAG_WORDS = {
    "Bag": dict(
        shown=[],
        correct=[("BagPickup", "Appear", "pickup"),
                 ("TimedOverlay", "Play", "gleam")]),
    "Kunin": dict(
        shown=[("BagPickup", "Appear", "pickup")],
        correct=[("BagPickup", "StartBagPickup", "pickup")]),
}
PROMPT_OF = {"Bag": "Prompt_Bag", "Kunin": "Prompt_Kunin"}

COLLIDER = """!u!61 &%(fid)s
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %(go)s}
  m_Enabled: 1
  serializedVersion: 3
  m_Density: 1
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_CompositeOperation: 0
  m_CompositeOrder: 0
  m_Offset: {x: 0, y: 0}
  m_SpriteTilingProperty:
    border: {x: 0, y: 0, z: 0, w: 0}
    pivot: {x: 0, y: 0}
    oldSize: {x: 0, y: 0}
    newSize: {x: 0, y: 0}
    adaptiveTilingThreshold: 0
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  m_Size: {x: %(w)s, y: %(h)s}
  m_EdgeRadius: 0
"""

GAMEOBJECT = """!u!1 &%(go)s
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: %(tr)s}
  - component: {fileID: %(mb)s}
  m_Layer: 0
  m_Name: BagPickup
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""

TRANSFORM = """!u!4 &%(tr)s
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %(go)s}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"""

BEHAVIOUR = """!u!114 &%(mb)s
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
  m_EditorClassIdentifier: Assembly-CSharp::BagPickup
  bag: {fileID: %(bag)s}
  tapArea: {fileID: %(collider)s}
  pickUp: {fileID: %(pickup)s}
  player: {fileID: %(player)s}
  animator: {fileID: 0}
  animatorTrigger: PickUpBag
  gleam: {fileID: %(gleam)s}
  tapPadding: 0.4
  waitForTapSeconds: 6
  fallbackGrabDelay: 0.4
  grabTimeout: 2.5
  settleAfterGrab: 0.35
  onPickedUp:
    m_PersistentCalls:
      m_Calls: []
"""

CALL = """        - m_Target: {fileID: %(target)s}
          m_TargetAssemblyTypeName: %(type)s, Assembly-CSharp
          m_MethodName: %(method)s
          m_Mode: 0
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: 
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
"""


def event(name, calls):
    out = "    %s:\n      m_PersistentCalls:\n        m_Calls:" % name
    if not calls:
        return out + " []\n"
    return out + "\n" + "".join(CALL % c for c in calls)


def script_guid():
    text = io.open(SCRIPT_META, encoding="utf-8").read()
    m = re.search(r"^guid: ([0-9a-f]{32})$", text, re.M)
    if m is None:
        sys.exit("%s has no guid - let Unity import BagPickup.cs first" % SCRIPT_META)
    return m.group(1)


def component_of(scene, go, ident):
    for c in scene.components(go):
        if ("m_EditorClassIdentifier: " + ident) in scene.block[c]:
            return c
    sys.exit("%s: %s has no %s" % (SCENE, scene.names.get(go, go), ident))


OBJECTIVE_HEAD = """  - word: %(word)s
    variants: []
    promptRoot: {fileID: %(prompt)s}
    instructionClip: {fileID: 0}
"""


def rebuild_objectives(scene, controller, targets):
    """Rewrite the objective list: both bag words point at BagPickup.

    The words that dress him are left exactly as they are - this change is
    not their business. Only Bag and Kunin are rewritten, and Kunin is built
    from scratch if an earlier version of this tool removed it.
    """
    block = scene.block[controller]
    start = block.index("  objectives:\n")
    end = block.index("  onAllObjectivesComplete:")
    body = block[start + len("  objectives:\n"):end]

    entries = re.split(r"^(?=  - word: )", body, flags=re.M)
    entries = [e for e in entries if e.strip()]
    by_word = {re.search(r"^  - word: (.*)$", e, re.M).group(1).strip(): e
               for e in entries}

    out, order = [], []
    for e in entries:
        word = re.search(r"^  - word: (.*)$", e, re.M).group(1).strip()
        if word in BAG_WORDS:
            continue                      # rebuilt below, in a fixed order
        out.append(e)
        order.append(word)

    for word in ("Bag", "Kunin"):
        prompt = scene.by_name(PROMPT_OF[word])
        head = by_word.get(word)
        if head is not None:
            head = head[:head.index("    onShown:")] if "    onShown:" in head \
                else head[:head.index("    onCorrect:")]
        else:
            head = OBJECTIVE_HEAD % dict(word=word, prompt=prompt)

        spec = BAG_WORDS[word]
        shown = [dict(target=targets[key], type=typ, method=meth)
                 for typ, meth, key in spec["shown"]]
        correct = [dict(target=targets[key], type=typ, method=meth)
                   for typ, meth, key in spec["correct"]]
        out.append(head + event("onShown", shown) + event("onCorrect", correct))
        order.append(word)

    scene.block[controller] = (block[:start] + "  objectives:\n"
                               + "".join(out) + block[end:])
    return order


def main():
    scene = Scene(SCENE)
    guid = script_guid()

    bag_go = scene.by_name("Prop_Bag")
    bag_tr = scene.component(bag_go, "!u!4 ")
    pickup = component_of(scene, bag_go, "Assembly-CSharp::PickUpItem")

    gleam_go = scene.by_name("Gleam_Bag")
    gleam_tr = scene.component(gleam_go, "!u!4 ")
    gleam = component_of(scene, gleam_go, "Assembly-CSharp::TimedOverlay")

    player_go = scene.by_name("player_character")
    player = component_of(scene, player_go, "Assembly-CSharp::PlayerMovement")

    prompt_rects = [scene.component(scene.by_name(n), "!u!224 ")
                    for n in ("Prompt_Bag", "Prompt_Kunin")]

    canvas_go = scene.by_name("UI_WordPrompts_Canvas")
    controller = component_of(scene, canvas_go, "Assembly-CSharp::SceneObjectiveController")

    # --- the bag, and what follows it ---
    for fid, label in ((bag_tr, "Prop_Bag"), (gleam_tr, "Gleam_Bag")):
        scene.set(fid, r"^  m_LocalPosition: \{.*\}$",
                  "  m_LocalPosition: {x: %s, y: %s, z: 0}" % (num(BAG_X), num(BAG_Y)),
                  label + " position")
    for rect in prompt_rects:
        scene.set(rect, r"^  m_AnchoredPosition: \{.*\}$",
                  "  m_AnchoredPosition: {x: %s, y: %s}"
                  % (num(BAG_X * 100.0), num((FLOOR_Y + BAG_H) * 100.0 + PROMPT_LIFT)),
                  "bag badge position")
    # Off in the saved scene as well, not only once Awake has run: the bag
    # belongs to the last word, and a backpack standing in the room through
    # the first three is a thing to say "Bag" at before being asked.
    scene.set(bag_go, r"^  m_IsActive: \d$", "  m_IsActive: 0", "Prop_Bag hidden")
    print("  Prop_Bag, its gleam and both badges moved to x %s, hidden at start"
          % num(BAG_X))

    # --- something to tap ---
    collider = None
    for c in scene.components(bag_go):
        if scene.block[c].startswith("!u!61 "):
            collider = c
    if collider is None:
        collider = scene.free_fileid(int(bag_go) + 100)
        scene.set(bag_go, r"^(  m_Component:\n)",
                  r"\1  - component: {fileID: %s}\n" % collider,
                  "Prop_Bag component list")
        scene.insert_after(bag_go, COLLIDER % dict(
            fid=collider, go=bag_go,
            w=num(BAG_SPRITE[0]), h=num(BAG_SPRITE[1])))
        print("  Prop_Bag given a trigger collider to tap")
    else:
        print("  Prop_Bag already has a collider")

    # --- the sequence itself ---
    existing = [f for f, b in scene.block.items()
                if "m_EditorClassIdentifier: Assembly-CSharp::BagPickup" in b]
    if existing:
        bag_pickup = existing[0]
        print("  BagPickup already in the scene")
    else:
        go = scene.free_fileid(880000001)
        tr = scene.free_fileid(int(go) + 1)
        mb = scene.free_fileid(int(tr) + 1)
        fields = dict(go=go, tr=tr, mb=mb, script=guid, bag=bag_go,
                      collider=collider, pickup=pickup, player=player, gleam=gleam)
        scene.insert_after(bag_go, GAMEOBJECT % fields)
        scene.insert_after(go, TRANSFORM % fields)
        scene.insert_after(tr, BEHAVIOUR % fields)
        bag_pickup = mb

        roots = [f for f, b in scene.block.items() if b.startswith("!u!1660057539 ")][0]
        scene.set(roots, r"^(  m_Roots:\n)", r"\1  - {fileID: %s}\n" % tr,
                  "scene roots")
        print("  BagPickup object added and wired")

    # --- the words ---
    order = rebuild_objectives(scene, controller,
                               dict(pickup=bag_pickup, gleam=gleam))
    scene.set(controller, r"^  maxActionWait: .*$",
              "  maxActionWait: %d" % MAX_ACTION_WAIT, "maxActionWait")
    print("  words now: %s" % ", ".join(order))

    io.open(SCENE, "w", encoding="utf-8").write(scene.text())
    print("  wrote %s" % SCENE)


main()
