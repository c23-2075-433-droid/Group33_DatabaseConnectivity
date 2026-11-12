## Part IX. Individual Release Contribution

**Student Name:** Daquis, Jhesza Mhei G.

**Group Number:** 33

**Release Version:** v0.1.0

**Assigned Release Responsibility:** Release preparation, build generation,
version update, packaging, and the bug fixing required for release readiness.

**Specific Contribution**

- **Version identification.** Raised Player Settings from Unity's default `1.0` to `0.1.0` and the Android version code from 1 to 2, and made the build state its own version on the About screen. Wrote `Assets/Scripts/VersionLabel.cs` to read `Application.version` at run time rather than baking a string into the generated menu scene, so the screen and the artifact cannot drift apart.
- **The build entry point.** Wrote `Assets/Editor/ReleaseBuild.cs`, the single way this project now produces a release: it reads the version instead of setting it, collects the enabled scenes instead of hardcoding them, refuses to run when the version, the scenes or the release notes are missing, and writes `BUILD_INFO.txt` beside the artifact recording the commit, the branch and whether the working tree was clean.
- **Release-readiness fixes.** Found and fixed two defects during the Part IV check. The Android package id was still `com.DefaultCompany.SALINLAHI`, and because `overrideDefaultApplicationIdentifier` was off, Unity regenerated the id from the company name — so setting the identifier alone would have been silently undone. Internet access was left to Unity's auto-detection although both the speech recogniser and the leaderboard are network services; it is now requested outright.
- **Build generation and packaging.** Produced `SALINLAHI-v0.1.0.apk` from `main` at `c035c8f` with a clean working tree, and packaged it as `SALINLAHI-v0.1.0.zip` with its README, release notes and build information.
- **Verification.** Confirmed the artifact's own metadata with `aapt dump badging` rather than trusting the build script, and compile-checked `Assets/Scripts` and `Assets/Editor` with Roslyn under `-warnaserror+` before starting the build.
- **Documentation.** Wrote `Docs/release/v0.1.0/README.txt`, `Docs/release/v0.1.0/RELEASE_NOTES.txt`, `Docs/RELEASE_PROCESS.md` and this report.

**Evidence**

| Item | Reference |
|---|---|
| Commit — version, build script, settings | `b1c56b1` "Give the build a version, and one way to produce it" |
| Commit — version shown in the menu scene | `0bec1d1` "Put the version under the About title in the menu scene" |
| Commit — the process document | `6b8c388` "Write down the release process, including what 1.0.0 would require" |
| Branch | `claude/capstone-build-release-3ef55d`, merged into `main` |
| Tag | `v0.1.0` |
| Files written | `Assets/Editor/ReleaseBuild.cs`, `Assets/Scripts/VersionLabel.cs`, `Docs/RELEASE_PROCESS.md`, `Docs/release/v0.1.0/README.txt`, `Docs/release/v0.1.0/RELEASE_NOTES.txt` |
| Artifact | `SALINLAHI-v0.1.0.zip`, attached to GitHub release `v0.1.0` |
| Build record | `BUILD_INFO.txt` inside the package |

### What I learned about the difference between developing software and releasing software

Developing it, I only ever needed the thing to work in front of me. Releasing
it, I had to answer questions the editor never asks: which version is this,
what commit produced it, what is missing, who can install it, and how. Writing
those answers down found two real defects — the package id was still Unity's
default, and the game never asked for internet access even though it cannot
hear a word without it. Both had survived four earlier builds unnoticed.
Development ends when the code runs; a release ends when someone else can run
it, know exactly what they are holding, and trace it back to the source.

*(approximately 100 words)*
