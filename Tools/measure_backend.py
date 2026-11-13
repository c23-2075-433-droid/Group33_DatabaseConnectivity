#!/usr/bin/env python3
"""Time the backend calls SALINLAHI actually makes, phase by phase.

Activity 10 asks for a measured performance baseline rather than an assumed
one. The game's only network traffic is the leaderboard: the board is read
straight off PostgREST, a rank is asked for with an RPC when the player is
outside the fetched page, the nickname check reads existing names, and a score
is submitted through the submit-score Edge Function. This script issues those
same requests, against the same project, with the same publishable key, and
splits each one into DNS, TCP, TLS and server time so the result says where
the quarter of a second goes instead of only that it went.

It never writes a row. The submit-score call is sent with a deliberately
invalid player_id, which the function refuses at its first validation check,
before it opens a database connection. Everything else is a read.

Usage
    python Tools/measure_backend.py [runs] [--csv Docs/perf/<name>.csv]

Reads Assets/Resources/SupabaseConfig.asset, which is gitignored and holds the
project URL and the publishable key. The key is used as a request header and is
never printed.
"""
import json
import re
import socket
import ssl
import statistics
import sys
import time
from pathlib import Path
from urllib.parse import urlsplit

REPO = Path(__file__).resolve().parent.parent
ASSET = REPO / "Assets" / "Resources" / "SupabaseConfig.asset"


def read_config():
    """Pull the URL, key and table names out of the Unity asset."""
    if not ASSET.exists():
        sys.exit("No SupabaseConfig.asset. Create it with "
                 "Tools > SALINLAHI > Create Supabase Config, then run this again.")
    text = ASSET.read_text(encoding="utf-8", errors="replace")

    def field(name, default=""):
        found = re.search(r"^\s*%s:\s*(.*)$" % name, text, re.M)
        return found.group(1).strip() if found else default

    url = field("projectUrl").rstrip("/")
    key = field("anonKey")
    if not url or not key:
        sys.exit("SupabaseConfig.asset has no project URL or key.")
    return {
        "url": url,
        "key": key,
        "scores": field("tableName", "player_scores"),
        "board": field("leaderboardTable", "leaderboard"),
        "submit": field("submitScoreFunction", "submit-score"),
        "rank": field("rankFunction", "leaderboard_rank"),
    }


def dechunk(raw):
    """Strip HTTP chunked framing, which PostgREST uses for these replies.

    Without this the measured size would count the chunk headers as payload
    and the JSON would not parse, so the rank call below would silently never
    be timed. It was, once.
    """
    body, rest = b"", raw
    while True:
        line, _, rest = rest.partition(b"\r\n")
        try:
            size = int(line.split(b";")[0], 16)
        except ValueError:
            return body + line + rest
        if size == 0:
            return body
        body += rest[:size]
        rest = rest[size + 2:]


class Call:
    """One request, timed in four parts.

    Opening a connection is separate from sending on it, because that is the
    whole question: a fresh TLS handshake costs more than the query behind it,
    so a second request on the same connection is not a second quarter second.
    """

    def __init__(self, host, port=443):
        self.host = host
        self.port = port
        self.dns_ms = self.tcp_ms = self.tls_ms = 0.0
        self.sock = None

    def open(self):
        start = time.perf_counter()
        info = socket.getaddrinfo(self.host, self.port, socket.AF_INET, socket.SOCK_STREAM)
        after_dns = time.perf_counter()
        raw = socket.socket(info[0][0], socket.SOCK_STREAM)
        raw.settimeout(15)
        raw.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        raw.connect(info[0][4])
        after_tcp = time.perf_counter()
        context = ssl.create_default_context()
        self.sock = context.wrap_socket(raw, server_hostname=self.host)
        after_tls = time.perf_counter()
        self.dns_ms = (after_dns - start) * 1000
        self.tcp_ms = (after_tcp - after_dns) * 1000
        self.tls_ms = (after_tls - after_tcp) * 1000
        return self

    def send(self, method, path, key, body=None, extra=None):
        """Returns (server_ms, status, bytes). server_ms is request to last byte."""
        headers = [
            "%s %s HTTP/1.1" % (method, path),
            "Host: %s" % self.host,
            "apikey: %s" % key,
            "Accept: application/json",
            "Connection: keep-alive",
        ]
        if extra:
            headers.extend(extra)
        payload = b""
        if body is not None:
            payload = body.encode("utf-8")
            headers.append("Content-Type: application/json")
            headers.append("Content-Length: %d" % len(payload))
        request = ("\r\n".join(headers) + "\r\n\r\n").encode("ascii") + payload

        start = time.perf_counter()
        self.sock.sendall(request)
        head = b""
        while b"\r\n\r\n" not in head:
            chunk = self.sock.recv(4096)
            if not chunk:
                break
            head += chunk
        head, _, rest = head.partition(b"\r\n\r\n")
        status = int(head.split(b" ")[1]) if head else 0
        length = 0
        found = re.search(rb"Content-Length:\s*(\d+)", head, re.I)
        if found:
            length = int(found.group(1))
            while len(rest) < length:
                chunk = self.sock.recv(8192)
                if not chunk:
                    break
                rest += chunk
            body = rest[:length]
        elif re.search(rb"Transfer-Encoding:\s*chunked", head, re.I):
            while not rest.endswith(b"0\r\n\r\n"):
                chunk = self.sock.recv(8192)
                if not chunk:
                    break
                rest += chunk
            body = dechunk(rest)
            length = len(body)
        else:
            body = rest
            length = len(body)
        done = time.perf_counter()
        return (done - start) * 1000, status, length, body

    def close(self):
        try:
            if self.sock:
                self.sock.close()
        except OSError:
            pass


