# DSF Observability — Agent and Process Monitoring

Three tools for monitoring what the factory is doing in real time.

---

## agent.log

All agent lifecycle events are written to `.claude/logs/agent.log` via `agent-logger.sh`.

```
[2026-04-30 14:22:01] -->  START  [architect]  id=1746012121000-4821  Branch ship-readiness audit
[2026-04-30 14:24:38] <--  DONE   [architect]  id=1746012121000-4821  Branch ship-readiness audit
```

Correlation ID (START↔DONE) makes it possible to calculate duration and match active agents.

---

## Terminal: agent-watch.sh

Text-based display — shows active agents with elapsed time and the last 8 completed.

```bash
# Once
bash .claude/hooks/agent-watch.sh

# Live (requires watch installed)
watch -n 1 bash .claude/hooks/agent-watch.sh

# Alternative without watch
while true; do clear; bash .claude/hooks/agent-watch.sh; sleep 1; done
```

Example output:
```
╔════════════════════════════════════════════════════════════════╗
║            DARK SOFTWARE FACTORY — AGENT MONITOR              ║
║  2026-04-30 14:23:15                                           ║
╠════════════════════════════════════════════════════════════════╣
║  ACTIVE                                                        ║
║  ▶ [developer]    F2: Infrastructure layer           47s       ║
╠════════════════════════════════════════════════════════════════╣
║  LAST COMPLETED                                                ║
║  ✓ [architect]    Transaction Compliance Service    138s       ║
╚════════════════════════════════════════════════════════════════╝
```

---

## GUI: agent-watch-gui.py

Floating tkinter window that always stays on top of other windows (including VS Code).

```bash
python3 .claude/hooks/agent-watch-gui.py
```

**Features:**
- Always on top (`-topmost`)
- No title bar — drag by the header band
- `✕` to close
- Spinner animation for active agents
- Updates every second automatically
- Positioned in the upper right corner at startup
- Shows last 6 completed agents with duration

**Requirements:** Python 3 with tkinter.

| Platform | Status | Installation |
|----------|--------|-------------|
| macOS | Included | Nothing to do |
| Windows | Included via python.org | See below |
| Linux | Often missing | `sudo apt install python3-tk` |

**Windows — install Python with tkinter:**

1. Download from [python.org/downloads](https://www.python.org/downloads/)
2. Run the installer — check **"Add python.exe to PATH"**
3. tkinter is included automatically (standard Windows installation)
4. Verify:
   ```cmd
   python -c "import tkinter; print('ok')"
   ```
5. Start monitor:
   ```cmd
   python .claude\hooks\agent-watch-gui.py
   ```

---

## bash.log

Orchestrator commands (dotnet, docker, git, gh) are logged to `.claude/logs/bash.log` via `bash-logger.sh`.

```
[2026-04-30 14:20:01] PRE   docker build --target test
[2026-04-30 14:21:43] POST  docker build --target test  PASS (102s)
```

Useful for viewing build times and identifying bottlenecks.

---

## Reset Logs

```bash
rm -f .claude/logs/agent.log .claude/logs/bash.log
rm -rf .claude/logs/.pending
```
