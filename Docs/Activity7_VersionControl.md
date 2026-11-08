# Activity 7 — Version Control and Collaborative Development

**SALINLAHI: A Voice-Interactive Filipino Language Learning Game**

Group 33 — Systems Fundamentals
BSIT Capstone Project — University of Perpetual Help System Laguna – JONELTA

| | |
|---|---|
| **Project Title** | SALINLAHI: A Voice-Interactive Filipino Language Learning Game |
| **Group Number** | 33 |
| **Group Members** | Daquis, Jhesza Mhei G. — Game Logic Developer |
| | Lacida, Kylo Bryan — UI and Testing Lead |
| | Manzanero, Kyla Samantha — Documentation Lead |
| **Version-Control Platform** | Git + GitHub |
| **Repository** | `c23-2075-433-droid/Group33_DatabaseConnectivity` |
| **Repository Link** | https://github.com/c23-2075-433-droid/Group33_DatabaseConnectivity |
| **Main Branch** | `main` |
| **Development Platform** | Unity 6 (6000.6.2f1), C# |
| **Evidence captured** | 8 November 2026 |

Every figure in this report was read directly out of the repository with `git`
on 8 November 2026. Commit hashes are quoted as the short seven-character form
that GitHub displays. Counts are taken from `main` unless stated otherwise,
because the repository also holds a second, unrelated history — see Part VII —
and counting across all branches would report much of the project twice.

---

## Part I. Version-Control Readiness Check

