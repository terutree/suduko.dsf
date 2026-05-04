# Scenario 04 — PEP Check

**Rule:** `pep_check`
**Rationale:** Transactions to a PEP (Politically Exposed Person) with amount > 50,000 NOK (5,000,000 cents) require manual review.

## Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-PEP-001",
  "sender":   { "accountId": "ACC-001",     "name": "Alice",      "country": "NO" },
  "receiver": { "accountId": "ACC-PEP-001", "name": "PEP Person", "country": "NO" },
  "amount": 5000001,
  "currency": "NOK"
}
```

## Expected

- HTTP status: `200`
- `status`: `PendingReview`
- `rules[]` contains entry where `rule=pep_check` and `status=Triggered`

## Boundary Check — PEP receiver but amount at threshold (must NOT trigger)

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-PEP-002",
  "sender":   { "accountId": "ACC-001",     "name": "Alice",      "country": "NO" },
  "receiver": { "accountId": "ACC-PEP-001", "name": "PEP Person", "country": "NO" },
  "amount": 5000000,
  "currency": "NOK"
}
```

- HTTP status: `200`
- `status`: `Approved`
- `rules[]` entry for `pep_check` has `status=Passed`

## Non-PEP receiver with large amount (must NOT trigger pep_check)

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-PEP-003",
  "sender":   { "accountId": "ACC-001", "name": "Alice", "country": "NO" },
  "receiver": { "accountId": "ACC-002", "name": "Bob",   "country": "NO" },
  "amount": 5000001,
  "currency": "NOK"
}
```

- HTTP status: `200`
- `rules[]` entry for `pep_check` has `status=Passed`
