#!/usr/bin/env python3
"""Check Docs/VoiceOverScript.csv against the words the game actually asks for.

The recording script and the fifteen built scenes drifted apart once already -
the CSV still describes a "Leaving for School" scene that was never built, and
called Kanin and Itlog tap targets long after they became spoken objectives.
This reads the word list out of the scenes themselves, so the next drift fails
here instead of being found by a child.

Run it from anywhere:

    python3 -I Tools/check_voiceover_script.py

Exits 0 when the script is complete and consistent, 1 otherwise, listing every
problem rather than stopping at the first.

The CSV is parsed with the same minimal RFC-4180 rules as
Assets/Editor/NarratorVoiceBankTool.cs, so a row that reads correctly here
reads correctly in Unity.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV = os.path.join(ROOT, "Docs", "VoiceOverScript.csv")
EDITOR = os.path.join(ROOT, "Assets", "Editor")
SCENES = os.path.join(ROOT, "Assets", "Scenes")

# The one scene with no builder: it is kept by hand and patched in place by
# MigrateLevel1ObjectiveSystem.cs, so its words are read from the scene file.
HAND_KEPT_SCENE = "Chapter1_Level1_UmagaNa"

# Rachel's lines are read aloud to a five-year-old and shown in a panel that
# shrinks text to 24pt and then truncates it (GameplayHUDLayout), so a long
# line silently loses its tail. One or two short sentences, no more.
MAX_TEXT = 90

# Wording the narration brief rules out: it is what made the old prompts feel
# like a drill rather than a story.
BANNED = [
    (re.compile(r"\bsay the word\b", re.I), 'say "Say the word..."'),
    (re.compile(r"\bnow say\b", re.I), 'say "Now say..."'),
    (re.compile(r"\bcan you say\b", re.I), 'say "Can you say..."'),
    (re.compile(r"\brepeat after me\b", re.I), 'say "Repeat after me..."'),
]

# Kylo is a boy. "her" also catches "her" as a possessive, which is the form
# the original script got wrong in every row. Teacher Rachel and Nanay are
# women, so a line that names one of them is allowed its feminine pronouns -
# the mistake being hunted is a feminine pronoun standing in for Kylo.
FEMININE = re.compile(r"\b(she|her|hers|herself)\b", re.I)
WOMAN_NAMED = re.compile(r"\b(Rachel|Nanay|Teacher)\b")


# --------------------------------------------------------------------------
# The CSV


def parse_csv(text):
    """Minimal RFC-4180 reader: quoted fields, "" escapes, newlines in quotes."""
    rows, fields, field, in_quotes = [], [], [], False
    i = 0
    while i < len(text):
        c = text[i]
        if in_quotes:
            if c != '"':
                field.append(c)
            elif i + 1 < len(text) and text[i + 1] == '"':
                field.append('"')
                i += 1
            else:
                in_quotes = False
        elif c == '"':
            in_quotes = True
        elif c == ",":
            fields.append("".join(field))
            field = []
        elif c in "\r\n":
            if c == "\r" and i + 1 < len(text) and text[i + 1] == "\n":
                i += 1
            fields.append("".join(field))
            field = []
            rows.append(fields)
            fields = []
        else:
            field.append(c)
        i += 1
    if field or fields:
        fields.append("".join(field))
        rows.append(fields)
    return rows


def read_script():
    """Rows as (line_number, id, text), header and blank lines dropped."""
    with open(CSV, encoding="utf-8") as handle:
        rows = parse_csv(handle.read())

    script = []
    for number, row in enumerate(rows, start=1):
        if len(row) < 6:
            continue
        name = row[0].strip()
        if not name or name == "File Name":
            continue
        line_id = os.path.splitext(name)[0]
        script.append((number, line_id, row[5].strip()))
    return script


# --------------------------------------------------------------------------
# What the game actually asks for


def scene_token(scene_name):
    """Chapter1_Level5_Almusal -> l1_s5, matching NarratorScript.SceneLineId.

    The game's Chapter is the recording script's Level and the game's Level is
    its Scene, which is why the two numbers swap names here.
    """
    match = re.match(r"Chapter(\d+)_Level(\d+)_", scene_name)
    if not match:
        return None
    return "l%s_s%s" % (match.group(1), match.group(2))


def words_from_builders():
    """{scene name: [words, in the order they are asked]} for the 14 built scenes."""
    scenes = {}
    for entry in sorted(os.listdir(EDITOR)):
        if not re.match(r"Build(Bahay|Paaralan|Parke)Scene\d+\.cs$", entry):
            continue
        with open(os.path.join(EDITOR, entry), encoding="utf-8") as handle:
            source = handle.read()

        path = re.search(r'ScenePath\s*=\s*"Assets/Scenes/([^"]+)\.unity"', source)
        array = re.search(r"controller\.objectives\s*=\s*new\[\]\s*\{([^}]*)\}", source)
        if not path or not array:
            print("  ! %s: no ScenePath or objectives array found" % entry)
            continue

        # var lakad = MakeObjective("Lakad", pLakad);
        by_variable = dict(
            re.findall(r"var\s+(\w+)\s*=\s*MakeObjective\(\s*\"([^\"]+)\"", source)
        )
        order = [name.strip() for name in array.group(1).split(",") if name.strip()]
        scenes[path.group(1)] = [by_variable[name] for name in order if name in by_variable]
    return scenes


def words_from_scene_file(scene_name):
    """The hand-kept scene's words, read straight out of its YAML."""
    path = os.path.join(SCENES, scene_name + ".unity")
    with open(path, encoding="utf-8") as handle:
        body = handle.read()

    block = re.search(r"\n  objectives:\n(.*?)(?=\n  [a-zA-Z])", body, re.S)
    if not block:
        return []
    return re.findall(r"^\s*-\s*word:\s*(\S+)", block.group(1), re.M)


