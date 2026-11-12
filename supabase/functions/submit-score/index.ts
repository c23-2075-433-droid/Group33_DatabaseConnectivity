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
// Deploy with JWT verification off - the game sends the project's publishable
// key, which is not a JWT, and the token above is what actually guards a row:
//
//   supabase functions deploy submit-score --no-verify-jwt
//
// SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY are provided by the Edge runtime.
// The service-role key exists only here, on the server. It is never in Unity,
// never in the repository and never in a build.

import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.4";

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

const refuse = (reason: string, status = 400) =>
  json({ ok: false, error: reason }, status);

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

// -------------------------------------------------------------------- entry

Deno.serve(async (request: Request) => {
  if (request.method !== "POST") return refuse("Use POST.", 405);

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

  const db = createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
    { auth: { persistSession: false } },
  );

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

  const since = (Date.now() - Date.parse(current.updated_at)) / 1000;
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

  return json({ ok: true, rank: await rankOf(db, playerId) });
});

/** Where this player now stands. A failure here is not worth refusing over. */
async function rankOf(
  db: ReturnType<typeof createClient>,
  playerId: string,
): Promise<number> {
  const { data, error } = await db.rpc("leaderboard_rank", { p_player_id: playerId });
  if (error || !data || data.length === 0) return 0;
  return data[0].rank ?? 0;
}
