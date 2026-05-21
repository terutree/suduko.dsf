# Scenario 06 — currency_restriction

## Description
A transaction submitted in Japanese Yen (JPY) — a currency not on the permitted list (NOK, EUR, USD, GBP) — must be rejected.

## Request

```http
POST /api/v1/screen
Content-Type: application/json

{
  "transactionId": "TX-CURRENCY-001",
  "sender":   { "accountId": "ACC-S-01", "name": "Taro Yamamoto", "country": "JP" },
  "receiver": { "accountId": "ACC-R-01", "name": "Oslo AS",       "country": "NO" },
  "amount": 5000000,
  "currency": "JPY"
}
```

## Expected Response

- HTTP status: `200 OK`
- `status`: `"Rejected"`
- `rules` array contains an entry where:
  - `rule`: `"currency_restriction"`
  - `status`: `"Triggered"`
  - `severity`: `"High"`

## Also Verify

- A permitted currency (e.g. `"NOK"`) with otherwise clean parameters returns `status: "Approved"` with `currency_restriction` rule showing `status: "Passed"`
