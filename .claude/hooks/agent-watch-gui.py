#!/usr/bin/env python3
"""
DSF Agent Monitor — floating GUI window, always on top.
Usage: python3 .claude/hooks/agent-watch-gui.py [path/to/agent.log]
"""

import sys
import re
import time
import tkinter as tk
from datetime import datetime
from pathlib import Path
import subprocess

# --- Config ---
BG       = "#1e1e2e"
FG       = "#cdd6f4"
GREEN    = "#a6e3a1"
YELLOW   = "#f9e2af"
RED      = "#f38ba8"
MUTED    = "#a6adc8"
FONT     = ("Courier New", 10)
WIDTH    = 720
REFRESH  = 1000  # ms

START_RE = re.compile(r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] -->  START  +(\[[^\]]+\]) +id=([^ ]+) +(.+)$')
DONE_RE  = re.compile(r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] <--  DONE   +(\[[^\]]+\]) +id=([^ ]+) +(.+)$')

SPIN = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]


def find_log():
    if len(sys.argv) > 1:
        return Path(sys.argv[1])
    try:
        root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"],
                                        stderr=subprocess.DEVNULL).decode().strip()
        return Path(root) / ".claude/logs/agent.log"
    except Exception:
        return Path(".claude/logs/agent.log")


def parse_log(log_file):
    starts = {}
    done_list = []
    first_start = None
    last_done = None
    agent_seconds = 0
    if not log_file.exists():
        return starts, done_list, first_start, agent_seconds, last_done
    with open(log_file) as f:
        for line in f:
            m = START_RE.search(line)
            if m:
                ts_str, typ, id_, desc = m.groups()
                try:
                    epoch = datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S").timestamp()
                except Exception:
                    epoch = time.time()
                if first_start is None:
                    first_start = epoch
                starts[id_] = (epoch, typ, desc.strip())
                continue
            m = DONE_RE.search(line)
            if m:
                ts_str, typ, id_, desc = m.groups()
                if id_ in starts:
                    start_epoch = starts.pop(id_)[0]
                    try:
                        done_epoch = datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S").timestamp()
                    except Exception:
                        done_epoch = time.time()
                    diff = int(done_epoch - start_epoch)
                    agent_seconds += diff
                    dur = f"{diff}s"
                    if last_done is None or done_epoch > last_done:
                        last_done = done_epoch
                else:
                    starts.pop(id_, None)
                    dur = "?"
                done_list.append((typ, desc.strip(), dur))
    return starts, done_list, first_start, agent_seconds, last_done


class AgentMonitor(tk.Tk):
    def __init__(self, log_file):
        super().__init__()
        self.log_file = log_file
        self.spin_idx = 0

        self.title("DSF Monitor")
        self.configure(bg=BG)
        self.resizable(False, False)
        self.wm_attributes("-topmost", True)

        self._build()
        self.update_idletasks()
        self._center()
        self.lift()
        self.focus_force()
        self.update_display()

    def _build(self):
        self.body = tk.Frame(self, bg=BG, padx=8, pady=6)
        self.body.pack(fill="both", expand=True)

        # Timestamp
        self.ts_label = tk.Label(self.body, text="", bg=BG, fg=MUTED,
                                  font=("Courier New", 8), anchor="w")
        self.ts_label.pack(fill="x")

        # Active section
        self.active_frame = tk.Frame(self.body, bg=BG)
        self.active_frame.pack(fill="x", pady=(4, 0))

        # Divider
        tk.Frame(self.body, bg="#313244", height=1).pack(fill="x", pady=4)

        # Done section
        self.done_frame = tk.Frame(self.body, bg=BG)
        self.done_frame.pack(fill="x")

    def _clear_frame(self, frame):
        for w in frame.winfo_children():
            w.destroy()

    def update_display(self):
        self.spin_idx = (self.spin_idx + 1) % len(SPIN)
        now = time.time()
        starts, done_list, first_start, agent_seconds, last_done = parse_log(self.log_file)

        # include currently active agents in total agent time
        total_agent = agent_seconds + sum(int(now - ep) for ep, _, _ in starts.values())

        ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        if first_start:
            def fmt(s):
                h, r = divmod(s, 3600)
                m, sec = divmod(r, 60)
                return f"{h}:{m:02d}:{sec:02d}" if h else f"{m}:{sec:02d}"
            # freeze wall clock when no active agents (round complete)
            wall_end = now if starts else (last_done or now)
            ts = f"{ts}   ⏱ {fmt(int(wall_end - first_start))}   🤖 {fmt(total_agent)}"
        self.ts_label.config(text=ts)

        # --- Active ---
        self._clear_frame(self.active_frame)
        active = list(starts.items())
        if not active:
            tk.Label(self.active_frame, text="  no active agents", bg=BG, fg=MUTED,
                     font=("Courier New", 9), anchor="w").pack(fill="x")
        else:
            tk.Label(self.active_frame, text="ACTIVE", bg=BG, fg=YELLOW,
                     font=("Courier New", 8, "bold"), anchor="w").pack(fill="x")
            for id_, (epoch, typ, desc) in active:
                elapsed = int(now - epoch)
                spin = SPIN[self.spin_idx]
                row = f"{spin} {typ:<16} {desc[:42]:<42} {elapsed:>4}s"
                tk.Label(self.active_frame, text=row, bg=BG, fg=GREEN,
                         font=("Courier New", 9), anchor="w").pack(fill="x")

        # --- Done ---
        self._clear_frame(self.done_frame)
        recent = done_list[-10:][::-1]
        if not recent:
            tk.Label(self.done_frame, text="  (none completed yet)", bg=BG, fg=MUTED,
                     font=("Courier New", 9), anchor="w").pack(fill="x")
        else:
            tk.Label(self.done_frame, text="COMPLETED", bg=BG, fg=MUTED,
                     font=("Courier New", 8, "bold"), anchor="w").pack(fill="x")
            for typ, desc, dur in recent:
                row = f"✓ {typ:<16} {desc[:42]:<42} {dur:>4}"
                tk.Label(self.done_frame, text=row, bg=BG, fg=MUTED,
                         font=("Courier New", 9), anchor="w").pack(fill="x")

        self.after(REFRESH, self.update_display)

    def _center(self):
        sw = self.winfo_screenwidth()
        self.geometry(f"+{sw - WIDTH - 20}+40")


if __name__ == "__main__":
    log = find_log()
    app = AgentMonitor(log)
    app.mainloop()
