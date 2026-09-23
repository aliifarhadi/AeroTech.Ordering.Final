#!/usr/bin/env bash
set -euo pipefail

# Proves the guardrail before any production Domain code exists:
#   stage 0 -> green
#   stage 1 -> red, and red ONLY because the canonical type set is absent
ROOT="$(cd "$(dirname "$0")" && pwd)"
TEST_PROJECT="$ROOT/tests/AeroTech.Ordering.Domain.ConformanceTests/AeroTech.Ordering.Domain.ConformanceTests.csproj"
STAGE_FILE="$ROOT/.canonical-stage"
LOG="$(mktemp)"
trap 'rm -f "$LOG"' EXIT

echo 0 > "$STAGE_FILE"
dotnet test "$TEST_PROJECT"

echo 1 > "$STAGE_FILE"
set +e
dotnet test "$TEST_PROJECT" >"$LOG" 2>&1
CODE=$?
set -e

if [ "$CODE" -eq 0 ]; then
  echo "ERROR: Stage 1 unexpectedly passed on an empty canonical Domain." >&2
  cat "$LOG"
  exit 1
fi

if ! grep -q "STAGE1_TYPE_SET_MISMATCH" "$LOG"; then
  echo "ERROR: Stage 1 did not fail for the expected missing canonical type set." >&2
  cat "$LOG"
  exit 1
fi

echo 0 > "$STAGE_FILE"
echo "GUARDRAIL PROOF OK: Stage 0 passed; Stage 1 failed only for the missing canonical type set."
