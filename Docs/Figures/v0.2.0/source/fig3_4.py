"""Figure 3.4 System Architecture and Network-Dependent Interactions (v0.2.0), print edition.
845 units wide, shown 484 pt wide and about 545 pt tall, which fits under its two caption lines
in the paper's page body (about 615 pt below the university header); 14-unit text prints at 8 pt."""
from svglib import SVG, panel, FONT
from xml.sax.saxutils import escape
W = 845
T = 14; S = 14; LH = 1.38
def ph(n):
    """Height of a panel holding n lines."""
    return round(50 + (n - 1) * S * LH + 20)
def frame(x, y, w, h, title, color="#1f2937"):
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="#ffffff" stroke="{color}" stroke-width="3"/>')
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="38" rx="12" fill="{color}"/><rect x="{x}" y="{y+24}" width="{w}" height="14" fill="{color}"/>')
    s.add(f'<text x="{x+16}" y="{y+20}" font-family="{FONT}" font-size="15" font-weight="bold" letter-spacing="1" fill="#ffffff" dominant-baseline="central">{escape(title)}</text>')
def P(x, y, w, title, lines, **kw):
    h = kw.pop("h", None) or ph(len(lines))
    panel(s, x, y, w, h, title, lines, size=S, tsize=T, lh=LH, **kw)
    return y + h

H = 952
s = SVG(W, H)
# ---- the device -------------------------------------------------------------
DEV = 567
frame(8, 8, W - 16, DEV - 8, "PLAYER DEVICE — ANDROID 8.0+, ARM64 — SALINLAHI v0.2.0")
y = P(22, 52, W - 44, "PRESENTATION AND GAME LOGIC", [
    "17 Unity scenes · listening panel · result and words-learned panels · journey map · profiles",
    "**SceneObjectiveController**: a scene's ordered objectives · RunScore · LevelProgress",
    "**AnswerJudge**: the word first; stress only for Tayo, Upo, Bukas, Basa, Bati, Pasok",
    "**VoicePromptDirector + VoiceNarrator**: Teacher Rachel's lines, hints and nudges"])
s.path([(W/2, y + 10), (W/2, y + 2)])
top = y + 10
cw = (W - 44 - 30) // 2
x1, x2 = 32, 32 + cw + 10
r1 = top + 32
r2 = r1 + ph(3) + 8
sh = r2 + ph(3) + 8 - top
s.add(f'<rect x="22" y="{top}" width="{W-44}" height="{sh}" rx="8" fill="#fbf7f1" stroke="#b7791f" stroke-width="2"/>')
s.text(38, top + 17, "SPEECH INPUT AND PRONUNCIATION — VoiceCommand picks the path", T, "bold", "start", "#b7791f")
P(x1, r1, cw, "ORDINARY LISTENING", [
    "Every word except the six where the",
    "bridge works: **UnitySpeechToText** →",
    "Android recognizer, fil-PH; text + loudness"], kind="white")
P(x2, r1, cw, "AUDIO BRIDGE — ANDROID 13+", [
    "Six words: **SalinlahiAudioRecognizer.java**",
    "records up to 12 s into memory and pipes it",
    "to the recognizer; gives up after failures"], kind="white")
P(x1, r2, cw, "FALLBACK CHECK (SIX WORDS)", [
    "No audio: **StressAnalyzer.JudgeBanded**",
    "on the loudness readings; no readings",
    "→ uncertain"], kind="warn")
P(x2, r2, cw, "ON-DEVICE CLASSIFIER (SIX WORDS)", [
    "**PronunciationModel**: per-word logistic",
    "regression built into the app, checked by",
    "safety-net judges; audio then discarded"], kind="good")
y = P(22, top + sh + 10, W - 44, "LOCAL DATA", [
    "**players.json**: up to 10 profiles · UUID · progress · stars · points · words learned",
    "**salinlahi_voice_attempts.csv**: 18 columns of text and numbers; no audio; not uploaded"], kind="device")
assert y <= DEV - 8, (y, DEV)

# ---- the network --------------------------------------------------------------
B = DEV + 54
BH = 258
sw = 236
P(8, B, sw, "SPEECH SERVICE", [
    "Android speech service",
    "(device provider, e.g.",
    "Google). The standard",
    "recognizer is asked first",
    "and may process audio",
    "on the provider's servers;",
    "the on-device one is tried",
    "only after failures."], kind="maybe", dash=True, h=BH)
fx = 8 + sw + 12
frame(fx, B, W - 8 - fx, BH, "SUPABASE (HTTPS)", color="#3b5b8c")
iw = 262
e = P(fx + 10, B + 46, iw, "POSTGREST", [
    "player_scores: INSERT",
    "nickname_claims: INSERT, SELECT",
    "leaderboard: SELECT · rank (rpc)"], kind="net")
P(fx + 10, e + 6, iw, "EDGE FUNCTION", [
    "submit-score: the only leaderboard",
    "writer; checks an ownership token"], kind="maybe", h=B + BH - 8 - e - 6)
lx = fx + 10 + iw + 10
P(lx, B + 46, W - 8 - 10 - lx, "LEAVES THE DEVICE", [
    "**Level record**: nickname,",
    "score, attempts, level, remarks",
    "**Leaderboard**: player ID,",
    "nickname, points, stars, token",
    "**Name claim**: nickname,",
    "installation ID",
    "Never: audio, the attempt log"], kind="white", dash=True, h=BH - 54)
# arrows between device and network
mid = (DEV + B) / 2
s.path([(96, DEV), (96, B - 2)]); s.label(102, mid, "audio", anchor="start", size=S)
s.path([(170, B - 2), (170, DEV + 2)]); s.label(176, mid, "text", anchor="start", size=S)
s.path([(380, DEV), (380, B - 2)]); s.label(372, mid, "level record ·\nname claim", anchor="end", size=S)
s.path([(560, B - 2), (560, DEV + 2)], dash=True); s.label(568, mid, "leaderboard\nrows · rank", anchor="start", size=S, weight="normal")
s.path([(765, DEV), (765, B - 2)]); s.label(757, mid, "totals", anchor="end", size=S)

# ---- legend -------------------------------------------------------------------
y = B + BH + 10
s.add(f'<rect x="8" y="{y}" width="{W-16}" height="{H-y-4}" rx="8" fill="#f7f7f5" stroke="#9ca3af" stroke-width="1.5"/>')
ky = y + 19
def key(x, fill, stroke, dash, text):
    d = ' stroke-dasharray="6 4"' if dash else ''
    s.add(f'<rect x="{x}" y="{ky-9}" width="26" height="18" rx="4" fill="{fill}" stroke="{stroke}" stroke-width="2"{d}/>')
    s.text(x + 34, ky, text, S, anchor="start")
key(22, "#e6f4ea", "#2f7d4a", False, "On the device, no network")
key(286, "#fff7ea", "#b7791f", True, "May use the network")
key(520, "#eef3fb", "#3b5b8c", False, "Network: Supabase")
s.text(22, ky + 23, "Stays on the device: the audio copy (memory only), attempt log, profiles, trained model.", S, anchor="start")
s.save("../Figure_3_4_System_Architecture_and_Network_Dependent_Interactions.svg", display_pt=484)
