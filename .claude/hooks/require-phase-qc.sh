#!/bin/bash
# Blocks Write/Edit to src/ when:
#   1. A phase is marked IMPLEMENTED but not QC-approved, OR
#   2. A phase is marked APPROVED but review says CONDITIONAL APPROVAL
#      (fix was required — re-QC is mandatory before genuine APPROVED)
#
# Convention in the plan document:
#   **QC-status:** NOT STARTED         ← phase not begun
#   **QC-status:** IMPLEMENTED         ← dev agent run, awaiting QC  → BLOCKS
#   **QC-status:** APPROVED            ← review+test OK, continue
#   **QC-status:** REJECTED            ← errors found, dev agent fixes → BLOCKS
#
# CONDITIONAL APPROVAL in the **Review:** line means a fix is required.
# Plan MUST be updated with new **Review:** from the re-QC round before APPROVED is valid.

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
[ -z "$REPO_ROOT" ] && exit 0

PLAN_FILE=$(find "$REPO_ROOT/doc" -name "*-plan.md" 2>/dev/null | head -1)
[ -z "$PLAN_FILE" ] && exit 0

# Check 1: IMPLEMENTED without QC
if grep -q "\*\*QC-status:\*\* IMPLEMENTED" "$PLAN_FILE"; then
  BLOCKED_PHASE=$(grep -B5 "\*\*QC-status:\*\* IMPLEMENTED" "$PLAN_FILE" | grep "^### F" | tail -1)

  echo ""
  echo "BLOCKED: Phase not QC-approved"
  echo ""
  echo "${BLOCKED_PHASE:-A phase} is IMPLEMENTED but is missing review+test."
  echo ""
  echo "Mandatory process (CLAUDE.md step 2b–2d):"
  echo "  1. Run review agent + test agent IN PARALLEL in the same message"
  echo "  2. Evaluate both results"
  echo "  3. Update plan: **QC-status:** IMPLEMENTED → APPROVED (or REJECTED)"
  echo "  4. On REJECTED: fix findings → back to step 1"
  echo "  5. Only on APPROVED: next phase can start"
  echo ""
  exit 2
fi

# Check 2: APPROVED but review says CONDITIONAL APPROVAL (re-QC not completed)
# The orchestrator CANNOT set APPROVED to bypass the hook when review requires a fix.
if grep -q "\*\*QC-status:\*\* APPROVED" "$PLAN_FILE"; then
  # For each phase with APPROVED: check if **Review:** line says CONDITIONAL APPROVAL
  # Use awk: when we see APPROVED line, read next lines until next phase heading or Review line
  BETINGET=$(awk '
    /\*\*QC-status:\*\* APPROVED/ { in_phase=1; next }
    in_phase && /\*\*Review:\*\*.*CONDITIONAL APPROVAL/ { print "found"; exit }
    in_phase && /^### F/ { in_phase=0 }
  ' "$PLAN_FILE")

  if [ -n "$BETINGET" ]; then
    echo ""
    echo "BLOCKED: APPROVED set without re-QC after CONDITIONAL APPROVAL"
    echo ""
    echo "Review said CONDITIONAL APPROVAL — a fix is required."
    echo "APPROVED is not valid without a new round of review+test."
    echo ""
    echo "Correct process:"
    echo "  1. Task(dev-agent, fix review findings)"
    echo "  2. Task(review-agent + test-agent IN PARALLEL)"
    echo "  3. Update **Review:** with new report from the re-QC round"
    echo "  4. Only then: APPROVED is valid"
    echo ""
    echo "NEVER: set APPROVED to bypass the hook."
    echo ""
    exit 2
  fi
fi

exit 0