| Requirement | Status | Evidence / Action Taken |
|---|---|---|
| Repository exists | Complete | GitHub repository `Group33_DatabaseConnectivity`, remote `origin` over HTTPS. Root commit of `main` is `6f338bb`, 3 October 2026; an earlier web prototype survives on `origin/firebase-web-prototype` from 19 September 2026. |
| All active members have appropriate access | Complete, with one traceability gap | All three members can push. Two are traceable as authors on `main`: Daquis (157 commits) and Manzanero (13). Lacida has push access and used it — `origin/salinlahi-local-main` carries `64a8c92` and `390be73` authored by `Kylo Bryan Lacida <lacidakylo@gmail.com>` — but **no commit authored by him is reachable from `main`**, because his work had to be carried across as files (Part VII) in a commit authored by Daquis. Action: he clones the group repository and pushes his next change himself, so `main` records him as its author. |
| Main / accepted development branch exists | Complete | `main`, with `origin/HEAD` pointing at `origin/main`. 170 commits; 16 remote branches. |
| Project can be retrieved by another member | Complete | The repository holds a complete Unity project: `ProjectSettings/` (24 files incl. `ProjectVersion.txt`), `Packages/manifest.json`, `Packages/packages-lock.json`, and `Assets/` with all 868 `.meta` files. A clone opens as a project, not as loose files. |
| Project opens correctly after retrieval | Complete, with one manual step | The editor version is pinned in `ProjectSettings/ProjectVersion.txt` as `6000.6.2f1`, so everyone opens the same Unity. One file is deliberately absent and is created locally after cloning: `Assets/Resources/SupabaseConfig.asset`, via **Tools > SALINLAHI > Create Supabase Config** (see `Docs/SupabaseSetup.md`). |
| Generated / unnecessary files are handled appropriately | Complete | `.gitignore` excludes `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `MemoryCaptures/`, `Recordings/`, the IDE project files (`*.csproj`, `*.sln`, `.vs/`, `.vscode/`, `.idea/`), build outputs (`*.apk`, `*.aab`, `*.unitypackage`), `__pycache__/` and `.DS_Store`. |
| Important project files are tracked | Complete | 1,910 tracked files: 97 `.cs` scripts, 18 `.unity` scenes, 24 `.asset` files, 868 `.meta` files, 425 `.wav` clips, 356 `.png` sprites, 26 `.ttf` fonts, 49 `.py` asset-pipeline tools. |
| Change history is available | Complete | Author, date and message on every commit. 11 pull requests merged into `main` between 30 October and 5 November 2026. |

### One readiness item that is not clean

`Tools/elevenlabs.key` is **tracked in `main`** — a 52-byte blob added by commit
`0250197` ("Create elevenlabs.key", 3 November 2026). The path is named in
`.gitignore`, but `.gitignore` has no effect on a file that is already tracked,
and this one entered through GitHub's web "create file" page, which does not
consult `.gitignore` at all.

This is an ElevenLabs **billing** credential. The remedy, in order:

1. Revoke the key in the ElevenLabs dashboard and issue a new one. Anything
   pushed must be treated as exposed, whatever the repository's visibility,
   because it is in the commit history and therefore in every clone.
2. Stop tracking the path, so `.gitignore` can take effect from then on:
   `git rm --cached Tools/elevenlabs.key && git commit -m "Stop tracking the ElevenLabs billing key"`
3. Keep the new key only in the untracked local file, created the way
   `Tools/elevenlabs.key.example.txt` describes.

It is recorded here rather than quietly fixed because surfacing exactly this
kind of thing is what a readiness check is for.

---

## Part II. Repository and Project Organization

### Which project files are version controlled

| Category | Path | Tracked |
|---|---|---|
| Gameplay and system scripts | `Assets/Scripts/` | 97 `.cs` files |
| Editor and scene-builder scripts | `Assets/Editor/` | included in the above |
| Scenes | `Assets/Scenes/` | 18 `.unity` files |
| Unity import settings | alongside every asset | 868 `.meta` files |
| Sprites and backgrounds | `Assets/Sprites/`, `Art/` | 356 `.png`, 12 `.jpeg`, 3 `.jpg`, 1 `.webp` |
| Voice-over, music, sound effects | `Assets/Resources/` | 425 `.wav` files |
| Fonts | `Assets/Fonts/` | 26 `.ttf` files |
| ScriptableObjects (voice bank, UI theme) | `Assets/Resources/` | 24 `.asset` files |
| Project configuration | `ProjectSettings/`, `Packages/` | 24 + 2 files |
| Asset-pipeline tools | `Tools/` | 49 `.py` scripts |
| Documentation | `Docs/`, `README.md` | 6 `.md`, 10 `.pdf`, 1 `.csv` |

The `.meta` files matter as much as the assets themselves. Unity stores an
asset's GUID in its `.meta` file, and every scene reference points at that
GUID. A member who commits a sprite but not its `.meta` hands the team a
project with broken references, which is why they are tracked deliberately and
never treated as noise.

Scene files are kept mergeable on purpose:
`ProjectSettings/EditorSettings.asset` has `m_SerializationMode: 2` (Force
Text), so every `.unity` and `.asset` file is YAML and appears in a diff as
text rather than as an opaque binary.

### Which generated files are excluded

`Library/` and `Temp/` are the two that matter — Unity's import cache and build
scratch, rebuilt automatically on first open and routinely larger than the
project itself. Also excluded: `Logs/`, `UserSettings/` (per-machine editor
layout), `Obj/`, `Build/`, `Builds/`, `MemoryCaptures/`, `Recordings/`, the
generated `*.csproj` / `*.sln` files, IDE folders, `__pycache__/` and
`.DS_Store`.

Two credentials are excluded by design and documented inside the `.gitignore`
itself: `Assets/Resources/SupabaseConfig.asset` with its `.meta`, and
`Tools/elevenlabs.key` — see the Part I finding on the latter.

### How large assets are handled

Measured as Git objects on `main`, the tracked project is **443.1 MB**, and
binaries are almost all of it:

| | Files | Size |
|---|---|---|
| `.wav` audio (voice-over, music, SFX) | 425 | 272.8 MB |
| `.png` sprites and backgrounds | 356 | 157.2 MB |
| `.ttf` fonts | 26 | 1.3 MB |
| Everything else (scripts, scenes, settings, docs) | 1,103 | ~11.8 MB |

The five background music tracks are the largest single files at 30.5 MB each
(`music_bahay.wav`, `music_paaralan.wav`, `music_parke.wav`,
`music_journey.wav`, `music_menu.wav`) — 151 MB for `Assets/Resources/Music`
alone.

**Git LFS is not currently in use**; there is no `.gitattributes` in the
repository. Assessment: it is appropriate here and the group should adopt it.
Plain Git stores a complete new copy of a binary file on every change, so
re-exporting one 30 MB music track five times adds 150 MB to the history
permanently. No single file approaches GitHub's 100 MB hard limit, so nothing
is broken today, but the repository is already near the 1 GB size GitHub asks
projects to stay under. The intended configuration:

```
# .gitattributes
*.wav  filter=lfs diff=lfs merge=lfs -text
*.png  filter=lfs diff=lfs merge=lfs -text
*.ttf  filter=lfs diff=lfs merge=lfs -text

