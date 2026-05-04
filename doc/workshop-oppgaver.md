# Workshop: Hands-on with Dark Software Factory

**Time:** ~55 min  
**Repo:** `dnb-compliance-dsf` (shared by facilitator)  
**Start:** `claude --dangerously-skip-permissions` in the root folder

You are working on top of a completed Transaction Compliance Service with five active rules. Choose a track based on ambition — everyone builds on the same codebase.

```bash
# Tail log in a separate window
tail -f .claude/logs/agent.log
```

---

## What the Factory Has Already Built

Two complete DSF cycles have been run and documented. This is the reference for what you will see during the demo.

### Cycle 1: Transaction Compliance Service — Initial Build

**Date:** 2026-04-27 | **Agents:** architect, 4× dev, 2× review, 2× eval

| Phase | Activity | Time |
|------|-----------|-----|
| Step 1 | Architecture and plan (4 phases) | ~10 min |
| F1 | Domain layer — rules, pipeline, types | ~10 min |
| F1 | Review + Docker fix | ~10 min |
| F2 | Infrastructure — stores, PEP mock | ~18 min |
| F3 | API layer + integration tests | ~7 min |
| F4 | GitHub Actions CI | ~2 min |
| Holdout R1 | Eval agent → **60% (3/5)** — 4 deviations | ~2 min |
| Fix | Dev agent fixes all 4 deviations | ~5 min |
| Holdout R2 | Eval agent → **89% (8/9)** — APPROVED | ~2 min |
| **Total** | | **~71 min** |

**What the holdout agent caught that unit tests did not:**
- All 4 rules used PascalCase `RuleName` (`AmountLimit`) — scenarios expect snake_case (`amount_threshold`)
- `RuleStatus` enum was named `Passed`/`Failed` — scenarios expect `Pass`/`Flag`
- `cumulative_daily_limit` got `Rejected` in pipeline — scenarios expect `Flagged`
- `InMemoryPepService` used `PEP-001` as ID — scenario used `ACC-PEP-001`

> **Key point:** 51/51 unit tests were green. Holdout still found 4 systematic deviations. Without the train/test separation, these would have passed to production.

**Review analysis (F1 — the only phase with findings):**

| Finding | Severity | Verdict |
|------|-------------|-------|
| `async` without `await` in return — "critical bug" | C | False positive — correct code |
| Input validation in domain records | M | Rejected — belongs in API layer |
| DI pattern for compliance limits | M | Rejected — premature abstraction |
| Missing per-rule audit logging | Mo | **Real finding — fixed** |

---

### Cycle 2: Velocity Check — 7-Day Amount Limit

**Date:** 2026-04-27 | **Agents:** architect, 2× dev, 1× review

| Phase | Activity | Time |
|------|-----------|-----|
| Step 1 | Architecture and plan (3 phases) | ~3 min |
| F1 | Core — `IVelocityStore`, rule, tests | ~3 min |
| F1 | Review (0 blocking findings) | ~2 min |
| F2+F3 | Infrastructure + DI + integration test | ~3 min |
| F2+F3 | Docker verification | ~5 min |
| Commits | | ~2 min |
| **Total** | | **~23 min** |

**Improvement from cycle 1 → 2:**
- The dev agent used correct `velocity_check` (snake_case) automatically — the skill update worked
- The dev agent detected and fixed a pre-existing test isolation problem itself — without review flagging it
- 0 blocking review findings

**Remaining gaps (not resolved in cycle 2):**
- No `scenarios/06-velocity-check.md` was written → eval agent had no answer key → holdout not run

> **Key point:** Process improvements from the analysis report after cycle 1 had a direct effect in cycle 2. Skills that learn from their own mistakes — without model retraining.

---

## Track 1: Foundational — "AI adds a rule for me" (~45 min)

**Goal:** Experience a complete DSF cycle. Give the agent one well-specified task and follow what happens.

### Task 1A: Currency Restrictions (recommended for everyone)

Copy this prompt directly into Claude:

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
| Agent repeats itself | Give it the error output explicitly, not just "try again" |

**Good reflection questions at the end:**
- Which tasks in your team most resemble this?
- What did you need to specify more precisely than you expected?
- What surprised you — positively or negatively?
- Where would you introduce human review in a real pipeline?
