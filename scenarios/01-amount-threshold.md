# Scenario 01 — Amount Threshold

**Rule:** `amount_threshold`
**Rationale:** Transactions exceeding 100,000 NOK (10,000,000 cents) must be flagged.

## Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-AMT-001",
  "sender":   { "accountId": "ACC-001",     "name": "Alice", "country": "NO" },
  "receiver": { "accountId": "ACC-002",     "name": "Bob",   "country": "NO" },
  "amount": 10000001,
  "currency": "NOK"
}
```

## Expected

- HTTP status: `200`
- `status`: `Flagged`
- `rules[]` contains entry where `rule=amount_threshold` and `status=Triggered`

## Boundary Check (below threshold — must NOT trigger)

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-AMT-002",
  "sender":   { "accountId": "ACC-001", "name": "Alice", "country": "NO" },
  "receiver": { "accountId": "ACC-002", "name": "Bob",   "country": "NO" },
  "amount": 10000000,
  "currency": "NOK"
}
```

- HTTP status: `200`
- `status`: `Approved`
- `rules[]` entry for `amount_threshold` has `status=Passed`