def objective_table():
    scenes = words_from_builders()
    scenes[HAND_KEPT_SCENE] = words_from_scene_file(HAND_KEPT_SCENE)
    return scenes


# --------------------------------------------------------------------------


def main():
    if not os.path.exists(CSV):
        print("Docs/VoiceOverScript.csv not found")
        return 1

    scenes = objective_table()
    script = read_script()
    text_by_id = {line_id: text for _, line_id, text in script}

    slots = sum(len(words) for words in scenes.values())
    vocabulary = sorted({w.lower() for words in scenes.values() for w in words})

    print(
        "%d scenes, %d objective slots, %d distinct words, %d script rows"
        % (len(scenes), slots, len(vocabulary), len(script))
    )

    problems = []

    # 1. Every line id appears once.
    seen = {}
    for number, line_id, _ in script:
        if line_id in seen:
            problems.append(
                "line %d: duplicate File Name '%s' (first seen on line %d)"
                % (number, line_id, seen[line_id])
            )
        seen[line_id] = number

    # 2. Every objective slot is narrated, and every word is taught.
    for scene_name in sorted(scenes):
        token = scene_token(scene_name)
        if token is None:
            problems.append("%s: not a Chapter<N>_Level<M>_ scene name" % scene_name)
            continue
        for word in scenes[scene_name]:
            for beat in ("story", "ask", "done"):
                want = "rachel_%s_%s_%s" % (token, word.lower(), beat)
                if want not in text_by_id:
                    problems.append(
                        "%s needs a %s line for '%s': %s" % (scene_name, beat, word, want)
                    )
        for beat in ("intro", "outro"):
            want = "rachel_%s_%s" % (token, beat)
            if want not in text_by_id:
                problems.append("%s has no scene %s: %s" % (scene_name, beat, want))

    for word in vocabulary:
        for beat in ("teach", "recall"):
            want = "rachel_word_%s_%s" % (word, beat)
            if want not in text_by_id:
                problems.append("'%s' has no %s line: %s" % (word, beat, want))
        if "word_%s" % word not in text_by_id:
            problems.append("'%s' has no word recording row: word_%s" % (word, word))

    # 3. {word} belongs to the say-it lines and nowhere else - VoiceNarrator
    #    splits on it there and prints it literally anywhere else.
    for number, line_id, text in script:
        count = text.count("{word}")
        if line_id.endswith("_ask"):
            if count != 1:
                problems.append(
                    "line %d: %s must hold exactly one {word}, found %d"
                    % (number, line_id, count)
                )
        elif count:
            problems.append("line %d: %s must not hold {word}" % (number, line_id))

    # 4. Length, pronouns and banned phrasing.
    for number, line_id, text in script:
        if len(text) > MAX_TEXT:
            problems.append(
                "line %d: %s is %d characters, over the %d limit"
                % (number, line_id, len(text), MAX_TEXT)
            )
        if FEMININE.search(text) and not WOMAN_NAMED.search(text):
            problems.append(
                "line %d: %s uses a feminine pronoun with no woman named - Kylo is a boy"
                % (number, line_id)
            )
        for pattern, description in BANNED:
            if pattern.search(text):
                problems.append("line %d: %s should not %s" % (number, line_id, description))

    if problems:
        print("\n%d problem(s):" % len(problems))
        for problem in problems:
            print("  - " + problem)
        return 1

    print("OK - every objective is narrated and every word is taught.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
