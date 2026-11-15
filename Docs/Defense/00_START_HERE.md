# Defense Preparation — Start Here

Pre-Final Defense, 20–23 November 2026 (Biñan). Written 15 November 2026 against
the repository at commit `94c79a8`.

Everything in this folder was read out of the project, not remembered. Where a
number here disagrees with the paper, the README or the release notes, the
project is right and the document is stale — `07_Paper_vs_Build.md` lists every
one.

## The files

| | For |
|---|---|
| **[01_System_Reviewer.md](01_System_Reviewer.md)** | How the whole system works. Plain English first, then the technical answer. **§12 is the ten things to know before you walk in.** |
| **[02_Script_Guide.md](02_Script_Guide.md)** | Every script a panelist might open, with one sentence to say about each. |
| **[03_Completion_Assessment.md](03_Completion_Assessment.md)** | What is done, partly done, untested and absent — with stated criteria, a weighted total, and the risks ranked. |
| **[04_Defense_Questions.md](04_Defense_Questions.md)** | 52 questions with answers, technical layers, follow-ups and the evidence to show. **The most important file — 45 of your 60 minutes are questions.** |
| **[05_Demo_Script.md](05_Demo_Script.md)** | The 5-minute demo, minute by minute, with a recovery plan for each way it can break. |
| **[06_Seven_Day_Plan.md](06_Seven_Day_Plan.md)** | Day by day to the defense, plus the LMS requirements checklist. |
| **[07_Paper_vs_Build.md](07_Paper_vs_Build.md)** | Chapters 1–3 against the build. Ten edits, in priority order. |

## The numbers, settled

Three different word counts are in circulation. These are the ones the project
actually has:

| | |
|---|---|
| Playable scenes | **15** of 16 designed |
| Spoken objectives | **72** |
| Distinct Filipino words | **53** |
| Objectives that re-ask an earlier word | **19** |
| Voice-over lines recorded | **401** of 436 (423 clips on disk) |
| Clips with a lip-sync track | **415** of 423 |
| Runtime C# | 67 scripts, 14,379 lines |
| Editor C# | 50 scripts, 14,810 lines |
| Commits | 351, three contributors |
| Completion by stated criteria | **≈82%** |

The paper says 73 objectives. The README says 66 objectives and 49 words. The
release notes say 74 and 54. All three are stale. Regenerate with:

```bash
python3 Tools/completeness_report.py
```

## The one thing to internalise

The game teaches, prompts, repeats and scaffolds 53 Filipino words, and the
whole loop works. What is **not** yet reliable is the thing the project is named
for: correcting a mispronunciation on a real device. Android's Filipino
dictation service returns the nearest real word rather than the sounds the child
made, so the checker is handed an already-corrected string.

That is the limitation Bhardwaj et al. (2022) and Santiago et al. (2025) predict,
it is recorded as the first known issue in v0.1.0, and it is what defines the
next version.

**Say it yourselves, in the demo, before anyone asks.** Stated by you it is a
finding. Found by a panelist it is a hole.
