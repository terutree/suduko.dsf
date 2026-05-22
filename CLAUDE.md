# CLAUDE.md — [Project Name]

## Product

**Sudoku SPA** — [one-sentence description of what this service/app does].

Typical task: a single page website that lets me play sudoku? include an approtiate tech stack that utilizes docker


### Domain Overview


 — Technical Specification
Overview
A responsive single-page web application (SPA) that allows users to:

Play Sudoku puzzles in-browser

Select difficulty levels

Validate entries in real time

Track elapsed time

Save and resume progress

Generate new puzzles

Toggle notes/pencil mode

Support keyboard and mobile interaction

The application should be lightweight, containerized with Docker, and easy to run locally or deploy to cloud infrastructure.

Goals
Functional Goals
Render a 9x9 Sudoku board

Generate valid Sudoku puzzles

Support multiple difficulty levels:

Easy

Medium

Hard

Expert

Allow:

Number entry

Pencil marks

Erasing

Undo/redo

Detect:

Invalid moves

Puzzle completion

Persist game state locally

Non-Functional Goals
Fast initial load

Mobile responsive

Accessible keyboard navigation


Technology	Purpose
Node.js	Runtime
Express	API server
PostgreSQL	Persistence
Prisma	ORM
Architecture
Browser
   │
   ▼
React SPA
   │
   ├── Sudoku Engine
   ├── State Store
   ├── Timer System
   ├── Validation Engine
   └── Persistence Layer
Optional backend:

React SPA
   │
REST API
   │
Node/Express
   │
PostgreSQL
Core Features
1. Game Board
Requirements
9x9 responsive grid

Highlight:

Selected cell

Matching numbers

Row/column

Invalid cells

Immutable starting clues

Interaction
Input	Action
Click cell	Select
Keyboard 1-9	Enter number
Backspace/Delete	Clear
Arrow keys	Navigate
Long press/mobile	Notes mode
2. Sudoku Generator
Requirements
Generate valid solvable puzzles

Ensure unique solution

Difficulty scaling

Suggested Strategy
Generate solved board

Remove cells strategically

Validate uniqueness using solver

Difficulty Tuning
Difficulty	Approximate Clues
Easy	36–45
Medium	32–35
Hard	28–31
Expert	22–27
Data Model
Cell Structure
type Cell = {
  value: number | null
  notes: number[]
  fixed: boolean
  invalid: boolean
}
Board Structure
type Board = Cell[][]
State Management
Global State
type GameState = {
  board: Board
  selectedCell: [number, number] | null
  difficulty: Difficulty
  elapsedTime: number
  notesMode: boolean
  history: Board[]
}
Validation Rules
Real-Time Validation
Duplicate number detection:

Row

Column

3x3 region

Win Detection
Puzzle is complete when:

All cells filled

No invalid states

Matches solved board

Persistence
Local Storage
Save:

Current board

Timer

Notes

Difficulty

History stack

Autosave
Every move

On page unload

UI Components
Components
Component	Responsibility
GameBoard	Render Sudoku grid
Cell	Individual square
Toolbar	Controls
Timer	Elapsed time
DifficultySelect	New game difficulty
NumberPad	Mobile input
Modal	Win/game dialogs
Styling
Design Requirements
Minimalist interface

Dark/light mode

Smooth transitions

Mobile-first responsive layout

Tailwind Guidelines
Use utility-first classes with:

CSS variables for themes

Grid layout for board

Flex layouts for controls

Accessibility
Requirements
Keyboard navigable

ARIA labels for cells

High contrast mode support

Screen-reader-friendly controls

Performance Targets
Metric	Goal
First load	< 2s
Lighthouse score	90+
Bundle size	< 300KB gzipped
Docker Setup
Project Structure
sudoku-app/
├── client/
├── server/          # optional
├── docker/
├── docker-compose.yml
└── README.md
Frontend Dockerfile
FROM node:22-alpine


