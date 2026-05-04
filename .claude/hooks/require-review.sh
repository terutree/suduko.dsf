#!/bin/bash
# Blocks git commit when a plan is COMPLETED without a registered review verdict.
# Exception: plan marked "Simple change".

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

  echo "$plan_content" | grep -q "Status:.*COMPLETED" || continue
  echo "$plan_content" | grep -qiE "Simple change|simple.change" && continue

  if echo "$plan_content" | grep -qiE "APPROVED|CONDITIONAL APPROVAL|REJECTED"; then
    continue
  fi

  iter_name=$(basename "$plan_file" -plan.md)

  echo ""
  echo "BLOCKED: Missing review verdict for ${iter_name}"
  echo ""
  echo "Plan is marked COMPLETED but contains no review verdict."
  echo "Run code review and add APPROVED / CONDITIONAL APPROVAL / REJECTED"
  echo "to the plan document before committing."
  echo ""
  echo "Exception: Add '**Type:** Simple change' to the plan to"
  echo "replace review with a note in the plan document."
  echo ""
  exit 2
done

exit 0
