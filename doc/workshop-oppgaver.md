# Workshop: Hands-on with Dark Software Factory

**Time:** ~55 min  
**Repo:** `dnb-compliance-dsf-run-en` (delt av fasilitator)  

---

## Start Claude i Dark Mode

Kjør dette i rotmappen av repoet **før du starter oppgavene:**

```bash
claude --dangerously-skip-permissions
```

> `--dangerously-skip-permissions` lar agentene kjøre uten å spørre om tillatelse for hver operasjon. Nødvendig for autonom kjøring. Bruk kun i trygge, lokale miljøer.

```bash
# Tail agent-log i eget vindu (valgfritt)
tail -f .claude/logs/agent.log
```

---

## Hva Factory-en Allerede Har Bygget

Én komplett DSF-syklus er kjørt og dokumentert. Dette er referansen for hva du vil se under demoen.

### Cycle 1: Transaction Compliance Service — Initial Build

**Dato:** 2026-05-04 | **Agenter:** architect, 4× dev, 4× review, 4× test, 1× eval

| Fase | Aktivitet | Tid |
|------|-----------|-----|
| Step 1 | Arkitektur og plan (4 faser) + 5 holdout-scenarioer | ~1 min 45s |
| F1 | Domain-lag — typer, interfaces | ~2 min 16s |
| F1 QC | Review + test (1 runde — APPROVED) | ~1 min |
| F2 | 4 regler + pipeline + 22 unit-tester | ~3 min 12s |
| F2 QC | Review + test (1 runde — APPROVED) | ~35s |
| F3 | API-lag + middleware + 13 integrasjonstester | ~4 min 46s |
| F3 QC R1 | Review + test (CONDITIONAL APPROVAL) | ~1 min 5s |
| F3 fix | Dev agent fikser net10→net8 + validering | ~14 min 26s |
| F3 QC R2 | Review + test (APPROVED) | ~38s |
| F4 | Dockerfile multi-stage + GitHub Actions | ~1 min 5s |
| F4 QC R1 | Review + test (CONDITIONAL APPROVAL) | ~1 min 30s |
| F4 fix | Dev agent legger til artifact upload | ~20s |
| F4 QC R2 | Review + test (APPROVED) | ~35s |
| Holdout | Eval agent → **5/5 (100%)** | ~1 min 10s |
| **Total** | | **~35 min** |

**Hva review-agentene fanget:**

| Funn | Alvorlighet | Fase | Løst |
|------|-------------|------|------|
| Dev agent oppgraderte til `net10.0` (lokal SDK) — bryter net8-beslutning | Major | F3 | ✅ Fikset |
| Null-validering manglet på `Sender`/`Receiver` — ga 500 i stedet for 400 | Major | F3 | ✅ Fikset |
| `.trx`-resultater ikke lastet opp i CI — DORA audit-gap | Major | F4 | ✅ Fikset |

**Testresultater:**
- 36/36 unit + integrasjonstester grønne
- 5/5 holdout-scenarioer bestått (100%)

> **Nøkkelpunkt:** Alle tre major-funn ble fanget av review-agenten og fikset før holdout-evalueringen. Holdout kjørte mot et rent bygg.

---

## Track 1: Foundational — "AI legger til en regel for meg" (~45 min)

**Mål:** Oppleve en komplett DSF-syklus. Gi agenten én veldefinert oppgave og følg med på hva som skjer.

### Task 1A: Valutabegrensning (anbefalt for alle)

Lim inn denne prompten direkte i Claude:

```
Read CLAUDE.md.
New compliance rule: currency_restriction.

Rule: Only NOK, EUR, USD, and GBP are permitted currencies.
Transactions in other currencies are rejected.
RuleName: currency_restriction
ScreeningStatus on match: Rejected
Severity: High

Start with architect agent, create plan in doc/currency-restriction-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

**Se etter:**
- Hvilke faser lager arkitekt-agenten?
- Hvilke edge cases skriver dev-agenten tester for?
- Hva finner review-agenten?

**Diskusjonsspørsmål etter kjøring:**
- Måtte du korrigere agenten underveis?
- Ville koden bestått code review i teamet ditt?

---

### Task 1B: Duplikat-sjekk (alternativ)

```
Read CLAUDE.md.
New compliance rule: duplicate_transaction.

Rule: The same TransactionId cannot be screened more than once
within 24 hours. Duplicates are rejected with Rejected.
RuleName: duplicate_transaction
ScreeningStatus on match: Rejected
Severity: High

Start with architect agent, create plan in doc/duplicate-transaction-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

---

## Track 2: Extended — "Jeg spesifiserer, AI leverer" (~55 min)

**Mål:** Skriv din egen spesifikasjon og observer hva som skjer når spec-en er vag vs. presis.

