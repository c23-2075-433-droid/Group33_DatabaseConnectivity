#!/bin/sh
# Tools/live_security_tests.sh - re-run the read-only security tests from
# Part VI against OUR OWN Supabase project, and print evidence that is safe
# to screenshot.
#
#   Tools/live_security_tests.sh
#
# The project URL and publishable key are read from the git-ignored
# Assets/Resources/SupabaseConfig.asset in the main checkout. NEITHER IS EVER
# PRINTED: the URL is shown with its project ref masked, and the key not at
# all, so the output can go straight into a report.
#
# Every test here is refused by design, so none of them writes anything:
#   - ids that are not UUIDs are rejected before any row is touched
#   - the one test that uses a real player id sends a deliberately wrong token
#   - the two reads are reads
# There is no test for "insert a score of -500" on purpose. Before the
# hardening migration that insert SUCCEEDS, and player_scores has no delete
# policy, so the junk row could not be removed afterwards. T16 evidences the
# same gap by reading.

set -u

CFG="$HOME/Documents/SALINLAHI/Assets/Resources/SupabaseConfig.asset"
[ -f "$CFG" ] || { echo "live_security_tests: no SupabaseConfig.asset at $CFG"; exit 2; }

URL=$(sed -nE 's/^ *projectUrl: *(.*)$/\1/p' "$CFG" | tr -d '\r')
KEY=$(sed -nE 's/^ *anonKey: *(.*)$/\1/p' "$CFG" | tr -d '\r')
URL=${URL%/}
[ -n "$URL" ] && [ -n "$KEY" ] || { echo "live_security_tests: URL or key missing from the config"; exit 2; }

REF=$(echo "$URL" | sed -E 's#https://([a-z0-9]{4})[a-z0-9]*\.supabase\.co#\1....supabase.co#')
echo "SALINLAHI live security tests - project $REF (our own)"
echo "Reads and refusals only. Nothing here writes a row."
echo

# curl that prints the status and a trimmed body, never the request headers.
call() {
  id="$1"; desc="$2"; shift 2
  tmp=$(mktemp)
  out=$(curl -s --max-time 25 -o "$tmp" -w '%{http_code}' "$@" \
        -H "apikey: $KEY" -H "Authorization: Bearer $KEY" 2>/dev/null)
  body=$(cut -c1-150 < "$tmp"); rm -f "$tmp"
  printf '%-5s %s\n' "$id" "$desc"
  printf '      HTTP %s  %s\n\n' "$out" "$body"
}

# Same, but with no apikey and no Authorization header: what a stranger who
# never had the app can reach.
call_nokey() {
  id="$1"; desc="$2"; shift 2
  tmp=$(mktemp)
  out=$(curl -s --max-time 25 -o "$tmp" -w '%{http_code}' "$@" 2>/dev/null)
  body=$(cut -c1-150 < "$tmp"); rm -f "$tmp"
  printf '%-5s %s\n' "$id" "$desc"
  printf '      HTTP %s  %s\n\n' "$out" "$body"
}

# Print the column names a row came back with, rather than a child's data.
columns() {
  id="$1"; desc="$2"; url="$3"
  tmp=$(mktemp)
  out=$(curl -s --max-time 25 -o "$tmp" -w '%{http_code}' "$url" \
        -H "apikey: $KEY" -H "Authorization: Bearer $KEY" 2>/dev/null)
  cols=$(tr ',' '\n' < "$tmp" | sed -nE 's/.*"([a-z_]+)": ?.*/\1/p' | sort -u | tr '\n' ' ')
  n=$(echo "$cols" | wc -w | tr -d ' ')
  rm -f "$tmp"
  printf '%-5s %s\n' "$id" "$desc"
  printf '      HTTP %s  %s columns returned: %s\n\n' "$out" "$n" "$cols"
}

# --- authorization: what the game's public key may NOT do -------------------

call T2 "GET  /rest/v1/leaderboard_owner?select=*   (the token hashes)" \
  "$URL/rest/v1/leaderboard_owner?select=*"

call T3 "POST /rest/v1/leaderboard  {total_points:999999}  (straight to the table)" \
  -X POST "$URL/rest/v1/leaderboard" -H "Content-Type: application/json" \
  -d '{"player_id":"00000000-0000-4000-8000-000000000000","nickname":"direct","total_points":999999,"total_stars":999}'

# --- the function's own validation, with an id that cannot exist ------------

call_nokey T15 "POST /functions/v1/submit-score  (no apikey header at all)" \
  -X POST "$URL/functions/v1/submit-score" -H "Content-Type: application/json" \
  -d '{"player_id":"not-a-uuid","nickname":"test","total_points":0,"total_stars":0,"token":""}'

# --- the token that owns a row, against a real player ----------------------
# The id is read from the board itself, so this is a real registered player,
# and the token is deliberately wrong. A wrong token is refused before the
# row is read, let alone written.

REAL=$(curl -s "$URL/rest/v1/leaderboard?select=player_id&limit=1" \
       -H "apikey: $KEY" -H "Authorization: Bearer $KEY" \
       | sed -E 's/.*"player_id":"([^"]+)".*/\1/')

if echo "$REAL" | grep -qE '^[0-9a-fA-F-]{36}$'; then
  call T10 "POST /functions/v1/submit-score  (real player, wrong token)" \
    -X POST "$URL/functions/v1/submit-score" -H "Content-Type: application/json" \
    -d "{\"player_id\":\"$REAL\",\"nickname\":\"hijacked\",\"total_points\":1,\"total_stars\":1,\"token\":\"wrong-token\"}"
else
  echo "T10   skipped - the board has no rows yet, so there is no registered player to test against"
  echo
fi

# --- the two findings, side by side ----------------------------------------

columns T16 "GET  /rest/v1/player_scores?select=*                  (finding 2)" \
  "$URL/rest/v1/player_scores?select=*&limit=1"

call T16b "GET  /rest/v1/player_scores?select=player_name,score,attempts,created_at" \
  "$URL/rest/v1/player_scores?select=player_name,score,attempts,created_at&limit=1"

echo "Done. Every request above was refused or was a read."
