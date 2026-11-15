#!/bin/sh
# Tools/secret_scan.sh - refuse to let a credential into the repository.
#
# Why this exists: it did not, and a real ElevenLabs API key was committed to
# this repository on 29 September 2026 (Tools/elevenlabs.key, commit 0250197).
# It was removed from tracking two commits later, which does not remove it from
# history - every clone of this repository still carries it. .gitignore only
# helps a file it already knows about, and that file was created before the
# rule that ignores it.
#
# So this checks instead of trusting, and it runs before a commit exists rather
# than after it has been pushed.
#
# Usage:
#   Tools/secret_scan.sh              scan what is staged (what the hook does)
#   Tools/secret_scan.sh --tracked    scan every tracked file
#   Tools/secret_scan.sh --history    scan every commit in this repository
#
# Install the hook once per clone:
#   git config core.hooksPath Tools/githooks
#
# A matched line is never printed. The point is to say where to look, not to
# copy the secret into a terminal, a log or a screenshot.

set -u

# Patterns for the credential shapes this project actually handles, plus the
# two filenames that are credentials whatever is inside them.
#   sk_...        ElevenLabs API key (the one that leaked)
#   sb_secret_... Supabase secret key - must never leave the server
#   eyJ...        a JWT: the legacy anon key, or worse, a service_role key
#   AIza...       Google API key
#   -----BEGIN ... PRIVATE KEY-----
PATTERN='sk_[A-Za-z0-9]{24,}|sb_secret_[A-Za-z0-9_-]{12,}|eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}|AIza[0-9A-Za-z_-]{30,}|-----BEGIN [A-Z ]*PRIVATE KEY-----'
NAMES='elevenlabs\.key$|\.keystore$|\.jks$|(^|/)SupabaseConfig\.asset$|(^|/)\.env$|(^|/)id_rsa$'

# Files that document these shapes on purpose: the example files show a key's
# first characters so a member knows what to paste where.
ALLOW='\.example\.txt$|\.example$|/secret_scan\.sh$|/githooks/|Docs/Activity9_|Docs/SupabaseSetup\.md$'

mode="${1:---staged}"
found=0

case "$mode" in
  --history)
    echo "Scanning every commit. This takes a moment."
    for rev in $(git rev-list --all); do
      for path in $(git diff-tree --no-commit-id --name-only -r "$rev" 2>/dev/null); do
        echo "$path" | grep -qE "$ALLOW" && continue
        if echo "$path" | grep -qE "$NAMES"; then
          echo "  $rev  $path  (a credential by filename)"
          found=1
          continue
        fi
        git cat-file -e "$rev:$path" 2>/dev/null || continue
        if git show "$rev:$path" 2>/dev/null | grep -qaE "$PATTERN"; then
          echo "  $rev  $path  (matches a credential pattern)"
          found=1
        fi
      done
    done
    ;;
  --tracked)
    files=$(git ls-files)
    ;;
  *)
    files=$(git diff --cached --name-only --diff-filter=ACM)
    ;;
esac

if [ "$mode" != "--history" ]; then
  for path in $files; do
    echo "$path" | grep -qE "$ALLOW" && continue
    [ -f "$path" ] || continue
    if echo "$path" | grep -qE "$NAMES"; then
      echo "  $path  (a credential by filename - it should be git-ignored)"
      found=1
      continue
    fi
    if grep -qaE "$PATTERN" "$path" 2>/dev/null; then
      echo "  $path  (matches a credential pattern)"
      found=1
    fi
  done
fi

if [ "$found" -eq 1 ]; then
  echo ""
  echo "secret_scan: the files above look like they carry a credential."
  echo ""
  echo "If that is right, take it out of the file, put the filename in"
  echo ".gitignore, and ROTATE the key at the service that issued it - a key"
  echo "that has been committed is a key that has to be replaced, because"
  echo "removing it later does not remove it from the history."
  echo ""
  echo "If it is a false positive, add the path to ALLOW in Tools/secret_scan.sh"
  echo "and say in the commit message why it is safe."
  exit 1
fi

echo "secret_scan: clean."
exit 0
