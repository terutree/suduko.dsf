# Scenario 03 — Cumulative Daily Limit

**Rule:** `cumulative_daily_limit`
**Rationale:** A single sender cannot send more than 500,000 NOK (50,000,000 cents) per calendar day (UTC).

## Strategy

Send 6 transactions of 9,000,000 cents from the same sender (`ACC-BULK-001`) in the same UTC day.
Each individual transaction is below the `amount_threshold` (10,000,000 cents) so that rule does not interfere.

Running totals:
- After TX 1: 9,000,000 — Approved
- After TX 2: 18,000,000 — Approved
- After TX 3: 27,000,000 — Approved
- After TX 4: 36,000,000 — Approved
- After TX 5: 45,000,000 — Approved
- After TX 6: 54,000,000 > 50,000,000 — **Flagged**

## Requests (send in order, same test run / UTC day)

```http
POST /api/v1/screen
Content-Type: application/json

{ "transactionId": "TX-CUM-001", "sender": { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" }, "receiver": { "accountId": "ACC-002", "name": "Bob", "country": "NO" }, "amount": 9000000, "currency": "NOK" }
```

```http
POST /api/v1/screen
Content-Type: application/json

{ "transactionId": "TX-CUM-002", "sender": { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" }, "receiver": { "accountId": "ACC-002", "name": "Bob", "country": "NO" }, "amount": 9000000, "currency": "NOK" }
```

```http
POST /api/v1/screen
Content-Type: application/json

{ "transactionId": "TX-CUM-003", "sender": { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" }, "receiver": { "accountId": "ACC-002", "name": "Bob", "country": "NO" }, "amount": 9000000, "currency": "NOK" }
```

```http
POST /api/v1/screen
Content-Type: application/json

{ "transactionId": "TX-CUM-004", "sender": { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" }, "receiver": { "accountId": "ACC-002", "name": "Bob", "country": "NO" }, "amount": 9000000, "currency": "NOK" }
```

```http
POST /api/v1/screen
Content-Type: application/json

{ "transactionId": "TX-CUM-005", "sender": { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" }, "receiver": { "accountId": "ACC-002", "name": "Bob", "country": "NO" }, "amount": 9000000, "currency": "NOK" }
```

```http
POST /api/v1/screen
Content-Type: application/json

{ "transactionId": "TX-CUM-006", "sender": { "accountId": "ACC-BULK-001", "name": "Bulk Sender", "country": "NO" }, "receiver": { "accountId": "ACC-002", "name": "Bob", "country": "NO" }, "amount": 9000000, "currency": "NOK" }
```

## Expected

- TX-CUM-001 through TX-CUM-005: `status=Approved`
- TX-CUM-006: `status=Flagged`, `rules[]` contains entry where `rule=cumulative_daily_limit` and `status=Triggered`
