# Transaction Compliance Service

A REST API that screens financial transactions against compliance rules and logs all decisions for audit. Built autonomously by AI agents using the Dark Software Factory pattern.

**Stack:** .NET 8 · ASP.NET Core Minimal API · C# 12 · Docker · GitHub Actions

---

## What It Does

Screens transactions in real time against four compliance rules:

| Rule | Threshold | Result |
|------|-----------|--------|
| `amount_threshold` | > 100,000 NOK (10,000,000 øre) | Flagged |
| `sanctioned_country` | Receiver in KP/IR/SY/CU/RU | Rejected |
| `cumulative_daily_limit` | Sender daily total > 500,000 NOK (50,000,000 øre) | Flagged |
| `pep_check` | PEP receiver AND amount > 50,000 NOK (5,000,000 øre) | PendingReview |

All amounts are `long` (cents/øre). Status priority when multiple rules trigger: `Rejected > PendingReview > Flagged > Approved`.

---

## API

```
POST /api/v1/screen          Screen a transaction → 200 ScreeningResponse | 400
GET  /api/v1/screen/{id}     Retrieve previous result → 200 | 404
GET  /health                 Health check → 200
```

Request:
```json
{
  "transactionId": "TX-001",
  "sender":   { "accountId": "ACC-001", "name": "Alice", "country": "NO" },
  "receiver": { "accountId": "ACC-002", "name": "Bob",   "country": "NO" },
  "amount": 10000001,
  "currency": "NOK"
}
```

Response:
```json
{
  "requestId": "3fa85f64-...",
  "transactionId": "TX-001",
  "status": "Flagged",
  "timestamp": "2026-05-04T12:00:00+00:00",
  "rules": [
    { "rule": "amount_threshold", "status": "Triggered", "severity": "High", "message": "..." }
  ]
}
```

Every response includes an `X-Request-Id` header matching the `requestId` in the body.

---

## Running Locally

```bash
# Build and run (no local .NET SDK required)
docker build -t compliance-api .
docker run -p 5000:8080 compliance-api
# API: http://localhost:5000

# Run all tests
docker build --target test -t compliance-test .
```

---

## Test Coverage

| Phase | Tests | Result |
|-------|-------|--------|
| Domain models + interfaces | 1 | ✅ 1/1 |
| 4 compliance rules + pipeline | 22 | ✅ 23/23 |
| API endpoints + middleware | 13 | ✅ 36/36 |
| Holdout scenarios (eval agent) | 5 | ✅ 5/5 (100%) |

---

## Project Structure

```
src/
├── Core/           Domain models, interfaces, 4 compliance rules, pipeline
├── Infrastructure/ In-memory stores, PEP service mock, country lists
└── Api/            Minimal API endpoints, X-Request-Id middleware, DI wiring

tests/
├── Core.Tests/     Unit tests — rules + pipeline (23 tests)
└── Api.Tests/      Integration tests via WebApplicationFactory (13 tests)

doc/
├── domain-rules.md              Detailed rule logic
├── compliance-service-plan.md   Architecture and phased implementation plan
└── compliance-service-analysis.md  Timing, review findings, process improvements

scenarios/                       Holdout evaluation scenarios (dev agent excluded)
.claude/skills/                  Agent skill files (architect, developer, reviewer, tester, eval)
```

---

## How It Was Built — Dark Software Factory

The service was built end-to-end by AI agents without human code writing. Total time: ~35 minutes.

```
Architect agent  →  Plan + holdout scenarios
Developer agent  →  Code per phase (F1–F4)
Review agent   ┐
Test agent     ┘  →  Parallel QC after each phase
Eval agent       →  Holdout evaluation (5/5 = 100%)
```

**4 implementation phases:**

| Phase | Delivered | QC rounds |
|-------|-----------|-----------|
| F1: Domain + Interfaces | Types, enums, all interfaces | 1 |
| F2: Rules + Pipeline | 4 rules, pipeline, in-memory stores, 22 unit tests | 1 |
| F3: API + Middleware | Endpoints, request-id middleware, DI, 13 integration tests | 2 |
| F4: Dockerfile + CI | Multi-stage Dockerfile, GitHub Actions workflow | 2 |

**Key quality gates enforced by hooks:**
- `require-plan.sh` — blocks writing to `src/` without an approved plan
- `require-phase-qc.sh` — blocks next phase until review + test both pass
- `require-analyse.sh` — blocks final commit without a written analysis

**Holdout principle:** The dev agent never sees `scenarios/` (`.claudeignore`). The eval agent evaluates the running API against these scenarios independently — same idea as train/test split in ML. Prevents agents from gaming the tests.

**Major findings caught by review agents:**
- Dev agent upgraded to `net10.0` (local SDK) — reverted to `net8.0` per architectural decision
- Incomplete input validation on composite types — `Sender`/`Receiver` null → 500 instead of 400
- Missing CI artifact upload for `.trx` test results (DORA compliance gap)

All three were caught and fixed before the final holdout evaluation.

---

## CI/CD

GitHub Actions runs on push/PR to `main`:
1. `dotnet build` — zero warnings enforced (`TreatWarningsAsErrors=true`)
2. `dotnet test` — results uploaded as artifacts
3. `dotnet format --verify-no-changes` — formatting gate

---

## References

| Resource | Content |
|---------|---------|
| [The Dark Factory Pattern (HackerNoon)](https://hackernoon.com/the-dark-factory-pattern-moving-from-ai-assisted-to-fully-autonomous-coding) | Holdout scenarios, satisfaction metrics, agent protocol |
| [StrongDM Software Factory](https://simonwillison.net/2026/Feb/7/software-factory/) | Zero human review, the `return true` problem |
| [Anthropic: Agentic Coding Trends 2026](https://resources.anthropic.com/hubfs/2026%20Agentic%20Coding%20Trends%20Report.pdf) | Industry data and maturity trends |
