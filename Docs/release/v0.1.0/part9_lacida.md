## Part IX. Individual Release Contribution

**Student Name:** Lacida, Kylo Bryan

**Group Number:** 33

**Release Version:** v0.1.0

**Assigned Release Responsibility:** Integration verification and repository
configuration — making sure the source `main` holds what the release notes
claim, and that the way the group merges cannot corrupt it.

**Specific Contribution**

- **Scene content that this release ships.** The Paaralan (school) level in
  v0.1.0 is largely this work: all five Paaralan scenes rebuilt from their
  builders, the school gate swinging open, Kylo breathing while he waits at it,
  and the guard waving back when the child says **Bati**. The wave is driven by
  a pose sequence rather than a one-off animation, so the same mechanism can
  drive any standing pose later.
- **Matching the game to the validated word list.** The taught words in the
  scenes were brought into line with the list the group had validated, which is
  what makes the figure quoted throughout this report — 54 distinct words
  across 74 spoken objectives — true of the build rather than of an older plan.
- **Repository configuration for safe integration.** Added `.gitattributes`
  turning on Unity's own merge driver and turning Git LFS off, with the
  reasoning recorded in the file. This matters more than its size suggests: a
  Unity scene is a large YAML file, and two members editing one scene produce a
  merge Git cannot resolve sensibly on its own. Left alone it silently
  corrupts a scene, and a corrupted scene in `main` is a corrupted release. It
  landed after v0.1.0 was tagged, so it protects every release from 0.2.0
  onward.
- **Integration during the release cycle.** Merged the release-engineering work
  into `main` alongside the scene work twice on release day, keeping one
  integrated source for the build to come from rather than two diverging ones.

**Evidence**

| Item | Reference |
|---|---|
| Commit — all five Paaralan scenes rebuilt | `8b6a6f9` "Rebuild all five Paaralan scenes" — **in v0.1.0** |
| Commit — gate, wave and idle breath | `eceab40`, `8aae730`, `c53d0a2` — **in v0.1.0** |
| Commit — words matched to the validated list | `dfc9aad` "Match the game to the validated word list" — **in v0.1.0** |
| Commit — repository configuration | `edaf33a` "Add .gitattributes: Unity's merge driver on, LFS off and why" — after the tag, so **not in v0.1.0** |
| Commit — guard's gate push and turn frame | `82f4792`, `0102bbe`, `eca4868` — after the tag, so **not in v0.1.0**; queued for 0.2.0 |
| Integration merges on release day | `06e4cd3`, `448b1eb` |
| Traceable author | `Kylo Bryan Lacida <lacidakylo@gmail.com>` — 45 commits on `main`, 198 files, +49,193 / −36,971 |

**A note on what is and is not in this release.** Four of these commits landed
after `c035c8f`, the commit the APK was built from, so the guard's gate push,
Kylo's turn through the gate and the `.gitattributes` change are **not** in
v0.1.0 even though they were written on the same day. Checking this rather than
assuming it is the whole point of tagging the release: `git merge-base
--is-ancestor <commit> c035c8f` answers it for any commit, and the answer went
into this report instead of a guess.

### What I learned about the difference between developing software and releasing software

Developing, I could finish a scene and see it work in the editor, and that felt
like done. Releasing showed me "done" has a date and a commit attached to it.
Four of my changes went in on release day and four did not, and the only reason
anyone can tell which is that the release was tagged — otherwise the notes
would have claimed the guard's gate push that testers do not have. I also
understood why `.gitattributes` matters only once I thought about releases: a
scene merged badly is not just my problem that day, it is a corrupted build
nobody can trace.

*(approximately 100 words)*
