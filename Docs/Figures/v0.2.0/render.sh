#!/bin/sh
# Renders every SVG in this folder to a PNG at 2x (needs Google Chrome).
cd "$(dirname "$0")"
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
for f in *.svg; do
  w=$(sed -n 's/.*<svg[^>]* width="\([0-9]*\)" height="\([0-9]*\)".*/\1/p' "$f" | head -1)
  h=$(sed -n 's/.*<svg[^>]* width="\([0-9]*\)" height="\([0-9]*\)".*/\2/p' "$f" | head -1)
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=2 \
    --window-size="$w,$h" --screenshot="$PWD/${f%.svg}.png" "file://$PWD/$f" >/dev/null 2>&1
  echo "$f -> ${f%.svg}.png (${w}x${h} @2x)"
done
