// ===== SALINLAHI Mini-Level: game logic (database not connected yet) =====
// NOTE: The Filipino vocabulary below should be checked by your Filipino validator.

// ----- Vocabulary: meaning, emoji, and the character action for each word -----
const WORDS = {
  // Home
  bukas: { en: "Open",        icon: "door-open",  action: "act" },
  sara:  { en: "Close",       icon: "door",       action: "act" },
  kuha:  { en: "Get / Take",  icon: "hand",       action: "collect" },
  upo:   { en: "Sit",         icon: "chair",      action: "duck" },
  tayo:  { en: "Stand up",    icon: "jump",       action: "act" },
  // School
  sulat: { en: "Write",       icon: "pencil",     action: "act" },
  basa:  { en: "Read",        icon: "book",       action: "act" },
  punta: { en: "Go to",       icon: "pin",        action: "walk" },
  // Park
  lakad: { en: "Walk",        icon: "footprints", action: "walk" },
  takbo: { en: "Run",         icon: "shoe",       action: "dash" },
  talon: { en: "Jump",        icon: "jump",       action: "jump" },
  hinto: { en: "Stop",        icon: "stop",       action: "halt" },
  yuko:  { en: "Bend down",   icon: "bend",       action: "duck" },
  // Store
  bili:  { en: "Buy",         icon: "cart",       action: "collect" },
  bayad: { en: "Pay",         icon: "money",      action: "act" },
  pili:  { en: "Choose",      icon: "check",      action: "act" },
  bigay: { en: "Give",        icon: "gift",       action: "act" }
};

