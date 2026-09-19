// ===== SALINLAHI Mini-Level: game logic (database not connected yet) =====

// ----- Game content: 3 levels based on the storyboard -----
const LEVELS = [
  {
    name: "Basic Words",
    intro: "Learn these action words!",
    words: [
      { word: "talon", en: "Jump", emoji: "🦘" },
      { word: "lakad", en: "Walk", emoji: "🚶" },
      { word: "tigil", en: "Stop",  emoji: "✋" }
    ],
    questions: [
      { emoji: "🪨", text: "A rock is in the way! Which Filipino word means JUMP?", answer: "talon", action: "jump" },
      { emoji: "🛤️", text: "The path is clear. Which Filipino word means WALK?", answer: "lakad", action: "walk" },
      { emoji: "🛑", text: "A stop sign! Which Filipino word means STOP?", answer: "tigil", action: "halt" }
    ]
  },
  {
    name: "More Actions",
    intro: "New action words to overcome obstacles!",
    words: [
      { word: "yuko", en: "Bend down", emoji: "🙇" },
      { word: "takbo", en: "Run", emoji: "🏃" },
      { word: "hinto", en: "Halt", emoji: "🚧" }
    ],
    questions: [
      { emoji: "🪵", text: "A low log is ahead! Which Filipino word means BEND DOWN?", answer: "yuko", action: "duck" },
      { emoji: "🐕", text: "A dog is chasing you! Which Filipino word means RUN?", answer: "takbo", action: "dash" },
      { emoji: "🚧", text: "Construction ahead! Which Filipino word means HALT?", answer: "hinto", action: "halt" }
    ]
  },
  {
    name: "Objects",
    intro: "Learn the Filipino names of these objects!",
    words: [
      { word: "aklat", en: "Book", emoji: "📕" },
      { word: "bag", en: "Bag", emoji: "🎒" },
      { word: "lapis", en: "Pencil", emoji: "✏️" },
      { word: "upuan", en: "Chair", emoji: "🪑" }
    ],
    questions: [
      { emoji: "📕", text: "Ano ang tawag dito? (What is this called?)", answer: "aklat", action: "collect" },
      { emoji: "🎒", text: "Ano ang tawag dito? (What is this called?)", answer: "bag", action: "collect" },
      { emoji: "✏️", text: "Ano ang tawag dito? (What is this called?)", answer: "lapis", action: "collect" },
      { emoji: "🪑", text: "Ano ang tawag dito? (What is this called?)", answer: "upuan", action: "collect" }
    ]
  }
];

const POINTS_PER_CORRECT = 10;
const PASS_RATIO = 0.6; // need 60% correct to unlock the next level

let state = {};
let bgX = 0;

// ----- Elements -----
const $ = id => document.getElementById(id);
const SCREENS = ["profileScreen", "learnScreen", "playScreen", "levelResultScreen", "finalScreen"];

function showScreen(id) {
  SCREENS.forEach(s => $(s).classList.add("hidden"));
  $(id).classList.remove("hidden");
}

// ===== 1. Start / Player Profile =====
$("startBtn").addEventListener("click", () => {
  const name = $("playerName").value.trim();
  const age = parseInt($("playerAge").value, 10);
  const learningLevel = $("learningLevel").value;

  if (name === "") { $("startError").textContent = "Please enter your name."; return; }
  if (isNaN(age) || age < 4 || age > 12) { $("startError").textContent = "Please enter an age from 4 to 12."; return; }
  $("startError").textContent = "";

  state = {
    name: name,
    age: age,
    learningLevel: learningLevel,
    levelIndex: 0,
    qIndex: 0,
    score: 0,
    levelCorrect: 0,
    levelsPassed: 0,
    levelReached: 1,
    wordsLearned: new Set(),
    locked: false
  };
  showLearn();
});

// ===== 2. Learn the words =====
function showLearn() {
  const level = LEVELS[state.levelIndex];
  $("learnTitle").textContent = "LEVEL " + (state.levelIndex + 1) + ": " + level.name.toUpperCase();
  $("learnSub").textContent = level.intro;

  const box = $("wordCards");
  box.innerHTML = "";
  level.words.forEach(w => {
    const card = document.createElement("div");
    card.className = "word-card";
    card.innerHTML =
      '<span class="emoji">' + w.emoji + '</span>' +
      '<strong>' + w.word.toUpperCase() + '</strong>' +
      '<span>' + w.en + '</span>';
    box.appendChild(card);
  });
  showScreen("learnScreen");
}

$("playLevelBtn").addEventListener("click", () => {
  state.qIndex = 0;
  state.levelCorrect = 0;
  showScreen("playScreen");
  showQuestion();
});

