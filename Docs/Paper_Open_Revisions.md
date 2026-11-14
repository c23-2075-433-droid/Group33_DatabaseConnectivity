# Chapters 1–3 — what is left

Everything from the earlier revision lists has been **applied directly to the Google Doc** and
verified by re-reading it afterwards. This file is now just the short list of what remains.

## Still open in the paper

1. `[CONFIRM: minimum RAM and free storage from device testing]` in 3.4.1 — needs a build on a
   real phone. Still highlighted.
2. `[CONFIRM: Table of development computers and test devices, Appendix E]` in 3.4.1 — only the
   group knows. Still highlighted.
3. **The figures are in the paper.** They live in Canva as three pages in
   **SALINLAHI - Chapter 3 Figures**: https://www.canva.com/d/7DdniU4u7wKFsjo
   — page 2 Figure 3.2a, page 3 Figure 3.2b, page 4 Figure 3.3. Page 1 is a leftover blank page
   from creating the design and can be deleted in Canva.

   3.5.1 now cites them as Figures 3.2a and 3.2b, and the captions are consistent.

   One layout thing left, which has to be done in Docs rather than through the API: the two
   Figure 3.2 images are **wrap-text (floating)** while Figures 3.1 and 3.3 are inline. A
   floating image does not reserve space in the text flow, so it drifts as the text above it
   grows and can end up overlapping a heading. Select each one and set it to **In line**. They
   are also different widths, 573 pt and 535 pt, so their edges do not align; matching them
   would look tidier.

   Earlier plain versions are still in `Docs/Figures/` if the Canva ones are ever not wanted.
4. **Chapter 4** has to match what Chapter 3 now claims: evaluation of functionality, usability
   and usefulness only, no pre/post test, and nothing in the instrument that the build does not
   have.

## Still open in the build

5. The Android bundle id is still `com.DefaultCompany.SALINLAHI`. Change it before the defense
   build.
7. The vocabulary may change again after the validation on 10 November. If it does, the figures
   that depend on it change with it: **74 objectives, 53 distinct words, 21 re-asks**, in 3.2.3,
   3.2.4 and 3.6.
8. **Bahay scene 6, Leaving for School**, is still designed-but-unbuilt and sits on the journey
   map as a locked tile. It stays on the validation form marked as planned.

## Decisions taken, and what they changed

- **Palengke removed from the game.** It was never visible — `BuildLevelSelectScene` leaves out
  any place with no scenes built — but it was the step the journey ended on, which is what let
  `IsLevelComplete(3)` be true. `JourneyMap.StepAfter` now returns `Steps.Length`, one past the
  end, and `LevelProgress` clamps to that, so Parke still earns its badge. Compile-checked, and
  all three levels verified to still register complete. `badge_palengke.png` and
  `map_palengke.png` are unused but left on disk.
- **Kylo is eight**, matching the Grades 2–3 players.
- **"Environment" is the paper's word** for Bahay, Paaralan and Parke. *Chapter*, *act* and
  *place* are gone from Chapter 3; *level* stays only where it names the game's own unit, which is
  what the UI prints and the database column records.
- **Stress marks** on the six ambiguous words, display only (`Assets/Scripts/WordStress.cs`).

## Uncommitted code in this worktree

`WordStress.cs` (new), `GameplayHUD.cs`, `WordsLearnedPanel.cs`, `JourneyMap.cs`,
`LevelProgress.cs`, `BuildLevelSelectScene.cs`. Your Unity runs
`preview/bangon-plus-typography`, which has not touched any of these, so it should merge cleanly.