// ----- Environments, each with 3 levels -----
// A question with a single word as its answer = "single" challenge.
// A question with an array as its answer = "sequence" challenge (say the words in order).
const ENVIRONMENTS = [
  {
    id: "home", name: "Home", icon: "home", theme: "Everyday actions", decor: ["frame", "sofa", "window"],
    levels: [
      {
        name: "Living Room", intro: "Learn these words for things you do at home!",
        words: ["bukas", "sara", "kuha"],
        questions: [
          { icon: "door", text: "The door is closed. Which Filipino word means OPEN?", answer: "bukas" },
          { icon: "window", text: "It is raining! Which Filipino word means CLOSE?", answer: "sara" },
          { icon: "teddy", text: "Your toy is on the table. Which Filipino word means GET / TAKE?", answer: "kuha" }
        ]
      },
      {
        name: "Bedroom", intro: "Two new words for your day at home!",
        words: ["upo", "tayo"],
        questions: [
          { icon: "chair", text: "Time to eat! Which Filipino word means SIT?", answer: "upo" },
          { icon: "bed", text: "Good morning! Which Filipino word means STAND UP?", answer: "tayo" },
          { icon: "teddy", text: "Pick up your toy. Which Filipino word means GET / TAKE?", answer: "kuha" }
        ]
      },
      {
        name: "Kitchen", intro: "Combine the words you learned!",
        words: ["bukas", "sara", "kuha", "upo", "tayo"],
        questions: [
          { icon: "door", text: "Keep the cold out! Which Filipino word means CLOSE?", answer: "sara" },
          { icon: "apple", text: "Stand up, get the apple, then sit down. Choose the words in order!", answer: ["tayo", "kuha", "upo"] },
          { icon: "teddy", text: "Open the door, get the toy, then close the door. Choose the words in order!", answer: ["bukas", "kuha", "sara"] }
        ]
      }
    ]
  },
  {
    id: "school", name: "School", icon: "school", theme: "Classroom actions", decor: ["bell", "books", "pencil"],
    levels: [
      {
        name: "Classroom", intro: "Learn these words for your classroom!",
        words: ["sulat", "basa", "punta"],
        questions: [
          { icon: "pencil", text: "Copy the lesson. Which Filipino word means WRITE?", answer: "sulat" },
          { icon: "book", text: "Open your book. Which Filipino word means READ?", answer: "basa" },
          { icon: "teacher", text: "The teacher calls you. Which Filipino word means GO TO?", answer: "punta" }
        ]
      },
      {
        name: "Hallway", intro: "Two new words for the school day!",
        words: ["tayo", "upo"],
        questions: [
          { icon: "bell", text: "Class starts! Which Filipino word means STAND UP?", answer: "tayo" },
          { icon: "chair", text: "Time to listen. Which Filipino word means SIT?", answer: "upo" },
          { icon: "sign", text: "Look at the sign. Which Filipino word means READ?", answer: "basa" }
        ]
      },
      {
        name: "School Yard", intro: "Combine the words you learned!",
        words: ["sulat", "basa", "punta", "tayo", "upo"],
        questions: [
          { icon: "teacher", text: "Walk to the teacher's desk. Which Filipino word means GO TO?", answer: "punta" },
          { icon: "clipboard", text: "Stand up, go to the board, then write. Choose the words in order!", answer: ["tayo", "punta", "sulat"] },
          { icon: "books", text: "Go to your seat, sit down, then read. Choose the words in order!", answer: ["punta", "upo", "basa"] }
        ]
      }
    ]
  },
  {
    id: "park", name: "Park", icon: "tree", theme: "Movement", decor: [],
    levels: [
      {
        name: "Playground", intro: "Learn these movement words!",
        words: ["lakad", "takbo", "talon"],
        questions: [
          { icon: "path", text: "The path is clear. Which Filipino word means WALK?", answer: "lakad" },
          { icon: "dog", text: "A dog is chasing you! Which Filipino word means RUN?", answer: "takbo" },
          { icon: "rock", text: "A rock is in the way! Which Filipino word means JUMP?", answer: "talon" }
        ]
      },
      {
        name: "Garden Path", intro: "Two new words for the park!",
        words: ["hinto", "yuko"],
        questions: [
          { icon: "stop", text: "A stop sign! Which Filipino word means STOP?", answer: "hinto" },
          { icon: "log", text: "A low branch! Which Filipino word means BEND DOWN?", answer: "yuko" },
          { icon: "tulip", text: "Enjoy the flowers. Which Filipino word means WALK?", answer: "lakad" }
        ]
      },
      {
        name: "Pond Trail", intro: "Combine the words you learned!",
        words: ["lakad", "takbo", "talon", "hinto", "yuko"],
        questions: [
          { icon: "duck", text: "A duck is crossing the path! Which Filipino word means STOP?", answer: "hinto" },
          { icon: "rock", text: "Walk, stop, then jump over the rock. Choose the words in order!", answer: ["lakad", "hinto", "talon"] },
          { icon: "tree", text: "Run, bend down under the branch, then jump the puddle. Choose the words in order!", answer: ["takbo", "yuko", "talon"] }
        ]
      }
    ]
  },
  {
    id: "store", name: "Community Store", icon: "store", theme: "Everyday words", decor: ["tag", "cart", "basket"],
    levels: [
      {
        name: "Fruit Stand", intro: "Learn these words for shopping!",
        words: ["pili", "bili", "bayad"],
        questions: [
          { icon: "apple", text: "So many fruits! Which Filipino word means CHOOSE?", answer: "pili" },
          { icon: "cart", text: "You want the apples. Which Filipino word means BUY?", answer: "bili" },
          { icon: "money", text: "Time to pay the cashier. Which Filipino word means PAY?", answer: "bayad" }
        ]
      },
      {
        name: "Market", intro: "Two new words for the market!",
        words: ["bigay", "kuha"],
        questions: [
          { icon: "gift", text: "A gift for your friend! Which Filipino word means GIVE?", answer: "bigay" },
          { icon: "basket", text: "Grab the basket. Which Filipino word means GET / TAKE?", answer: "kuha" },
          { icon: "teddy", text: "You like this toy. Which Filipino word means BUY?", answer: "bili" }
        ]
      },
      {
        name: "Shopping Street", intro: "Combine the words you learned!",
        words: ["pili", "bili", "bayad", "bigay", "kuha"],
        questions: [
          { icon: "receipt", text: "The cashier is waiting. Which Filipino word means PAY?", answer: "bayad" },
          { icon: "apple", text: "Choose the fruit, buy it, then pay. Choose the words in order!", answer: ["pili", "bili", "bayad"] },
          { icon: "bag", text: "Get the bag, pay for it, then give it to your mom. Choose the words in order!", answer: ["kuha", "bayad", "bigay"] }
        ]
      }
    ]
  }
];

const POINTS_PER_CORRECT = 10;
const PASS_RATIO = 0.6;     // need 60% correct to unlock the next level
const STEP_MS = 950;        // time for one character action

// ----- Helpers -----
const $ = id => document.getElementById(id);
// Returns an <img> tag for an icon file in the images folder
function icon(name, cls) {
  return '<img class="icon ' + (cls || "") + '" src="images/' + name + '.svg" alt="">';
}
const SCREENS = ["profileScreen", "envScreen", "levelsScreen", "learnScreen", "playScreen", "resultScreen", "recordsScreen"];

function showScreen(id) {
  SCREENS.forEach(s => $(s).classList.add("hidden"));
  $(id).classList.remove("hidden");
}

