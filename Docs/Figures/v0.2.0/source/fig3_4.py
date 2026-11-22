"""Figure 3.4 System Architecture and Network-Dependent Interactions (v0.2.0).
A4 portrait layout (1000 x 1460 units)."""
from svglib import SVG, panel, FONT
from xml.sax.saxutils import escape
s = SVG(1000, 1460)
T = 13  # panel title size
def frame(x, y, w, h, title, color="#1f2937"):
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="#ffffff" stroke="{color}" stroke-width="3"/>')
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="40" rx="12" fill="{color}"/><rect x="{x}" y="{y+26}" width="{w}" height="14" fill="{color}"/>')
    s.add(f'<text x="{x+16}" y="{y+21}" font-family="{FONT}" font-size="15" font-weight="bold" letter-spacing="1.2" fill="#ffffff" dominant-baseline="central">{escape(title)}</text>')

frame(10, 10, 980, 905, "PLAYER DEVICE — ANDROID 8.0+ (API 26), ARM64 — SALINLAHI v0.2.0")
panel(s, 30, 62, 940, 108, "PRESENTATION LAYER", [
    "17 Unity scenes: 15 gameplay scenes, MainMenu, LevelSelect",
    "GameplayHUD + VoiceStatusMachine (listening panel) · SceneResultPanel · WordsLearnedPanel · RachelPortraitAnimator",
    "MainMenuController · ProfilePickerPanel · LevelSelectController · WordStress (stress marks on screen)"], tsize=T)
panel(s, 30, 190, 940, 128, "GAME LOGIC LAYER", [
    "**SceneObjectiveController** — the ordered objectives of a scene",
    "**AnswerJudge** — the word first (PronunciationChecker + WordVariants); stress only for Tayo, Upo, Bukas, Basa, Bati, Pasok",
    "**VoicePromptDirector + VoiceNarrator** — what Teacher Rachel says, and when (hints, retries, nudges)",
    "RunScore · SessionScoreTracker · LevelProgress · JourneyMap · PlayerMovement · NicknameAvailability"], tsize=T)
s.path([(500, 190), (500, 172)])
s.add('<rect x="30" y="338" width="940" height="372" rx="8" fill="#fbf7f1" stroke="#b7791f" stroke-width="2"/>')
s.text(46, 358, "SPEECH INPUT AND PRONUNCIATION LAYER — VoiceCommand chooses the listening path", T, "bold", "start", "#b7791f")
s.path([(500, 338), (500, 320)])
panel(s, 45, 378, 455, 155, "ORDINARY LISTENING", [
    "All words except the six on phones where",
    "the audio bridge works",
    "**UnitySpeechToText** (yasirkula) → Android",
    "SpeechRecognizer, fil-PH: text + loudness"], kind="white", tsize=T)
panel(s, 515, 378, 440, 155, "AUDIO BRIDGE — SIX WORDS, ANDROID 13+", [
    "**SalinlahiAudioRecognizer.java** records into",
    "memory (max. 12 s) and pipes the same audio",
    "to SpeechRecognizer (EXTRA_AUDIO_SOURCE);",
    "gives up for the session after failures"], kind="white", tsize=T)
panel(s, 45, 545, 455, 150, "FALLBACK STRESS CHECK (SIX WORDS)", [
    "Used when no audio was recorded",
    "**StressAnalyzer.JudgeBanded** on loudness",
    "readings; no readings → uncertain"], kind="warn", tsize=T)
panel(s, 515, 545, 440, 150, "ON-DEVICE CLASSIFIER (SIX WORDS)", [
    "**PronunciationModel**: logistic regression per",
    "word, weights embedded in the app; checked",
    "by safety-net judges; no network; audio is",
    "discarded after judging"], kind="good", tsize=T)
panel(s, 30, 728, 940, 92, "LOCAL DATA LAYER", [
    "**players.json** (PlayerProfileStore): up to 10 profiles · UUID · progress · stars · points · words learned",
    "**salinlahi_voice_attempts.csv** (VoiceAttemptLog): 18 columns of text and numbers; no audio; never uploaded"], kind="device", tsize=T)
panel(s, 30, 832, 940, 70, "STAYS ON THE DEVICE", [
    "The game's copy of the audio (memory only) · the attempt log · profiles and progress · the trained model"], kind="device", dash=True, tsize=T)

# bottom row
panel(s, 10, 1000, 300, 330, "SPEECH SERVICE", [
    "Android speech-recognition",
    "service (device provider,",
    "e.g. Google)",
    "",
    "The standard recognizer",
    "is requested; it may",
    "process audio on the",
    "provider's servers. The",
    "on-device recognizer is",
    "tried only after failures."], kind="maybe", dash=True, tsize=T)
frame(330, 1000, 660, 330, "SUPABASE BACKEND (HTTPS, UnityWebRequest)", color="#3b5b8c")
panel(s, 345, 1052, 315, 140, "POSTGREST", [
    "player_scores: INSERT",
    "nickname_claims: INSERT, SELECT",
    "leaderboard: SELECT (read only)",
    "rpc leaderboard_rank"], kind="net", tsize=T)
panel(s, 345, 1202, 315, 118, "EDGE FUNCTION submit-score", [
    "The only leaderboard writer;",
    "issues and checks an",
    "ownership token"], kind="maybe", tsize=T)
panel(s, 670, 1052, 305, 268, "LEAVES THE DEVICE", [
    "**Level record** (5th scene):",
    "nickname · score · attempts",
    "· level · remarks",
    "**Leaderboard** (main menu):",
    "player ID · nickname · points",
    "· stars · ownership token",
    "**Nickname claim** (dialog):",
    "nickname · installation ID",
    "No audio, no attempt log"], kind="white", dash=True, tsize=T)
# arrows
s.path([(120, 915), (120, 998)]); s.label(126, 955, "audio", anchor="start", size=13)
s.path([(200, 998), (200, 917)]); s.label(206, 955, "text", anchor="start", size=13)
s.path([(430, 915), (430, 998)]); s.label(422, 957, "level record ·\nnickname claim", anchor="end", size=13)
s.path([(600, 998), (600, 917)], dash=True); s.label(608, 957, "leaderboard rows ·\nrank · names", anchor="start", size=13, weight="normal")
s.path([(880, 915), (880, 998)]); s.label(872, 957, "totals\n(main menu)", anchor="end", size=13)

# legend
y = 1370
s.add(f'<rect x="10" y="{y-22}" width="980" height="104" rx="8" fill="#f7f7f5" stroke="#9ca3af" stroke-width="1.5"/>')
def key(x, fill, stroke, dash, text):
    d = ' stroke-dasharray="6 4"' if dash else ''
    s.add(f'<rect x="{x}" y="{y-10}" width="30" height="20" rx="4" fill="{fill}" stroke="{stroke}" stroke-width="2"{d}/>')
    s.text(x + 40, y, text, 13, anchor="start")
key(30, "#e6f4ea", "#2f7d4a", False, "On the device; no network")
key(300, "#fff7ea", "#b7791f", True, "May use the network (speech service)")
key(640, "#eef3fb", "#3b5b8c", False, "Network: Supabase")
s.text(30, y + 34, "Answer checking, the classifier and local saving need no network. Recognition may; the level record, nickname", 13, anchor="start")
s.text(30, y + 54, "claim and leaderboard do. The game itself never saves or uploads audio.", 13, anchor="start")
s.save("../Figure_3_4_System_Architecture_and_Network_Dependent_Interactions.svg")