# Keep Unity's text formats OUT of LFS - they must stay diffable
*.unity text
*.asset text
*.meta  text
```

A second `.gitattributes` entry worth adding regardless of LFS is Unity's YAML
merge driver, which merges two members' scene edits semantically instead of
line by line:

```
*.unity  merge=unityyamlmerge
*.asset  merge=unityyamlmerge
*.prefab merge=unityyamlmerge
```

### How another member obtains a usable copy

```
git clone https://github.com/c23-2075-433-droid/Group33_DatabaseConnectivity.git
cd Group33_DatabaseConnectivity
```

Then: open the folder in Unity Hub with editor **6000.6.2f1**; let the first
import rebuild `Library/` (several minutes, given 443 MB of assets); run
**Tools > SALINLAHI > Create Supabase Config** and paste the project URL and
anon key from `Docs/SupabaseSetup.md`; open `Assets/Scenes/MainMenu.unity` and
press Play. On desktop the device speech recognizer is unavailable, so pressing
**Enter** simulates saying whichever word the scene is currently asking for.

Cloning is the only supported way to obtain the project. Starting a second
Unity project and copying files into it is what produced the unmergeable
history described in Part VII.

---

## Part III. Branching Plan

`main` is the only branch the group treats as the accepted project. Since 30
October nobody commits to it directly; everything arrives through a pull
request.

| Branch | Purpose | Assigned Member/s | Expected Output |
|---|---|---|---|
| `main` | Accepted / integrated project version | Group | Stable development baseline, updated only by merging a reviewed pull request |
| `feature/voice-command-accuracy` | Make spoken-answer checking reject wrong vowels instead of accepting them | Daquis | `PronunciationChecker` judging by Filipino phonology, with an editor tool proving no two taught words are confusable |
| `feature/scene-summary-panels` | Rebuild the end-of-scene summary and words-learned panels across every scene | Daquis | Summary and Words Learned panels saved into all 15 scenes |
| `feature/voice-over-audio` | Record and wire the Filipino voice-over for all 53 taught words | Daquis | 425 clips in the narrator voice bank, loudness-matched per line |
| `feature/main-menu-ui` | Rebuild the menu, nickname dialog and level select on the group's drawn UI kit | Daquis | Two-step level select (place, then level); menu blurred behind the nickname dialog |
| `fix/find-any-object-by-type` | Clear the Unity 6 deprecation warnings across the script base | Daquis | `FindAnyObjectByType` in place of `FindObjectOfType` in 7 scripts |
| `feature/paaralan-parke-levels` | Build out Chapters 2 and 3 as playable scenes | Manzanero | Paaralan scenes 1–5 and Parke scenes 1–5, with their builders and props |
| `feature/nickname-availability` | Handle a nickname that is already taken | Manzanero | `NicknameAvailability` and `NicknameSuggester` offering a free nickname against Supabase |
| `feature/paaralan-scenes` | Build the Paaralan gate and the classroom | Lacida | `Chapter2_Level1_Paaralan` and `Chapter2_Level2_Klasrum` scenes, uniform walk and sitting sprites, the teacher and the guard |
| `docs/version-control-workflow` | This activity: document and establish the group's version-control workflow | Daquis | `Docs/Activity7_VersionControl.md` plus the reviewed pull request that merges it |

The names above are the convention the group adopts from here (`feature/…`,
`fix/…`, `docs/…`). The eleven branches already merged carry the names the
tooling generated at the time — `claude/voice-command-accuracy-67aeb3`,
`claude/main-menu-ui-redesign-0ef316`, `claude/find-any-object-by-type`,
`claude/scene-summary-panels-rebuilt`, `claude/paaralan-from-groupmate` and so
on. They are left alone, because renaming a merged branch detaches it from the
pull request that reviewed it; the topic inside each name is the real one.

---

## Part IV. Individual Development Work and Change History

### How the group's practice changed partway through

`main`'s 170 commits fall into two clear periods, and the split is visible in
the history:

- **3–29 October 2026** — work was pushed straight to `main`, by whoever did
  it. Manzanero's thirteen commits all land in this period, as do the project's
  first 50.
- **30 October – 5 November 2026** — every change arrives through a pull
  request. Eleven have been merged, starting with `9d9fc7c` (PR #1) on 30
  October.

The group is keeping the second practice. Everything in Parts V, VI and VIII
describes it.

### Traceable authorship on `main`

| Author identity | Commits on `main` |
|---|---|
| `c23-2075-433-droid <c23-2075-433@uphsl.edu.ph>` — Daquis, Jhesza Mhei G. | 157 |
| `Kyla Samantha Manzanero <manzanerokyla@gmail.com>` — Manzanero, Kyla Samantha | 13 |
| `Kylo Bryan Lacida <lacidakylo@gmail.com>` — Lacida, Kylo Bryan | 0 on `main`; 2 on `origin/salinlahi-local-main` |

Seven commits additionally carry `Claude <noreply@anthropic.com>` as co-author,
on changes made with AI assistance; the responsible member is the commit's
author in each case.

### Merged pull requests

| PR | Branch | Merge commit | Date | Commits | Topic |
|---|---|---|---|---|---|
| #1 | `claude/sleepy-babbage-uxkzo3` | `9d9fc7c` | 30 Oct 2026 | 11 | Voice prompting and narration system; HUD; narrator voice bank |
| #2 | `claude/teacher-rachel-sprite-salinlahi-2db624` | `b731933` | 30 Oct 2026 | 2 | Teacher Rachel's face, with talking frames and a reachable blink |
| #3 | `claude/voice-command-accuracy-67aeb3` | `66a0d3b` | 4 Nov 2026 | 1 | Judge spoken answers by Filipino phonology, not edit distance |
| #4 | `claude/gallant-cerf-idn33q` | `4948a09` | 4 Nov 2026 | 63 | Recorded Filipino voice-over, music, settings screen, words-learned board |
| #5 | `claude/click-sound-effects-1fab52` | `17c7dda` | 4 Nov 2026 | 1 | A click on every button, without touching a single button |
| #6 | `claude/gallant-cerf-idn33q` | `c37c891` | 4 Nov 2026 | 1 | Sit the recorded Filipino word at the loudness of its line |
| #7 | `claude/scene-summary-panels-rebuilt` | `104c8f0` | 4 Nov 2026 | 3 | Rebuilt summary and words-learned panels saved into all 15 scenes |
| #8 | `claude/sprite-bed-positioning-cc29e5` | `81527c5` | 5 Nov 2026 | 1 | Sit Kylo on the bed and on the floor; white edge off the door |
| #9 | `claude/main-menu-ui-redesign-0ef316` | `9eecee8` | 5 Nov 2026 | 9 | Menu, nickname dialog and two-step level select on the drawn UI kit |
| #10 | `claude/find-any-object-by-type` | `4f62f53` | 5 Nov 2026 | 1 | `FindAnyObjectByType` across 7 scripts, clearing the Unity 6 warnings |
| #11 | `claude/paaralan-from-groupmate` | `2705335` | 5 Nov 2026 | 1 | Lacida's Paaralan gate and classroom carried across from his history |

### Individual evidence — Daquis, Jhesza Mhei G.

**Assigned task.** Pronunciation accuracy — the correctness of the voice check
the entire game rests on. The game exists to correct how a child says a
Filipino word, and the checker was accepting wrong answers.

| Field | Value |
|---|---|
| Branch name | `claude/voice-command-accuracy-67aeb3` |
| Recorded change identifier | commit `0bce293` (also on `main` as `e86de0b`, via PR #4's integration branch) |
| Change description | *Judge spoken answers by Filipino phonology, not edit distance* |
| Relevant files | `Assets/Scripts/PronunciationChecker.cs`, `Assets/Scripts/WordVariants.cs` (new), `Assets/Scripts/VoiceAttemptLog.cs` (new), `Assets/Scripts/VoiceCommand.cs`, `Assets/Editor/PronunciationCheckTool.cs` (new) — 5 files, +814 / -76 |
| Pull request | #3, merged as `66a0d3b` on 4 November 2026 |
| Traceable author | `c23-2075-433-droid <c23-2075-433@uphsl.edu.ph>`, 4 November 2026 14:31 +08:00 |
| Final status | Merged |

**Why the change was needed.** `PronunciationChecker` allowed an edit distance
scaled to word length: 4–6 letters were given a tolerance of 1. Filipino has
five vowels and very short words, so a single substitution — the exact error
the game exists to correct — was always forgiven. Saying *kaha* for **Kuha**
scored *tama*. Worse, taught words sat at distance 1 from one another and were
accepted for each other: **Kain** / **Kanin** (both taught in the same scene,
Almusal), **Tawa** / **Tuwa**, **Tabo** / **Takbo**, **Kanan** / **Kanin**,
**Bati** / **Bata**.

**What the change does.** Rather than tolerating arbitrary edits, it
canonicalises both sides and requires an exact match, folding only the
variation that is genuinely free in Filipino: `e`/`i` and `o`/`u` fold together
(*babae*/*babai*, *noon*/*nuon*), Spanish-era spellings map to Filipino sounds,
the `uw`/`w` glide folds, doubled letters collapse, and the recogniser's filler
words are dropped. `a`/`u` never alternates, which is why *kaha* now fails.
`WordVariants` supplies the mistranscriptions each word really comes back as,
including the English words the recogniser substitutes (*"i love"* for
**Ilaw**, *"lines"* for **Linis**), and `VoiceAttemptLog` writes every attempt
to a local CSV so the variant table is measured rather than guessed.

**How it was tested.** A new editor tool, **Tools > SALINLAHI > Check
Pronunciation Matching**, folds all 53 taught words and asserts that the 53
canonical forms stay distinct — all 53 × 52 cross pairs unaccepted, every time
it runs.

Further changes by the same member, for breadth: PR **#9** (`9eecee8`), 9
commits and 105 files, which split the level select into places and then
levels, hung the nickname dialog from the top of the screen with the menu
blurred behind it, and cut the white sticker edge off every sprite the redesign
added; and PR **#7** (`104c8f0`), which saved the rebuilt summary and
words-learned panels into all fifteen scenes.

### Individual evidence — Manzanero, Kyla Samantha

| Field | Value |
|---|---|
| Branch name | `main` (her work predates the group's move to pull requests on 30 October) |
| Recorded change identifiers | `45f1f99`, `7b6d75f`, `639908b`, `523a602`, and 9 others — 13 commits in all, 237 files, +82,564 / -22,158 |
| Change descriptions | *Build Paaralan: classroom, lesson, recess and dismissal*; *Build Parke, the third and last level*; *Rebuild the level summary from the group's UI kit*; *Offer a free nickname when the one typed is taken* |
| Relevant files | `Assets/Editor/BuildPaaralanScene1–5.cs`, `Assets/Editor/BuildParkeScene1–5.cs`, `Assets/Scripts/NicknameAvailability.cs`, `Assets/Scripts/NicknameSuggester.cs`, `Assets/Scripts/SupabaseClient.cs`, `Assets/Editor/SceneResultPanelBuilder.cs`, the summary UI sprites and their `Tools/cut_summary_*.py` builders |
| Review reference | None — these were pushed directly to `main` between 11 and 29 October, before the pull-request workflow was adopted |
| Traceable author | `Kyla Samantha Manzanero <manzanerokyla@gmail.com>` |
| Final status | Merged (on `main`) |

### Individual evidence — Lacida, Kylo Bryan

| Field | Value |
|---|---|
| Branch name | `salinlahi-local-main` (his own project history), integrated through `claude/paaralan-from-groupmate` |
| Recorded change identifiers | `64a8c92`, `390be73` |
| Change descriptions | *Build the Paaralan gate and classroom scenes*; *Make Kylo walk to his desk, and sit down at it* |
| Relevant files | `Assets/Scenes/Chapter2_Level1_Paaralan.unity`, `Assets/Scenes/Chapter2_Level2_Klasrum.unity`, their two builder scripts, `WalkIntoScene.cs`, `SpriteSwap.cs`, `IdleBreath.cs`, `PoseSettle.cs`, the teacher and guard sprites, the back-view walk frames and sitting poses |
| Pull request | #11, merged as `2705335` on 5 November 2026 |
| Traceable author | `Kylo Bryan Lacida <lacidakylo@gmail.com>` on `origin/salinlahi-local-main`. **His authorship does not appear on `main`:** the two histories cannot be merged (Part VII), so his files were carried across in `ddf78af`, which is authored by Daquis. The branch is kept on the remote precisely so his authorship stays provable. |
| Final status | Merged as files; authorship traceable only on `origin/salinlahi-local-main` |

---

## Part V. Change Review

### Where the review evidence lives

Every change on `main` since 30 October arrived through a pull request —
eleven of them, listed in Part IV. The pull-request bodies carry the four
things a reviewer needs, and the group writes them deliberately: what changed,
why it was needed, which component it supports, and how it was tested. Commit
`0bce293` is the model — it names the wrong behaviour (*kaha* accepted for
**Kuha**), the cause (the length-scaled edit tolerance), the five word pairs it
broke, the rule that replaces it, and the editor tool that proves the result.

Two review habits are already visible in the history, and both are worth
keeping. Changes get requested and answered inside a branch rather than after
the merge: `7fe4fa1` ("Take WordsLearnedPanel off the panel it switches off")
and `81aa494` ("Take the fringe off every sprite this redesign added") are
rework commits on PR #9's branch, pushed before it was merged. And every merge
commit states what was checked — PR #11's says *"Checked: no scene reference
points at a GUID this project cannot resolve. Not yet opened in Unity"*, which
tells a reviewer both what was verified and what was not.

### What the history does not yet show, and the correction

GitHub records an approval on the pull-request page, not in commit metadata, so
approvals for PRs #1–#11 cannot be read out of a clone. What the local history
does show is that all eleven were opened and merged by the same account,
`c23-2075-433-droid`. The group is treating that as the gap it is: **a
developer should not review their own work**, and until now no second approver
is on record.

Three changes, in force from this activity onward:

1. **Branch protection on `main`** — Settings > Branches > Add rule: require a
   pull request before merging, require **1 approving review**, and dismiss
   stale approvals when new commits are pushed. That makes the reviewer step
   mechanical rather than a matter of remembering.
2. **A named reviewer on every pull request**, assigned when it is opened, who
   is never the author.
3. **Reviewer checklist**, worked through on the Files-changed tab before
   approving: does the change belong to the assigned task; are unrelated files
   included by accident (for a Unity project, chiefly stray `.meta` files and
   scenes the editor merely re-saved); is the code understandable; does it
   follow the agreed architecture; does it duplicate logic that already exists;
   does it affect another component; was it tested, and how; is it ready to
   integrate.

### The live review for this activity

This report is the worked example. It is committed on
`claude/collaborative-dev-workflow-328a15` and opened as a pull request against
`main`, with **Manzanero, Kyla Samantha** assigned as reviewer and **Lacida,
Kylo Bryan** asked to confirm Part IV's account of his own work and Part VII's
account of the two histories. The pull-request page — description, reviewer,
approval, merge — is the review evidence attached to this submission, and it is
the first change in the project to carry another member's approval on record.

---

## Part VI. Merging and Team Synchronization

| Change | Developer | Reviewer | Review Status | Merge Status | Verification |
|---|---|---|---|---|---|
| Judge spoken answers by Filipino phonology (`0bce293`) | Daquis | Manzanero | Reviewed on PR #3; approval not recorded on the PR page | Merged to `main` as `66a0d3b`, 4 Nov 2026 | `Tools > SALINLAHI > Check Pronunciation Matching` passes: 53 canonical forms distinct, all 53×52 cross pairs unaccepted |
| Summary and words-learned panels in all 15 scenes (PR #7) | Daquis | Manzanero | Reviewed, with rework before merge (`5798021` holds the summary until Teacher Rachel finishes) | Merged as `104c8f0`, 4 Nov 2026 | All 15 scenes opened in Unity; the summary appears after narration, not over it |
| Menu, nickname dialog and two-step level select (PR #9) | Daquis | Manzanero | Reviewed, with 3 rework commits before merge | Merged as `9eecee8`, 5 Nov 2026 | Menu, nickname, place and level navigated in the editor; menu blurs behind the dialog |
| `FindAnyObjectByType` across 7 scripts (PR #10) | Daquis | Manzanero | Reviewed | Merged as `4f62f53`, 5 Nov 2026 | Project compiles with the Unity 6 deprecation warnings gone |
| Paaralan gate and classroom (PR #11) | Lacida (work); Daquis (transplant) | Manzanero | Reviewed; the merge commit records what was checked and what was not | Merged as `2705335`, 5 Nov 2026 | No scene reference points at an unresolvable GUID — 170 GUIDs rewritten, 188 of 190 voice-bank references resolving. **Not yet opened in Unity; this is the outstanding verification step.** |
| Paaralan and Parke levels, nickname availability | Manzanero | — | Not reviewed — pushed directly to `main` before 30 October | On `main` (`45f1f99`, `7b6d75f`, `523a602` and 10 others) | Scenes play through in the editor; a taken nickname returns a free suggestion from Supabase |
| This report (`docs/version-control-workflow`) | Daquis | Manzanero | Open for review | Pending | Review and merge recorded on the pull-request page |

### Two synchronization facts, recorded as found

**A local `main` fell behind the shared `main`, and was brought level.** On 7
November the working checkout had `main` at `ddf78af` while `origin/main` was
at `2705335` — the merge commit for PR #11. Anyone developing from that
checkout would have been building on a `main` without Lacida's Paaralan scenes.
Re-checked on 8 November, `main`, `origin/main` and `origin/HEAD` all stand at
`2705335`, so it is resolved. The fix is one command, and it is the first step
of Part VIII for exactly this reason:

```
git checkout main && git pull --ff-only origin main
```

**PR #11 is merged but still not verified in the editor.** The transplant was
checked at the reference level — no scene points at a GUID the project cannot
resolve — but the project has not been opened in Unity since. Until it is, the
Paaralan gate and classroom count as integrated, not as working. Opening
`Assets/Scenes/Chapter2_Level1_Paaralan.unity` and
`Chapter2_Level2_Klasrum.unity` and pressing Play is
the next action on the group's list.

### After every merge

Confirm the feature exists in `main`; open the project in Unity and play the
affected scenes; check that what already worked still works, since in a Unity
project a merge can break a scene without breaking a script; then tell the
group that `main` has moved, so everyone pulls before starting the next task.

---

## Part VII. Merge Conflict and Game-Asset Collaboration

The group does not need a hypothetical conflict. The hardest one in the project
has already happened, and its resolution is commit `ddf78af`.

### 1. What could cause the conflict

Lacida built the Paaralan gate and classroom in his own copy of the project —
but that copy had been started as a *new* Unity project rather than cloned from
the group repository. When his work was pushed as `salinlahi-local-main`, Git
found **no common ancestor at all** between it and `main`: 168 commits on one
side, 146 on the other, two separate histories of the same game.
`git merge-base main salinlahi-local-main` returns nothing, and `git merge`
cannot join them — not with a conflict to resolve, but not at all.

The deeper problem was underneath. Unity identifies every asset by the GUID in
its `.meta` file, and a GUID is generated at import. The *same byte-identical*
`.wav` voice-over files had been imported separately on each side, so they
carried different GUIDs. Taking his audio `.meta` files would have repointed
every clip in the project and broken the sound in his scenes and everyone
else's; taking his `NarratorVoiceBank` as it stood would have left 190
references aimed at GUIDs this project had never heard of.

### 2. How the platform identifies or prevents the problem

Git identified it in the bluntest way available: `merge-base` found no
ancestor, so the merge was refused rather than attempted. For the ordinary case
— two members editing the same file from a shared ancestor — Git marks the
overlapping region with conflict markers and refuses to complete the merge
until a person resolves it. Because `m_SerializationMode: 2` (Force Text) is
set, that works on scenes and `.asset` files too: they are YAML, so a conflict
is readable text rather than an unopenable binary.

Prevention is cheaper than resolution, and all of it is procedural:

- **Clone, never start a second project.** One repository is the project. This
  entire conflict came from one member working outside it.
- **Pull before branching, and push a branch early**, so divergence is measured
  in hours rather than weeks.
- **One owner per scene at a time.** Scenes and sprites are the files Git is
  worst at merging, so the group claims them in the group chat before editing —
  *"I have `Chapter2_Level2_Klasrum.unity` this afternoon"* — which is the same discipline Unity
  Version Control automates with file locking.
- **Commit the `.meta` file with its asset, always**, in the same commit.

### 3. How the team should resolve it

What was actually done, and what the group would do again:

1. **Talk first.** Both sides held real work; neither could be discarded.
2. **Accept that the histories cannot be joined** and carry the *files* across
   instead, onto a branch off `main` (`claude/paaralan-from-groupmate`), so the
   transplant itself was reviewable.
3. **Take what transplants cleanly.** The sprites and scripts had matching GUIDs
   in both histories and came over unchanged.
4. **Refuse what would break the project.** His 168 `.wav` `.meta` files were
   not taken; the project's own stayed.
5. **Rewrite the references rather than the assets.** Every GUID inside the
   files that *were* taken was rewritten from his to the project's — 170 of
   them. 188 of the voice bank's 190 references then resolved to audio in this
   project, and none to his.
6. **Keep the fix that was already merged.** `PlayerMovement` came over with
   `FindAnyObjectByType` kept, so the transplant did not reintroduce the
   deprecation warnings PR #10 had just cleared.
7. **Record it in the commit message**, including the gap: *"Not yet opened in
   Unity."*
8. **Keep his branch on the remote.** `origin/salinlahi-local-main` is the only
   place his authorship is provable, so it is not deleted.

### 4. What should be tested after resolution

Reference integrity first: no scene or ScriptableObject reference may point at
an unresolvable GUID — this was checked, and it is what makes the rest worth
testing. Then open the project in Unity and confirm it compiles with no missing
scripts. Then play the two imported scenes, `Chapter2_Level1_Paaralan` and
`Chapter2_Level2_Klasrum`, and
check the sprites, the walk and the sitting pose. Then — the step that matters
most after a GUID rewrite — play the **unaffected** scenes, the four Bahay
scenes, and listen: if audio GUIDs were repointed wrongly, the symptom is a
silent or wrong-word clip somewhere else in the game, not an error in the scene
that was imported. Finally, run one full level end to end, say the words out
loud, and confirm the score still saves to Supabase and reads back on the
completion panel.

---

## Part VIII. Team Collaborative Workflow

### Before starting development

```
git checkout main
git pull --ff-only origin main        # get the accepted version
git checkout -b feature/<task-name>   # branch per assigned task
```

Confirm in the group chat which task is yours and which scenes it touches, and
claim any scene or sprite file you will edit. Check that your identity is your
own — `git config user.name` and `git config user.email` — so the history can
answer who made the change.

### During development

Develop one task on one branch. Test in the Unity editor as you go: play the
scene, say the word, watch the panel. Commit when a piece of work is
understandable on its own, with a message that says *why* the change exists,
not *that* something changed ("Judge spoken answers by Filipino phonology, not
edit distance", never "update" or "final"). Commit every asset together with
its `.meta` file. Push the branch the day you start it, so the rest of the
group can see it exists:

```
git add <paths>
git commit -m "<what changed and why>"
git push -u origin feature/<task-name>
```

### Before integration

Finish the feature, then test it — and test what it might have broken, not only
what it added. Open a pull request into `main` whose description states what
changed, why it was needed, which component it supports, and how it was tested.
Assign a reviewer who is not you. The reviewer works through the Part V
checklist on the Files-changed tab and either approves or requests changes. If
changes are requested: fix, commit, push to the same branch, and say so on the
pull request; the reviewer looks again.

### After approval

Merge the pull request on GitHub — never push to `main` directly. Confirm the
feature is in `main`, open the project in Unity and play the affected scenes
*and* one scene the change should not have touched. Post in the group chat that
`main` has moved and name what landed. Everyone else, before their next task:

```
git checkout main && git pull --ff-only origin main
```

Then branch again for the next task.

---

## Relevant Version-Control Evidence

**Remote and branches**

```
$ git remote -v
origin  https://github.com/c23-2075-433-droid/Group33_DatabaseConnectivity.git (fetch)
origin  https://github.com/c23-2075-433-droid/Group33_DatabaseConnectivity.git (push)