function shuffle(arr) {
  const a = arr.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

// Stop waiting for the database after a number of milliseconds
function withTimeout(promise, ms) {
  let timer;
  const timeout = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new Error("Timed out")), ms);
  });
  return Promise.race([promise, timeout]).finally(() => clearTimeout(timer));
}

// Restart a CSS animation class on an element
function setClass(el, base, extra) {
  el.className = base;
  void el.offsetWidth; // force reflow so the animation restarts
  if (extra) el.className = base + " " + extra;
}

// ----- Game state -----
let state = { name: "", envId: null, levelIndex: 0, qIndex: 0, score: 0, correct: 0, seq: [], locked: false, progress: {} };
let bgX = 0;

function currentEnv() { return ENVIRONMENTS.find(e => e.id === state.envId); }
function currentLevel() { return currentEnv().levels[state.levelIndex]; }
function currentQuestion() { return currentLevel().questions[state.qIndex]; }

// ===== 1. Enter nickname =====
$("startBtn").addEventListener("click", async () => {
  const name = $("playerName").value.trim();
  if (name === "") { $("startError").textContent = "Please enter a nickname."; return; }
  $("startError").textContent = "";

  state.name = name;
  state.progress = {};
  ENVIRONMENTS.forEach(e => state.progress[e.id] = 0); // levels passed per environment

  // RETRIEVE: load this player's saved records so unlocked levels are restored
  const btn = $("startBtn");
  btn.disabled = true;
  btn.textContent = "LOADING...";
  try {
    const records = await withTimeout(window.SalinlahiDB.getPlayerRecords(name), 15000);
    state.progress = progressFromRecords(records);
  } catch (err) {
    console.warn("Could not load saved progress:", err);
  }
  btn.disabled = false;
  btn.textContent = "START GAME";
  showEnvs();
});

// A level counts as unlocked when it and all earlier levels were passed
function progressFromRecords(records) {
  const progress = {};
  ENVIRONMENTS.forEach(env => {
    const passed = new Set(
      records.filter(r => r.environment === env.name && r.remarks === "Passed").map(r => r.level)
    );
    let n = 0;
    while (passed.has(n + 1)) n++;
    progress[env.id] = n;
  });
  return progress;
}

// ===== 2. Choose an environment =====
function showEnvs() {
  $("envGreeting").textContent = "Hi, " + state.name + "! Where do you want to learn?";
  const grid = $("envGrid");
  grid.innerHTML = "";
  ENVIRONMENTS.forEach(env => {
    const card = document.createElement("button");
    card.className = "env-card";
    card.innerHTML =
      '<span class="env-emoji">' + icon(env.icon, "big") + '</span>' +
      '<strong>' + env.name + '</strong>' +
      '<span class="env-theme">' + env.theme + '</span>' +
      '<span class="env-progress">Levels passed: ' + state.progress[env.id] + '/' + env.levels.length + '</span>';
    card.addEventListener("click", () => { state.envId = env.id; showLevels(); });
    grid.appendChild(card);
  });
  showScreen("envScreen");
}

$("changeNameBtn").addEventListener("click", () => showScreen("profileScreen"));

// ===== 3. Level list =====
function showLevels() {
  const env = currentEnv();
  $("levelsTitle").innerHTML = icon(env.icon, "inline") + " " + env.name.toUpperCase();
  $("levelsSub").textContent = "Pass a level to unlock the next one.";

  const list = $("levelList");
  list.innerHTML = "";
  env.levels.forEach((lvl, i) => {
    const passed = i < state.progress[env.id];
    const locked = i > state.progress[env.id];
    const btn = document.createElement("button");
    btn.className = "level-btn";
    btn.disabled = locked;
    btn.innerHTML =
      '<span>Level ' + (i + 1) + ': ' + lvl.name + '</span>' +
      '<span>' + icon(locked ? "lock" : passed ? "check" : "play", "inline") + '</span>';
    btn.addEventListener("click", () => { state.levelIndex = i; showLearn(); });
    list.appendChild(btn);
  });
  showScreen("levelsScreen");
}

$("backToEnvBtn").addEventListener("click", showEnvs);

// ===== 4. Learn the words =====
function showLearn() {
  const env = currentEnv();
  const level = currentLevel();
  $("learnTitle").textContent = env.name.toUpperCase() + " • LEVEL " + (state.levelIndex + 1) + ": " + level.name.toUpperCase();
  $("learnSub").textContent = level.intro;

  const box = $("wordCards");
  box.innerHTML = "";
  level.words.forEach(w => {
    const card = document.createElement("div");
    card.className = "word-card";
    card.innerHTML =
      '<span class="emoji">' + icon(WORDS[w].icon, "big") + '</span>' +
      '<strong>' + w.toUpperCase() + '</strong>' +
      '<span>' + WORDS[w].en + '</span>';
    box.appendChild(card);
  });
  showScreen("learnScreen");
}

