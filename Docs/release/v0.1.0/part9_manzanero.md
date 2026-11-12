## Part IX. Individual Release Contribution

**Student Name:** Manzanero, Kyla Samantha

**Group Number:** 33

**Release Version:** v0.1.0

**Assigned Release Responsibility:** Release content and documentation — the
levels, summary screens and interface wording that v0.1.0 actually ships, and
verifying they are present and correct in the integrated source the release was
built from.

**Specific Contribution**

- **Two of the three places in this release.** The Paaralan (school) and Parke
  (park) levels were built in this work — ten of the fifteen playable scenes
  v0.1.0 ships, including the classroom, the lesson, recess and dismissal, and
  the whole Parke level. Everything the release notes say about Chapters 2 and 3
  rests on it.
- **The level summary screen.** Rebuilt from the group's own UI kit, which is
  the summary panel named in the Part VI test "the summary panel shows the
  score, stars and words learned" — a check the device run passed.
- **Recording which words were practised, not just how many.** This changed
  what the game stores about a session: the specific words a child used rather
  than a count. It is what makes the "words learned" panel possible, and it is
  the learning data the evaluation phase will rest on.
- **The nickname suggester.** When a child types a nickname that is already
  taken, the game offers a free one instead of refusing. Part VI's save/load
  check exercised the profile this creates.
- **Putting the interface in English,** including three saved scenes whose
  titles had been left in Tagalog, so the game reads consistently to the
  teachers and evaluators who will assess it.
- **Sprite presentation fixes** — removing the white sticker edge left around
  the sprites and planting Kylo properly on the floor, which is part of why
  Part VI's "required assets load" check found nothing missing or misdrawn.

**Evidence**

| Item | Reference |
|---|---|
| Commit — Paaralan level built | `45f1f99` "Build Paaralan: classroom, lesson, recess and dismissal" — **in v0.1.0** |
| Commit — Parke level built | `7b6d75f` "Build Parke, the third and last level" — **in v0.1.0** |
| Commit — level summary rebuilt | `639908b` "Rebuild the level summary from the group's UI kit" — **in v0.1.0** |
| Commit — words practised recorded | `f8db368` "Record which words were practised, not just how many" — **in v0.1.0** |
| Commit — nickname suggester | `523a602` "Offer a free nickname when the one typed is taken" — **in v0.1.0** |
| Commit — interface in English | `e6c2e9a`, `b50a944` — **in v0.1.0** |
| Commit — sprite edges and footing | `fd38c32` — **in v0.1.0** |
| Traceable author | `Kyla Samantha Manzanero <manzanerokyla@gmail.com>` — 13 commits on `main`, 237 files, +82,564 / −22,158 |

**A note on the release cycle itself.** Every commit listed above was verified
to be inside `c035c8f`, the commit the released APK was built from, with
`git merge-base --is-ancestor`. All of this work predates the release cycle
documented in Parts IV to VII: it is content the release carries rather than
release engineering performed during it. The release preparation, build
generation and packaging for v0.1.0 were carried out by Daquis, and the
integration and repository configuration by Lacida, as the responsibility
table in `Docs/RELEASE_PROCESS.md` records.

### What I learned about the difference between developing software and releasing software

Building the Paaralan and Parke levels, I judged my work by whether it played
correctly in the editor. The release changed what counts as evidence. Ten of
the scenes I built are in v0.1.0, and the proof is not my memory of finishing
them but that each commit is provably inside `c035c8f`, the commit the APK came
from. Development asks whether the feature works; releasing asks which build
contains it, whether a stranger can run that build, and what is honestly still
missing. The failed pronunciation check taught the group that last part.

*(approximately 100 words)*
