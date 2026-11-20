#!/usr/bin/env python3
"""Verify that the leaderboard path still behaves, not merely that it responds.

Activity 10 asks for functional validation before performance measurement, and
for the reason this project keeps running into: a table existing and a screen
existing are not evidence that the feature works. These checks exercise the
same requests the game makes and assert on what comes back.

Thirteen checks. All read only except two that are MEANT to be refused, and
the four nickname-claim ones, which insert a throwaway name on purpose - see
check 10.

    1. the board can be read with the key the game ships
    2. the board returns only the five columns the panel draws
    3. the rows arrive ordered by the database, not by the client
    4. a known player's rank comes back from the rank function
    5. an unknown player's rank comes back empty rather than wrong
    6. the recent scores read is ordered newest first
    7. the game's key cannot INSERT into the leaderboard table
    8. the game's key cannot UPDATE it either
    9. the token table cannot be read at all
   10. a free nickname can be claimed
   11. claiming the same nickname twice is refused
   12. the refusal ignores case, so "Kylo" and "kylo" are one name
   13. the game's key cannot delete a claim and free the name again

Checks 10 to 13 are the only ones here that write. They claim one name of the
form ztest<6 hex> per run, and that row stays: the claims table deliberately
has no delete policy, which is check 13. One junk row per run is the price of
testing the thing that matters - that the second child really is refused -
against the live database rather than against a description of it.

Checks 7 and 8 try a write that Row Level Security must refuse. The row count
is taken before and after, so a pass is proved by the table being unchanged
rather than only by the status code.

Usage
    python Tools/check_backend.py
"""
import json
import secrets
import sys
from pathlib import Path
from urllib.parse import urlsplit

sys.path.insert(0, str(Path(__file__).resolve().parent))
from measure_backend import Call, read_config  # noqa: E402

PASS, FAIL = "PASS", "FAIL"
failures = 0


def report(name, ok, detail):
    global failures
    if not ok:
        failures += 1
    print("%s  %-46s %s" % (PASS if ok else FAIL, name, detail))


def request(host, key, method, path, body=None, extra=None):
    call = Call(host).open()
    try:
        _, status, size, payload = call.send(method, path, key, body, extra)
        return status, size, payload
    finally:
        call.close()


def count(host, key, table):
    status, _, payload = request(host, key, "GET", "/rest/v1/%s?select=player_id" % table)
    if status != 200:
        return None
    try:
        return len(json.loads(payload.decode("utf-8")))
    except ValueError:
        return None