$("playLevelBtn").addEventListener("click", () => {
  state.qIndex = 0;
  state.score = 0;
  state.correct = 0;
  applyScene();
  showScreen("playScreen");
  showQuestion();
});

// ===== 5. Play =====
function applyScene() {
  const env = currentEnv();
  const indoor = env.id !== "park";
  $("scene").className = "scene " + (indoor ? "indoor " : "") + "scene-" + env.id;
  $("decor").innerHTML = env.decor.map(n => icon(n, "decor-img")).join("");
}

// Words the player can choose from: everything learned so far in this environment
function getPool() {
  const env = currentEnv();
  const set = new Set();
  env.levels.slice(0, state.levelIndex + 1).forEach(l => l.words.forEach(w => set.add(w)));
  currentLevel().questions.forEach(q => [].concat(q.answer).forEach(w => set.add(w)));
  return Array.from(set);
}

function showQuestion() {
  const env = currentEnv();
  const level = currentLevel();
  const q = currentQuestion();
  const isSequence = Array.isArray(q.answer);

  $("hudPlayer").textContent = "Player: " + state.name;
  $("hudLevel").textContent = env.name + " • Level " + (state.levelIndex + 1) + " • " + (state.qIndex + 1) + "/" + level.questions.length;
  $("hudScore").textContent = "Score: " + state.score;

  $("obstacle").innerHTML = icon(q.icon, "obstacle-img");
  setClass($("obstacle"), "obstacle", "enter");
  setClass($("character"), "character");
  $("bubble").classList.add("hidden");

  $("questionText").textContent = q.text;
  $("feedback").textContent = "";
  $("feedback").className = "feedback";

  state.seq = [];
  const box = $("options");
  box.innerHTML = "";
  shuffle(getPool()).forEach(word => {
    const btn = document.createElement("button");
    btn.textContent = word;
    btn.addEventListener("click", () => isSequence ? addToSequence(word) : resolveQuestion([word]));
    box.appendChild(btn);
  });

  if (isSequence) {
    const clear = document.createElement("button");
    clear.className = "clear-btn";
    clear.textContent = "Clear";
    clear.addEventListener("click", () => {
      if (state.locked) return;
      state.seq = [];
      $("bubble").classList.add("hidden");
    });
    box.appendChild(clear);
  }
  state.locked = false;
}

function showBubble(words) {
  $("bubble").innerHTML = icon("mic", "inline") + " " + words.map(w => w.toUpperCase()).join(" → ") + "!";
  $("bubble").classList.remove("hidden");
}

function addToSequence(word) {
  if (state.locked) return;
  const q = currentQuestion();
  state.seq.push(word);
  showBubble(state.seq);
  if (state.seq.length === q.answer.length) resolveQuestion(state.seq.slice());
}

function resolveQuestion(spoken) {
  if (state.locked) return;
  state.locked = true;

  const q = currentQuestion();
  const answers = [].concat(q.answer);
  const isCorrect = spoken.length === answers.length && spoken.every((w, i) => w === answers[i]);

  document.querySelectorAll("#options button").forEach(b => b.disabled = true);
  showBubble(spoken);

  let delay;
  if (isCorrect) {
    state.score += POINTS_PER_CORRECT;
    state.correct += 1;
    $("feedback").textContent = "Tama! Magaling!";
    $("feedback").className = "feedback correct";

    // The character performs each spoken command one after another
    answers.forEach((w, i) => {
      setTimeout(() => {
        setClass($("character"), "character", WORDS[w].action);
        scrollBackground();
      }, i * STEP_MS);
    });
    setTimeout(() => setClass($("obstacle"), "obstacle", "cleared"), (answers.length - 1) * STEP_MS + 400);
    delay = answers.length * STEP_MS + 600;
  } else {
    setClass($("character"), "character", "bump");
    setClass($("obstacle"), "obstacle", "shake");
    $("feedback").textContent = "Mali. The correct answer is: " + answers.map(w => w.toUpperCase()).join(" → ");
    $("feedback").className = "feedback wrong";
    delay = 2200;
  }
  $("hudScore").textContent = "Score: " + state.score;
  setTimeout(nextQuestion, delay);
}

