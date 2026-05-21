# currency_restriction — Final Analysis
**Date:** 2026-05-21

---

## Timing

| Phase | Activity | Duration (approx.) |
|-------|----------|-------------------|
| Plan | Architect agent | ~30s |
| F1 | Dev agent (rule + pipeline + DI) | ~45s |
| F1 | Review agent | ~15s |
| F1 | Test agent (build + smoke) | ~111s |
| F2 | Dev agent (unit + integration tests) | ~61s |
| F2 | Review round 1 | ~65s |
| F2 | Test round 1 | ~44s |
| F2 | Dev fix round 1 (4 findings) | ~63s |
| F2 | Review round 2 | ~19s |
| F2 | Test round 2 | ~14s |
| F2 | Dev fix round 2 (1 finding) | ~29s |
| F2 | Final review + test | ~25s |
| Eval | Holdout evaluation | ~75s |

**Total:** ~10 minutes end-to-end (excluding Docker startup and Docker not running delay)

---

## Review Analysis

### Findings per phase

| Phase | Round | # Findings | Severities | Verdict |
|-------|-------|-----------|------------|---------|
| F1 | 1 | 2 | 2× Minor | APPROVED |
| F2 | 1 | 4 | 2× Moderate, 2× Minor | CONDITIONAL APPROVAL |
| F2 | 2 | 1 | 1× Moderate (new) | CONDITIONAL APPROVAL |
| F2 | 3 | 0 | — | APPROVED |

### Most common finding types

| Type | Count | Description |
|------|-------|-------------|
| Redundant tests | 2 | Fact tests duplicating Theory assertions (F2 round 1 finding #1, F2 round 2) |
| Missing state isolation | 1 | Hardcoded receiver account ID without Guid suffix |
| Incomplete assertions | 1 | Integration tests missing Severity+Message on triggered results |
| Thin integration coverage | 1 | Only 1 permitted-currency integration test (NOK) instead of all 4 |
| Inline list (no named constant) | 1 | Allowed currencies as inline array in Program.cs |

### Takeaway

The rule implementation itself (F1) was clean — only Minor findings. Test quality (F2) required 2 fix rounds, driven by redundant test duplication. The reviewer consistently caught the "Theory already covers this, delete the Fact" pattern in both rounds. This suggests dev agents default to writing both a Theory AND a standalone Fact for the same case rather than trusting Theory coverage.

---

## Test Analysis

| Phase | New tests | Green/Total |
|-------|-----------|-------------|
| F1 baseline | 0 new | 36/36 |
| F2 initial | 13 new | 49/49 |
| F2 after fix round 1 | — | 51/51 |
| F2 after fix round 2 | −1 (deleted redundant) | 50/50 |
| **Final** | **+14 net** | **50/50** |

**Holdout satisfaction score:** 15/15 (100%)

All 6 scenario files passed including the new `06-currency-restriction.md` scenario. Existing rules unaffected — 0 regressions across 2 fix rounds.

---

## Process Improvements

### 1. Dev agents write redundant Fact+Theory duplicates → update developer SKILL.md

**Finding:** The dev agent wrote 2 standalone `[Fact]` tests that exactly duplicated assertions already present in `[Theory]` blocks. The reviewer caught this in both round 1 and round 2 (the fix introduced a new instance of the same pattern). This cost an extra QC round.

**Action:** Add to `developer/SKILL.md` checklist under Testing:
> - [ ] **No Fact/Theory duplication**: if a `[Theory]` already covers a case (e.g. rule name assertion in `permitted_currency_passes`), do NOT add a standalone `[Fact]` that asserts the same property. The Theory is sufficient.

### 2. Integration tests should assert Severity+Message on triggered rules → update developer SKILL.md

**Finding:** The initial integration tests only asserted `RuleStatus.Triggered` but omitted `Severity` and `Message`. These fields reach the API consumer and should be verified end-to-end.

**Action:** Add to `developer/SKILL.md` checklist under Testing:
> - [ ] **Integration tests assert full triggered rule fields**: for each triggered rule assertion, verify `Rule`, `Status`, `Severity`, and `Message` — not just `Rule` and `Status`. Use `.Single(r => r.Rule == "...")` then assert all fields.

### 3. Permitted-list rules need Theory coverage for all list members → update developer SKILL.md

**Finding:** The initial integration tests covered only 1 of the 4 permitted currencies (NOK). If the DI registration accidentally omitted EUR/USD/GBP, no integration test would fail.

**Action:** Add to `developer/SKILL.md` checklist under Testing:
> - [ ] **Permitted-list rules: Theory covers all list members at integration level**: for any rule with an explicit allow-list (e.g. permitted currencies, sanctioned countries), write a `[Theory]` integration test covering every allowed value — not just one representative.

### 4. Inline allowed lists in Program.cs → update architect SKILL.md

**Finding (Minor):** The allowed currency list `new[] { "NOK", "EUR", "USD", "GBP" }` was inlined in `Program.cs` without a named constant. The existing pattern for `SanctionedCountries` uses `CountryLists`. Minor for now but establishes a precedent.

**Action:** Add to `architect/SKILL.md` under Established Decisions:
> - **Named constants for rule lists**: permitted values (country lists, currency lists) must be in a static constants class (e.g. `CountryLists`, `CurrencyLists`) — not inline array literals in `Program.cs`. Plan must specify the constants class in the file list.