def main():
    cfg = read_config()
    host = urlsplit(cfg["url"]).hostname
    print("host : %s" % host)
    print()

    board_path = ("/rest/v1/%s?select=player_id,nickname,total_points,total_stars,updated_at"
                  "&order=total_points.desc,total_stars.desc,updated_at.asc&limit=10" % cfg["board"])

    # 1. the board reads at all
    status, size, payload = request(host, cfg["key"], "GET", board_path)
    rows = []
    try:
        rows = json.loads(payload.decode("utf-8"))
    except ValueError:
        pass
    report("board read with the shipped key",
           status == 200 and isinstance(rows, list),
           "HTTP %s, %d row(s), %d bytes" % (status, len(rows), size))

    # 2. only what the panel draws comes back
    allowed = {"player_id", "nickname", "total_points", "total_stars", "updated_at"}
    extra_keys = set()
    for row in rows:
        extra_keys |= set(row.keys()) - allowed
    report("board returns only the five display columns",
           not extra_keys,
           "no other column" if not extra_keys else "also returned %s" % sorted(extra_keys))

    # 3. the database did the ordering
    keys = [(-row.get("total_points", 0), -row.get("total_stars", 0)) for row in rows]
    report("board ordered points then stars, by the database",
           keys == sorted(keys),
           "%d row(s) in order" % len(rows))

    # 4 and 5. the rank function
    if rows:
        who = rows[0]["player_id"]
        status, _, payload = request(host, cfg["key"], "POST", "/rest/v1/rpc/%s" % cfg["rank"],
                                     '{"p_player_id":"%s"}' % who)
        ranked = []
        try:
            ranked = json.loads(payload.decode("utf-8"))
        except ValueError:
            pass
        report("rank function answers for a player on the board",
               status == 200 and len(ranked) == 1 and ranked[0].get("rank") == 1,
               "HTTP %s, rank %s for the top row" % (status, ranked[0].get("rank") if ranked else "none"))
    else:
        report("rank function answers for a player on the board", False, "the board is empty")

    status, _, payload = request(host, cfg["key"], "POST", "/rest/v1/rpc/%s" % cfg["rank"],
                                 '{"p_player_id":"00000000-0000-0000-0000-000000000000"}')
    report("rank function returns nothing for an unknown player",
           status == 200 and payload.decode("utf-8").strip() == "[]",
           "HTTP %s, body %s" % (status, payload.decode("utf-8").strip()[:20]))

    # 6. the recent scores read
    status, size, payload = request(host, cfg["key"], "GET",
                                    "/rest/v1/%s?select=*&order=created_at.desc&limit=20" % cfg["scores"])
    stamps = []
    try:
        stamps = [row["created_at"] for row in json.loads(payload.decode("utf-8"))]
    except (ValueError, KeyError):
        pass
    report("recent scores ordered newest first",
           status == 200 and stamps == sorted(stamps, reverse=True),
           "HTTP %s, %d row(s), %d bytes" % (status, len(stamps), size))

    # 7, 8 and 9. what the key must NOT be able to do
    before = count(host, cfg["key"], cfg["board"])
    status, _, payload = request(
        host, cfg["key"], "POST", "/rest/v1/%s" % cfg["board"],
        '{"player_id":"11111111-1111-1111-1111-111111111111","nickname":"rls-probe",'
        '"total_points":999999,"total_stars":999}')
    after = count(host, cfg["key"], cfg["board"])
    report("the game's key cannot insert a leaderboard row",
           status in (401, 403, 404, 405) and before == after,
           "HTTP %s, rows %s before and %s after" % (status, before, after))

    status, _, _ = request(
        host, cfg["key"], "PATCH",
        "/rest/v1/%s?player_id=eq.%s" % (cfg["board"], rows[0]["player_id"] if rows else "x"),
        '{"total_points":999999}')
    after = count(host, cfg["key"], cfg["board"])
    unchanged = True
    if rows:
        status2, _, payload2 = request(host, cfg["key"], "GET", board_path)
        try:
            unchanged = json.loads(payload2.decode("utf-8"))[0]["total_points"] == rows[0]["total_points"]
        except (ValueError, KeyError, IndexError):
            unchanged = False
    # PostgREST answers a refused UPDATE with 204, not 401: with no update
    # policy the statement matches zero rows and succeeds at changing nothing.
    # So the status is not the evidence here. The row is.
    report("the game's key cannot change a leaderboard row",
           unchanged,
           "HTTP %s, and the top row's points are unchanged" % status)

    status, _, payload = request(host, cfg["key"], "GET", "/rest/v1/leaderboard_owner?select=*")
    leaked = []
    if status == 200:
        try:
            leaked = json.loads(payload.decode("utf-8"))
        except ValueError:
            leaked = []
    report("no token hash is readable with the game's key",
           status in (401, 403, 404) or not leaked,
           "HTTP %s, %d row(s) returned" % (status, len(leaked)))

    # 10 to 13. the nickname claim, which is what stops two children sharing a
    # name. The insert IS the check: the game never asks whether a name is
    # free, it asks the database to decide, and these four assert that the
    # database does.
    claims = "/rest/v1/%s" % cfg["claims"]
    name = "ztest" + secrets.token_hex(3)
    device = "22222222-2222-2222-2222-222222222222"
    body = '{"nickname":"%s","device_id":"%s"}' % (name, device)

    status, _, payload = request(host, cfg["key"], "POST", claims, body,
                                 ["Prefer: return=minimal"])
    first_ok = status in (200, 201, 204)
    report("a free nickname can be claimed",
           first_ok,
           "HTTP %s for %s" % (status, name))

    status, _, payload = request(host, cfg["key"], "POST", claims, body,
                                 ["Prefer: return=minimal"])
    report("claiming the same nickname twice is refused",
           status == 409,
           "HTTP %s on the second claim (409 is the unique index saying no)" % status)

    shouting = '{"nickname":"%s","device_id":"%s"}' % (name.upper(), device)
    status, _, payload = request(host, cfg["key"], "POST", claims, shouting,
                                 ["Prefer: return=minimal"])
    report("the refusal ignores case",
           status == 409,
           "HTTP %s for %s" % (status, name.upper()))

    status, _, _ = request(host, cfg["key"], "DELETE",
                           "%s?nickname=eq.%s" % (claims, name))
    _, _, payload = request(host, cfg["key"], "GET",
                            "%s?select=nickname&nickname=eq.%s" % (claims, name))
    still_there = []
    try:
        still_there = json.loads(payload.decode("utf-8"))
    except ValueError:
        pass
    # As with the leaderboard UPDATE above, the status is not the evidence: with
    # no delete policy the statement matches zero rows and succeeds at deleting
    # nothing. The row still being there is.
    report("the game's key cannot free a claimed nickname",
           bool(still_there),
           "HTTP %s, and the claim is %s" % (
               status, "still there" if still_there else "gone - or was never made"))

    print()
    print("%d of 13 checks failed" % failures if failures else "all 13 checks passed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
