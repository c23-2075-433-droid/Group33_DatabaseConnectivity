* { box-sizing: border-box; }

body {
  margin: 0;
  padding: 16px;
  font-family: "Fredoka", "Trebuchet MS", Arial, sans-serif;
  background: #f6efdc;
  color: #3b2a1a;
  display: flex;
  justify-content: center;
}

.game { width: 100%; max-width: 720px; }
.hidden { display: none !important; }

/* ===== Title bar (wooden sign) ===== */
.title-bar {
  text-align: center;
  background: #7a4f2a;
  border: 4px solid #4e3119;
  border-radius: 14px;
  padding: 10px 16px;
  margin-bottom: 14px;
  color: #fff8e6;
}
.title-bar h1 { margin: 0; font-size: 2.2rem; letter-spacing: 4px; }
.title-bar p { margin: 2px 0 0; font-size: 0.9rem; }

/* ===== Panels and ribbons ===== */
.panel {
  background: #fffaf0;
  border: 4px solid #c9a978;
  border-radius: 16px;
  padding: 18px;
  text-align: center;
}

.ribbon {
  display: inline-block;
  background: #2e9e6b;
  color: #fff;
  font-weight: 700;
  padding: 6px 22px;
  border-radius: 10px;
  margin-bottom: 12px;
  letter-spacing: 1px;
}