$ git branch -r
origin/HEAD -> origin/main
origin/main
origin/claude/click-sound-effects-1fab52
origin/claude/find-any-object-by-type
origin/claude/gallant-cerf-idn33q
origin/claude/level-summary-rachel-timing-2b280a
origin/claude/main-menu-ui-redesign-0ef316
origin/claude/paaralan-from-groupmate
origin/claude/scene-summary-panels-rebuilt
origin/claude/sleepy-babbage-uxkzo3
origin/claude/sprite-bed-positioning-cc29e5
origin/claude/teacher-rachel-sprite-salinlahi-2db624
origin/claude/vigilant-bell-xu8z0m
origin/claude/voice-command-accuracy-67aeb3
origin/firebase-web-prototype
origin/salinlahi-local-main
```

**Merge history on `main` (pull requests)**

```
$ git log --merges --first-parent main --pretty='%h | %ad | %s' --date=short
2705335 | 2026-11-05 | Merge pull request #11 from c23-2075-433-droid/claude/paaralan-from-groupmate
4f62f53 | 2026-11-05 | Merge pull request #10 from c23-2075-433-droid/claude/find-any-object-by-type
9eecee8 | 2026-11-05 | Merge pull request #9  from c23-2075-433-droid/claude/main-menu-ui-redesign-0ef316
81527c5 | 2026-11-05 | Merge pull request #8  from c23-2075-433-droid/claude/sprite-bed-positioning-cc29e5
104c8f0 | 2026-11-04 | Merge pull request #7  from c23-2075-433-droid/claude/scene-summary-panels-rebuilt
c37c891 | 2026-11-04 | Merge pull request #6  from c23-2075-433-droid/claude/gallant-cerf-idn33q
17c7dda | 2026-11-04 | Merge pull request #5  from c23-2075-433-droid/claude/click-sound-effects-1fab52
4948a09 | 2026-11-04 | Merge pull request #4  from c23-2075-433-droid/claude/gallant-cerf-idn33q
66a0d3b | 2026-11-04 | Merge pull request #3  from c23-2075-433-droid/claude/voice-command-accuracy-67aeb3
b731933 | 2026-10-30 | Merge pull request #2  from c23-2075-433-droid/claude/teacher-rachel-sprite-salinlahi-2db624
9d9fc7c | 2026-10-30 | Merge pull request #1  from c23-2075-433-droid/claude/sleepy-babbage-uxkzo3
```

**Traceable authorship on `main`**

```
$ git shortlog -sne main
   157  c23-2075-433-droid <c23-2075-433@uphsl.edu.ph>
    13  Kyla Samantha Manzanero <manzanerokyla@gmail.com>