services:
  client:
    build: ./client
    ports:
      - "5173:5173"
    volumes:
      - ./client:/app
      - /app/node_modules

  server:
    build: ./server
    ports:
      - "3000:3000"
    volumes:
      - ./server:/app
      - /app/node_modules
Development Workflow
Local Development
docker compose up
Frontend:

http://localhost:5173
Backend:

http://localhost:3000
Testing Strategy
Unit Tests
Area	Tool
Sudoku logic	Vitest
Components	React Testing Library
E2E Tests
Tool	Purpose
Playwright	Full gameplay testing
Deployment Options
Recommended
Platform	Notes
Vercel	Frontend deployment
Fly.io	Full-stack Docker deployment
Railway	Simple container hosting
Future Enhancements
Potential Features
Multiplayer races

Daily challenge mode

Leaderboards

User accounts

Hint engine

AI-generated difficulty balancing

Progressive Web App support

Statistics dashboard

Security Considerations
Validate all backend input

Rate limit API endpoints

Sanitize persisted data

Avoid exposing solved boards unnecessarily

Suggested Milestones
Phase 1
Basic board rendering

Number input

Validation

Phase 2
Puzzle generation

Difficulty levels

Timer

Phase 3
Persistence

Notes mode

Undo/redo

Phase 4
Docker deployment

Testing

Accessibility improvements

Success Criteria
The application is considered complete when:

Users can fully play Sudoku on desktop/mobile

Puzzle generation is reliable

Progress persists across refreshes

Dockerized setup works with one command

Lighthouse accessibility/performance targets are met


## Stack

Offline-capable (optional PWA support)

Dockerized development and deployment workflow
Frontend
Technology	Purpose
React + TypeScript	SPA framework
Vite	Fast development/build tooling
Zustand	Lightweight state management
Tailwind CSS	Styling
React Hook Form	Optional form handling
Framer Motion	UI animations
LocalStorage API	Save game progress
Backend (Optional)
A backend is optional because Sudoku logic can run fully client-side.

### Established Decisions (do not reopen)

<!-- Document architectural decisions that are settled. -->
<!-- Example: "All amounts as long (cents) — never decimal" -->

---

## Development Model: Agent-Based

The main conversation is the **orchestrator** — writes no code itself, dispatches to agents via the Task tool.

### Context Discipline

The orchestrator keeps context LEAN:
- Plan document (reference)
- Agent summaries (not full output)
- Changed files (paths)
- User dialogue

---

## CLAUDE.md — Scalability and Context Management

CLAUDE.md is index and protocol — not complete domain documentation. As it grows, the quality of all agent interactions degrades (the context window fills with noise).

### When should something be split out?

| Signal | Action |
|--------|--------|
| CLAUDE.md > 200 lines | Split according to the table below |
| Same detail found in CLAUDE.md and a skill | Move to skill, point from CLAUDE.md |
| New domain rule overview with > 5 rules | Move to `doc/domain-rules.md` |
| New subsystem with its own responsibility | Create `src/[module]/CLAUDE.md` |

### What belongs where

| Content | Location |
|---------|-----------|
| Protocol, established decisions, architecture map | `CLAUDE.md` (root) |
| Domain rules with thresholds and logic | `doc/domain-rules.md` |
| Build environment, test patterns, language checklists | `.claude/skills/developer/SKILL.md` |
| Architecture principles, phase format | `.claude/skills/architect/SKILL.md` |
| Domain logic per layer | `src/Core/CLAUDE.md`, `src/Api/CLAUDE.md` |

### Hierarchical CLAUDE.md Files

Claude Code reads CLAUDE.md in all parent directories + current directory. Use this when a layer has its own conventions:

```
CLAUDE.md                    ← protocol, established decisions (index)
src/Core/CLAUDE.md           ← core domain contracts
src/Api/CLAUDE.md            ← API conventions, middleware
src/Infrastructure/CLAUDE.md ← external dependencies
```

---

## Holdout Scenarios — The Core Principle

The `scenarios/` folder contains acceptance criteria written as plain-text HTTP scenarios.

