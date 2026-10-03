SALINLAHI — UI font
===================

Drop a .ttf or .otf file into this folder and every generated UI screen
picks it up automatically on the next builder run. No code changes needed.

See Assets/Editor/UIFont.cs for the lookup: a font whose filename contains
"Fredoka" is preferred; otherwise the first font in this folder is used. If
the folder is empty, the UI falls back to Arial, which does NOT match the
lettering painted into the button and panel art.


Recommended: Fredoka
--------------------

  https://fonts.google.com/specimen/Fredoka

Chunky, rounded and geometric — the closest free match to the "OKAY",
"CANCEL" and "Enter Nickname:" lettering in the sprites, and still legible
at the small size used for the leaderboard rows.

Download > unzip > drag "Fredoka-SemiBold.ttf" (or Bold) into this folder.


Alternatives, if you want something bouncier
--------------------------------------------

  Baloo 2   https://fonts.google.com/specimen/Baloo+2
  Chewy     https://fonts.google.com/specimen/Chewy
  Sniglet   https://fonts.google.com/specimen/Sniglet

Chewy is the most playful but gets muddy at small sizes, so if you pick it,
consider bumping the leaderboard font size in LevelCompleteUIBuilder.cs.


Licensing
---------

All of the above are Google Fonts released under the SIL Open Font License
(OFL), which permits use in an academic project and in commercial games,
including redistribution inside this repository. Keep the OFL.txt that ships
in the font's zip alongside the .ttf so the license travels with it.

Avoid fonts pulled from random "free font" sites — many are unlicensed
rips, which is a problem for a graded submission.


After adding the font
---------------------

Re-run the builders so existing scenes pick it up:

  Tools > SALINLAHI > Add Nickname Dialog To Main Menu
  Tools > SALINLAHI > Add Database Demo To Level 1

The Console will log "[SALINLAHI] UI font: Assets/Fonts/<your font>" to
confirm which file is in use.
