# Transaction Compliance Service — Final Analysis

**Date:** 2026-05-04
**Holdout satisfaction:** 5/5 (100%)
**Total tests:** 36/36

---

## Timing Table

| Phase | Activity | Duration (approx) |
|-------|----------|-------------------|
| Planning | Architect agent — plan + 5 holdout scenarios | ~75s |
| Plan commit | Orchestrator writes scenarios, git commit | ~30s |
| F1 | Dev agent — domain models + interfaces | ~136s |
| F1 QC | Review + test in parallel (1 round) | ~60s |
| F2 | Dev agent — 4 rules + pipeline + unit tests | ~192s |
| F2 QC | Review + test in parallel (1 round) | ~35s |
| F3 | Dev agent — API layer + middleware + integration tests | ~286s |
| F3 QC round 1 | Review + test (CONDITIONAL APPROVAL) | ~65s |
| F3 fix | Dev agent — revert net10→net8, add validation | ~866s |
| F3 QC round 2 | Review + test (APPROVED) | ~38s |
| F4 | Dev agent — Dockerfile + GitHub Actions | ~65s |
| F4 QC round 1 | Review + test (CONDITIONAL APPROVAL) | ~90s |
| F4 fix | Dev agent — add artifact upload step | ~20s |
| F4 QC round 2 | Review + test (APPROVED) | ~35s |
| Holdout eval | Eval agent — 5 scenarios × 10 sub-checks | ~70s |

**Total estimated:** ~2,100s (~35 minutes)

---

## Review Analysis

### Findings Per Phase

| Phase | Round | Finding | Severity | Resolved |
|-------|-------|---------|----------|---------|
| F1 | 1 | Solution only contains Core + Core.Tests (expected for F1) | Minor/info | N/A |
| F2 | 1 | Sender country not screened (out of scope per spec) | Minor/info | N/A |
| F2 | 1 | Test helper `FixedResultRule` hardcodes rule name strings | Minor | Accepted |
| F3 | 1 | `Api.csproj` and `Api.Tests.csproj` upgraded to `net10.0` (diverges from net8 decision) | **Major** | Fixed in F3 round 2 |
| F3 | 1 | Input validation incomplete — null Sender/Receiver/Currency not guarded (→ 500 instead of 400) | **Major** | Fixed in F3 round 2 |
| F3 | 1 | Validation test coverage gaps | Minor | Fixed with 3 new tests |
| F3 | 1 | Generic `IEnumerable<string>` DI registration for SanctionedCountryRule | Minor | Fixed with factory lambda |
| F3 | 2 | Exception handler excluded from Development env (intentional) | Minor | Accepted |
| F4 | 1 | CI `.trx` results not uploaded as artifact (DORA audit gap) | **Major** | Fixed in F4 round 2 |
| F4 | 1 | Publish output path verbose | Minor | Accepted |

### Finding Type Summary

| Type | Count |
|------|-------|
| Major (required fix) | 3 |
| Minor (accepted/info) | 7 |

### Most Common Finding Types

1. **Framework/runtime drift** — dev agent upgraded to net10.0 (local SDK) without respecting established net8 decision
2. **Incomplete validation** — endpoint guarded only one field; null inputs on required composite types not covered
3. **CI audit trail** — test results generated but not persisted; missing artifact upload

### Takeaway

Two of three major findings were caused by the dev agent adapting to the local environment (net10 SDK) rather than the established architectural decision. The third (incomplete validation) is a classic endpoint boundary failure — records bind nullable properties without enforcing requiredness. These are recurring patterns in agent-generated API code.

---

## Test Analysis

| Phase | Tests Added | Green/Total |
|-------|------------|-------------|
| F1 | 1 (placeholder) | 1/1 |
| F2 | +22 (rules + pipeline) | 23/23 |
| F3 | +10 integration (first pass) | 33/33 |
| F3 fix | +3 validation tests | 36/36 |
| F4 | 0 (infra only) | 36/36 |

**Holdout evaluation:** 5/5 scenarios satisfied (100%)
Sub-checks evaluated: 10 (boundary values, negative cases, combined rule priority)

---

## Process Improvements

### For `developer/SKILL.md`

1. **Add to C#/.NET checklist:** "When targeting `net8.0` on a machine with a newer SDK, do NOT upgrade the target framework. Add `runtimeconfig.template.json` with `rollForward: Major` instead — or note the discrepancy and let the orchestrator decide."

2. **Add to ASP.NET Core checklist:** "Validate ALL required reference-type fields at the endpoint boundary before calling the pipeline — not just string fields. Records do not enforce non-null on binding. Add explicit null-guards for composite types like `Sender`, `Receiver`, etc. and return 400."

3. **Add to GitHub Actions checklist:** "Always include `actions/upload-artifact` after test step with `if: always()` and `path: **/*.trx` — test evidence must be persisted for DORA compliance."

### For `architect/SKILL.md`

1. **Add to principles:** "For API phases, specify in the acceptance criteria that all required request fields (including composite types) must return 400 on null/missing — not just string fields. This prevents the common partial-validation pattern."

2. **Add to principles:** "When writing phase F_n acceptance criteria that include a CI step: explicitly include 'CI uploads test artifacts' as a criterion. This ensures the dev agent includes the upload-artifact step without waiting for review to catch it."
