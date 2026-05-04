#!/bin/bash
# Blocks git commit when a plan is completed without a corresponding final report.
# A plan is considered complete when:
#   1. Marked with "Status: COMPLETED", OR
#   2. Has at least one "[x] APPROVED" phase AND no "[ ] Not started" phases remaining
# Exception: plan marked with "Simple change".

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // ""')

if ! echo "$COMMAND" | grep -qE '^git commit'; then
  exit 0
fi

REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null)
[ -z "$REPO_ROOT" ] && exit 0

STAGED=$(git diff --cached --name-only 2>/dev/null)

for plan_file in $(echo "$STAGED" | grep -E "^doc/.*-plan\.md$"); do
  plan_content=$(git show ":$plan_file" 2>/dev/null)

  # Skip simple changes
  echo "$plan_content" | grep -qiE "Simple change|simple.change" && continue

  # Check if the plan is complete
  is_complete=0

  # Trigger 1: explicit COMPLETED marker
  if echo "$plan_content" | grep -q "Status:.*COMPLETED"; then
    is_complete=1
  fi

  # Trigger 2: all phases APPROVED, no "Not started" remaining
  if echo "$plan_content" | grep -q "\[x\] APPROVED" && \
     ! echo "$plan_content" | grep -q "\[ \] Not started"; then
    is_complete=1
  fi

  [ "$is_complete" -eq 0 ] && continue

  iter_name=$(basename "$plan_file" -plan.md)
  analyse_file="$REPO_ROOT/doc/${iter_name}-analyse.md"
  analyse_staged=$(echo "$STAGED" | grep -E "^doc/${iter_name}-analyse\.md$")

  if [ -n "$analyse_staged" ] || [ -f "$analyse_file" ]; then
    continue
  fi

  echo ""
  echo "BLOCKED: Missing final report for ${iter_name}"
  echo ""
  echo "All phases are APPROVED but doc/${iter_name}-analyse.md does not exist."
  echo "Write the final report and include it in the same commit as the plan."
  echo ""
  echo "The final report must contain:"
  echo "  - Timing table"
  echo "  - Review analysis (findings per phase)"
  echo "  - Test analysis + satisfaction score"
  echo "  - Process improvements (update skills)"
  echo ""
  echo "Exception: Add '**Type:** Simple change' to the plan."
  echo ""
  exit 2
done

exit 0
