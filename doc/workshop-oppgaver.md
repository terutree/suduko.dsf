# Workshop: Hands-on with Dark Software Factory

**Time:** ~55 min  
**Repo:** `dnb-compliance-dsf-run-en` (shared by facilitator)

---

## Start Claude in Dark Mode

Run this in the root of the repo **before starting the tasks:**

```bash
claude --dangerously-skip-permissions
```

> `--dangerously-skip-permissions` lets agents run without asking for permission on each operation. Required for autonomous execution. Use only in safe, local environments.

```bash
# Tail agent log in a separate window (optional)
tail -f .claude/logs/agent.log
```

---

## What the Factory Has Already Built

One complete DSF cycle has been run and documented. This is the reference for what you will see during the demo.

### Cycle 1: Transaction Compliance Service — Initial Build

**Date:** 2026-05-04 | **Agents:** architect, 4× dev, 4× review, 4× test, 1× eval

| Phase | Activity | Time |
|-------|----------|------|
| Step 1 | Architecture and plan (4 phases) + 5 holdout scenarios | ~1 min 45s |
| F1 | Domain layer — types, interfaces | ~2 min 16s |
| F1 QC | Review + test (1 round — APPROVED) | ~1 min |
| F2 | 4 rules + pipeline + 22 unit tests | ~3 min 12s |
| F2 QC | Review + test (1 round — APPROVED) | ~35s |
| F3 | API layer + middleware + 13 integration tests | ~4 min 46s |
| F3 QC R1 | Review + test (CONDITIONAL APPROVAL) | ~1 min 5s |
| F3 fix | Dev agent fixes net10→net8 + validation | ~14 min 26s |
| F3 QC R2 | Review + test (APPROVED) | ~38s |
| F4 | Dockerfile multi-stage + GitHub Actions | ~1 min 5s |
| F4 QC R1 | Review + test (CONDITIONAL APPROVAL) | ~1 min 30s |
| F4 fix | Dev agent adds artifact upload step | ~20s |
| F4 QC R2 | Review + test (APPROVED) | ~35s |
| Holdout | Eval agent → **5/5 (100%)** | ~1 min 10s |
| **Total** | | **~35 min** |

**What the review agents caught:**

| Finding | Severity | Phase | Resolved |
|---------|----------|-------|----------|
| Dev agent upgraded to `net10.0` (local SDK) — breaks net8 decision | Major | F3 | ✅ Fixed |
| Null validation missing on `Sender`/`Receiver` — returned 500 instead of 400 | Major | F3 | ✅ Fixed |
| `.trx` results not uploaded in CI — DORA audit gap | Major | F4 | ✅ Fixed |

**Test results:**
- 36/36 unit + integration tests green
- 5/5 holdout scenarios passed (100%)

> **Key point:** All three major findings were caught by the review agent and fixed before holdout evaluation. Holdout ran against a clean build.

---

## Track 1: Foundational — "AI adds a rule for me" (~45 min)

**Goal:** Experience a complete DSF cycle. Give the agent one well-specified task and follow what happens.

### Task 1A: Currency Restriction (recommended for everyone)

Paste this prompt directly into Claude:

```
Read CLAUDE.md.
New compliance rule: currency_restriction.

Rule: Only NOK, EUR, USD, and GBP are permitted currencies.
Transactions in other currencies are rejected.
RuleName: currency_restriction
ScreeningStatus on match: Rejected
Severity: High

Start with architect agent, create plan in doc/currency-restriction-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

**Watch for:**
- Which phases does the architect agent create?
- Which edge cases does the dev agent write tests for?
- What does the review agent find?

**Discussion questions after run:**
- Did you need to correct the agent along the way?
- Would the code pass code review in your team?

---

### Task 1B: Duplicate Transaction Check (alternative)

```
Read CLAUDE.md.
New compliance rule: duplicate_transaction.

Rule: The same TransactionId cannot be screened more than once
within 24 hours. Duplicates are rejected with Rejected.
RuleName: duplicate_transaction
ScreeningStatus on match: Rejected
Severity: High

Start with architect agent, create plan in doc/duplicate-transaction-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

---

## Track 2: Extended — "I specify, AI delivers" (~55 min)

