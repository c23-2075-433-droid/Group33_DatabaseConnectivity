SALINLAHI v0.1.1 - how to install and run this build
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

Landscape only, and either way up: the screen no longer has to be held one
particular way round. Turn off portrait lock if it comes up rotated.


Installing
----------

1. If v0.1.0 is already on the device you can install straight over it. The
   package name has not changed since v0.1.0 (ph.edu.uphsl.group33.salinlahi)
   and the version code goes up, so Android will accept the upgrade and
   nicknames and progress saved under v0.1.0 carry over.

   If a build OLDER than v0.1.0 is on the device - SALINLAHI_VoiceTest,
   _v2, _v3, or the Mid-Act7 SALINLAHI.apk - uninstall it first. Those carry
   the old Unity default package name (com.DefaultCompany.SALINLAHI) and would
   install side by side as a separate app.

2. Copy SALINLAHI-v0.1.1.apk to the device.

3. Open it with the device's file manager and allow "install from this
   source" if Android asks. It is an unknown source because the APK is signed
   with a development key, not a Play Store one; this build is for testing and
   is not meant for Play Store distribution.

4. Launch SALINLAHI and allow the microphone permission when it is asked for.
   Denying it leaves the game unable to hear anything. Teacher Rachel now
   says so out loud when the mic is blocked, rather than only printing it.


Playing it
----------

1. Type a nickname on first launch. It names the local save and the
   leaderboard entry. Several children can each have their own profile on one
   device.

2. Press ADVENTURE, then pick an unlocked level on the map. Levels unlock in
   order, so the first run starts at Bahay.

3. A word appears on screen and Teacher Rachel says what to do - out loud, in
   her own voice, for every line in the game. Say that word back, clearly, in
   Filipino. Correct answers move the story on; wrong ones ask again. The
   whole word has to be said: "ba" for "Bangon" is no longer accepted.

4. The mic button shows whether the game is listening. The speaker button
   replays the instruction. After ten seconds of silence the crickets come
   out, and Rachel nudges once.

5. At the end of a scene a summary panel shows the score, the stars, TOTAL
   ATTEMPTS and the words learned. Scores reach the leaderboard from the main
   menu, which now scrolls - ten children can share one tablet and all still
   find their names.

Nothing is typed during gameplay. If a child cannot be heard, check the
microphone permission, the volume and the internet connection before assuming
the level is stuck.


Which build is this?
--------------------

Main menu > SETTINGS gear > ABOUT. The version is printed under the title. It
should read v0.1.1 for this release; if it does not, this is a different build
and any bug report should quote what the About screen actually says.

BUILD_INFO.txt in this folder records the exact commit this APK was built
from, along with the Unity version and whether the source tree was clean.


Known issues in this build
--------------------------

See RELEASE_NOTES.txt. The ones most likely to be noticed:

  Wrong pronunciation is caught now, as far as anyone has checked. The phone
  test on 17 November said a word wrong on purpose and the game refused it,
  where v0.1.0 accepted it. That is one phone and a few words, so if a wrong
  answer is let through while you are playing, it is worth writing down which
  word and which phone - that is the one result this build is gathering.

  Eleven words have no recording of the word itself, so Rachel teaches them
  by caption while she speaks the sentence around them: Pagkain, Tapon, Suot,
  Padyak, Alok, Apir, Hugas, Kinig, Sali, Tara, Salamin.

  The MINI GAME button on the main menu does nothing yet.

  The Palengke level is not in the game. The sixth Bahay scene has been
  dropped; the home chapter now ends at breakfast. Neither is shown on the
  map any more, so the journey no longer ends on a locked tile.

  A word badge sits low on the screen in Paaralan 5.

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
