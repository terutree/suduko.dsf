#!/usr/bin/env bash
# Logs key orchestrator Bash commands to .claude/logs/agent.log
# Args: PRE | POST
# Stdin: Claude Code hook JSON payload

set -euo pipefail

EVENT="${1:-PRE}"
LOG_FILE="$(git rev-parse --show-toplevel 2>/dev/null || pwd)/.claude/logs/agent.log"
mkdir -p "$(dirname "$LOG_FILE")"

TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
INPUT=$(cat)

if command -v jq &>/dev/null; then
    CMD=$(echo "$INPUT" | jq -r '.tool_input.command // ""' 2>/dev/null)
else
    CMD=$(echo "$INPUT" | python3 -c "
import sys,json
d=json.load(sys.stdin)
print(d.get('tool_input',{}).get('command',''))
" 2>/dev/null || echo "")
fi

# Only log commands worth watching
case "$CMD" in
    *"dotnet test"*)   LABEL="TEST" ;;
    *"dotnet build"*)  LABEL="BUILD" ;;
    *"dotnet format"*) LABEL="FORMAT" ;;
    *"dotnet run"*)    LABEL="API" ;;
    *"git push"*)      LABEL="PUSH" ;;
    *"git commit"*)    LABEL="COMMIT" ;;
    *"gh run view"*)   LABEL="CI" ;;
    *"gh run list"*)   LABEL="CI" ;;
    *)                 exit 0 ;;
esac

SHORT_CMD=$(echo "$CMD" | tr -s ' ' | cut -c1-70)

if [[ "$EVENT" == "PRE" ]]; then
    echo "[$TIMESTAMP]      $LABEL ...  $SHORT_CMD" >> "$LOG_FILE"
else
    if command -v jq &>/dev/null; then
        RESPONSE=$(echo "$INPUT" | jq -r '.. | strings' 2>/dev/null | head -c 1000)
    else
        RESPONSE=$(echo "$INPUT" | python3 -c "import sys,json; print(str(json.load(sys.stdin))[:1000])" 2>/dev/null || echo "")
    fi

    if echo "$RESPONSE" | grep -qiE "error|failed|failure|FAILED|Build FAILED"; then
        STATUS="FAIL"
    else
        STATUS="OK  "
    fi

    echo "[$TIMESTAMP]      $LABEL $STATUS  $SHORT_CMD" >> "$LOG_FILE"
fi
