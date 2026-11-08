SALINLAHI — vocabulary font
===========================

The font used for the Filipino words and their English meanings on the
WORDS LEARNED board, and for nothing else. The rest of the UI still uses
Fredoka through Assets/Editor/UIFont.cs; this folder is looked at only by
Assets/Editor/VocabularyFont.cs.

It is separate because the board's lettering is meant to match the chunky,
distressed capitals painted into the art (Art/words_learned_reference.png),
which is a different job from the rounded lettering on the buttons.


What is in here now: Luckiest Guy
---------------------------------

  https://fonts.google.com/specimen/Luckiest+Guy

Apache 2.0 (LuckiestGuy-LICENSE.txt is the licence, keep it beside the
.ttf). Taken from the official google/fonts repository, so it is a clean
copy rather than a reupload from a free-font site. Chunky, comic, all-caps
by design — the nearest freely redistributable match to the reference.


The first choice: Gagalin
-------------------------

  https://ipassas.com/typefaces/

Gagalin Regular by Iordanis Passas is what the reference lettering actually
is: the same chunky comic shapes but with the rough, distressed edges that
Luckiest Guy's clean outlines do not have. It is free for personal AND
commercial use under the ipassas EULA, and that EULA allows redistribution
as long as the agreement travels with the files and the notice
"Font by ipassas design / ipassas.com" is kept.

It is not in here because the only official download is an email checkout
on ipassas.com, which has to be done by a person.

To switch to it:

  1. Go to https://ipassas.com/product/gagalin/ and "Add to cart, free",
     then check out. The files arrive by email (OTF, TTF, WOFF, WOFF2).
  2. Drop Gagalin-Regular.ttf (or .otf) into THIS folder, along with the
     licence text file from the download.
  3. Re-run Tools > SALINLAHI > Restyle Scene Result Panels.

Nothing else to change. VocabularyFont.Get() prefers any font in this
folder whose filename contains "Gagalin" and only falls back to Luckiest
Guy when there isn't one.


One thing to watch
------------------

UIFont.Get() and UIThemeTool.FindFont() both search ALL of Assets/Fonts,
this subfolder included. Today they never reach anything in here: UIFont
takes the first path containing "Fredoka", and FindFont matches
"Fredoka-Bold.ttf" and "Fredoka-Medium.ttf" by name. But if the Fredoka
folder is ever emptied, their last-resort fallback is "the first font
found", which could then be this one. Keep Fredoka where it is, or give
those two a fallback of their own.
