// ===== SALINLAHI Mini-Level: basic game logic (no database yet) =====

const COMMANDS = ["lakad", "talon", "tigil", "yuko", "takbo"];

// Each obstacle asks the player to choose the correct Filipino command
const obstacles = [
  { emoji: "🕳️", prompt: "There is a hole in the ground! What should you do?", answer: "talon" },
  { emoji: "🛑", prompt: "A stop sign! What should you do?", answer: "tigil" },
  { emoji: "🌿", prompt: "A low branch is ahead! What should you do?", answer: "yuko" },
  { emoji: "🛤️", prompt: "The path is clear. What should you do?", answer: "lakad" },
  { emoji: "🐕", prompt: "A dog is chasing you! What should you do?", answer: "takbo" }
];

const POINTS_PER_CORRECT = 20;
const PASSING_CORRECT = 3;
const CURRENT_LEVEL = 1;

let state = { name: "", index: 0, score: 0, correct: 0, locked: false };

// ----- Elements -----
const startScreen = document.getElementById("startScreen");
const gameScreen = document.getElementById("gameScreen");
const resultScreen = document.getElementById("resultScreen");
const playerNameInput = document.getElementById("playerName");
const startError = document.getElementById("startError");
const playerLabel = document.getElementById("playerLabel");
const progressLabel = document.getElementById("progressLabel");
const scoreLabel = document.getElementById("scoreLabel");
const character = document.getElementById("character");
const obstacleEmoji = document.getElementById("obstacleEmoji");
const obstaclePrompt = document.getElementById("obstaclePrompt");
const optionsBox = document.getElementById("options");
const feedback = document.getElementById("feedback");
const resultTitle = document.getElementById("resultTitle");
const resultText = document.getElementById("resultText");
const saveStatus = document.getElementById("saveStatus");

// ----- Screen helper -----
function showScreen(screen) {
  [startScreen, gameScreen, resultScreen].forEach(s => s.classList.add("hidden"));
  screen.classList.remove("hidden");
}

// ----- Start -----
document.getElementById("startBtn").addEventListener("click", () => {
  const name = playerNameInput.value.trim();
  if (name === "") {
    startError.textContent = "Please enter a nickname first.";
    return;
  }
  startError.textContent = "";
  state = { name: name, index: 0, score: 0, correct: 0, locked: false };
  playerLabel.textContent = "Player: " + state.name;
  showScreen(gameScreen);
  showObstacle();
});

// ----- Show the current obstacle -----
function showObstacle() {
  const ob = obstacles[state.index];
  obstacleEmoji.textContent = ob.emoji;
  obstaclePrompt.textContent = ob.prompt;
  progressLabel.textContent = "Obstacle " + (state.index + 1) + "/" + obstacles.length;
  scoreLabel.textContent = "Score: " + state.score;
  feedback.textContent = "";
  feedback.className = "feedback";
  state.locked = false;

  optionsBox.innerHTML = "";
  COMMANDS.forEach(cmd => {
    const btn = document.createElement("button");
    btn.textContent = cmd;
    btn.addEventListener("click", () => checkAnswer(cmd));
    optionsBox.appendChild(btn);
  });
}

// ----- Check the chosen command -----
function checkAnswer(chosen) {
  if (state.locked) return;
  state.locked = true;

  const ob = obstacles[state.index];
  if (chosen === ob.answer) {
    state.score += POINTS_PER_CORRECT;
    state.correct += 1;
    feedback.textContent = "Tama! (Correct)";
    feedback.className = "feedback correct";
    // Move the character forward based on correct answers
    const percent = (state.correct / obstacles.length) * 85;
    character.style.left = percent + "%";
  } else {
    feedback.textContent = "Mali. The answer is '" + ob.answer + "'.";
    feedback.className = "feedback wrong";
  }
  scoreLabel.textContent = "Score: " + state.score;

  setTimeout(nextObstacle, 1200);
}

function nextObstacle() {
  state.index += 1;
  if (state.index < obstacles.length) {
    showObstacle();
  } else {
    finishGame();
  }
}

// ----- End of round -----
function finishGame() {
  const passed = state.correct >= PASSING_CORRECT;
  const remarks = passed ? "Completed" : "Failed";

  // The record that will be saved to the database in a later step
  const record = {
    player_name: state.name,
    score: state.score,
    level: CURRENT_LEVEL,
    correct_commands: state.correct,
    remarks: remarks
  };

  resultTitle.textContent = passed ? "Magaling! Level Completed" : "Subukan muli! (Try again)";
  resultText.textContent =
    state.name + ", you got " + state.correct + " out of " + obstacles.length +
    " correct. Score: " + state.score + ".";

  showScreen(resultScreen);
  saveRecord(record);
}

// ----- PLACEHOLDER: database save (to be replaced when Firestore is connected) -----
function saveRecord(record) {
  console.log("Record ready to save:", record);
  saveStatus.textContent = "(Database not connected yet. Record shown in the browser console.)";
}

// ----- Play again -----
document.getElementById("playAgainBtn").addEventListener("click", () => {
  character.style.left = "0%";
  playerNameInput.value = state.name;
  showScreen(startScreen);
});