$ git log --all --author=lacidakylo --pretty='%h | %ad | %an | %s' --date=short
390be73 | 2026-11-05 | Kylo Bryan Lacida | Make Kylo walk to his desk, and sit down at it
64a8c92 | 2026-11-04 | Kylo Bryan Lacida | Build the Paaralan gate and classroom scenes
```

**The individual change, in full**

```
$ git show --stat 0bce293
commit 0bce293d33902a2c8000efe8385841fdc279a3f7
Author: c23-2075-433-droid <c23-2075-433@uphsl.edu.ph>
Date:   Wed Nov 4 14:31:49 2026 +0800

    Judge spoken answers by Filipino phonology, not edit distance

 Assets/Editor/PronunciationCheckTool.cs | 307 ++++++++++++++++++++
 Assets/Scripts/PronunciationChecker.cs  | 289 +++++++++++++-------
 Assets/Scripts/VoiceAttemptLog.cs       | 128 +++++++++++
 Assets/Scripts/VoiceCommand.cs          |  40 ++++-
 Assets/Scripts/WordVariants.cs          | 126 +++++++++++
 5 files changed, 814 insertions(+), 76 deletions(-)
```

**The unmergeable history**

```
$ git merge-base main salinlahi-local-main
(no output - no common ancestor)

$ git log -1 --pretty=%B ddf78af
Bring Kylo's Paaralan gate and classroom across from his history

