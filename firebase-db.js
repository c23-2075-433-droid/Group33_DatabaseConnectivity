// ===== SALINLAHI: Firebase Firestore connection =====
// Loads the Firebase SDK from Google's CDN and reads your own settings from
// firebase-config.js (NOT uploaded to GitHub). See firebase-config-example.js.

import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.2/firebase-app.js";
import {
  initializeFirestore, collection, doc, setDoc, getDocs,
  query, where, orderBy, limit, serverTimestamp
} from "https://www.gstatic.com/firebasejs/10.12.2/firebase-firestore.js";

const COLLECTION = "player_scores";
let db = null;
let ready = false;

async function init() {
  try {
    const { firebaseConfig } = await import("./firebase-config.js");
    const app = initializeApp(firebaseConfig);
    // Auto-detect long polling helps on networks/browsers where the default connection stalls
    db = initializeFirestore(app, { experimentalAutoDetectLongPolling: true });
    ready = true;
    console.log("Firebase connected.");
  } catch (err) {
    console.error("Firebase is not configured. Create firebase-config.js from firebase-config-example.js.", err);
  }
}
const initPromise = init();

// SAVE: add one record to the player_scores collection
async function saveRecordToDB(record) {
  await initPromise;
  if (!ready) throw new Error("Database is not configured.");
  const ref = doc(collection(db, COLLECTION));           // auto-generated unique ID
  await setDoc(ref, {
    player_id: ref.id,
    ...record,
    created_at: serverTimestamp()
  });
  return ref.id;
}

// RETRIEVE: all records of one player (newest first)
async function getPlayerRecords(playerName) {
  await initPromise;
  if (!ready) throw new Error("Database is not configured.");
  const q = query(collection(db, COLLECTION), where("player_name", "==", playerName));
  const snap = await getDocs(q);
  const rows = snap.docs.map(d => {
    const data = d.data();
    return { ...data, created_ms: data.created_at ? data.created_at.toMillis() : Date.now() };
  });
  rows.sort((a, b) => b.created_ms - a.created_ms);
  return rows;
}

// RETRIEVE: highest scores of all players
async function getTopScores(max) {
  await initPromise;
  if (!ready) throw new Error("Database is not configured.");
  const q = query(collection(db, COLLECTION), orderBy("score", "desc"), limit(max || 10));
  const snap = await getDocs(q);
  return snap.docs.map(d => d.data());
}

// Make the functions available to script.js
window.SalinlahiDB = { saveRecordToDB, getPlayerRecords, getTopScores };