function scrollBackground() {
  bgX -= 140;
  $("hills").style.backgroundPositionX = (bgX * 0.5) + "px";
  $("ground").style.backgroundPositionX = bgX + "px";
}

function nextQuestion() {
  state.qIndex += 1;
  if (state.qIndex < currentLevel().questions.length) {
    showQuestion();
  } else {
    endLevel();
  }
}

// ===== 6. Level result / reward =====
function endLevel() {
  const env = currentEnv();
  const level = currentLevel();
  const total = level.questions.length;
  const needed = Math.ceil(total * PASS_RATIO);
  const passed = state.correct >= needed;
  const levelNumber = state.levelIndex + 1;

  // Unlock the next level if this one was newly passed
  if (passed && levelNumber > state.progress[env.id]) state.progress[env.id] = levelNumber;

  $("resBanner").textContent = passed ? "MISSION COMPLETE!" : "KEEP GOING!";
  $("resTitle").textContent = passed ? "Magaling!" : "Subukan muli!";
  $("resEnv").innerHTML = icon(env.icon, "inline") + " " + env.name + " • Level " + levelNumber + ": " + level.name;
  $("statCorrect").textContent = state.correct + " / " + total;
  $("statScore").textContent = state.score;
  $("medal").innerHTML = icon(passed ? "medal" : "star", "medal-img");

  // Buttons
  const nextBtn = $("nextBtn");
  nextBtn.classList.remove("hidden");
  if (passed && state.levelIndex < env.levels.length - 1) {
    nextBtn.textContent = "NEXT LEVEL";
    nextBtn.onclick = () => { state.levelIndex += 1; showLearn(); };
  } else if (!passed) {
    nextBtn.textContent = "TRY AGAIN";
    nextBtn.onclick = () => showLearn();
  } else {
    nextBtn.classList.add("hidden"); // last level passed
  }

  // The record that will be saved to the database in a later step
  const record = {
    player_name: state.name,
    environment: env.name,
    level: levelNumber,
    score: state.score,
    correct_answers: state.correct,
    total_questions: total,
    remarks: passed ? "Passed" : "Failed"
  };

  showScreen("resultScreen");
  saveRecord(record);
}

$("resLevelsBtn").addEventListener("click", showLevels);
$("resEnvBtn").addEventListener("click", showEnvs);

// ===== SAVE: send the level record to Firebase Firestore =====
async function saveRecord(record) {
  $("saveStatus").textContent = "Saving your progress...";
  try {
    const id = await withTimeout(window.SalinlahiDB.saveRecordToDB(record), 15000);
    $("saveStatus").textContent = "Progress saved! (Record ID: " + id + ")";
  } catch (err) {
    console.error("Save failed:", err);
    $("saveStatus").textContent = "Could not save to the database. Check your internet and Firebase setup.";
  }
}

// ===== RETRIEVE + DISPLAY: My Progress and Top Scores =====
function fillTable(boxId, headers, rows) {
  const box = $(boxId);
  box.innerHTML = "";
  if (rows.length === 0) { box.textContent = "No records yet."; return; }
  const table = document.createElement("table");
  const head = document.createElement("tr");
  headers.forEach(h => { const th = document.createElement("th"); th.textContent = h; head.appendChild(th); });
  table.appendChild(head);
  rows.forEach(r => {
    const tr = document.createElement("tr");
    r.forEach(cell => { const td = document.createElement("td"); td.textContent = cell; tr.appendChild(td); });
    table.appendChild(tr);
  });
  box.appendChild(table);
}

async function showRecords() {
  showScreen("recordsScreen");
  $("myRecords").textContent = "Loading...";
  $("topScores").textContent = "Loading...";
  try {
    const mine = await withTimeout(window.SalinlahiDB.getPlayerRecords(state.name), 15000);
    fillTable("myRecords", ["Environment", "Level", "Score", "Result", "Date"],
      mine.slice(0, 10).map(r => [r.environment, r.level, r.score, r.remarks, new Date(r.created_ms).toLocaleString()]));
  } catch (err) {
    console.error(err);
    $("myRecords").textContent = "Could not load your records.";
  }
  try {
    const top = await withTimeout(window.SalinlahiDB.getTopScores(10), 15000);
    fillTable("topScores", ["Player", "Environment", "Level", "Score"],
      top.map(r => [r.player_name, r.environment, r.level, r.score]));
  } catch (err) {
    console.error(err);
    $("topScores").textContent = "Could not load top scores.";
  }
}

$("progressBtn").addEventListener("click", showRecords);
$("resProgressBtn").addEventListener("click", showRecords);
$("recordsBackBtn").addEventListener("click", showEnvs);