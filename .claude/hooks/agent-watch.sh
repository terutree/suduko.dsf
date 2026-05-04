#!/usr/bin/env bash
# Visualizes active and recently completed agents based on agent.log
# Usage: watch -n 1 bash .claude/hooks/agent-watch.sh
#    or: bash .claude/hooks/agent-watch.sh

LOG_FILE="$(git rev-parse --show-toplevel 2>/dev/null || pwd)/.claude/logs/agent.log"

if [[ ! -f "$LOG_FILE" ]]; then
    echo "No agent.log found — start a DSF cycle first."
    exit 0
fi

python3 - "$LOG_FILE" <<'PYEOF'
import sys, re, time
from datetime import datetime

log_file = sys.argv[1]
now = time.time()

START_RE = re.compile(r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] -->  START  +(\[[^\]]+\]) +id=([^ ]+) +(.+)$')
DONE_RE  = re.compile(r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] <--  DONE   +(\[[^\]]+\]) +id=([^ ]+) +(.+)$')

starts = {}   # id -> (ts_epoch, type, desc)
done_list = []  # [(id, ts_str, type, desc, duration_str)]

with open(log_file) as f:
    for line in f:
        m = START_RE.search(line)
        if m:
            ts_str, typ, id_, desc = m.groups()
            try:
                epoch = datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S").timestamp()
            except Exception:
                epoch = now
            starts[id_] = (epoch, typ, desc)
            continue
        m = DONE_RE.search(line)
        if m:
            ts_str, typ, id_, desc = m.groups()
            if id_ in starts:
                start_epoch = starts.pop(id_)[0]
                try:
                    done_epoch = datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S").timestamp()
                except Exception:
                    done_epoch = now
                dur = f"{int(done_epoch - start_epoch)}s"
            else:
                starts.pop(id_, None)
                dur = "?"
            done_list.append((id_, ts_str, typ, desc, dur))

W = 64
print("╔" + "═" * W + "╗")
print("║" + "            DARK SOFTWARE FACTORY — AGENT MONITOR             " + "║")
print("║  {:<{}}║".format(datetime.now().strftime("%Y-%m-%d %H:%M:%S"), W - 2))
print("╠" + "═" * W + "╣")

active = list(starts.items())  # [(id, (epoch, type, desc))]
if not active:
    print("║  {:<{}}║".format("No active agents", W - 2))
else:
    print("║  {:<{}}║".format("ACTIVE", W - 2))
    for id_, (epoch, typ, desc) in active:
        elapsed = int(now - epoch)
        line = f"▶ {typ:<14}  {desc[:40]:<40} {elapsed:>3}s"
        print("║  {:<{}}║".format(line, W - 2))

print("╠" + "═" * W + "╣")
print("║  {:<{}}║".format("LAST COMPLETED", W - 2))

recent = done_list[-8:][::-1]
if not recent:
    print("║  {:<{}}║".format("(none yet)", W - 2))
else:
    for _, _, typ, desc, dur in recent:
        line = f"✓ {typ:<14}  {desc[:40]:<40} {dur:>4}"
        print("║  {:<{}}║".format(line, W - 2))

print("╚" + "═" * W + "╝")
print()
print("  Live update:  watch -n 1 bash .claude/hooks/agent-watch.sh")
PYEOF