**The dev agent NEVER sees `scenarios/`** (`.claudeignore`). Same principle as train/test split in ML: the agent writing the code cannot read the answer key it is evaluated against. StrongDM discovered that agents who can see the tests write code that games them — including `return true`.

**The eval agent** is a separate agent that:
- Receives the URL to the running service + content from `scenarios/`
- Never sees the source code
- Reports a **satisfaction score**: number of scenarios passing / total

### Running Holdout Evaluation

```bash
# Start service locally
# [add your run command here]

# In a new terminal — dispatch eval agent
# Read .claude/skills/eval/SKILL.md
# Task(eval-agent, "Read scenarios/ and evaluate against http://localhost:[port]")
```

The eval agent blocks the pipeline at < 80% satisfaction.

---

## CI Self-Healing

Standard pattern when CI fails after push:

```bash
# Check CI status
gh run list --limit 5

# Fetch failure details
gh run view --log-failed

# Dispatch dev agent with CI output
# "CI failing on branch [branch]. Output: [paste error]. Analyze and fix."
```

---

## Agent Protocol

### New Feature

#### Step 1: Plan (always)

```
Read .claude/skills/architect/SKILL.md
Task(architect-agent, feature description + existing code structure)
Save plan in doc/[feature]-plan.md
Evaluate: Are the phases properly scoped (3-5)? Is the dependency graph correct?

For features that introduce a new domain rule:
  Verify that the plan contains ## Holdout-scenario with filename and content.
  The orchestrator writes the scenario file directly (Write tool) based on the plan spec.
  The orchestrator is not subject to .claudeignore — Task agents are.
  Scenario MUST be committed BEFORE step 2 starts.
  The eval agent reads scenarios/ directly — .claudeignore blocks automatic indexing,
  not explicit Read calls. The dev agent never reads scenarios/ because it is never instructed to.

Proceed directly to Step 2.
```

No human approval of plan. The orchestrator decides itself.

#### Step 2: Implementation per Phase

```
2a. IMPLEMENT
    Read .claude/skills/developer/SKILL.md
    Task(dev-agent, phase description + acceptance criteria + context from previous phase)
    Receive: changed files
    Update plan: **QC-status:** NOT STARTED → IMPLEMENTED
    ← require-phase-qc.sh now BLOCKS all further writes to src/

2b. QUALITY CONTROL (PARALLEL — two Task calls in the SAME message, ALWAYS)
    ╔══════════════════════════════════════════════════════════╗
    ║  STOP. Run review + test BEFORE next phase. No exceptions. ║
    ╚══════════════════════════════════════════════════════════╝
    Task(review-agent, .claude/skills/reviewer/SKILL.md + changed files)
    Task(test-agent,  .claude/skills/tester/SKILL.md + acceptance criteria)
    Receive: both results

2c. EVALUATE
    Both OK → update plan, next phase
    Review REJECTED → Task(dev-agent, fix findings) → back to 2b
    Test failed → Task(dev-agent, fix errors) → back to 2b

2d. UPDATE PLAN (Edit — mandatory, hook blocks otherwise)
    - **QC-status:** IMPLEMENTED → APPROVED (or REJECTED)
    - **Review:** verdict + findings table
    - **Test:** result
    - Acceptance criteria checked off
    ← require-phase-qc.sh now allows next phase
```

**QC-status convention** (in plan document per phase):

| Value | Meaning | Hook |
|-------|-----------|------|
| `NOT STARTED` | Phase not begun | Allowed |
| `IMPLEMENTED` | Dev agent run — awaiting QC | **BLOCKED** |
| `APPROVED` | Review+test OK | Allowed |
| `REJECTED` | Errors found — dev agent fixes | **BLOCKED** |

#### Step 3: Holdout Evaluation (after final phase)

```
Task(eval-agent, .claude/skills/eval/SKILL.md + "Service running at http://localhost:[port]")
Satisfaction ≥ 80% → proceed to Step 4
Satisfaction < 80% → Task(dev-agent, fix scenario failures) → run eval again
```

