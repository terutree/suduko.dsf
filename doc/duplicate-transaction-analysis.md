# duplicate_transaction — Final Analysis

**Date:** 2026-05-21  
**Feature:** `duplicate_transaction` compliance rule  
**Holdout satisfaction:** 7/7 (100%)

---

## Timing Table

| Phase | Activity | Notes |
|-------|----------|-------|
| Plan | Architect agent | Single pass — no revision |
| Scenario | Orchestrator writes scenarios/07 | Before implementation |
| F1 | Dev → Review (CONDITIONAL) → Fix → Review (REJECTED: TOCTOU) → Fix → Review (APPROVED) | 3 review rounds |
| F2 | Dev → Review (APPROVED) → Test (PASS) | Clean pass |
| F3 | Dev → Review (CONDITIONAL: duplicate test) → Fix → Review (APPROVED) → Test (PASS) | 2 review rounds |
| Holdout | Eval agent | 100% — no remediation needed |

---

## Review Analysis

### Findings per phase

| Phase | # | Severity | Finding | Resolved |
|-------|---|----------|---------|---------|
| F1 | 1 | Mo | Closed-over `isDuplicate` in `AddOrUpdate` factory — CAS retry hazard | Yes |
| F1 | 2 | Mo | `CancellationToken` accepted but never observed | Yes |
| F1 | 3 | Mi | `seenAt` semantics split (caller and store both read clock) | Deferred — acceptable |
| F1 | 4 | Mi | No null guard on `transactionId` | Deferred to F3 |
| F1 (round 2) | 5 | Mo | TOCTOU race on expired entry: non-atomic read-check-write in expiry branch | Yes — `TryRemove(KeyValuePair)` |
| F2 | 6 | Mo | Pre-existing `DateTime.UtcNow` in pipeline vs `TimeProvider` in rule | Pre-existing, noted |
| F2 | 7 | Mo | Pipeline comment claims short-circuit but all rules still run for duplicates | Pre-existing design, noted |
| F3 | 8 | Fail | `requestId_differs_between_first_and_second_post` was subset of existing test | Yes — removed |

### Most common finding types

1. **Concurrency correctness** (F1): Two findings around `ConcurrentDictionary` usage — first a closure mutation hazard, then a TOCTOU on the expiry path. Both required architectural changes, not just one-line fixes.
2. **Test duplication** (F3): Standalone test asserting a fact already covered by a broader test.
3. **Pre-existing clock inconsistency** (F2): `DateTime.UtcNow` in `ScreeningPipeline` vs `TimeProvider` in new rule — surfaced by the new rule's correct usage.

### Takeaway

The concurrency pattern for `ConcurrentDictionary` with expiry is non-trivial. The dev agent's initial `AddOrUpdate`-with-closure approach was a common mistake; the reviewer caught it. The subsequent `TryAdd` refactor introduced a TOCTOU in the expiry path — also caught by the reviewer. The final `TryRemove(KeyValuePair<K,V>)` pattern is correct and idiomatic. This class of finding (concurrent expiry logic) should be added to the developer checklist.

---

## Test Analysis

| Phase | New tests | Total | Green/Total |
|-------|-----------|-------|-------------|
| F1 | 0 | 50 | 50/50 |
| F2 | 0 | 50 | 50/50 |
| F3 | +8 (3 store + 2 rule + 3 integration) | 58 | 58/58 |
| Holdout | 15 test cases across 7 scenarios | — | 15/15 |

All 58 automated tests pass. Holdout: 100% satisfaction.

---

## Process Improvements

### → developer/SKILL.md

Add to **Checklist: C# / .NET**:

> **`ConcurrentDictionary` expiry pattern:** When implementing a time-windowed cache with expiry, the `AddOrUpdate` factory with a captured mutable variable is fragile — the factory can be retried by the CAS loop. Use `TryAdd` for the fast path, then `TryRemove(KeyValuePair<K,V>)` for atomic compare-and-delete on the expiry branch. Never use a closed-over `bool` to communicate the result of a factory.

Add to **Checklist: Testing**:

> **No standalone test for a sub-assertion:** If a `[Fact]` asserts only a subset of what an existing test already asserts (e.g., `requestId` uniqueness that is already checked at the end of a duplicate-detection test), remove the standalone test — it adds noise without coverage.

### → architect/SKILL.md

Add to **Principles**:

> **Time-windowed deduplication stores:** When designing a store that expires entries after a time window (e.g., `duplicate_transaction`), explicitly specify the atomic replacement pattern in the plan: `TryAdd` for new entries, `TryRemove(KeyValuePair<K,V>)` for expired-entry replacement. Do not leave the expiry mechanism unspecified — dev agents default to `AddOrUpdate` which has a CAS-retry hazard.
