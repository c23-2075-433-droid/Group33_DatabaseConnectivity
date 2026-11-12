# SALINLAHI — Release Process

Group 33 · BSIT Capstone · University of Perpetual Help System Laguna – JONELTA

This is the procedure the group follows for every release from v0.1.0 onward.
It exists because the three APKs in the repository root —
`SALINLAHI_VoiceTest.apk`, `_v2.apk`, `_v3.apk` — cannot be tied to a commit,
a feature set, or a test result. Nobody can now say what is in them or what
source produced them.

## The rule

A release is never built from a personal working copy. It is built from the
integrated `main` branch, after review, with a clean working tree.

If `git status` is not empty, the build is not a release. `BUILD_INFO.txt`
records this, and says so in plain words when the tree was dirty.

## The loop

```
Develop on a branch
      ↓
Pull request → review → merge into main
      ↓
Decide the new version number  (see "Numbering")
      ↓
Update Player Settings > Version, and Android versionCode
      ↓
Write Docs/release/v<version>/README.txt and RELEASE_NOTES.txt
      ↓
Build            Tools > SALINLAHI > Build Release APK
      ↓
Test the packaged APK on a device that is not the developer's editor
      ↓
Record the test results; a failure goes into Known Issues or blocks the release
      ↓
Tag the source   git tag -a v<version>
      ↓
Publish          GitHub release, with the zipped package attached
```

### 1–2. Integrate first

Work lands on `main` through a pull request, as established in Activity 7.
Nothing is released from a branch.

### 3. Decide the version number

Decide it from what changed, using the table under **Numbering** below, and
decide it *before* building — not by looking at the artifact afterwards. Write
the number down in the pull request that closes the milestone, so the whole
group agrees on it before anyone builds.

### 4. Update the version

Edit **Edit > Project Settings > Player > Version** — one field, which is the
single source of truth. `Application.version` reads it, so the About screen,
the APK filename and `BUILD_INFO.txt` all follow from it automatically. Do not
hardcode a version anywhere else.

Also raise **Player > Android > Bundle Version Code** by one. Android refuses
to install an APK whose version code is lower than the one already on the
device.

### 5. Write the release notes before building

`ReleaseBuild` refuses to start unless `Docs/release/v<version>/README.txt`
and `RELEASE_NOTES.txt` both exist. This is deliberate: notes written after
the fact get written from memory, and a package handed over without
instructions is not a release.

Copy the previous version's two files as a starting point and edit them.

### 6. Build

```
Tools > SALINLAHI > Build Release APK
```

or, without opening the editor — note that the Unity editor must be **closed**,
since batchmode cannot take the project lock:

```bash
/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath ~/Documents/SALINLAHI \
  -buildTarget Android \
  -executeMethod ReleaseBuild.BuildAndroid \
  -logFile ~/salinlahi-build.log
```

The result is `Build/Release/SALINLAHI-v<version>/`, holding the APK,
`README.txt`, `RELEASE_NOTES.txt` and `BUILD_INFO.txt`. Zip that folder as
`SALINLAHI-v<version>.zip`.

Build from the **main checkout**, never from a worktree:
`Assets/Resources/SupabaseConfig.asset` holds the database keys, is gitignored,
and exists only there. A build made without it has a dead leaderboard, and
opening `MainMenu.unity` without it writes the reference away as null.

Android only. Voice is the mechanic, the speech recognizer is Android/iOS
only, and a desktop build cannot be answered by speaking.

### 7. Test the package, not the editor

Pressing Play in Unity is not release testing. Install the APK on a phone and
run at least:

| Check | Expected |
|---|---|
| Application launches | Main menu appears |
| Version is identifiable | SETTINGS > ABOUT reads the version just built |
| Main feature works | A spoken Filipino word is accepted and acts |
| Wrong answers rejected | A wrong vowel is not accepted |
| Required assets load | Backgrounds, characters, music, narration all present |
| Save/load works | A profile survives closing and reopening the app |
| Leaderboard works | A finished scene's score appears on the board |
| Application closes normally | No hang, no crash on exit |

Whoever tests it should not be the person who built it, where that is
possible. Record the actual result of every row, including failures. A failed
row either goes into Known Issues or stops the release — it is never left
blank.

### 8. Tag the source

```bash
git tag -a v0.1.0 -m "SALINLAHI v0.1.0 — first controlled release"
git push origin main --tags
```

The tag is what makes the release reproducible. Given a tag, the group can
check out the exact source that produced a particular APK:

```bash
git checkout v0.1.0
```

`BUILD_INFO.txt` inside the package carries the same commit hash, so the
artifact points back at the source even when it has been separated from the
repository.

### 9. Publish

Create a GitHub release against the tag and attach
`SALINLAHI-v<version>.zip`. Artifacts are never committed to the repository —
`*.apk` and `Build/` are gitignored, and a repository full of 60 MB binaries
becomes unusable.

Name the artifact after its version. Never `final.zip`, `final2.zip`,
`latest-final.zip`.

## Numbering

`MAJOR.MINOR.PATCH`, as in `0.1.0`.

| Change | Next version | Why |
|---|---|---|
| A bug fix only — a wrong score, a mispositioned badge, a crash | `0.1.0` → **`0.1.1`** | PATCH: a correction, no new capability |
| A new working feature — the Palengke level, the remaining narrator recordings, a mini-game | `0.1.0` → **`0.2.0`** | MINOR: a new capability. PATCH resets to 0 |
| A redesign that breaks existing saved data or the leaderboard schema | `0.9.x` → **`1.0.0`** | MAJOR: existing data cannot be read by the new version |

While the capstone is in development the version stays below `1.0.0`. That is
the honest signal: it says the system is incomplete.

### What has to be true for 1.0.0

The group considers the project ready for `1.0.0` when all of these hold:

1. All 16 scenes are built, including the sixth Bahay scene and the whole
   Palengke level — nothing locked on the map that is not meant to be locked.
2. All 443 narrator lines are recorded, including the 15 repeated cue and
   feedback lines that are still silent.
3. Every taught word has been through pronunciation testing with real
   children, and the accepted-variant list has been tuned from those logs.
4. The leaderboard has been exercised by more than one device at once, and a
   failed upload is proven to recover.
5. No known issue in the release notes is one that blocks a child from
   finishing a level.
6. The usability and usefulness evaluation in Chapter 1 §1.4.2 has been
   administered and its results written up.
7. The release has been tested by someone outside the group, on a phone the
   group does not own.

Until then, releases are `0.x` and are labelled development or testing builds.

## Who does what

| Responsibility | Member |
|---|---|
| Release preparation, version update, build generation, packaging | Daquis, Jhesza Mhei G. |
| Release testing on device | whoever did not build it |
| Release notes | the member whose feature the release is named for |
| Integration verification — that `main` holds what the notes claim | all three, at the pull request |

## Release history

| Version | Date | Type | Source | Notes |
|---|---|---|---|---|
| v0.1.0 | 12 Nov 2026 | Development milestone | tag `v0.1.0` | First controlled release. 15 scenes, 54 words, leaderboard, 423 voice clips. |

Earlier artifacts — `SALINLAHI_VoiceTest.apk`, `_v2`, `_v3` (Sep 2026) and the
Mid-Act7 `SALINLAHI.apk` (Nov 2026) — predate this process. They carry no
version number and no recorded source, and are not part of the release
history. They are kept only as evidence of the problem this process solves.
