# Scenario 02 — Sanctioned Country

**Rule:** `sanctioned_country`
**Rationale:** Transactions to sanctioned countries (KP, IR, SY, CU, RU) must always be rejected.

## Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-SANC-001",
  "sender":   { "accountId": "ACC-001", "name": "Alice",    "country": "NO" },
  "receiver": { "accountId": "ACC-999", "name": "Evilcorp", "country": "RU" },
  "amount": 100000,
  "currency": "NOK"
}
```

## Expected

- HTTP status: `200`
- `status`: `Rejected`
- `rules[]` contains entry where `rule=sanctioned_country` and `status=Triggered`

## Non-Sanctioned Country (must NOT trigger)

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-SANC-002",
  "sender":   { "accountId": "ACC-001", "name": "Alice", "country": "NO" },
  "receiver": { "accountId": "ACC-002", "name": "Bob",   "country": "SE" },
  "amount": 100000,
  "currency": "NOK"
}
```

- HTTP status: `200`
- `status`: `Approved`
- `rules[]` entry for `sanctioned_country` has `status=Passed`
