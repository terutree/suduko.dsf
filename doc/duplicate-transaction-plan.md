# duplicate_transaction: Architecture and Plan

**Date:** 2026-05-21

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| New store interface | `ITransactionSeenStore` in `src/Core/Stores/` | `IScreeningResultStore` is keyed by `requestId`, not `transactionId`. The duplicate rule needs an index on `transactionId` → `DateTimeOffset`. A dedicated store keeps responsibilities separate and follows the existing `IDailyAggregateStore` pattern. |
| Store semantics | `TryRecordAsync(transactionId, seenAt) → bool` — returns `true` on first write, `false` if already recorded within 24 h | Atomic read-then-write avoids a race condition in the in-memory implementation; matches the ConcurrentDictionary TryAdd pattern already in use |
| Rule placement | `src/Core/Rules/DuplicateTransactionRule.cs` | Follows all other rules — no pipeline changes |
| Clock abstraction | `TimeProvider` (.NET 8 built-in) injected into rule and store | Enables deterministic unit tests without date arithmetic hacks |
| Rule ordering | Register as first rule in DI (before `AmountThresholdRule`) | Fail-fast: reject duplicates immediately, avoid unnecessary store writes from downstream rules |
| DetermineStatus mapping | `duplicate_transaction` → `Rejected` | Rule logic specifies Rejected on match |
| Severity | `High` | Specified in feature request |
| Trigger message | `"Transaction has already been screened within the past 24 hours"` | Clear, auditable, includes time frame |

## New Types / Endpoints

```
src/Core/Stores/ITransactionSeenStore.cs
    Task<bool> TryRecordAsync(string transactionId, DateTimeOffset seenAt, CancellationToken ct)
    // Returns true = first occurrence (Passed), false = duplicate within 24 h (Triggered)
    // Implementations must purge entries older than 24 h on read or via a background sweep

src/Core/Rules/DuplicateTransactionRule.cs
    IScreeningRule — injects ITransactionSeenStore, TimeProvider

src/Infrastructure/Stores/InMemoryTransactionSeenStore.cs
    ConcurrentDictionary<string, DateTimeOffset> — keyed by transactionId, value = first-seen timestamp
    TryRecordAsync: TryAdd for new entry; on conflict check if stored timestamp > now − 24 h
```

No new API endpoints — the rule integrates into the existing POST /api/v1/screen pipeline.

## Dependency Graph

```
F1 (store interface + in-memory impl)
    └── F2 (rule class + pipeline wiring + DetermineStatus update)
            └── F3 (unit tests + integration tests)
```

F1 must be complete before F2 can compile. F2 must be complete before F3 can exercise the full pipeline.

## Implementation Plan

### F1: ITransactionSeenStore — interface and in-memory implementation
**QC-status:** APPROVED
**Review:** APPROVED — all moderate findings resolved; minor findings (null guard, seenAt semantics) deferred to F3
**Test:** PASS — 50/50
**Delivers:** The store contract and a thread-safe in-memory implementation that the rule will depend on.
**Files:**
- `src/Core/Stores/ITransactionSeenStore.cs` *(new)*
- `src/Infrastructure/Stores/InMemoryTransactionSeenStore.cs` *(new)*

**Acceptance Criteria:**
- [ ] `ITransactionSeenStore` is in `TransactionCompliance.Core.Stores` namespace
- [ ] `TryRecordAsync(string transactionId, DateTimeOffset seenAt, CancellationToken ct) → Task<bool>` returns `true` for a brand-new `transactionId`
- [ ] `TryRecordAsync` returns `false` when the same `transactionId` was previously recorded within the past 24 hours (seenAt − stored timestamp < 24 h)
- [ ] `TryRecordAsync` returns `true` (not duplicate) when the same `transactionId` was seen more than 24 hours ago (treated as expired — the old entry is replaced)
- [ ] `InMemoryTransactionSeenStore` uses `ConcurrentDictionary<string, DateTimeOffset>` to guarantee thread safety
- [ ] `InMemoryTransactionSeenStore` accepts `TimeProvider` via constructor to allow clock injection in tests
- [ ] All comparisons use UTC (`DateTimeOffset.UtcNow` via `TimeProvider.GetUtcNow()`)

---

