#!/usr/bin/env bash
# Smoke-test the published Linux build in a virtual X server.
#
# Usage:
#   ./scripts/test-linux-ui.sh artifacts/linux-x64/BatchConvertToCompressedFile [output.png]
set -euo pipefail

BINARY="${1:?path to the published Linux executable is required}"
OUTPUT="${2:-linux-ui-smoke.png}"

if [[ ! -x "$BINARY" ]]; then
  echo "Executable not found or not executable: $BINARY" >&2
  exit 1
fi

BINARY="$(realpath "$BINARY")"
OUTPUT="$(realpath -m "$OUTPUT")"
LOG="$(mktemp)"

xvfb-run -a -s "-screen 0 1400x900x24" bash -c '
  set -euo pipefail
  binary="$1"
  output="$2"
  log="$3"

  "$binary" >"$log" 2>&1 &
  app_pid=$!
  cleanup() { kill "$app_pid" 2>/dev/null || true; wait "$app_pid" 2>/dev/null || true; }
  trap cleanup EXIT

  for _ in $(seq 1 20); do
    if xdotool search --name "Batch Convert to Compressed File" >/dev/null 2>&1; then
      break
    fi
    sleep 0.5
  done

  if ! xdotool search --name "Batch Convert to Compressed File" >/dev/null 2>&1; then
    echo "Application window did not appear. Log:" >&2
    cat "$log" >&2
    exit 1
  fi

  sleep 2
  xdotool search --name "Batch Convert to Compressed File" getwindowgeometry %@
  import -window root "$output"
  echo "Screenshot: $output"
' _ "$BINARY" "$OUTPUT" "$LOG"

rm -f "$LOG"
