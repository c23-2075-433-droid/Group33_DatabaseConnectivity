SALINLAHI v0.2.0 - how to install and run this build
====================================================

SALINLAHI is a voice-interactive game for learning Filipino words. The child
says a Filipino word out loud and the game responds. Speaking is the control:
there is no on-screen joystick or button that advances a level in its place.

New in this release: for six words whose meaning changes with their stress -
Tayo, Upo, Bukas, Basa, Bati and Pasok - the game also checks HOW the word
was said, not only which word it was (ta-YO "stand up", not TA-yo "we").
See "The six stress words" below before testing it.


What you need
-------------

  Android phone or tablet, Android 8.0 (Oreo) or newer.
    The stress check for the six words needs Android 13 or newer. On Android
    8-12, and on phones whose recognizer will not take the game's audio, the
    game still plays; those six words fall back to a rougher check.
  A microphone (the built-in one is fine) and permission to use it.
  An internet connection, for the whole session:
    - the game asks for the device's standard Filipino speech recognizer,
      which may send the audio to its provider (for example Google) to turn
      it into text;
    - the leaderboard and the nickname check are hosted, so they need it too.
  Google's speech recognition installed and enabled - standard on Android.
  Sound turned up, and somewhere quiet enough to be heard.

Landscape only, either way up. Turn off portrait lock if it comes up rotated.


Installing
----------

1. v0.1.0 or v0.1.1 can be upgraded in place: the package name is unchanged
   (ph.edu.uphsl.group33.salinlahi) and the version code goes up (4), so
   Android accepts the upgrade and saved nicknames and progress carry over.

   IMPORTANT: a test build made from the source between v0.1.1 and this
   release also reports "0.1.1" on its About screen, because the version was
   not raised until now. If such a build is on the device, install this one
   over it; the About screen must then read v0.2.0.

   Anything older than v0.1.0 (SALINLAHI_VoiceTest, _v2, _v3, the Mid-Act7
   SALINLAHI.apk) carries the old package name and must be uninstalled first.

2. Copy SALINLAHI-v0.2.0.apk to the device.

3. Open it with the device's file manager and allow "install from this
   source" if Android asks. The APK is signed with a development key; it is a
   testing build, not for Play Store distribution.

4. Launch SALINLAHI and allow the microphone permission when it is asked for.


Playing it
----------

1. Type a nickname on first launch. It names the local save and the
   leaderboard entry. Several children can each have their own profile on one
   device. If the name is already used - on this device, or claimed by another
   device online - a free alternative is filled in; press OKAY again.

2. Press ADVENTURE, then pick an unlocked level on the map.

3. A word appears and Teacher Rachel says what to do. Say that word back,
   clearly, in Filipino, on its own. Correct answers move the story on; wrong
   ones ask again.

4. The mic button shows whether the game is listening. The speaker button
   replays the instruction. After ten seconds of silence Rachel nudges.

5. At the end of a scene a summary panel shows the score, the stars, the
   attempts and the words learned.


The six stress words
--------------------

Tayo, Upo, Bukas, Basa, Bati and Pasok get one of three answers:

  Correct     - said the taught way (ta-YO, u-PO, bu-KAS, BA-sa, BA-ti,
                PA-sok). The story moves on.
  Other word  - said the other way (TA-yo "we", U-po "gourd", BU-kas
                "tomorrow", ba-SA "wet", ba-TI "beaten", pa-SOK "inserted").
                Counted as a miss; Rachel says "Malapit na!" and the word.
  Say it again - the game could not tell. TRY AGAIN shows without the wrong
                sound, nothing is counted, and Rachel repeats the instruction.
                Saying the word with other words ("tayo po") also gets this.

Every other word is judged by the recognized text alone, as in v0.1.1.

The stress check was trained and tested on recordings of five adult speakers
only. It has NOT been tested with Grade 2-3 children. Asking again about one
answer in five is normal for it.


Which build is this?
--------------------

Main menu > SETTINGS gear > ABOUT. It should read v0.2.0. BUILD_INFO.txt in
this folder records the exact commit, the Unity version and whether the
source tree was clean.

Quick check that the stress check is present: in Umaga Na, when Tayo is
asked, say TA-yo ("we") clearly on its own. This build refuses it or asks
again; v0.1.1 accepted it.


Known issues in this build
--------------------------

See RELEASE_NOTES.txt. The ones most likely to be noticed:

  On the six stress words a child may be asked to say the word again several
  times. There is no limit on how often; nothing is counted against them.

  Kinig and Tapon have no recording of the word itself; Rachel teaches them
  by caption while she speaks the sentence around them.

  The MINI GAME button on the main menu does nothing yet.


Who to tell
-----------

Group 33 - BSIT Capstone, University of Perpetual Help System Laguna - JONELTA
  Daquis, Jhesza Mhei G.
  Lacida, Kylo Bryan
  Manzanero, Kyla Samantha

When reporting a problem, please say: the version from the About screen, the
phone model and Android version, the level and the word being asked for, and
whether the phone was online.