### F2: DuplicateTransactionRule — rule, pipeline wiring, DetermineStatus
**QC-status:** APPROVED
**Review:** APPROVED — two moderate pre-existing findings noted (DateTime.UtcNow in pipeline, no actual short-circuit); no acceptance criteria failures
**Test:** PASS — 50/50
**Delivers:** The rule class, its registration in DI, and the `DetermineStatus` switch arm for `duplicate_transaction`.
**Files:**
- `src/Core/Rules/DuplicateTransactionRule.cs` *(new)*
- `src/Core/Pipeline/ScreeningPipeline.cs` *(edit — add `duplicate_transaction` → `Rejected` arm)*
- `src/Api/Program.cs` *(edit — register `ITransactionSeenStore`, `TimeProvider`, and `DuplicateTransactionRule` as first rule)*

**Acceptance Criteria:**
- [ ] `DuplicateTransactionRule` implements `IScreeningRule`
- [ ] Constructor accepts `ITransactionSeenStore` and `TimeProvider`
- [ ] `EvaluateAsync` calls `TryRecordAsync(request.TransactionId, now, ct)`; if it returns `false` the rule returns `RuleResult("duplicate_transaction", RuleStatus.Triggered, RuleSeverity.High, "Transaction has already been screened within the past 24 hours")`
- [ ] If `TryRecordAsync` returns `true` (first occurrence) the rule returns `RuleResult("duplicate_transaction", RuleStatus.Passed, RuleSeverity.High, "Transaction ID is unique")`
- [ ] `ScreeningPipeline.DetermineStatus` has a `case "duplicate_transaction": hasRejected = true; break;` arm
- [ ] `DuplicateTransactionRule` is registered in DI **before** `AmountThresholdRule` (first `AddSingleton<IScreeningRule>` call)
- [ ] `ITransactionSeenStore` is registered as `Singleton` backed by `InMemoryTransactionSeenStore`
- [ ] `TimeProvider.System` is registered as `Singleton` (if not already present)
- [ ] Posting a transaction with a `transactionId` not seen before returns `status: "Approved"` (assuming other rules pass) with `duplicate_transaction` rule `status: "Passed"`
- [ ] Posting the same `transactionId` a second time returns `status: "Rejected"` with `duplicate_transaction` rule `status: "Triggered"`

---

### F3: Unit tests and integration tests
**QC-status:** APPROVED
**Review:** APPROVED — duplicate test removed; all acceptance criteria met
**Test:** PASS — 58/58
**Delivers:** Full test coverage for the store, rule, and end-to-end pipeline behaviour.
**Files:**
- `tests/Core.Tests/Rules/DuplicateTransactionRuleTests.cs` *(new)*
- `tests/Core.Tests/Stores/InMemoryTransactionSeenStoreTests.cs` *(new)*
- `tests/Api.Tests/ScreeningEndpointDuplicateTests.cs` *(new)*

**Acceptance Criteria:**
- [ ] `InMemoryTransactionSeenStoreTests`: first call returns `true`; second call within 24 h returns `false`; call after 24 h + 1 s returns `true` (expired); uses `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` or manual subclass
- [ ] `DuplicateTransactionRuleTests`: rule returns `Triggered` when store returns `false`; rule returns `Passed` when store returns `true`; uses a mock/stub for `ITransactionSeenStore`
- [ ] `ScreeningEndpointDuplicateTests`: integration test posts the same `TransactionId` twice via `WebApplicationFactory`; first response is `Approved`; second response is `Rejected` with `duplicate_transaction` rule `Triggered`; a third `TransactionId` that is different passes cleanly
- [ ] All existing tests continue to pass (no regression)
- [ ] Tests use unique `transactionId` values per test (e.g. `Guid.NewGuid()` suffix) to avoid cross-test state contamination in the shared in-memory store

---

## Holdout Scenario

**File:** `scenarios/07-duplicate-transaction.md`
**Scenario:** First submission approved; second submission with same TransactionId within 24h rejected.

## Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Race condition on first-seen write | Low — `ConcurrentDictionary.TryAdd` is atomic | Rely on `TryAdd` semantics; document in implementation |
| Cross-test state pollution in integration tests | Medium — `WebApplicationFactory` shares singleton store across tests in same fixture | Use unique `transactionId` per test; document in test class |
| `TimeProvider` not registered when other tests create the `WebApplicationFactory` | Low | Register `TimeProvider.System` unconditionally in `Program.cs`; existing test factory inherits it |
| 24-hour window drift across DST or leap seconds | Negligible for in-memory UTC arithmetic | All comparisons use `DateTimeOffset` UTC — no local time |
| Memory growth in long-running instances | Low (demo/workshop scope) | No mitigation needed for in-memory demo |