#### Step 4: Integration and Commit

```
[add your build/test command here] — all phases green
Update plan with final status
Theme-based commits — one commit per logical theme
```

#### Step 5: Analysis and Process Improvement — MANDATORY, ALWAYS

**Written automatically after the final phase. Cannot be skipped.**
Hook `require-analyse.sh` blocks commit if all phases are APPROVED without a corresponding analysis.

```
Write doc/[feature]-analysis.md (in the SAME commit as the final plan update):
  - Timing table (per phase: activity + duration)
  - Review analysis: findings per phase in table format, most common finding types, takeaway
  - Test analysis: tests per phase, green/total, holdout satisfaction score
  - Process improvements: concrete actions → update developer-SKILL.md and architect-SKILL.md

Do not wait for the user to ask for it. Do not assume it is done. Write it.
```

### Simple Change (< 150 lines, < 3 files)

```
Task(dev-agent, change description)
Task(review-agent, changed files)
OK → commit with "Type: Simple change" in plan
```

---

## Quality Gates

| Gate | Controls | Blocks |
|------|-----------|-----------|
| Orchestrator evaluates plan | Orchestrator | Step 2 |
| Plan exists | `require-plan.sh` hook | Writing to src/ |
| Phase QC approved | `require-phase-qc.sh` hook | Writing to src/ (next phase) |
| Code review | Review agent | Next phase |
| Unit + integration tests | Test agent | Next phase |
| Holdout evaluation | Eval agent | Commit |
| Review verdict in plan | `require-review.sh` hook | Commit of completed plan |
| Final report (analysis) | `require-analyse.sh` hook | Commit (blocked automatically) |

---

## Non-Negotiable Rules

- **NEVER implement without a plan** — `require-plan.sh` blocks commit regardless
- **NEVER let the orchestrator write code** — all code via agents. Applies to ALL files in `src/` and `tests/`, including one-liner fixes. No exceptions.
- **NEVER bypass `require-phase-qc.sh`** — the hook is a safety mechanism, not an obstacle. Setting `APPROVED` in the plan to remove the hook block without actually completing QC is a process violation. The hook now also detects APPROVED with CONDITIONAL APPROVAL review.
- **NEVER start the next phase without APPROVED QC** — `require-phase-qc.sh` blocks physically. Set `**QC-status:** IMPLEMENTED` after dev agent, `APPROVED` after review+test
- **NEVER reuse QC results from a previous round** — after any dev agent fix of review findings: run review agent + test agent in parallel again. Do not reuse report from the pre-fix round.
- **ALWAYS run review and test in parallel** — two Task calls in the SAME message after each phase
- **ALWAYS update plan document after each phase** — QC status + review findings + test result. Done BEFORE next phase starts
- **ALWAYS run holdout evaluation** after the final phase — satisfaction score documented in analysis
- **ALWAYS write final report** (`doc/[feature]-analysis.md`) automatically after the final phase — in the same commit as the plan update. Hook blocks commit without it.
- **NEVER use `subagent_type: "eval"` or `subagent_type: "architect"`** — these agent types do not exist. If used, they trigger an internal retry that costs $1-2 per failed call. Always use `general-purpose` and inject the skill content into the prompt.

### What do you do when the hook blocks?

```
Hook blocks src/ edit because IMPLEMENTED exists in plan
→ CORRECT: Run review agent + test agent in parallel. Update plan. Then optionally Task(dev-agent).
→ WRONG:   Change plan to APPROVED to remove the block.

Hook blocks because APPROVED + CONDITIONAL APPROVAL in Review line
→ CORRECT: Task(dev-agent, fix findings) → re-QC in parallel → update Review line with new report → APPROVED
→ WRONG:   Write code yourself. Keep CONDITIONAL APPROVAL line unchanged.
```

---

## Commands

```bash
# Build + test
# [add your build command here]

# Run locally
# [add your run command here]

# CI status
gh run list --limit 5
gh run view --log-failed
```
