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
  the Bag word   onShown brings the bag out, onCorrect arms the pickup.
                 It used to take the bag outright
  the Kunin word REMOVED - see below
  maxActionWait  4 -> 12 seconds

WHY KUNIN GOES. The saved scene taught five words, and the last two were
"Bag" (which only gleamed) and "Kunin" (which took the bag). The new flow
puts the taking in the player's hands instead, between those two: the bag is
already gone by the time Kunin would be asked, so the word would be left
pointing at an empty floor. The builder dropped Kunin for its own reasons
some time ago, so this brings the saved scene the same way. If it should
come back, the honest shape is for KUNIN rather than Bag to arm the pickup -
that is a one-line change here and in the builder, not a rewrite.

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

MAX_ACTION_WAIT = 12            # longer than BagPickup's wait for a tap
DROP_WORDS = ("Kunin",)

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


def rebuild_objectives(scene, controller, bag_pickup):
    """Rewrite the objective list: drop Kunin, hand the bag to BagPickup."""
    block = scene.block[controller]
    start = block.index("  objectives:\n")
    end = block.index("  onAllObjectivesComplete:")
    body = block[start + len("  objectives:\n"):end]

    entries = re.split(r"^(?=  - word: )", body, flags=re.M)
    entries = [e for e in entries if e.strip()]

    kept, dropped = [], []
    for e in entries:
        word = re.search(r"^  - word: (.*)$", e, re.M).group(1).strip()
        if word in DROP_WORDS:
            dropped.append(word)
            continue

        # Every objective gains an onShown; only the bag puts anything in it.
        shown = [dict(target=bag_pickup, type="BagPickup", method="Appear")] \
            if word == "Bag" else []

        if word == "Bag":
            correct = [dict(target=bag_pickup, type="BagPickup", method="Arm")]
            corr_text = event("onCorrect", correct)
        else:
            # Left exactly as it was - these words are not this change's business.
            corr_text = e[e.index("    onCorrect:"):]

        # head is everything before onCorrect, which is where an onShown
        # from a previous run sits. Cut it there and write a fresh one, or
        # running this twice stacks two onShown blocks on the same objective
        # and the bag appears twice.
        head = e[:e.index("    onCorrect:")]
        if "    onShown:" in head:
            head = head[:head.index("    onShown:")]
        kept.append(head + event("onShown", shown) + corr_text)

    scene.block[controller] = (block[:start] + "  objectives:\n"
                               + "".join(kept) + block[end:])
    return [re.search(r"^  - word: (.*)$", k, re.M).group(1).strip() for k in kept], dropped


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

    prompt_go = scene.by_name("Prompt_Bag")
    prompt_rect = scene.component(prompt_go, "!u!224 ")

    canvas_go = scene.by_name("UI_WordPrompts_Canvas")
    controller = component_of(scene, canvas_go, "Assembly-CSharp::SceneObjectiveController")

    # --- the bag, and what follows it ---
    for fid, label in ((bag_tr, "Prop_Bag"), (gleam_tr, "Gleam_Bag")):
        scene.set(fid, r"^  m_LocalPosition: \{.*\}$",
                  "  m_LocalPosition: {x: %s, y: %s, z: 0}" % (num(BAG_X), num(BAG_Y)),
                  label + " position")
    scene.set(prompt_rect, r"^  m_AnchoredPosition: \{.*\}$",
              "  m_AnchoredPosition: {x: %s, y: %s}"
              % (num(BAG_X * 100.0), num((FLOOR_Y + BAG_H) * 100.0 + PROMPT_LIFT)),
              "Prompt_Bag position")
    # Off in the saved scene as well, not only once Awake has run: the bag
    # belongs to the last word, and a backpack standing in the room through
    # the first three is a thing to say "Bag" at before being asked.
    scene.set(bag_go, r"^  m_IsActive: \d$", "  m_IsActive: 0", "Prop_Bag hidden")
    print("  Prop_Bag, its gleam and its badge moved to x %s, and hidden at start"
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
    kept, dropped = rebuild_objectives(scene, controller, bag_pickup)
    scene.set(controller, r"^  maxActionWait: .*$",
              "  maxActionWait: %d" % MAX_ACTION_WAIT, "maxActionWait")
    print("  words now: %s%s" % (", ".join(kept),
                                 "   (dropped %s)" % ", ".join(dropped) if dropped else ""))

    io.open(SCENE, "w", encoding="utf-8").write(scene.text())
    print("  wrote %s" % SCENE)


main()