### Task 2A: Geografisk risikovurdering

Du bestemmer detaljene — men prompten MÅ spesifisere:
- Hvilke land er høyrisikoland?
- Hvilken status produserer regelen?
- Beløpsgrense eller alle beløp?
- RuleName (snake_case)

Startpunkt:

```
Read CLAUDE.md.
New compliance rule: geo_risk_flag.

[Fyll inn regellogikken selv — vær så presis som mulig]

Start with architect agent, create plan in doc/geo-risk-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

> **Tips:** Start vagt med vilje i én kjøring, se hva agenten gjetter. Kjør igjen med presis spec. Sammenlign resultater.

---

### Task 2B: Runde beløp (høyverdige runde beløp)

Transaksjoner med "mistenkelig runde" beløp er et kjent AML-mønster.

```
Read CLAUDE.md.
New compliance rule: round_amount_flag.

Rule: Transactions where the amount (in NOK) is exactly divisible by
10,000 AND amount > 50,000 NOK are flagged for manual review.
Examples: 50,000, 100,000, 200,000 → flagged.
51,000, 99,999 → not flagged.
RuleName: round_amount_flag
ScreeningStatus on match: Flagged
Severity: Medium

Start with architect agent, create plan in doc/round-amount-plan.md.
Implement phase by phase per the protocol in CLAUDE.md.
```

---

### Task 2C: CI-feil og selvhelbredelse

Kjør Track 1 eller 2A/2B til koden er ferdig. Introduser deretter en feil manuelt:

1. Endre en terskelverdi i koden (ikke i tester)
2. Push — CI feiler
3. Gi Claude denne prompten:

```
CI is failing after the last push. Fetch failure details with gh run view --log-failed.
Analyze the failure and fix it. All tests should be green.
```

**Observer:** Leser agenten CI-output? Identifiserer den rotårsaken uten hint?

---

## Track 3: Factory — "Fasiten skrives FØR koden" (~55 min, ambisiøs)

**Mål:** Opplev train/test-separasjonen i praksis. Skriv holdout-scenariet FØR implementasjonen.

### Steg 3.1: Skriv scenario manuelt (10 min)

Velg én av reglene fra Track 2. Opprett scenariofilen FØR du gir implementasjonsoppgaven:

```markdown
# scenarios/06-[rule-name]-yours.md

## Scenario: [Rule Name]
**Request:**
POST /api/v1/screen
{
  "transactionId": "TEST-[RULE]-001",
  "sender": { "accountId": "ACC-SENDER-001", "name": "Test Sender", "country": "NO" },
  "receiver": { "accountId": "ACC-RECEIVER-001", "name": "Test Receiver", "country": "[land]" },
  "amount": [beløp i øre],
  "currency": "[valuta]"
}

**Expected status:** [Approved | Rejected | Flagged | PendingReview]
**Active rule:** [regelnavn i snake_case]
**Rationale:** [Forklar hvorfor dette scenariet skal gi forventet status]
```

### Steg 3.2: Implementer uten å vise scenario (20 min)

Start implementasjon med prompten fra Track 2. Dev-agenten ser aldri `scenarios/`.

### Steg 3.3: Kjør holdout-evaluering (15 min)

```bash
# Start API
docker build -t compliance-api . && docker run -p 5000:8080 compliance-api

# I Claude — ny prompt:
Read .claude/skills/eval/SKILL.md.
Read the scenarios/ folder.
API is running at http://localhost:5000.
Evaluate all scenarios and report satisfaction score.
```

**Diskusjonsspørsmål:**
- Besto scenariet du skrev? Hvis ikke — hva manglet i spesifikasjonen?
- Hva ville skjedd om agenten hadde sett fasiten?

### Steg 3.4 (bonus): Modifiser en skill og kjør ny regel

Legg til et punkt i `.claude/skills/developer/SKILL.md`. Kjør en ny regel. Se om agenten følger det nye punktet automatisk.

---

## Fasilitatortips

**Vanlige problemer:**

| Problem | Løsning |
|---------|---------|
| Agent skriver kode direkte (ikke via Task) | Påminn: "Du er orchestrator — dispatsch til agenter" |
| CI feiler pga. formattering | `docker build --target test` lokalt før push |
| Hooks blokkerer commit | Les feilmeldingen — den forklarer hva som mangler |
| Agent gjentar seg selv | Gi den faktisk feiloutput, ikke bare "prøv igjen" |

**Gode refleksjonsspørsmål til slutt:**
- Hvilke oppgaver i teamet ditt ligner mest på dette?
- Hva måtte du spesifisere mer presist enn du forventet?
- Hva overrasket deg — positivt eller negativt?
- Hvor ville du innført menneskelig review i en reell pipeline?
