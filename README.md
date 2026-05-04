# Transaction Compliance Service — Dark Software Factory

An empty repository set up for autonomous, agent-driven development of a **Transaction Compliance Service** in .NET 8/C#. The infrastructure follows best practices from the Dark Software Factory pattern, including holdout scenarios and satisfaction metrics.

---

## What is a Dark Software Factory?

A Dark Software Factory is an autonomous development pipeline where AI agents take a task description from plain text to finished, tested, and reviewed code — without human code writing.

```
Task (plain text)
       ↓
   Architect agent    → Plan
       ↓
   Developer agent   → Code + unit tests
       ↓
   Review agent  ┐
   Test agent    ┘  → (parallel) Quality assurance
       ↓
   Eval agent        → Holdout evaluation (satisfaction score)
       ↓
   Commit + PR
```

Humans define *what* and *why*. Agents deliver *how*.

---

## Repository Structure

```
dnb-compliance-dsf/
├── CLAUDE.md                    ← The contract with agents (read this first)
├── .claudeignore                ← scenarios/ excluded from all dev agents
│
├── .claude/
│   ├── settings.json            ← Permissions and hook configuration
│   └── hooks/
│       ├── require-plan.sh      ← Blocks code writing without a plan document
│       ├── require-review.sh    ← Blocks COMPLETED commit without review verdict
│       └── require-analyse.sh  ← Blocks COMPLETED commit without final report
│
├── .claude/skills/
│   ├── architect/SKILL.md       ← Architecture + phased implementation plan
│   ├── developer/SKILL.md       ← Implementation + unit tests (C#/.NET)
│   ├── reviewer/SKILL.md        ← Code review + security
│   ├── tester/SKILL.md          ← Test quality + API smoke test
│   └── eval/SKILL.md            ← Holdout evaluator (satisfaction score)
│
├── scenarios/                   ← HOLDOUT: the dev agent never sees this
│   └── README.md                ← created by orchestrator during planning
│
├── doc/                         ← Plan and analysis documents (auto-generated)
│
└── src/                         ← Built by agents (empty at startup)
    ├── Api/
    ├── Core/
    └── Infrastructure/
```

---

## Key Concepts

### 1. CLAUDE.md — The Contract

`CLAUDE.md` is the most important file in the repo. It defines:
- What is to be built (compliance rules, API contract)
- Technical stack and architecture requirements
- The agent protocol (which steps, which order)
- Non-negotiable rules

The better the `CLAUDE.md`, the better the output from the agents. This is the briefing for a senior developer starting on day one.

### 2. Holdout Scenarios — Train/Test Separation

The `scenarios/` folder contains acceptance criteria written as plain-text HTTP scenarios. These are listed in `.claudeignore` — the dev agent implementing the code **never** sees these files.

**Why?** StrongDM discovered that agents who can see the tests write code that games them — including `return true`. The separation between implementation and evaluation is what makes autonomous coding safe enough for production use.

Same principle as train/test split in machine learning: the model is trained without knowing the answer key.

```
Dev agent:  sees CLAUDE.md + src/ + doc/  →  writes code
Eval agent: sees scenarios/ + running API →  evaluates behavior
```

### 3. Satisfaction Metric

Instead of binary pass/fail ("all tests green"), the eval agent uses a **satisfaction score**: number of holdout scenarios satisfied / total number.

```
Satisfaction: 4/5 scenarios satisfied (80%)
- 01-amount-threshold: PASS
- 02-sanctioned-country: PASS
- 03-normal-payment: PASS
- 04-cumulative-daily-limit: FAIL (cumulative limit not activated)
- 05-pep-check: PASS
```

Pipeline blocks at < 80% satisfaction.

**Why not just count tests?** Tests written by the dev agent are "in-distribution" — they test what the dev agent thought it implemented. Holdout scenarios are "out-of-distribution" — they test that the service actually fulfills the requirements.

### 4. Self-Evolving Skills

