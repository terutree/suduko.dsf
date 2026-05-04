# Transaction Compliance Service: Architecture and Plan

**Date:** 2026-05-04

---

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Amount type | `long` (cents/øre) | Avoid floating-point rounding; established decision |
| API style | Minimal API with `IEndpointRouteBuilder` extensions | Established decision |
| Rule pipeline | `IScreeningRule[]` registered in DI, iterated by `IScreeningPipeline` | Open/closed principle — new rules without modifying pipeline |
| Request identity | `X-Request-Id` middleware generates `requestId` before pipeline | Pipeline takes `requestId` as parameter, never generates Guid internally |
| Result storage | `IScreeningResultStore` — `InMemoryScreeningResultStore` (ConcurrentDictionary) | Enables `GET /api/v1/screen/{requestId}` retrieval |
| Daily aggregation | `IDailyAggregateStore` — `InMemoryDailyAggregateStore` | Keyed by `(accountId, utcDate)`, thread-safe increment |
| PEP service | `IPepService` — `InMemoryPepService` with `ACC-PEP-001` | Interface boundary; never direct HTTP from rule |
| Status priority | `Rejected > PendingReview > Flagged > Approved` | Strictest rule wins |
| Test target | Docker multi-stage `test` target runs `dotnet test` | CI-compatible, no local dotnet required |
| All success responses | `200 OK` | Screening is a query, not resource creation |

---

## New Types / Endpoints

```csharp
// Core domain
enum ScreeningStatus { Approved, Flagged, PendingReview, Rejected }
enum RuleStatus      { Passed, Triggered }
enum RuleSeverity    { Low, Medium, High }

record PartyInfo(string AccountId, string Name, string Country);
record ScreeningRequest(string TransactionId, PartyInfo Sender, PartyInfo Receiver,
                        long Amount, string Currency, string? Description = null);
record RuleResult(string Rule, RuleStatus Status, RuleSeverity Severity, string Message);
record ScreeningResponse(string RequestId, string TransactionId, ScreeningStatus Status,
                         DateTimeOffset Timestamp, IReadOnlyList<RuleResult> Rules);

interface IScreeningRule
{
    Task<RuleResult> EvaluateAsync(ScreeningRequest request, CancellationToken ct = default);
}

interface IScreeningPipeline
{
    Task<ScreeningResponse> ScreenAsync(string requestId, ScreeningRequest request,
                                        CancellationToken ct = default);
}

interface IPepService        { Task<bool> IsPepAsync(string accountId, CancellationToken ct); }
interface IScreeningResultStore
{
    Task SaveAsync(ScreeningResponse response, CancellationToken ct);
    Task<ScreeningResponse?> GetAsync(string requestId, CancellationToken ct);
}
interface IDailyAggregateStore
{
    Task<long> GetDailyTotalAsync(string accountId, DateOnly date, CancellationToken ct);
    Task AddAsync(string accountId, DateOnly date, long amount, CancellationToken ct);
}

// Endpoints
POST /api/v1/screen          → 200 ScreeningResponse | 400 ValidationProblem
GET  /api/v1/screen/{id}     → 200 ScreeningResponse | 404
GET  /health                 → 200 { status: "Healthy" }
```

---

## Dependency Graph

```
F1: Domain + Interfaces (Core models, IScreeningRule, IPepService, stores)
 └─ F2: Rules + Pipeline (4 rules, ScreeningPipeline, InMemory impls)
     └─ F3: API + Middleware (endpoints, X-Request-Id, DI wiring)
         └─ F4: Infrastructure + CI (Dockerfile multi-stage, GitHub Actions)
```

F1 has no dependencies. F2 depends on F1. F3 depends on F2. F4 depends on F3.

---

## Implementation Plan

### F1: Domain Models and Interfaces

**QC-status:** APPROVED
**Review:** APPROVED — all 11 checklist items pass. No findings requiring action.
**Test:** PASS — build 0 errors/0 warnings, placeholder test green.

**Delivers:** All C# types, enums, interfaces. No logic — only contracts and domain model.

**Files:**
- `src/Core/Models/ScreeningRequest.cs`
- `src/Core/Models/ScreeningResponse.cs`
- `src/Core/Models/PartyInfo.cs`
- `src/Core/Models/RuleResult.cs`
- `src/Core/Models/Enums.cs` (ScreeningStatus, RuleStatus, RuleSeverity)
- `src/Core/Rules/IScreeningRule.cs`
- `src/Core/Pipeline/IScreeningPipeline.cs`
- `src/Core/Services/IPepService.cs`
- `src/Core/Stores/IScreeningResultStore.cs`
- `src/Core/Stores/IDailyAggregateStore.cs`
- `src/Core/Core.csproj`
- `tests/Core.Tests/Core.Tests.csproj`
- `TransactionCompliance.sln`
- `Directory.Build.props` (nullable, implicit usings, TreatWarningsAsErrors)

