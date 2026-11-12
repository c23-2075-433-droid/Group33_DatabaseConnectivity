SALINLAHI v0.1.0 - how to install and run this build
====================================================

SALINLAHI is a voice-interactive game for learning Filipino words. The child
says a Filipino word out loud and the game responds. Speaking is the control:
there is no on-screen joystick or button that advances a level in its place.


What you need
-------------

  Android phone or tablet, Android 8.0 (Oreo) or newer.
  A microphone (the built-in one is fine) and permission to use it.
  An internet connection, for the whole session. This is not optional:
    - the Filipino speech recognizer runs on the device's online recognition
      service, so without a connection the game cannot hear answers at all;
    - the leaderboard is hosted, so scores neither upload nor display offline.
  Google's speech recognition installed and enabled - standard on Android.
  Sound turned up, and somewhere quiet enough to be heard.

Landscape only. Turn off portrait lock if the screen comes up rotated.


Installing
----------

1. If an older SALINLAHI build is on the device, uninstall it first. This
   release changed the app's package name from the Unity default
   (com.DefaultCompany.SALINLAHI) to ph.edu.uphsl.group33.salinlahi, so the
   two would otherwise install side by side as separate apps. Nicknames and
   progress saved in the older build do not carry over.

2. Copy SALINLAHI-v0.1.0.apk to the device.

3. Open it with the device's file manager and allow "install from this
   source" if Android asks. It is an unknown source because the APK is signed
   with a development key, not a Play Store one; this build is for testing and
   is not meant for Play Store distribution.

4. Launch SALINLAHI and allow the microphone permission when it is asked for.
   Denying it leaves the game unable to hear anything.


Playing it
----------

1. Type a nickname on first launch. It names the local save and the
   leaderboard entry. Several children can each have their own profile on one
   device.

2. Press ADVENTURE, then pick an unlocked level on the map. Levels unlock in
   order, so the first run starts at Bahay.

3. A word appears on screen and Teacher Rachel says what to do. Say that word
   out loud, clearly, in Filipino. Correct answers move the story on; wrong
   ones ask again. The game checks Filipino pronunciation properly, so a wrong
   vowel is not accepted - saying "kaha" for "Kuha" counts as wrong, on
   purpose.

4. The mic button shows whether the game is listening. The speaker button
   replays the instruction.

5. At the end of a scene a summary panel shows the score, the stars and the
   words learned. Scores reach the leaderboard from the main menu.

Nothing is typed during gameplay. If a child cannot be heard, check the
microphone permission, the volume and the internet connection before assuming
the level is stuck.


Which build is this?
--------------------

Main menu > SETTINGS gear > ABOUT. The version is printed under the title. It
should read v0.1.0 for this release; if it does not, this is a different build
and any bug report should quote what the About screen actually says.

BUILD_INFO.txt in this folder records the exact commit this APK was built
from, along with the Unity version and whether the source tree was clean.


Known issues in this build
--------------------------

See RELEASE_NOTES.txt. The ones most likely to be noticed:

  Teacher Rachel is silent for 15 of her repeated lines ("Sabihin...",
  "Ulitin...", and the correct/incorrect/not-heard replies). The written
  caption still appears, so the game remains playable.

  The MINI GAME button on the main menu does nothing yet.

  The Palengke level and the sixth Bahay scene are not built. They are shown
  locked on the map.

  The opening prompt of a scene waits a while before the child may speak.


Who to tell
-----------

Group 33 - BSIT Capstone, University of Perpetual Help System Laguna - JONELTA
  Daquis, Jhesza Mhei G.
  Lacida, Kylo Bryan
  Manzanero, Kyla Samantha

When reporting a problem, please say: the version from the About screen, the
phone model and Android version, the level and the word being asked for, and
whether the phone was online.
