# Scenario 07 — duplicate_transaction

## Description
The same `TransactionId` cannot be screened more than once within 24 hours.
The first submission must be approved (assuming no other rule triggers).
The second submission of the identical `TransactionId` must be rejected immediately.

## Part A — First submission (clean transaction)

### Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-DUP-001",
  "sender":   { "accountId": "ACC-DUP-S-01", "name": "Erik Andersen", "country": "NO" },
  "receiver": { "accountId": "ACC-DUP-R-01", "name": "Bergen AS",     "country": "NO" },
  "amount": 500000,
  "currency": "NOK"
}
```

### Expected Response

- HTTP status: `200 OK`
- `status`: `"Approved"`
- `rules` array contains an entry where:
  - `rule`: `"duplicate_transaction"`
  - `status`: `"Passed"`

## Part B — Second submission (duplicate within 24 hours)

Submit the **identical request body** (same `transactionId: "TX-DUP-001"`) a second time immediately after Part A.

### Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-DUP-001",
  "sender":   { "accountId": "ACC-DUP-S-01", "name": "Erik Andersen", "country": "NO" },
  "receiver": { "accountId": "ACC-DUP-R-01", "name": "Bergen AS",     "country": "NO" },
  "amount": 500000,
  "currency": "NOK"
}
```

### Expected Response

- HTTP status: `200 OK`
- `status`: `"Rejected"`
- `rules` array contains an entry where:
  - `rule`: `"duplicate_transaction"`
  - `status`: `"Triggered"`
  - `severity`: `"High"`

## Also Verify

- A different `transactionId` (e.g. `"TX-DUP-002"`) submitted after Part B returns `status: "Approved"` — confirming the block is scoped to the specific `transactionId`, not the sender or amount.
- The `requestId` in the response differs between Part A and Part B — each call produces a new `requestId` even though the `transactionId` is the same.
