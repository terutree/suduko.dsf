# Domain Rules — Transaction Compliance Service

Detailed rule logic. CLAUDE.md contains only table rows and pointers to this file.

---

## Active Rules

### amount_threshold

**Status:** Active  
**RuleName:** `amount_threshold`  
**ScreeningStatus on match:** `Flagged`  
**Severity:** `High`

Transactions with an amount > 100,000 NOK (10,000,000 cents) require extended validation.

```
amount > 10_000_000L → Flagged, severity High
```

Threshold is exclusive (`>`), not inclusive (`>=`).

---

### sanctioned_country

**Status:** Active  
**RuleName:** `sanctioned_country`  
**ScreeningStatus on match:** `Rejected`  
**Severity:** `High`

Transactions to countries on the sanctions list are always rejected.

```
Receiver.Country ∈ SanctionedCountries → Rejected, severity High
```

Sanctioned countries: `KP`, `IR`, `SY`, `CU`, `RU`  
Comparison: case-insensitive, but ISO 3166-1 alpha-2 is always uppercase — validate input.

---

### cumulative_daily_limit

**Status:** Active  
**RuleName:** `cumulative_daily_limit`  
**ScreeningStatus on match:** `Flagged`  
**Severity:** `High`

The same sender cannot send more than 500,000 NOK (50,000,000 cents) per calendar day (UTC).

```
SUM(amount for Sender.AccountId, UTC date) > 50_000_000L → Flagged, severity High
```

Aggregate is calculated over all approved and flagged transactions (not rejected). Resets at UTC midnight.

---

### pep_check

**Status:** Active  
**RuleName:** `pep_check`  
**ScreeningStatus on match:** `PendingReview`  
**Severity:** `Medium`

Receiver on the PEP list AND amount > 50,000 NOK (5,000,000 cents) → manual review.

```
Receiver.AccountId ∈ PepList AND amount > 5_000_000L → PendingReview, severity Medium
```

PEP service always behind `IPepService` interface. `InMemoryPepService` includes `ACC-PEP-001` for holdout compatibility.

---

### currency_restriction

**Status:** Active  
**RuleName:** `currency_restriction`  
**ScreeningStatus on match:** `Rejected`  
**Severity:** `High`

Only NOK, EUR, USD, and GBP are permitted currencies. Transactions in any other currency are rejected.

```
Currency ∉ { NOK, EUR, USD, GBP } → Rejected, severity High
```

Comparison: case-insensitive (`OrdinalIgnoreCase`). Null/empty currency is treated as non-permitted → Rejected.  
Allowed list is a constructor parameter in `CurrencyRestrictionRule` — injected from `Program.cs`.

---

## Pipeline Status Mapping

Strictest status wins when multiple rules trigger:

```
Rejected > PendingReview > Flagged > Approved
```

| Rule | Status on match |
|-------|-----------------|
| `sanctioned_country` | `Rejected` |
| `currency_restriction` | `Rejected` |
| `pep_check` | `PendingReview` |
| `amount_threshold` | `Flagged` |
| `cumulative_daily_limit` | `Flagged` |

---

## New Rule — Checklist

When a new rule is added:

- [ ] Add section in this file with full logic
- [ ] Add table row in `CLAUDE.md` (one line)
- [ ] Add mapping in the pipeline status table above
- [ ] Create `scenarios/0N-[rule-name].md` BEFORE implementation starts
- [ ] Update `architect/SKILL.md` pipeline status mapping
- [ ] Update `developer/SKILL.md` snake_case list
