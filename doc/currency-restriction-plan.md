# currency_restriction: Architecture and Plan
**Date:** 2026-05-21

---

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Allowed currencies | `NOK`, `EUR`, `USD`, `GBP` (static set) | Defined by rule spec; no external config needed at this stage |
| Comparison | `StringComparer.OrdinalIgnoreCase` | ISO 4217 is uppercase by convention but defensive matching is free |
| DI registration | Inline `HashSet<string>` passed to constructor | Mirrors `SanctionedCountryRule` pattern; no new interface needed |
| Pipeline status | `currency_restriction` → `Rejected` | Spec: any non-permitted currency is Rejected |
| Severity | `High` | Specified in feature request |

---

## New Types / Endpoints

No new types or endpoints. Pure rule addition.

**Modified files:**
- `src/Core/Rules/CurrencyRestrictionRule.cs` — new file
- `src/Core/Pipeline/ScreeningPipeline.cs` — add `currency_restriction` → `Rejected` case
- `src/Api/Program.cs` — register `CurrencyRestrictionRule` in DI
- `tests/Core.Tests/Rules/CurrencyRestrictionRuleTests.cs` — new file
- `tests/Api.Tests/ScreeningEndpointCurrencyTests.cs` — new file

---

## Dependency Graph

```
F1: Rule + pipeline + DI
        ↓
F2: Tests (unit + integration)
```

F2 depends on F1.

---

## Implementation Plan

### F1: Rule implementation, pipeline mapping, and DI registration
**QC-status:** NOT STARTED
**Delivers:** `CurrencyRestrictionRule`, updated `ScreeningPipeline.DetermineStatus`, DI wiring in `Program.cs`

**Files:**
- `src/Core/Rules/CurrencyRestrictionRule.cs` (new)
- `src/Core/Pipeline/ScreeningPipeline.cs` (update `DetermineStatus`)
- `src/Api/Program.cs` (register rule)

**Acceptance Criteria:**
- [ ] `CurrencyRestrictionRule` implements `IScreeningRule`; constructor accepts `IEnumerable<string>` allowed currencies and stores as `IReadOnlySet<string>` with `OrdinalIgnoreCase`
- [ ] `EvaluateAsync` returns `RuleStatus.Triggered` / `RuleSeverity.High` / `"Currency is not permitted"` when `request.Currency` is not in the allowed set
- [ ] `EvaluateAsync` returns `RuleStatus.Passed` when currency is in the allowed set
- [ ] `ScreeningPipeline.DetermineStatus` maps `"currency_restriction"` → `hasRejected = true`
- [ ] `Program.cs` registers `CurrencyRestrictionRule` with the four permitted currencies (`NOK`, `EUR`, `USD`, `GBP`)
- [ ] `docker build --target test` passes with no new compilation errors

---

### F2: Unit tests and integration tests
**QC-status:** NOT STARTED
**Delivers:** Full test coverage for `CurrencyRestrictionRule` and end-to-end via `WebApplicationFactory`

**Files:**
- `tests/Core.Tests/Rules/CurrencyRestrictionRuleTests.cs` (new)
- `tests/Api.Tests/ScreeningEndpointCurrencyTests.cs` (new)

**Acceptance Criteria:**
- [ ] Unit tests cover: permitted currencies (NOK, EUR, USD, GBP each) → `RuleStatus.Passed`
- [ ] Unit tests cover: non-permitted currency (e.g. `JPY`, `CHF`) → `RuleStatus.Triggered`, `RuleSeverity.High`
- [ ] Unit tests cover: case-insensitive match (`nok`, `Eur`) → `RuleStatus.Passed`
- [ ] Integration test: POST `/api/v1/screen` with `Currency: "JPY"` → HTTP 200, `status: "Rejected"`, rules array contains `currency_restriction` triggered
- [ ] Integration test: POST `/api/v1/screen` with `Currency: "NOK"` → `currency_restriction` passes (status not Rejected from this rule alone)
- [ ] Integration test: null or empty `Currency` → response includes `currency_restriction` triggered (non-permitted)
- [ ] `docker build --target test` green

---

## Holdout Scenario

**File:** `scenarios/06-currency-restriction.md`
**Scenario:** Transaction in Japanese Yen (JPY) — a currency not on the permitted list — must be Rejected with `currency_restriction` triggered.

**Request:**
```json
POST /api/v1/screen
{
  "transactionId": "TX-CURRENCY-001",
  "sender":   { "accountId": "ACC-S-01", "name": "Taro Yamamoto", "country": "JP" },
  "receiver": { "accountId": "ACC-R-01", "name": "Oslo AS",       "country": "NO" },
  "amount": 5000000,
  "currency": "JPY"
}
```

**Expected status:** `Rejected`
**Active rule:** `currency_restriction` with `RuleStatus.Triggered` and `RuleSeverity.High`

---

## Risks

| # | Risk | Mitigation |
|---|------|------------|
| 1 | `DetermineStatus` precedence — if `currency_restriction` and `pep_check` both trigger, `Rejected` must win | Verify `hasRejected` check comes before `hasPendingReview` in priority chain |
| 2 | Null/empty `Currency` — current model allows any string | Rule must treat null/empty as non-permitted and trigger `Rejected` |
| 3 | Future allowed-currency list changes require a code deploy | Acceptable for now; list is a constructor parameter to ease future externalisation |
