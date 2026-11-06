#!/usr/bin/env bash
# Opens the About window of the published Linux build and captures a screenshot.
# Usage: ./scripts/test-linux-about.sh artifacts/linux-x64/BatchConvertToCompressedFile [output.png]
set -euo pipefail

BINARY="${1:?path to the published Linux executable is required}"
OUTPUT="${2:-linux-about-smoke.png}"

BINARY="$(realpath "$BINARY")"
OUTPUT="$(realpath -m "$OUTPUT")"

xvfb-run -a -s "-screen 0 1400x900x24" bash -c '
  set -euo pipefail
  binary="$1"
  output="$2"

  "$binary" >/tmp/bcctf-about.log 2>&1 &
  app_pid=$!
  cleanup() { kill "$app_pid" 2>/dev/null || true; wait "$app_pid" 2>/dev/null || true; }
  trap cleanup EXIT

  for _ in $(seq 1 20); do
    xdotool search --name "Batch Convert to Compressed File" >/dev/null 2>&1 && break
    sleep 0.5
  done

  wid=$(xdotool search --name "Batch Convert to Compressed File" | head -1)
  xdotool windowfocus "$wid" || true
  xdotool key --window "$wid" alt+h
  sleep 1
  xdotool key a
  sleep 2

  xdotool search --name "About Batch Convert to Compressed File" getwindowgeometry %@
  import -window root "$output"
  echo "Screenshot: $output"
' _ "$BINARY" "$OUTPUT"