def summarise(name, totals, parts, status, size):
    totals = sorted(totals)
    row = {
        "metric": name,
        "n": len(totals),
        "min_ms": round(totals[0], 1),
        "median_ms": round(statistics.median(totals), 1),
        "mean_ms": round(statistics.fmean(totals), 1),
        "max_ms": round(totals[-1], 1),
        "http": status,
        "bytes": size,
    }
    row.update({k: round(statistics.median(v), 1) for k, v in parts.items()})
    return row


def main():
    runs = 20
    csv_path = None
    args = sys.argv[1:]
    for i, arg in enumerate(args):
        if arg == "--csv" and i + 1 < len(args):
            csv_path = Path(args[i + 1])
        elif arg.isdigit():
            runs = int(arg)

    cfg = read_config()
    host = urlsplit(cfg["url"]).hostname
    board_path = ("/rest/v1/%s?select=player_id,nickname,total_points,total_stars,updated_at"
                  "&order=total_points.desc,total_stars.desc,updated_at.asc&limit=10" % cfg["board"])
    rank_path = "/rest/v1/rpc/%s" % cfg["rank"]
    names_path = "/rest/v1/%s?select=player_name&player_name=ilike.Kylo*&limit=200" % cfg["scores"]
    recent_path = "/rest/v1/%s?select=*&order=created_at.desc&limit=20" % cfg["scores"]
    submit_path = "/functions/v1/%s" % cfg["submit"]

    print("host : %s" % host)
    print("runs : %d per metric" % runs)
    print()

    results = []

    # 1. The board, on a fresh connection every time: what a child waits for
    #    when the LEADERBOARDS plate is tapped after the app has just started.
    totals, parts = [], {"dns_ms": [], "tcp_ms": [], "tls_ms": [], "server_ms": []}
    status = size = 0
    warm_totals = []
    for _ in range(runs):
        call = Call(host).open()
        server_ms, status, size, body = call.send("GET", board_path, cfg["key"])
        totals.append(call.dns_ms + call.tcp_ms + call.tls_ms + server_ms)
        for key, value in (("dns_ms", call.dns_ms), ("tcp_ms", call.tcp_ms),
                           ("tls_ms", call.tls_ms), ("server_ms", server_ms)):
            parts[key].append(value)
        # 2. The rank RPC the board asks for next, on the connection already
        #    open. This is the cost of the second call, not of a second
        #    handshake.
        player = ""
        try:
            rows = json.loads(body.decode("utf-8"))
            player = rows[0]["player_id"] if rows else ""
        except (ValueError, KeyError, IndexError):
            player = ""
        if player:
            warm_ms, _, _, _ = call.send("POST", rank_path, cfg["key"],
                                         '{"p_player_id":"%s"}' % player)
            warm_totals.append(warm_ms)
        call.close()
    results.append(summarise("board_fetch_cold", totals, parts, status, size))
    if warm_totals:
        results.append(summarise("rank_rpc_warm", warm_totals,
                                 {"dns_ms": [0], "tcp_ms": [0], "tls_ms": [0],
                                  "server_ms": warm_totals}, 200, 0))

    # 3. The other two reads, and the function, each on a fresh connection.
    for name, method, path, body in (
            ("nickname_lookup_cold", "GET", names_path, None),
            ("recent_scores_cold", "GET", recent_path, None),
            ("submit_function_refusal", "POST", submit_path, '{"player_id":"not-a-uuid"}')):
        totals, parts = [], {"dns_ms": [], "tcp_ms": [], "tls_ms": [], "server_ms": []}
        status = size = 0
        for _ in range(runs):
            call = Call(host).open()
            server_ms, status, size, _ = call.send(method, path, cfg["key"], body)
            totals.append(call.dns_ms + call.tcp_ms + call.tls_ms + server_ms)
            for key, value in (("dns_ms", call.dns_ms), ("tcp_ms", call.tcp_ms),
                               ("tls_ms", call.tls_ms), ("server_ms", server_ms)):
                parts[key].append(value)
            call.close()
        results.append(summarise(name, totals, parts, status, size))

    head = ("metric", "n", "min_ms", "median_ms", "mean_ms", "max_ms",
            "dns_ms", "tcp_ms", "tls_ms", "server_ms", "bytes", "http")
    widths = [24, 3, 9, 10, 9, 9, 8, 8, 9, 10, 7, 5]
    print("".join(str(h).rjust(w) for h, w in zip(head, widths)))
    for row in results:
        print("".join(str(row.get(h, "")).rjust(w) for h, w in zip(head, widths)))

    if csv_path:
        csv_path.parent.mkdir(parents=True, exist_ok=True)
        lines = [",".join(head)]
        lines += [",".join(str(row.get(h, "")) for h in head) for row in results]
        csv_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        print()
        print("wrote %s" % csv_path)


if __name__ == "__main__":
    main()
