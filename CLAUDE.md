# CLAUDE.md — Transaction Compliance Service

## Product

**Transaction Compliance Service** — a REST API that screens transactions against compliance rules (amount thresholds, sanctions lists, PEP checks, geographic restrictions) and logs all decisions for audit.

Typical DNB Liv task: well-defined rule logic, requires precision and full traceability. DORA and Solvency II relevant.

### Compliance Rules

Detailed rule logic with thresholds, pipeline mapping, and checklist: **`doc/domain-rules.md`**

| RuleName | ScreeningStatus | Short description |
|----------|----------------|-------------|
| `amount_threshold` | `Flagged` | > 100,000 NOK |
| `sanctioned_country` | `Rejected` | KP/IR/SY/CU/RU |
| `cumulative_daily_limit` | `Flagged` | > 500,000 NOK/day per sender |
| `pep_check` | `PendingReview` | PEP + > 50,000 NOK |
| `currency_restriction` | `Rejected` | Only NOK/EUR/USD/GBP permitted |
| `duplicate_transaction` | `Rejected` | Same TransactionId within 24 h |

All amounts are handled as `long` (cents/øre) — never `decimal`.

### API Contract

```csharp
// POST /api/v1/screen
record ScreeningRequest(
    string TransactionId,
    PartyInfo Sender,
    PartyInfo Receiver,
    long Amount,       // cents/øre
    string Currency,   // ISO 4217
    string? Description = null
);

record PartyInfo(string AccountId, string Name, string Country); // ISO 3166-1 alpha-2

// Response
record ScreeningResponse(
    string RequestId,
    string TransactionId,
    ScreeningStatus Status,      // Approved | Rejected | Flagged | PendingReview
    DateTimeOffset Timestamp,
    IReadOnlyList<RuleResult> Rules
);

record RuleResult(string Rule, RuleStatus Status, RuleSeverity Severity, string Message);

// GET /api/v1/screen/{requestId}  — retrieve previous result
// GET /health                      — health check
```

---

## Stack

| Component | Choice |
|-----------|------|
| Runtime | .NET 8, ASP.NET Core |
| API style | Minimal API (IEndpointRouteBuilder extensions) |
| Language | C# 12, nullable reference types enabled |
| Solution structure | `src/Api`, `src/Core`, `src/Infrastructure`, `tests/Api.Tests`, `tests/Core.Tests` |
| Testing | xUnit + FluentAssertions + WebApplicationFactory |
| Screening rule pattern | `IScreeningRule` pipeline — rules registered in DI |
| Traceability | Request-ID middleware (X-Request-Id header) |
| Audit logging | `ILogger<T>` — all decisions (approved/rejected/flagged) |
| CI/CD | GitHub Actions (dotnet build + test + format) |
| PEP service | Mock via interface (HttpClient-based production impl. omitted) |

### Established Decisions (do not reopen)

- Amounts always `long` (cents/øre) — no `decimal`, no `double`
- All API responses: `{ requestId, transactionId, status, timestamp, rules[] }`
- All success status codes: 200 OK (not 201 for screening — it is a query, not a resource creation)
- `IScreeningRule` is the contract — new rules are added without modifying the pipeline
- PEP service is always behind an interface — never direct HttpClient calls from the rule

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
| New rule overview with > 5 rules | Move to `doc/domain-rules.md` |
| New subsystem with its own responsibility | Create `src/[module]/CLAUDE.md` |

### What belongs where

| Content | Location |
|---------|-----------|
| Protocol, established decisions, architecture map | `CLAUDE.md` (root) |
| Compliance rules with thresholds and logic | `doc/domain-rules.md` |
| C# checklists, build environment, test patterns | `.claude/skills/developer/SKILL.md` |
| Architecture principles, phase format | `.claude/skills/architect/SKILL.md` |
| Domain logic per layer | `src/Core/CLAUDE.md`, `src/Api/CLAUDE.md` |

### Hierarchical CLAUDE.md Files

Claude Code reads CLAUDE.md in all parent directories + current directory. Use this when a layer has its own conventions:

```
CLAUDE.md                    ← protocol, established decisions (index)
src/Core/CLAUDE.md           ← IScreeningRule contract, domain rules
src/Api/CLAUDE.md            ← Minimal API conventions, middleware
src/Infrastructure/CLAUDE.md ← PEP mock, CountryLists, external dependencies
```

### New Rule — Update Order

Order is mandatory when adding a new compliance rule:

```
1. Feature description (prompt to orchestrator) — specify full rule logic here
2. Architect agent creates plan
3. Developer agent implements
4. After approved implementation:
   a. Add to doc/domain-rules.md (detailed logic)
   b. Add to CLAUDE.md rule overview (one line)
   c. Add scenario to scenarios/ (holdout)
5. Commit includes all three files
```

CLAUDE.md never receives detailed rule logic — only a table row and a pointer.

---

## Holdout Scenarios — The Core Principle

The `scenarios/` folder contains acceptance criteria written as plain-text HTTP scenarios.

**The dev agent NEVER sees `scenarios/`** (`.claudeignore`). Same principle as train/test split in ML: the agent writing the code cannot read the answer key it is evaluated against. StrongDM discovered that agents who can see the tests write code that games them — including `return true`.

**The eval agent** is a separate agent that:
- Receives the URL to the running API + content from `scenarios/`
- Never sees the source code
- Reports a **satisfaction score**: number of scenarios passing / total

### Running Holdout Evaluation

```bash
# Start API locally
docker build -t compliance-api . && docker run -p 5000:8080 compliance-api

# In a new terminal — dispatch eval agent
# Read .claude/skills/eval/SKILL.md
# Task(eval-agent, "Read scenarios/ and evaluate against http://localhost:5000")
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

The GitHub Actions pipeline posts a failure summary as a PR comment on failure — copy and paste to the dev agent.

---

## Agent Protocol

### New Feature

#### Step 1: Plan (always)

```
Read .claude/skills/architect/SKILL.md
Task(architect-agent, feature description + existing code structure)
Save plan in doc/[feature]-plan.md
Evaluate: Are the phases properly scoped (3-5)? Is the dependency graph correct?

For features that introduce a new compliance rule:
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
Task(eval-agent, .claude/skills/eval/SKILL.md + "API running at http://localhost:5000")
Satisfaction ≥ 80% → proceed to Step 4
Satisfaction < 80% → Task(dev-agent, fix scenario failures) → run eval again
```

#### Step 4: Integration and Commit

```
docker build --target test -t compliance-test . — all phases green
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

Everything runs via Docker — no local dotnet installation required.

```bash
# Build + test (all projects)
docker build --target test -t compliance-test .

# Run API locally
docker build -t compliance-api .
docker run -p 5000:8080 compliance-api
# API available at http://localhost:5000

# CI status
gh run list --limit 5
gh run view --log-failed
```

---

## Sanctions List (demo data)

```csharp
public static class CountryLists
{
    public static readonly string[] SanctionedCountries =
        ["KP", "IR", "SY", "CU", "RU"];

    public static readonly string[] HighRiskCountries =
        [..SanctionedCountries, "AF", "IQ", "LY", "YE", "SO"];
}
```
