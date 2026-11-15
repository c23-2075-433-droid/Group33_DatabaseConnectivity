// supabase/functions/submit-score/index.ts
//
// The only thing allowed to write the SALINLAHI leaderboard.
//
// The game cannot write the table itself: Row Level Security gives the anon
// role a select policy and nothing else (see
// supabase/migrations/20261112_leaderboard.sql). Everything that changes a
// row comes through here, where it can be checked first.
//
// What this is actually defending against, and what it is not:
//
//   A player_id is made on the device. It is an identifier, not a secret, and
//   it is not treated as one. The first submission for an id REGISTERS it and
//   gets a token back; every submission after that has to present the token.
//   Only a SHA-256 of it is stored, in a table the anon role cannot read. So
//   learning another child's player_id buys nothing.
//
//   The totals are still the client's word. Nothing here replays a child
//   saying "paa" into a microphone, so no server can know the true score.
//   What it can do is refuse the absurd: totals must be whole and positive,
//   stars cannot exceed what the game has stars to give, points may only go
//   up and only by so much at a time, and a row cannot be rewritten every
//   few milliseconds. That bounds what a forged submission is worth without
//   pretending to a certainty that is not available.
//
// Deploying: the dashboard's editor is enough, and the one setting that has
// to be changed is "Verify JWT with legacy secret", on the function's
// Settings tab. Turn it OFF.
//
// It requires a JWT signed by the project's legacy secret in the
// Authorization header. A legacy anon key is one; an sb_publishable_ key is
// not a JWT at all, and that is what the game ships - so with the setting on,
// a project on the newer keys answers 401 to everything. Off is also what the
// dashboard recommends for a function that does its own checking, which this
// one does: the platform's check was never what protected a row. The token
// below is.
//
// Turning that setting off means the platform checks nothing, so this function
// also checks the game's own publishable key itself - see keyAccepted(). That
// is a gate against traffic that never had the app, not authentication: the key
// ships inside the APK. The token above is still what owns a row.
//
// The service key exists only here, on the server. It is never in Unity,
// never in the repository and never in a build.

import { createClient } from "npm:@supabase/supabase-js@2";

// ----------------------------------------------------------------- limits

/** Longest nickname. The game's input field stops at the same number. */
const MAX_NICKNAME = 16;

/**
 * Every star the game has to give: fifteen playable scenes at five each,
 * with room to spare for the scenes still to be built. Stars are genuinely
 * bounded, so this is a real ceiling rather than a guess.
 */
const MAX_STARS = 200;

/**
 * Most points one submission may add. Points are earned per run and a child
 * may replay, so there is no honest absolute ceiling - but there is an honest
 * ceiling on how much can be earned between two visits to the main menu, even
 * a long offline stretch of them. A forged submission can therefore inch a
 * score up; it cannot jump it to a million.
 */
const MAX_POINT_GAIN = 1000;

/** Least time between two updates of the same row. */
const MIN_SECONDS_BETWEEN_UPDATES = 5;

// ------------------------------------------------------------------ helpers

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });

const refuse = (reason: string, status = 400) => {
  // Every refusal is written to the function's log before it is sent.
  //
  // Nothing watched this before, which meant a flood of forged submissions and
  // an ordinary afternoon looked exactly alike from outside: both were silence.
  // These lines are what make a pattern visible in the Supabase dashboard's
  // function logs - a hundred "token does not own this player" in a minute is
  // somebody working through player ids, and it should be possible to see that.
  //
  // What is logged is the reason and the status, never the token and never the
  // nickname. A player id is logged as its first eight characters: enough to
  // tell repeated attempts on one row from attempts spread across many,
  // without writing children's identifiers into a log in full.
  console.warn(JSON.stringify({ event: "refused", status, reason }));
  return json({ ok: false, error: reason }, status);
};

/** A player id, shortened for the log. Enough to group by, not enough to use. */
const shortId = (id: string) => (id.length >= 8 ? id.slice(0, 8) + "…" : "?");

/** Something worth seeing in the log that is not a refusal. */
const note = (event: string, fields: Record<string, unknown> = {}) =>
  console.log(JSON.stringify({ event, ...fields }));

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

