#!/usr/bin/env python3
"""
DSF Cost Report — token usage and cost from Claude Code transcript logs.
Correlates individual API calls (timestamp in JSONL) against agent windows (agent.log)
to distribute cost per subagent and orchestrator.

Usage:
    python3 .claude/hooks/cost-report.py
    python3 .claude/hooks/cost-report.py --all   # all sessions, not just the last run
"""

import json
import sys
import re
import subprocess
from pathlib import Path
from datetime import datetime
import time as time_mod

# ── Prices per million tokens (USD) ──────────────────────────────────────────
PRICES = {
    "claude-opus-4-7":   dict(inp=15.00, out=75.00, cw=18.75, cr=1.50),
    "claude-opus-4-6":   dict(inp=15.00, out=75.00, cw=18.75, cr=1.50),
    "claude-sonnet-4-6": dict(inp=3.00,  out=15.00, cw=3.75,  cr=0.30),
    "claude-sonnet-4-5": dict(inp=3.00,  out=15.00, cw=3.75,  cr=0.30),
    "claude-haiku-4-5":  dict(inp=0.80,  out=4.00,  cw=1.00,  cr=0.08),
}

START_RE = re.compile(
    r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] -->  START  +(\[[^\]]+\]) +id=([^ ]+) +(.+)$'
)
DONE_RE = re.compile(
    r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] <--  DONE   +(\[[^\]]+\]) +id=([^ ]+) +(.+)$'
)


def find_project_dir(repo_root: Path) -> Path:
    sanitized = str(repo_root).replace("/", "-")
    return Path.home() / ".claude" / "projects" / sanitized


def get_price(model: str):
    for key, p in PRICES.items():
        if key in model:
            return p
    return None


def short_model(model: str) -> str:
    for key in PRICES:
        if key in model:
            return key
    return model[:30]


def calc_cost(usage: dict, model: str) -> float:
    p = get_price(model)
    if not p:
        return 0.0
    M = 1_000_000
    inp  = usage.get("input_tokens", 0)
    out  = usage.get("output_tokens", 0)
    cw   = usage.get("cache_creation_input_tokens", 0)
    cr   = usage.get("cache_read_input_tokens", 0)
    return (inp * p["inp"] + out * p["out"] + cw * p["cw"] + cr * p["cr"]) / M


def parse_calls(jsonl: Path, since_epoch: float = 0.0) -> list:
    """Parse JSONL → list of (epoch, usage, model) for each API response."""
    calls = []
    try:
        with open(jsonl) as f:
            for line in f:
                try:
                    obj = json.loads(line)
                except Exception:
                    continue
                ts = obj.get("timestamp")
                if not ts:
                    continue
                try:
                    epoch = datetime.fromisoformat(ts.replace("Z", "+00:00")).timestamp()
                except Exception:
                    continue
                if epoch < since_epoch:
                    continue
                msg = obj.get("message", {})
                usage = msg.get("usage") or obj.get("usage")
                model = msg.get("model") or obj.get("model") or ""
                if usage:
                    calls.append((epoch, usage, model))
    except Exception:
        pass
    return calls


def parse_agent_log(log_file: Path):
    """Return (first_start_epoch, list of (desc, type, start_epoch, done_epoch|None))."""
    agents = {}
    done = []
    first = None
    last_done = None
    if not log_file.exists():
        return None, [], None
    with open(log_file) as f:
        for line in f:
            m = START_RE.search(line)
            if m:
                ts_str, typ, id_, desc = m.groups()
                epoch = datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S").timestamp()
                if first is None:
                    first = epoch
                agents[id_] = (desc.strip(), typ, epoch)
                continue
            m = DONE_RE.search(line)
            if m:
                ts_str, typ, id_, desc = m.groups()
                epoch = datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S").timestamp()
                if id_ in agents:
                    d, t, s = agents.pop(id_)
                    done.append((d, t, s, epoch))
                    if last_done is None or epoch > last_done:
                        last_done = epoch
    for id_, (d, t, s) in agents.items():
        done.append((d, t, s, None))
    done.sort(key=lambda x: x[2])
    return first, done, last_done


TYPE_ABBREV = {
    "general-purpose": "gp",
    "explore":         "ex",
    "plan":            "pl",
    "code-reviewer":   "cr",
}


def short_type(typ: str) -> str:
    inner = typ.strip("[]")
    return "[" + TYPE_ABBREV.get(inner, inner[:4]) + "]"


def agent_label(typ: str, desc: str, max_len: int = 44) -> str:
    prefix = short_type(typ) + " "
    return (prefix + desc)[:max_len]


def find_agent_for_call(epoch: float, agents: list) -> str:
    """Find which agent's window an API call falls within. Returns label."""
    best = None
    best_start = -1
    for desc, typ, start, end in agents:
        e = end if end is not None else float("inf")
        if start <= epoch <= e:
            if start > best_start:
                best_start = start
                best = agent_label(typ, desc)
    return best or "orchestrator"


def empty_bucket(model: str = "") -> dict:
    return dict(input=0, output=0, cache_write=0, cache_read=0, cost=0.0,
                model=model, calls=0, first=None, last=None)


def add_call(bucket: dict, epoch: float, usage: dict, model: str):
    bucket["input"]       += usage.get("input_tokens", 0)
    bucket["output"]      += usage.get("output_tokens", 0)
    bucket["cache_write"] += usage.get("cache_creation_input_tokens", 0)
    bucket["cache_read"]  += usage.get("cache_read_input_tokens", 0)
    bucket["cost"]        += calc_cost(usage, model)
    bucket["calls"]       += 1
    if model:
        bucket["model"] = model
    if bucket["first"] is None or epoch < bucket["first"]:
        bucket["first"] = epoch
    if bucket["last"] is None or epoch > bucket["last"]:
        bucket["last"] = epoch