**Acceptance Criteria:**
- [ ] Solution builds with `dotnet build` — zero warnings, zero errors
- [ ] All 5 enums/status values present: Approved, Rejected, Flagged, PendingReview
- [ ] `IScreeningPipeline.ScreenAsync` signature includes `string requestId` parameter
- [ ] `IPepService` interface is defined in Core, not Infrastructure
- [ ] `IDailyAggregateStore` has separate `GetDailyTotalAsync` and `AddAsync` methods
- [ ] Nullable reference types enabled globally

---

### F2: Compliance Rules and Pipeline

**QC-status:** APPROVED
**Review:** APPROVED — 15/15 checklist items pass. Minor: sender-country not screened (out of spec, receiver-only per domain rules). No blocking findings.
**Test:** PASS — 23/23 green. All boundary values covered. Rejected-not-increment test present.

**Delivers:** All 4 rules, pipeline orchestration, in-memory store implementations, unit tests.

**Files:**
- `src/Core/Rules/AmountThresholdRule.cs`
- `src/Core/Rules/SanctionedCountryRule.cs`
- `src/Core/Rules/CumulativeDailyLimitRule.cs`
- `src/Core/Rules/PepCheckRule.cs`
- `src/Core/Pipeline/ScreeningPipeline.cs`
- `src/Infrastructure/Services/InMemoryPepService.cs`
- `src/Infrastructure/Stores/InMemoryScreeningResultStore.cs`
- `src/Infrastructure/Stores/InMemoryDailyAggregateStore.cs`
- `src/Infrastructure/CountryLists.cs`
- `src/Infrastructure/Infrastructure.csproj`
- `tests/Core.Tests/Rules/AmountThresholdRuleTests.cs`
- `tests/Core.Tests/Rules/SanctionedCountryRuleTests.cs`
- `tests/Core.Tests/Rules/CumulativeDailyLimitRuleTests.cs`
- `tests/Core.Tests/Rules/PepCheckRuleTests.cs`
- `tests/Core.Tests/Pipeline/ScreeningPipelineTests.cs`

**Rule logic:**
| Rule | Condition | Status | Severity |
|------|-----------|--------|----------|
| `amount_threshold` | `Amount > 10_000_000L` | Flagged | High |
| `sanctioned_country` | `Receiver.Country ∈ [KP,IR,SY,CU,RU]` | Rejected | High |
| `cumulative_daily_limit` | `dailyTotal + Amount > 50_000_000L` (approved+flagged only, UTC day) | Flagged | High |
| `pep_check` | `IsPepAsync(Receiver.AccountId) AND Amount > 5_000_000L` | PendingReview | Medium |

Pipeline status priority: `Rejected > PendingReview > Flagged > Approved`

`InMemoryDailyAggregateStore` key: `(accountId, DateOnly.FromDateTime(DateTime.UtcNow))`

**Acceptance Criteria:**
- [ ] `amount_threshold` triggers on 10,000,001 cents, passes on 10,000,000 cents
- [ ] `sanctioned_country` rejects all 5 countries (KP, IR, SY, CU, RU), approves NO
- [ ] `cumulative_daily_limit` triggers when running total exceeds 50,000,000 — not before
- [ ] Rejected transactions do NOT increment daily aggregate
- [ ] `pep_check` triggers only when both conditions are true (PEP AND amount > 5M)
- [ ] `InMemoryPepService` returns `true` for `ACC-PEP-001`
- [ ] Pipeline returns `Rejected` when `sanctioned_country` and `pep_check` both trigger
- [ ] All unit tests green (`dotnet test`)

---

### F3: API Layer and Middleware

**QC-status:** APPROVED
**Review:** APPROVED — 16/16 checklist items pass. Minor: exception handler excluded from Development (intentional), redundant null cast, no cumulative limit integration test (covered in Core.Tests). No blockers.
**Test:** PASS — 36/36 green. All 10 named tests present and passing.

**Delivers:** Minimal API endpoints, X-Request-Id middleware, DI wiring, integration tests.

**Files:**
- `src/Api/Program.cs`
- `src/Api/Endpoints/ScreeningEndpoints.cs`
- `src/Api/Middleware/RequestIdMiddleware.cs`
- `src/Api/Api.csproj`
- `tests/Api.Tests/Api.Tests.csproj`
- `tests/Api.Tests/ScreeningEndpointTests.cs`
- `tests/Api.Tests/HealthEndpointTests.cs`

