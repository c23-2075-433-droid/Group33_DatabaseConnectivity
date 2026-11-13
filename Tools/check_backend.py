#!/usr/bin/env python3
"""Verify that the leaderboard path still behaves, not merely that it responds.

Activity 10 asks for functional validation before performance measurement, and
for the reason this project keeps running into: a table existing and a screen
existing are not evidence that the feature works. These checks exercise the
same requests the game makes and assert on what comes back.

Nine checks, all read only except two that are MEANT to be refused:

    1. the board can be read with the key the game ships
    2. the board returns only the five columns the panel draws
    3. the rows arrive ordered by the database, not by the client
    4. a known player's rank comes back from the rank function
    5. an unknown player's rank comes back empty rather than wrong
    6. the recent scores read is ordered newest first
    7. the game's key cannot INSERT into the leaderboard table
    8. the game's key cannot UPDATE it either
    9. the token table cannot be read at all

Checks 7 and 8 try a write that Row Level Security must refuse. The row count
is taken before and after, so a pass is proved by the table being unchanged
rather than only by the status code.

Usage
    python Tools/check_backend.py
"""
import json
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

    print()
    print("%d of 9 checks failed" % failures if failures else "all 9 checks passed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
