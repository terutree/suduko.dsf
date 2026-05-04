#!/usr/bin/env bash
# Logs Task agent lifecycle events to .claude/logs/agent.log
# Correlation ID written at START, matched at DONE via .claude/logs/.pending/
# Args: START | DONE
# Stdin: Claude Code hook JSON payload

set -euo pipefail

EVENT="${1:-EVENT}"
LOG_DIR="$(git rev-parse --show-toplevel 2>/dev/null || pwd)/.claude/logs"
LOG_FILE="$LOG_DIR/agent.log"
PENDING_DIR="$LOG_DIR/.pending"
mkdir -p "$LOG_DIR" "$PENDING_DIR"

TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
INPUT=$(cat)

if command -v jq &>/dev/null; then
    DESCRIPTION=$(echo "$INPUT" | jq -r '.tool_input.description // "unknown"' 2>/dev/null)
    SUBAGENT=$(echo "$INPUT" | jq -r '.tool_input.subagent_type // ""' 2>/dev/null)
else
    DESCRIPTION=$(echo "$INPUT" | python3 -c "
import sys,json
d=json.load(sys.stdin)
ti=d.get('tool_input',{})
print(ti.get('description') or 'unknown')
" 2>/dev/null || echo "unknown")
    SUBAGENT=""
fi

[[ -n "$SUBAGENT" && "$SUBAGENT" != "null" ]] && TYPE="[$SUBAGENT]" || TYPE="[agent]"

DESC_HASH=$(echo "$DESCRIPTION" | md5 -q 2>/dev/null || echo "$DESCRIPTION" | md5sum | cut -c1-8)
DESC_HASH="${DESC_HASH:0:8}"
PENDING_FILE="$PENDING_DIR/$DESC_HASH"

case "$EVENT" in
    START)
        ID="$(date +%s%3N)-$$"
        echo "$ID" > "$PENDING_FILE"
        echo "[$TIMESTAMP] -->  START  $TYPE  id=$ID  $DESCRIPTION" >> "$LOG_FILE"
        ;;
    DONE)
        if [[ -f "$PENDING_FILE" ]]; then
            ID=$(cat "$PENDING_FILE")
            rm -f "$PENDING_FILE"
        else
            ID="unknown"
        fi
        echo "[$TIMESTAMP] <--  DONE   $TYPE  id=$ID  $DESCRIPTION" >> "$LOG_FILE"
        ;;
    *)
        echo "[$TIMESTAMP]      $EVENT   $TYPE  $DESCRIPTION" >> "$LOG_FILE"
        ;;
esac
