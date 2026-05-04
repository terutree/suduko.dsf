# DSF Runbook — Replicating the Experiment Cycles

Commands to re-run the two documented cycles in a clean setup.
Order is mandatory. A new cycle does not start before the previous one is committed.

---

## Preparation

```bash
cd dnb-compliance-dsf
claude --dangerously-skip-permissions

# Tail log in a separate terminal window
tail -f .claude/logs/agent.log
```

---

## Cycle 1: Transaction Compliance Service — Initial Build

**Expected time:** ~60–80 min (improved setup may reduce from 71 min)  
**Agents:** architect, 4× dev, 2× review, 2× eval

### Prompt to the Orchestrator

CLAUDE.md is read automatically at startup and contains everything — stack, rules, API contract, agent protocol. A minimal prompt is sufficient:

```
Build Transaction Compliance Service.
```

Verbose version (useful for demo where observers need to understand what is happening):

```
Read CLAUDE.md and doc/domain-rules.md.

Build Transaction Compliance Service as a .NET 8 solution according to CLAUDE.md.
Implement all five compliance rules from domain-rules.md with full test coverage.
The project should be production-ready with GitHub Actions CI and Docker support.

Start with architect agent:
Read .claude/skills/architect/SKILL.md
Create implementation plan in doc/transaction-compliance-plan.md

Implement phase by phase per the agent protocol in CLAUDE.md.
```

### Expected Flow

```
Step 1  → architect agent creates plan (3–5 phases + dependency graph)
Step 2a → dev agent F1: domain layer (rules, pipeline, types, Core.Tests)
Step 2b → review agent + test agent in parallel
Step 2a → dev agent F2: infrastructure (PEP mock, stores)
Step 2b → review agent + test agent in parallel
Step 2a → dev agent F3: API layer + integration tests
Step 2b → review agent + test agent in parallel
Step 2a → dev agent F4: GitHub Actions CI + Dockerfile
Step 3  → eval agent holdout (target: ≥ 80%)
Step 4  → commit
Step 5  → doc/transaction-compliance-analysis.md (automatic)
```

### After Run — Verify

```bash
docker build --target test -t compliance-test .
docker build -t compliance-api .
docker run -p 5000:8080 compliance-api

curl -s -X POST http://localhost:5000/api/v1/screen \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "TEST-001",
    "sender": {"accountId": "ACC-001", "name": "Test", "country": "NO"},
    "receiver": {"accountId": "ACC-002", "name": "Test", "country": "NO"},
    "amount": 500000,
    "currency": "NOK"
  }' | jq .
```

Expected: `"status": "Approved"`

```bash
# Sanctioned country
curl -s -X POST http://localhost:5000/api/v1/screen \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "TEST-002",
    "sender": {"accountId": "ACC-001", "name": "Test", "country": "NO"},
    "receiver": {"accountId": "ACC-003", "name": "Evil Corp", "country": "KP"},
    "amount": 10000,
    "currency": "NOK"
  }' | jq .
```

Expected: `"status": "Rejected"`

### Known Deviations from Experiment (resolved in new setup)

Experiment cycle 1 got 60% holdout in the first round due to four spec ambiguities.
These are now incorporated into skills and CLAUDE.md — expecting ≥ 80% in the first round.

| Deviation | Resolution |
|-------|---------|
| PascalCase rule names | `developer/SKILL.md` + `architect/SKILL.md` |
| Wrong `RuleStatus` enum | `developer/SKILL.md` |
| `DailyLimit` got `Rejected` | `architect/SKILL.md` pipeline mapping |
| `ACC-PEP-001` missing in mock | `developer/SKILL.md` |

---

## Cycle 2: Velocity Check — 7-Day Amount Limit

**Expected time:** ~20–30 min  
**Agents:** architect, 2× dev, 1× review, 1× eval

### Step 0: Write Holdout Scenario BEFORE Prompt (new requirement)

Create the file manually OR ask the orchestrator to do it after the plan is created:

```markdown
<!-- scenarios/06-velocity-check.md -->
## Scenario: Velocity Check — Weekly Limit Exceeded

**Request:**
POST /api/v1/screen
{
  "transactionId": "TEST-VELOCITY-001",
  "sender": { "accountId": "ACC-VELOCITY-TEST", "name": "Test Sender", "country": "NO" },
  "receiver": { "accountId": "ACC-REC-001", "name": "Receiver", "country": "NO" },
  "amount": 100000001,
  "currency": "NOK"
}

**Expected status:** Flagged
**Active rule:** velocity_check
**Rationale:** 100,000,001 cents (1,000,000.01 NOK) exceeds the 7-day limit of 100,000,000 cents (1,000,000 NOK).
```

```bash
# Commit scenario BEFORE implementation
git add scenarios/06-velocity-check.md
git commit -m "test: holdout scenario for velocity_check"
```

### Prompt to the Orchestrator

Minimal prompt:

```
New compliance rule: velocity_check.
```

Verbose version:

```
Read CLAUDE.md and doc/domain-rules.md.

New compliance rule: velocity_check.

Rule: If the total amount sent from an account in the last 7 calendar days
exceeds 1,000,000 NOK (100,000,000 cents), the transaction is flagged.
Rolling 7-day window (not a fixed week) — UTC dates.
Accumulate amount BEFORE check (include current transaction).
RuleName: velocity_check
ScreeningStatus on match: Flagged
Severity: High

Holdout scenario is already written: scenarios/06-velocity-check.md

Start with architect agent:
Read .claude/skills/architect/SKILL.md
Create implementation plan in doc/velocity-check-plan.md

Implement phase by phase per the agent protocol in CLAUDE.md.
```

### Expected Flow

```
Step 1  → architect agent (plan incl. IVelocityStore, VelocityCheckRule)
Step 2a → dev agent F1: Core (IVelocityStore, VelocityCheckRule, Core.Tests)
Step 2b → review agent + test agent in parallel
Step 2a → dev agent F2: Infrastructure (InMemoryVelocityStore, DI, API registration)
Step 2b → review agent + test agent in parallel
Step 3  → eval agent holdout against scenarios/06
Step 4  → commit
Step 5  → doc/velocity-check-analysis.md (automatic)
```

### After Run — Verify Velocity

```bash
docker run -p 5000:8080 compliance-api

# Large transaction — should be flagged
curl -s -X POST http://localhost:5000/api/v1/screen \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "TEST-VELOCITY-001",
    "sender": {"accountId": "ACC-VELOCITY-TEST", "name": "Test", "country": "NO"},
    "receiver": {"accountId": "ACC-REC-001", "name": "Recv", "country": "NO"},
    "amount": 100000001,
    "currency": "NOK"
  }' | jq '.status, (.rules[] | select(.rule == "velocity_check"))'
```

Expected: `"Flagged"` + rule with `"status": "Flag"`

---

## Update domain-rules.md After Cycle 2

After velocity_check is committed — verify that velocity_check is already in `doc/domain-rules.md`.
If not, add the section manually and commit:

```bash
git add doc/domain-rules.md
git commit -m "docs: velocity_check confirmed in domain-rules"
```

---

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Hook blocks commit | Read terminal output — it explains what is missing |
| Holdout < 80% | Read eval report. Rule name snake_case? RuleStatus.Pass/Flag? Pipeline mapping correct? |
| Docker build fails | `docker build --target test 2>&1 \| tail -50` |
| CI fails | `gh run view --log-failed` → copy output to orchestrator |
| Agent writes code directly | Remind of orchestrator role: all code via Task agents |
