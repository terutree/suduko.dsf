# Scenario 05 — Combined Rules (Strictest Wins)

**Rules:** `sanctioned_country` + `amount_threshold` + `pep_check`
**Rationale:** When multiple rules trigger, the strictest status wins: Rejected > PendingReview > Flagged > Approved.

## Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-COMB-001",
  "sender":   { "accountId": "ACC-001",     "name": "Alice",      "country": "NO" },
  "receiver": { "accountId": "ACC-PEP-001", "name": "PEP Person", "country": "IR" },
  "amount": 10000001,
  "currency": "NOK"
}
```

## Expected

- HTTP status: `200`
- `status`: `Rejected` — strictest status wins (Rejected beats PendingReview and Flagged)
- `rules[]` contains ALL THREE entries triggered:
  - `rule=sanctioned_country`, `status=Triggered`
  - `rule=amount_threshold`, `status=Triggered`
  - `rule=pep_check`, `status=Triggered`

## Rationale

- `sanctioned_country`: IR is a sanctioned country → Rejected
- `amount_threshold`: 10,000,001 > 10,000,000 threshold → Flagged
- `pep_check`: ACC-PEP-001 is PEP AND amount > 5,000,000 → PendingReview
- Pipeline returns `Rejected` as the final status (highest priority)