**Acceptance Criteria:**
- [ ] `POST /api/v1/screen` returns `200` with `ScreeningResponse` (requestId, transactionId, status, timestamp, rules[])
- [ ] Response `X-Request-Id` header matches `requestId` in body
- [ ] `GET /api/v1/screen/{requestId}` returns `200` for a known result, `404` for unknown
- [ ] `GET /health` returns `200 { "status": "Healthy" }`
- [ ] Missing/null required fields return `400` with ProblemDetails
- [ ] `POST` with `Receiver.Country=RU` returns `status=Rejected` and rule `sanctioned_country` in rules[]
- [ ] All integration tests green using `WebApplicationFactory`
- [ ] Rules DI registration: all 4 rules registered as `IScreeningRule` (multiple registration pattern)

---

### F4: Dockerfile and GitHub Actions

**QC-status:** APPROVED
**Review:** APPROVED — all 18 checks pass. Minor: publish path verbose, build-stage comment absent. No blockers.
**Test:** PASS — 36/36 green, Dockerfile stages verified, CI yml valid, upload-artifact step present.

**Delivers:** Multi-stage Dockerfile with `test` target, GitHub Actions workflow (build + test + format).

**Files:**
- `Dockerfile`
- `.github/workflows/ci.yml`
- `.dockerignore`
- `.gitignore`

**Dockerfile stages:**
```
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  → restore, build

FROM build AS test
  → dotnet test --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
  → publish, ENTRYPOINT
```

**Acceptance Criteria:**
- [ ] `docker build --target test -t compliance-test .` runs all tests and exits 0
- [ ] `docker build -t compliance-api . && docker run -p 5000:8080 compliance-api` serves requests at `http://localhost:5000`
- [ ] GitHub Actions workflow triggers on push/PR to `main`
- [ ] CI runs: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`
- [ ] `.dockerignore` excludes `bin/`, `obj/`, `.git/`

---

## Holdout Scenarios

### 01-amount-threshold.md

**Request:**
```
POST /api/v1/screen
{
  "transactionId": "TX-AMT-001",
  "sender":   { "accountId": "ACC-001", "name": "Alice",   "country": "NO" },
  "receiver": { "accountId": "ACC-002", "name": "Bob",     "country": "NO" },
  "amount": 10000001,
  "currency": "NOK"
}
```
**Expected:** `status=Flagged`, `rules[]` contains `rule=amount_threshold` with `status=Triggered`

---

### 02-sanctioned-country.md

**Request:**
```
POST /api/v1/screen
{
  "transactionId": "TX-SANC-001",
  "sender":   { "accountId": "ACC-001", "name": "Alice",   "country": "NO" },
  "receiver": { "accountId": "ACC-999", "name": "Evilcorp","country": "RU" },
  "amount": 100000,
  "currency": "NOK"
}
```
**Expected:** `status=Rejected`, `rules[]` contains `rule=sanctioned_country` with `status=Triggered`

---

### 03-cumulative-daily-limit.md

**Strategy:** Send 6 transactions of 9,000,000 cents from same sender. Each is below `amount_threshold` (10M). Running total after 6 = 54,000,000 > 50,000,000 limit. Transaction 6 must trigger `cumulative_daily_limit`.

**Requests (send in order, same test run / UTC day):**
```
POST /api/v1/screen  × 6
{
  "transactionId": "TX-CUM-00N",   // N = 1..6
  "sender":   { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" },
  "receiver": { "accountId": "ACC-002",      "name": "Bob",         "country": "NO" },
  "amount": 9000000,
  "currency": "NOK"
}
```
**Expected TX-CUM-001 through TX-CUM-005:** `status=Approved` (running total ≤ 50M)
**Expected TX-CUM-006:** `status=Flagged`, `rules[]` contains `rule=cumulative_daily_limit` with `status=Triggered`

---

### 04-pep-check.md

**Request:**
```
POST /api/v1/screen
{
  "transactionId": "TX-PEP-001",
  "sender":   { "accountId": "ACC-001",     "name": "Alice",   "country": "NO" },
  "receiver": { "accountId": "ACC-PEP-001", "name": "PEP Person","country": "NO" },
  "amount": 5000001,
  "currency": "NOK"
}
```
**Expected:** `status=PendingReview`, `rules[]` contains `rule=pep_check` with `status=Triggered`

---

### 05-combined.md

**Request (PEP receiver in sanctioned country):**
```
POST /api/v1/screen
{
  "transactionId": "TX-COMB-001",
  "sender":   { "accountId": "ACC-001",     "name": "Alice",     "country": "NO" },
  "receiver": { "accountId": "ACC-PEP-001", "name": "PEP Person","country": "IR" },
  "amount": 10000001,
  "currency": "NOK"
}
```
**Expected:** `status=Rejected` (Rejected beats PendingReview and Flagged)
**Expected rules triggered:** `sanctioned_country` (Triggered), `amount_threshold` (Triggered), `pep_check` (Triggered)
**Rationale:** All three rules fire; pipeline returns strictest status = `Rejected`