async function sha256(value: string): Promise<string> {
  const bytes = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(bytes))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}

function newToken(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return btoa(String.fromCharCode(...bytes))
    .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

/** Constant-time compare, so a wrong token cannot be found a byte at a time. */
function sameSecret(a: string, b: string): boolean {
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

/**
 * A nickname a child typed, reduced to something safe to show other children.
 * Control characters out, whitespace collapsed, length capped. Returns null
 * when nothing usable is left.
 */
function cleanNickname(raw: unknown): string | null {
  if (typeof raw !== "string") return null;
  const cleaned = raw
    // deno-lint-ignore no-control-regex
    .replace(/[\u0000-\u001f\u007f-\u009f]/g, " ")
    .replace(/\s+/g, " ")
    .trim()
    .slice(0, MAX_NICKNAME);
  return cleaned.length > 0 ? cleaned : null;
}

function wholeNumber(value: unknown, ceiling: number): number | null {
  if (typeof value !== "number" || !Number.isInteger(value)) return null;
  if (value < 0 || value > ceiling) return null;
  return value;
}

/**
 * Whether this request carries the game's own publishable key.
 *
 * Turning off "Verify JWT with legacy secret" is what lets a project on
 * sb_publishable_ keys call this function at all (see the note at the top), and
 * the cost of it is that the platform then checks nothing: the endpoint answers
 * anybody on the internet. The token design holds up under that - a stranger
 * cannot change a row they have no token for, and the live tests in
 * Docs/Activity9_Security.md show it refusing exactly that - but the
 * registration branch below needs no token by definition, so with no gate at
 * all a stranger can put rows on a board that children read.
 *
 * So the key the game sends is checked here. It is a gate and not
 * authentication, and the difference matters: the key ships inside the APK and
 * anyone who unpacks one can read it. What it stops is unauthenticated traffic
 * that never had the app - which is what arrives at a public URL.
 *
 * Set it as a function secret named SALINLAHI_PUBLISHABLE_KEY (Edge Functions →
 * Secrets in the dashboard). While it is unset the gate is open and says so in
 * the log, so deploying this code cannot lock the game out of its own
 * leaderboard; setting the secret is what switches the check on.
 */
function keyAccepted(request: Request): boolean {
  const expected = Deno.env.get("SALINLAHI_PUBLISHABLE_KEY");
  if (!expected) {
    note("gate-open", {
      detail: "SALINLAHI_PUBLISHABLE_KEY is not set; the key is not being checked.",
    });
    return true;
  }
  const sent = request.headers.get("apikey") ??
    (request.headers.get("authorization") ?? "").replace(/^Bearer\s+/i, "");
  return sameSecret(sent, expected);
}

// -------------------------------------------------------------------- entry

Deno.serve(async (request: Request) => {
  if (request.method !== "POST") return refuse("Use POST.", 405);
  if (!keyAccepted(request)) return refuse("This request did not come from the game.", 401);

  let body: Record<string, unknown>;
  try {
    body = await request.json();
  } catch {
    return refuse("The body is not JSON.");
  }

  const playerId = typeof body.player_id === "string" ? body.player_id : "";
  if (!UUID.test(playerId)) return refuse("player_id must be a UUID.");

  const nickname = cleanNickname(body.nickname);
  if (nickname === null) return refuse("A nickname is required.");

  const points = wholeNumber(body.total_points, Number.MAX_SAFE_INTEGER);
  const stars = wholeNumber(body.total_stars, MAX_STARS);
  if (points === null) return refuse("total_points must be a whole number, zero or more.");
  if (stars === null) return refuse("total_stars is outside what the game can award.");

  const token = typeof body.token === "string" ? body.token : "";

  const db = createClient(Deno.env.get("SUPABASE_URL")!, serviceKey(), {
    auth: { persistSession: false },
  });

  const { data: owner, error: ownerError } = await db
    .from("leaderboard_owner")
    .select("token_hash")
    .eq("player_id", playerId)
    .maybeSingle();

  if (ownerError) return refuse("Could not read the player record.", 500);

  // ---- first time this player has ever submitted: register them ----

  if (!owner) {
    if (points > MAX_POINT_GAIN) {
      return refuse("That is more points than a new player can arrive with.");
    }

    const issued = newToken();
    const { error: insertError } = await db.from("leaderboard").insert({
      player_id: playerId,
      nickname,
      total_points: points,
      total_stars: stars,
    });
    // A duplicate key means two devices raced with the same id, which should
    // not happen and is not something to paper over by overwriting a row.
    if (insertError) return refuse("Could not register this player.", 409);

    const { error: tokenError } = await db.from("leaderboard_owner").insert({
      player_id: playerId,
      token_hash: await sha256(issued),
    });
    if (tokenError) {
      // Without an owner row nothing could ever update this score again, so
      // take the half-made player back out rather than leave it stranded.
      await db.from("leaderboard").delete().eq("player_id", playerId);
      return refuse("Could not register this player.", 500);
    }

    // A registration is the one write that needs no token, so it is the one
    // worth being able to count afterwards.
    note("registered", { player: shortId(playerId), points, stars });
    return json({ ok: true, token: issued, rank: await rankOf(db, playerId) });
  }

  // ---- a player who is already on the board ----

  if (!token) return refuse("This player is registered; a token is required.", 403);
  if (!sameSecret(await sha256(token), owner.token_hash)) {
    return refuse("That token does not own this player.", 403);
  }

  const { data: current, error: currentError } = await db
    .from("leaderboard")
    .select("total_points, total_stars, updated_at")
    .eq("player_id", playerId)
    .maybeSingle();

  if (currentError || !current) return refuse("Could not read the current score.", 500);

  // An unparseable timestamp is not a reason to wave a submission through:
  // without a readable "last updated" there is no window to measure, and the
  // rate limit is the only thing standing between one row and a tight loop.
  // Postgres always answers with an ISO timestamp, so this is the branch that
  // should never run - which is exactly why it should not silently allow.
  const since = (Date.now() - Date.parse(current.updated_at)) / 1000;
  if (!Number.isFinite(since)) {
    return refuse("Could not read when this score was last saved.", 500);
  }
  if (since < MIN_SECONDS_BETWEEN_UPDATES) {
    return refuse("Too many updates. Try again in a moment.", 429);
  }

  // Totals are running counts, so they never fall. One that does is a stale
  // device rather than an attack - a second phone with an older save - and
  // the right answer either way is to keep the higher figure.
  const nextPoints = Math.max(points, current.total_points);
  const nextStars = Math.max(stars, current.total_stars);

  if (nextPoints - current.total_points > MAX_POINT_GAIN) {
    return refuse("That is a bigger jump in points than the game can award at once.");
  }

  const { error: updateError } = await db
    .from("leaderboard")
    .update({
      nickname,                       // a rename updates this row, never adds one
      total_points: nextPoints,
      total_stars: nextStars,
      updated_at: new Date().toISOString(),
    })
    .eq("player_id", playerId);

  if (updateError) return refuse("Could not save the score.", 500);

  note("updated", {
    player: shortId(playerId),
    points: nextPoints,
    stars: nextStars,
    gained: nextPoints - current.total_points,
  });
  return json({ ok: true, rank: await rankOf(db, playerId) });
});

/**
 * The key that may write this table, from whichever scheme the project is on.
 *
 * Both are set by the Edge runtime with nothing to configure. Newer projects
 * carry SUPABASE_SECRET_KEYS, a JSON dictionary whose "default" entry is the
 * one that bypasses RLS; older ones carry SUPABASE_SERVICE_ROLE_KEY as a
 * plain string. Reading both means this function does not care which kind of
 * project it is deployed to.
 */
function serviceKey(): string {
  const legacy = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  if (legacy) return legacy;
  try {
    return JSON.parse(Deno.env.get("SUPABASE_SECRET_KEYS") ?? "{}")["default"] ?? "";
  } catch {
    return "";
  }
}

/** Where this player now stands. A failure here is not worth refusing over. */
async function rankOf(
  db: ReturnType<typeof createClient>,
  playerId: string,
): Promise<number> {
  const { data, error } = await db.rpc("leaderboard_rank", { p_player_id: playerId });
  if (error || !data || data.length === 0) return 0;
  return data[0].rank ?? 0;
}