// ===== 3. Play =====
function shuffle(arr) {
  const a = arr.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

function showQuestion() {
  const level = LEVELS[state.levelIndex];
  const q = level.questions[state.qIndex];

  $("hudPlayer").textContent = "Player: " + state.name;
  $("hudLevel").textContent = "Level " + (state.levelIndex + 1) + " • " + (state.qIndex + 1) + "/" + level.questions.length;
  $("hudScore").textContent = "Score: " + state.score;

  $("obstacle").textContent = q.emoji;
  $("obstacle").className = "obstacle enter";
  $("character").className = "character";
  $("bubble").classList.add("hidden");

  $("questionText").textContent = q.text;
  $("feedback").textContent = "";
  $("feedback").className = "feedback";

  const optionsBox = $("options");
  optionsBox.innerHTML = "";
  shuffle(level.words.map(w => w.word)).forEach(word => {
    const btn = document.createElement("button");
    btn.textContent = word;
    btn.addEventListener("click", () => checkAnswer(word));
    optionsBox.appendChild(btn);
  });
  state.locked = false;
}

function checkAnswer(chosen) {
  if (state.locked) return;
  state.locked = true;

  const q = LEVELS[state.levelIndex].questions[state.qIndex];
  document.querySelectorAll("#options button").forEach(b => b.disabled = true);

  // Show what the player "said" in a speech bubble
  $("bubble").textContent = "🎤 " + chosen.toUpperCase() + "!";
  $("bubble").classList.remove("hidden");

  if (chosen === q.answer) {
    state.score += POINTS_PER_CORRECT;
    state.levelCorrect += 1;
    state.wordsLearned.add(q.answer);
    $("character").className = "character " + q.action;
    $("obstacle").className = "obstacle cleared";
    $("feedback").textContent = "Tama! Magaling!";
    $("feedback").className = "feedback correct";
    scrollBackground();
  } else {
    $("character").className = "character bump";
    $("obstacle").className = "obstacle shake";
    $("feedback").textContent = "Mali. The correct word is '" + q.answer.toUpperCase() + "'.";
    $("feedback").className = "feedback wrong";
  }
  $("hudScore").textContent = "Score: " + state.score;

  setTimeout(nextQuestion, 1500);
}

function scrollBackground() {
  bgX -= 140;
  $("hills").style.backgroundPositionX = (bgX * 0.5) + "px";
  $("ground").style.backgroundPositionX = bgX + "px";
}

function nextQuestion() {
  state.qIndex += 1;
  if (state.qIndex < LEVELS[state.levelIndex].questions.length) {
    showQuestion();
  } else {
    endLevel();
  }
}

// ===== 4. Level result =====
function endLevel() {
  const level = LEVELS[state.levelIndex];
  const total = level.questions.length;
  const needed = Math.ceil(total * PASS_RATIO);
  const passed = state.levelCorrect >= needed;

  state.levelReached = state.levelIndex + 1;
  if (passed) state.levelsPassed += 1;

  $("levelResultTitle").textContent = passed
    ? "Level " + (state.levelIndex + 1) + " Complete!"
    : "Level " + (state.levelIndex + 1) + " not passed";
  $("levelResultText").textContent =
    "You got " + state.levelCorrect + " out of " + total + " correct." +
    (passed ? " Magaling!" : " You need " + needed + " correct to move on.");

  const btn = $("nextBtn");
  if (passed && state.levelIndex < LEVELS.length - 1) {
    btn.textContent = "NEXT LEVEL";
    btn.onclick = () => { state.levelIndex += 1; showLearn(); };
  } else {
    btn.textContent = "SEE RESULTS";
    btn.onclick = finishGame;
  }
  showScreen("levelResultScreen");
}

// ===== 5. Completion / Reward =====
function finishGame() {
  const allPassed = state.levelsPassed === LEVELS.length;
  const remarks = allPassed ? "Completed" : "Failed";

  // This record will be saved to the database in a later step
  const record = {
    player_name: state.name,
    age: state.age,
    learning_level: state.learningLevel,
    score: state.score,
    level: state.levelReached,
    words_learned: state.wordsLearned.size,
    remarks: remarks
  };

  $("finalBanner").textContent = allPassed ? "MISSION COMPLETE!" : "KEEP GOING!";
  $("finalTitle").textContent = allPassed ? "Magaling!" : "Subukan muli!";
  $("statWords").textContent = record.words_learned;
  $("statLevels").textContent = state.levelsPassed + " / " + LEVELS.length;
  $("statScore").textContent = record.score;

  showScreen("finalScreen");
  saveRecord(record);
}

// ===== PLACEHOLDER: database save (replaced when Firestore is connected) =====
function saveRecord(record) {
  console.log("Record ready to save:", record);
  $("saveStatus").textContent = "(Database not connected yet. Record shown in the browser console.)";
}

// ----- Play again -----
$("playAgainBtn").addEventListener("click", () => {
  bgX = 0;
  $("hills").style.backgroundPositionX = "0px";
  $("ground").style.backgroundPositionX = "0px";
  showScreen("profileScreen");
});
