#!/bin/bash
# Blocks Write/Edit to source code when no plan document exists.
# Runs as a PreToolUse hook for Write and Edit.

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // .tool_input.path // ""')

if [ -z "$FILE_PATH" ]; then
  exit 0
fi

SOURCE_DIRS=(
  "src/Api/"
  "src/Core/"
  "src/Infrastructure/"
  "tests/"
)

is_source=false
for dir in "${SOURCE_DIRS[@]}"; do
  if echo "$FILE_PATH" | grep -q "$dir"; then
    is_source=true
    break
  fi
done

if [ "$is_source" = false ]; then
  exit 0
fi

REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null)
if [ -z "$REPO_ROOT" ]; then
  REPO_ROOT=$(git -C "$(dirname "$FILE_PATH")" rev-parse --show-toplevel 2>/dev/null)
fi

if [ -n "$REPO_ROOT" ]; then
  RECENT_PLAN=$(git -C "$REPO_ROOT" log --oneline -10 --pretty=format: --name-only -- "doc/*-plan.md" 2>/dev/null | grep -v "^$" | head -1)
  if [ -n "$RECENT_PLAN" ]; then
    exit 0
  fi

  UNSTAGED_PLAN=$(find "$REPO_ROOT/doc" -name "*-plan.md" 2>/dev/null | head -1)
  if [ -n "$UNSTAGED_PLAN" ]; then
    exit 0
  fi
fi

echo ""
echo "BLOCKED: Source code without plan document"
echo ""
echo "File: $FILE_PATH"
echo ""
echo "Create doc/[feature]-plan.md before implementation starts."
echo "See CLAUDE.md Agent Protocol Step 1."
echo ""
exit 2
