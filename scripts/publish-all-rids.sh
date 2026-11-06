#!/usr/bin/env bash
# Cross-publishes the application for every supported runtime identifier.
# Usage: ./scripts/publish-all-rids.sh [output-directory]
set -euo pipefail

OUTPUT_ROOT="${1:-artifacts}"
RIDS=(win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64)

for rid in "${RIDS[@]}"; do
  echo "=== Publishing $rid"
  dotnet publish BatchConvertToCompressedFile/BatchConvertToCompressedFile.csproj \
    -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=embedded \
    -o "$OUTPUT_ROOT/$rid" --nologo
done