After each completed feature, a `doc/[feature]-analysis.md` is written that analyzes:
- What went well
- Which errors were found in review
- What is missing in test coverage

**Skills are updated directly** based on the findings. A bug that occurred because the dev agent did not check boundary values → new item in the `developer/SKILL.md` checklist. The next feature implementation catches the error automatically.

After 9 iterations in a parallel project, `developer/SKILL.md` has accumulated 80+ project-specific gotchas from real bugs — this is the strongest mechanism in the setup.

### 5. CI Self-Healing

After push to GitHub:

```bash
# Check CI status
gh run list --limit 5

# Fetch failure details on failure
gh run view --log-failed

# Dispatch dev agent with CI output
# Prompt: "CI failing. Output: [paste error]. Analyze and fix."
```

The GitHub Actions pipeline (set up by the dev agent in the first feature) is configured to post a failure summary as a PR comment — copy and paste to the dev agent.

---

## Getting Started

### First Run (demo)

```bash
# 1. Start Claude Code without permission prompts (required for autonomous running)
cd dnb-compliance-dsf-experiment
claude --dangerously-skip-permissions

# 2. Optional: start agent monitor in a separate terminal window
/opt/homebrew/bin/python3.13 .claude/hooks/agent-watch-gui.py
# or: while true; do clear; bash .claude/hooks/agent-watch.sh; sleep 1; done

# 3. Give the orchestrator one prompt:
```

```
Build Transaction Compliance Service.
```

```bash
# The orchestrator reads CLAUDE.md automatically and starts the architect agent.
# Follow along in agent monitor or: tail -f .claude/logs/agent.log
```

### Hooks — What They Do

| Hook | Trigger | Blocks |
|------|---------|-----------|
| `require-plan.sh` | Write/Edit to `src/` | Code writing without a plan document |
| `require-review.sh` | `git commit` | Commit of COMPLETED plan without review verdict |
| `require-analyse.sh` | `git commit` | Commit of COMPLETED plan without final report |

Hooks run automatically — they are configured in `.claude/settings.json`.

---

## Agent Protocol (short version)

Full protocol is in `CLAUDE.md`. Short version:

```
1. Architect agent  → doc/[feature]-plan.md
2. Developer agent  → src/ + tests (per phase)
3. Review agent  ┐
   Test agent    ┘  → (parallel) → fix on deviations
4. Eval agent       → satisfaction score ≥ 80%
5. Commit + analysis → doc/[feature]-analysis.md → update skills
```

---

## Use as Workshop Template

This repo is designed for workshops where participants build their own Dark Software Factory.

**For participants (Tracks 1–3 from the workshop):**
1. Fork or clone the repo
2. Read `CLAUDE.md` — especially the compliance rules and stack description
3. Start Claude Code and follow the agent protocol
4. Build Payment Screening Service (Track 1) or Transaction Compliance Service (Tracks 2–3)

**For the facilitator:**
- The demo sequence is documented in `CLAUDE.md` under "Demo 1" and "Demo 2"
- Run through the agent protocol once in advance
- Have a backup: if the network fails, show a pre-built branch

---

## References

| Resource | Content |
|---------|---------|
| [The Dark Factory Pattern (HackerNoon)](https://hackernoon.com/the-dark-factory-pattern-moving-from-ai-assisted-to-fully-autonomous-coding) | 4-phase implementation, holdout scenarios, AGENTS.md |
| [The Dark Software Factory (BCG Platinion)](https://www.bcgplatinion.com/insights/the-dark-software-factory) | Enterprise perspective, intent thinking |
| [StrongDM Software Factory](https://simonwillison.net/2026/Feb/7/software-factory/) | Zero human review, satisfaction metric |
| [Anthropic: Agentic Coding Trends 2026](https://resources.anthropic.com/hubfs/2026%20Agentic%20Coding%20Trends%20Report.pdf) | Industry data and maturity trends |