**Goal:** Write your own specification and observe what happens when the spec is vague vs. precise.

### Task 2A: Geographic Risk Assessment

You decide the details — but the prompt MUST specify:
- Which countries are high risk?
- What status does the rule produce?
- Amount threshold or all amounts?
- RuleName (snake_case)

Starting point:

```
Read CLAUDE.md.
New compliance rule: geo_risk_flag.

[Fill in the rule logic yourself — be as precise as possible]

Start with architect agent, create plan in doc/geo-risk-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

> **Tip:** Start vague on purpose for one run, see what the agent guesses. Run again with a precise spec. Compare the results.

---

### Task 2B: Round Amount Screening (high-value round amounts)

Transactions with "suspiciously round" amounts are a well-known AML pattern.

```
Read CLAUDE.md.
New compliance rule: round_amount_flag.

Rule: Transactions where the amount (in NOK) is exactly divisible by
10,000 AND amount > 50,000 NOK are flagged for manual review.
Examples: 50,000, 100,000, 200,000 → flagged.
51,000, 99,999 → not flagged.
RuleName: round_amount_flag
ScreeningStatus on match: Flagged
Severity: Medium

Start with architect agent, create plan in doc/round-amount-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

---

### Task 2C: CI Failure and Self-Healing

Run Track 1 or 2A/2B until the code is finished. Then manually introduce a failure:

1. Change a threshold value in the code (not in tests)
2. Push — CI fails
3. Give Claude this prompt:

```
CI is failing after the last push. Fetch failure details with gh run view --log-failed.
Analyze the failure and fix it. All tests should be green.
```

**Observe:** Does the agent read CI output? Does it identify the root cause without hints?

---

## Track 3: Factory — "The answer key is written BEFORE the code" (~55 min, ambitious)

**Goal:** Experience the train/test separation in practice. Write the holdout scenario BEFORE implementation.

### Step 3.1: Write Scenario Manually (10 min)

Choose one of the rules from Track 2. Create the scenario file BEFORE giving the implementation task:

```markdown
# scenarios/06-[rule-name]-yours.md

## Scenario: [Rule Name]
**Request:**
POST /api/v1/screen
{
  "transactionId": "TEST-[RULE]-001",
  "sender": { "accountId": "ACC-SENDER-001", "name": "Test Sender", "country": "NO" },
  "receiver": { "accountId": "ACC-RECEIVER-001", "name": "Test Receiver", "country": "[country]" },
  "amount": [amount in cents],
  "currency": "[currency]"
}

**Expected status:** [Approved | Rejected | Flagged | PendingReview]
**Active rule:** [rule name in snake_case]
**Rationale:** [Explain why this scenario should produce the expected status]
```

### Step 3.2: Implement Without Showing Scenario (20 min)

Start implementation with the prompt from Track 2. The dev agent never sees `scenarios/`.

### Step 3.3: Run Holdout Evaluation (15 min)

```bash
# Start API
docker build -t compliance-api . && docker run -p 5000:8080 compliance-api

# In Claude — new prompt:
Read .claude/skills/eval/SKILL.md.
Read the scenarios/ folder.
API is running at http://localhost:5000.
Evaluate all scenarios and report satisfaction score.
```

**Discussion questions:**
- Did the scenario you wrote pass? If not — what was missing from the specification?
- What would have happened if the agent had seen the answer key?

### Step 3.4 (bonus): Modify a Skill and Run a New Rule

Add an item to `.claude/skills/developer/SKILL.md`. Run a new rule. See if the agent follows the new item automatically.

---

## Facilitator Tips

**Common problems:**

| Problem | Solution |
|---------|---------|
| Agent writes code directly (not via Task) | Remind: "You are the orchestrator — dispatch to agents" |
| CI fails due to formatting | `docker build --target test` locally before push |
| Hooks block commit | Read the error message — it explains what is missing |
| Agent repeats itself | Give it the actual error output, not just "try again" |

**Good reflection questions at the end:**
- Which tasks in your team most resemble this?
- What did you need to specify more precisely than you expected?
- What surprised you — positively or negatively?
- Where would you introduce human review in a real pipeline?