def fmt_tokens(n: int) -> str:
    if n >= 1_000_000:
        return f"{n/1_000_000:.1f}M"
    if n >= 1_000:
        return f"{n/1_000:.0f}k"
    return str(n)


def fmt_dur(s) -> str:
    if s is None:
        return "…"
    s = int(s)
    h, r = divmod(s, 3600)
    m, sec = divmod(r, 60)
    return f"{h}:{m:02d}:{sec:02d}" if h else f"{m}:{sec:02d}"


def main():
    show_all = "--all" in sys.argv

    try:
        repo_root = Path(
            subprocess.check_output(
                ["git", "rev-parse", "--show-toplevel"], stderr=subprocess.DEVNULL
            ).decode().strip()
        )
    except Exception:
        repo_root = Path.cwd()

    project_dir = find_project_dir(repo_root)
    log_file = repo_root / ".claude/logs/agent.log"

    if not project_dir.exists():
        print(f"Project directory not found: {project_dir}")
        sys.exit(1)

    first_agent_epoch, agents, last_done_epoch = parse_agent_log(log_file)

    since = 0.0 if (show_all or not first_agent_epoch) else first_agent_epoch - 60

    # ── Parse all JSONL files, filter by time ────────────────────────────────
    jsonl_files = sorted(project_dir.glob("*.jsonl"), key=lambda p: p.stat().st_mtime)
    if not show_all and first_agent_epoch:
        jsonl_files = [p for p in jsonl_files if p.stat().st_mtime >= first_agent_epoch - 60]

    all_calls = []
    for p in jsonl_files:
        all_calls.extend(parse_calls(p, since_epoch=since))
    all_calls.sort(key=lambda x: x[0])

    if not all_calls:
        print("No API calls found.")
        sys.exit(0)

    # ── Assign each API call to an agent bucket ───────────────────────────────
    buckets = {}  # label → bucket
    for epoch, usage, model in all_calls:
        label = find_agent_for_call(epoch, agents)
        if label not in buckets:
            buckets[label] = empty_bucket(model)
        add_call(buckets[label], epoch, usage, model)

    # ── Sort: agent log order → orchestrator last ─────────────────────────────
    agent_order = []
    seen = set()
    for desc, typ, start, end in agents:
        label = agent_label(typ, desc)
        if label not in seen and label in buckets:
            agent_order.append(label)
            seen.add(label)
    if "orchestrator" in buckets:
        agent_order.append("orchestrator")
    # fall back for any leftover labels
    for label in buckets:
        if label not in seen and label != "orchestrator":
            agent_order.append(label)

    # ── Layout ────────────────────────────────────────────────────────────────
    W_LABEL = 44
    W_MODEL = 20
    W_TOK   = 8
    W_DUR   = 7
    W_COST  = 8

    def row(label, model, inp, out, cw, cr, dur, cost, bold=False):
        tok_total = inp + out + cw + cr
        b = "★ " if bold else "  "
        return (
            f"{b}{label[:W_LABEL]:<{W_LABEL}} "
            f"{short_model(model):<{W_MODEL}} "
            f"{fmt_tokens(inp):>{W_TOK}} "
            f"{fmt_tokens(out):>{W_TOK}} "
            f"{fmt_tokens(cw):>{W_TOK}} "
            f"{fmt_tokens(cr):>{W_TOK}} "
            f"{fmt_tokens(tok_total):>{W_TOK}} "
            f"{dur:>{W_DUR}} "
            f"${cost:>6.3f}"
        )

    header = (
        f"  {'Agent':<{W_LABEL}} "
        f"{'Model':<{W_MODEL}} "
        f"{'Inp':>{W_TOK}} "
        f"{'Out':>{W_TOK}} "
        f"{'CacheW':>{W_TOK}} "
        f"{'CacheR':>{W_TOK}} "
        f"{'Total':>{W_TOK}} "
        f"{'Time':>{W_DUR}} "
        f"{'Cost':>8}"
    )
    sep = "─" * len(header)

    print()
    print("  DSF COST REPORT")
    if first_agent_epoch:
        print(f"  Run started: {datetime.fromtimestamp(first_agent_epoch).strftime('%Y-%m-%d %H:%M:%S')}")
    print()
    print(header)
    print(sep)

    tot = dict(input=0, output=0, cache_write=0, cache_read=0, cost=0.0)

    for label in agent_order:
        b = buckets[label]
        # duration = agent.log window for named agents, first/last call for orchestrator
        dur_s = None
        for desc, typ, start, end in agents:
            if agent_label(typ, desc) == label:
                if end:
                    dur_s = end - start
                elif b["last"]:
                    dur_s = b["last"] - start
                break
        if dur_s is None and b["first"] and b["last"]:
            dur_s = b["last"] - b["first"]

        print(row(label, b["model"], b["input"], b["output"],
                  b["cache_write"], b["cache_read"], fmt_dur(dur_s), b["cost"]))
        for k in ("input", "output", "cache_write", "cache_read"):
            tot[k] += b[k]
        tot["cost"] += b["cost"]

    print(sep)

    wall = None
    if first_agent_epoch:
        wall = (last_done_epoch or time_mod.time()) - first_agent_epoch

    print(row("TOTAL", "", tot["input"], tot["output"],
              tot["cache_write"], tot["cache_read"], fmt_dur(wall), tot["cost"], bold=True))
    print()

    if agents:
        print("  Agent log:")
        for desc, typ, start, end in agents:
            dur = fmt_dur((end - start) if end else None)
            st = datetime.fromtimestamp(start).strftime("%H:%M:%S")
            print(f"    {st}  {typ:<18} {dur:>7}  {desc[:50]}")
    print()


if __name__ == "__main__":
    main()