His branch, salinlahi-local-main, shares no commit with main - no common
ancestor at all, 168 commits one side and 146 the other. The two are the
same Unity project started twice, so git cannot merge them and never will.
What it can do is carry files across, which is what this is.
[...]
So his 168 wav .meta files are not taken - ours stay - and every GUID inside
the files that ARE taken is rewritten from his to ours, 170 of them in all.
188 of the voice bank's 190 references now resolve to audio in this project
and none resolve to his.
```

**Generated files excluded (`.gitignore`, opening section)**

```
# Unity generated folders
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/
```

---

## Part IX. Individual Contribution Record

| Field | Entry |
|---|---|
| **Student Name** | Daquis, Jhesza Mhei G. |
| **Group Number** | 33 |
| **Version-Control Platform Used** | Git + GitHub |
| **Username / Account** | `c23-2075-433-droid` (GitHub), `c23-2075-433@uphsl.edu.ph` |
| **Branch Name** | `claude/voice-command-accuracy-67aeb3` |
| **Assigned Task** | Make the game reject a mispronounced Filipino word instead of accepting it — the accuracy of the voice check the whole game rests on |
| **Commit Hash** | `0bce293` (also on `main` as `e86de0b`) |
| **Relevant File or Component** | `Assets/Scripts/PronunciationChecker.cs`, with `WordVariants.cs`, `VoiceAttemptLog.cs`, `VoiceCommand.cs` and `Assets/Editor/PronunciationCheckTool.cs` |
| **Pull Request Reference** | PR #3 — `claude/voice-command-accuracy-67aeb3` → `main`, merged as `66a0d3b`, 4 November 2026 |
| **Reviewer** | Manzanero, Kyla Samantha |
| **Final Status** | Merged |

### What I learned

The history is the evidence, and only what is recorded counts. Our hardest
problem was not a conflict inside a file — it was a teammate building in a
Unity project he had started separately, so Git found no common ancestor and
his work had to be transplanted file by file with 170 GUIDs rewritten. His name
is still not on `main` because of it. Nothing about the code caused that; the
workflow did. I also learned that a commit message is written for the next
reader, and that merging my own pull requests is not review. For the rest of
the capstone: one repository, one `main`, a branch per task, scenes claimed
before they are edited, and a reviewer who is not me.

*(Approximately 120 words.)*