.sub { margin: 4px 0 12px; }
.quote { font-style: italic; color: #3b3b9c; font-weight: 600; margin: 10px 0 0; }
.error { color: #c0392b; min-height: 1.2em; margin: 6px 0 0; }

/* ===== Profile ===== */
.profile-body {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
  flex-wrap: wrap;
}
.profile-form { text-align: left; min-width: 240px; }
.profile-form label { display: block; font-weight: 600; margin: 8px 0 3px; }
.profile-form input, .profile-form select {
  width: 100%;
  padding: 10px;
  font-family: inherit;
  font-size: 1rem;
  border: 2px solid #c9a978;
  border-radius: 8px;
  background: #fff;
}
.profile-form .btn { margin-top: 14px; width: 100%; }
.profile-art { font-size: 8rem; line-height: 1; }

/* ===== Buttons ===== */
.btn, .options button {
  font-family: inherit;
  font-weight: 700;
  font-size: 1.05rem;
  color: #fff;
  background: #e8672a;
  border: none;
  border-bottom: 5px solid #b44c17;
  border-radius: 10px;
  padding: 10px 22px;
  cursor: pointer;
}
.btn:hover, .options button:hover { background: #f07a3e; }
.btn:active, .options button:active { transform: translateY(2px); border-bottom-width: 3px; }
.options button:disabled { background: #b9a999; border-bottom-color: #8f8072; cursor: not-allowed; }

/* ===== Learn the words ===== */
.word-cards {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
  margin-bottom: 16px;
}
.word-card {
  background: #fff;
  border: 3px solid #c9a978;
  border-radius: 14px;
  padding: 10px 16px;
  min-width: 120px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.word-card .emoji { font-size: 2.2rem; }
.word-card strong { font-size: 1.5rem; letter-spacing: 1px; }
.word-card span:last-child { color: #6b5a48; }

/* ===== HUD ===== */
.hud {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-weight: 700;
  background: #fffaf0;
  border: 3px solid #c9a978;
  border-radius: 12px;
  padding: 8px 14px;
  margin-bottom: 10px;
}

/* ===== Scene ===== */
.scene {
  position: relative;
  height: 270px;
  overflow: hidden;
  border: 4px solid #4e3119;
  border-radius: 14px;
  background: linear-gradient(#7fcdf5, #d8f1ff);
}

.sun { position: absolute; top: 10px; right: 20px; font-size: 2.6rem; }
.cloud { position: absolute; font-size: 2.6rem; opacity: 0.9; }
.c1 { top: 18px; left: 12%; }
.c2 { top: 50px; left: 55%; }

.hills {
  position: absolute;
  left: 0; right: 0; bottom: 56px;
  height: 100px;
  background:
    radial-gradient(circle at 25% 100%, #74bd62 0 70px, transparent 71px),
    radial-gradient(circle at 75% 100%, #5aa851 0 90px, transparent 91px);
  background-size: 360px 100px;
  background-repeat: repeat-x;
  transition: background-position 0.9s ease;
}

.ground {
  position: absolute;
  left: 0; right: 0; bottom: 0;
  height: 56px;
  background:
    linear-gradient(#58a83c 0 16px, #8a5a2c 16px);
  border-top: 3px solid #3f7e2b;
  transition: background-position 0.9s ease;
}

.character {
  position: absolute;
  left: 12%;
  bottom: 44px;
  font-size: 3.6rem;
  line-height: 1;
  transform-origin: bottom center;
}

.obstacle {
  position: absolute;
  left: 62%;
  bottom: 44px;
  font-size: 3.4rem;
  line-height: 1;
}

.bubble {
  position: absolute;
  left: 8%;
  bottom: 140px;
  background: #fff;
  border: 3px solid #3b2a1a;
  border-radius: 16px;
  padding: 6px 14px;
  font-weight: 700;
  font-size: 1.2rem;
}
.bubble::after {
  content: "";
  position: absolute;
  bottom: -14px;
  left: 30px;
  border-width: 12px 10px 0 10px;
  border-style: solid;
  border-color: #3b2a1a transparent transparent transparent;
}

/* ===== Character and obstacle animations ===== */
.character.jump    { animation: jump 0.9s ease; }
.character.collect { animation: jump 0.9s ease; }
.character.duck    { animation: duck 0.9s ease; }
.character.dash    { animation: dash 0.9s ease; }
.character.walk    { animation: walk 0.9s ease; }
.character.halt    { animation: halt 0.9s ease; }
.character.bump    { animation: shake 0.6s ease; }

.obstacle.enter   { animation: slideIn 0.6s ease; }
.obstacle.cleared { animation: clearAway 0.9s forwards; }
.obstacle.shake   { animation: shake 0.6s ease; }

@keyframes jump {
  0%   { transform: translate(0, 0); }
  45%  { transform: translate(60px, -95px); }
  100% { transform: translate(0, 0); }
}
@keyframes duck {
  0%, 100% { transform: scaleY(1); }
  50%      { transform: scaleY(0.55) translateX(30px); }
}
@keyframes dash {
  0%   { transform: translateX(0); }
  50%  { transform: translateX(130px); }
  100% { transform: translateX(0); }
}
@keyframes walk {
  0%   { transform: translateX(0); }
  50%  { transform: translateX(50px); }
  100% { transform: translateX(0); }
}
@keyframes halt {
  0%, 100% { transform: scale(1); }
  50%      { transform: scale(1.2); }
}
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  25%      { transform: translateX(-8px) rotate(-4deg); }
  75%      { transform: translateX(8px) rotate(4deg); }
}
@keyframes slideIn {
  from { transform: translateX(180px); opacity: 0; }
  to   { transform: translateX(0); opacity: 1; }
}
@keyframes clearAway {
  to { transform: scale(0.2) translateY(-50px); opacity: 0; }
}

/* ===== Question and options ===== */
.question { font-weight: 600; font-size: 1.1rem; text-align: center; margin: 12px 0 8px; }
.options { display: flex; flex-wrap: wrap; gap: 10px; justify-content: center; }
.options button { min-width: 110px; text-transform: uppercase; }
.feedback { min-height: 1.6em; text-align: center; font-weight: 700; font-size: 1.1rem; margin: 10px 0 0; }
.feedback.correct { color: #2e8b3d; }
.feedback.wrong { color: #c0392b; }

/* ===== Completion ===== */
.mission-banner {
  display: inline-block;
  background: #f5b82e;
  color: #7a4f00;
  font-weight: 700;
  font-size: 1.4rem;
  padding: 6px 22px;
  border-radius: 12px;
  border: 3px solid #d99a10;
  margin-bottom: 8px;
}
#finalTitle { margin: 6px 0 12px; color: #2b4c9b; font-size: 2rem; }
.final-card {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 24px;
  background: #fff;
  border: 3px solid #c9a978;
  border-radius: 14px;
  padding: 14px 20px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}
.stats { list-style: none; margin: 0; padding: 0; text-align: left; font-size: 1.1rem; }
.stats li { margin: 6px 0; }
.medal { font-size: 4rem; }
.save-status { color: #777; font-style: italic; min-height: 1.2em; }
