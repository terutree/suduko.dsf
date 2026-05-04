---
marp: true
theme: default
paginate: true
style: |
  @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700;900&display=swap');

  :root {
    --dnb-blue: #003865;
    --dnb-light: #5ba3d0;
    --dnb-accent: #007ac7;
    --dnb-green: #14555a;
    --dnb-mint: #28b482;
    --dnb-warn: #e67e22;
    --dnb-danger: #c0392b;
    --dnb-bg: #f5f7fa;
  }

  section {
    font-family: 'Inter', 'Segoe UI', sans-serif;
    background: #ffffff;
    color: #1a1a2e;
    padding: 48px 56px;
  }

  section.lead {
    background: linear-gradient(145deg, #003865 0%, #005a9e 60%, #007ac7 100%);
    color: #ffffff;
    display: flex;
    flex-direction: column;
    justify-content: center;
  }
  section.lead h1 {
    color: #ffffff;
    font-size: 2.6em;
    font-weight: 900;
    border: none;
    line-height: 1.1;
    margin-bottom: 0.2em;
  }
  section.lead h2 {
    color: rgba(255,255,255,0.75);
    font-weight: 300;
    font-size: 1.3em;
    margin-top: 0;
  }
  section.lead p {
    color: rgba(255,255,255,0.9);
    font-size: 1em;
    margin-top: 1.5em;
  }
  section.lead .badge {
    display: inline-block;
    background: rgba(255,255,255,0.15);
    border: 1px solid rgba(255,255,255,0.3);
    border-radius: 20px;
    padding: 4px 14px;
    font-size: 0.8em;
    margin-right: 8px;
    margin-top: 1em;
  }

  section.dark {
    background: linear-gradient(160deg, #1a1a2e 0%, #0f2340 100%);
    color: #e8eef5;
  }
  section.dark h1 { color: #5ba3d0; border-color: #2a4a70; }
  section.dark h2 { color: #a8c4e0; }
  section.dark table th { background: #0f2340; }
  section.dark code { background: #0f2340; color: #7ec8e3; }
  section.dark blockquote {
    border-color: #5ba3d0;
    background: rgba(91,163,208,0.1);
    color: #c8daea;
  }

  section.section-break {
    background: linear-gradient(135deg, #003865 0%, #005a9e 100%);
    color: white;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
  }
  section.section-break h1 {
    color: white;
    border: none;
    font-size: 2.8em;
    font-weight: 900;
  }
  section.section-break h2 {
    color: rgba(255,255,255,0.6);
    font-weight: 300;
  }
  section.section-break .num {
    font-size: 5em;
    font-weight: 900;
    color: rgba(255,255,255,0.1);
    line-height: 1;
    margin-bottom: -0.2em;
  }

  section.highlight-card {
    background: #f5f7fa;
  }

  h1 {
    color: #003865;
    border-bottom: 3px solid #003865;
    padding-bottom: 10px;
    font-weight: 700;
    font-size: 1.6em;
    margin-bottom: 0.6em;
  }
  h2 { color: #003865; font-weight: 600; margin-top: 1em; }
  h3 { color: #005a9e; font-weight: 600; margin-top: 0.8em; }

  table {
    font-size: 0.82em;
    border-collapse: collapse;
    width: 100%;
    margin: 0.5em 0;
  }
  th {
    background: #003865;
    color: white;
    padding: 8px 12px;
    text-align: left;
    font-weight: 600;
  }
  td { padding: 7px 12px; vertical-align: top; }
  tr:nth-child(even) { background: #f0f4f8; }
  tr:nth-child(odd) { background: #ffffff; }

  code {
    background: #eef2f7;
    padding: 2px 7px;
    border-radius: 4px;
    font-size: 0.88em;
    color: #003865;
  }
  pre {
    background: #1a1a2e;
    color: #c8daea;
    border-radius: 8px;
    padding: 20px 24px;
    font-size: 0.75em;
    line-height: 1.6;
    border-left: 4px solid #007ac7;
  }
  pre code {
    background: none;
    color: inherit;
    padding: 0;
  }

  blockquote {
    border-left: 4px solid #003865;
    background: #eef2f7;
    padding: 14px 20px;
    margin: 16px 0;
    font-style: italic;
    border-radius: 0 6px 6px 0;
    color: #003865;
  }

  .pill {
    display: inline-block;
    border-radius: 12px;
    padding: 3px 10px;
    font-size: 0.78em;
    font-weight: 600;
    margin: 2px;
  }
  .pill-blue { background: #003865; color: white; }
  .pill-green { background: #14555a; color: white; }
  .pill-orange { background: #e67e22; color: white; }
  .pill-red { background: #c0392b; color: white; }
  .pill-light { background: #c8daea; color: #003865; }

  ul { margin: 0.4em 0; padding-left: 1.4em; }
  li { margin: 0.35em 0; font-size: 0.92em; }

  footer {
    font-size: 0.65em;
    color: #888;
  }
---

<!-- _class: lead -->

# Dark Software Factory

## Fra klartekst til produksjonsklar kode — uten menneskelig kodeskriving

**Transaction Compliance Service · DNB Liv**

---

<!-- _class: section-break -->

<div class="num">01</div>

# Hva er Dark Software Factory?

## Konseptet og motivasjonen

---

# Autonom utviklingspipeline

En **Dark Software Factory** er en pipeline der AI-agenter tar en oppgave fra klartekst til ferdig, testet og reviewet kode — uten at mennesker skriver én linje.

```
Oppgave (klartekst)
    ↓
Architect-agent   →  Plan + arkitektur
    ↓
Developer-agent   →  Kode + unit-tester
    ↓
Review-agent  ┐
Test-agent    ┘  → (parallelt) Kvalitetssikring
    ↓
Eval-agent        →  Holdout-evaluering
    ↓
Commit + PR       →  Produksjonsklar leveranse
```

> **Mennesket definerer** *hva* og *hvorfor*.
> **Agentene leverer** *hvordan*.

---

# Industriell virkelighet — ikke hypotese

<br>

| Selskap | Hva de gjør | Status |
|---------|-------------|--------|
| **StrongDM** | Ingen menneskeskrevet kode siden juli 2025 | Produksjon |
| **Spotify** | 650 AI-genererte pull requests per måned | Produksjon |
| **Stripe** | 1 300+ AI-PRs per uke | Produksjon |
| **Cursor** | 70% av ny kode skrevet av AI | Produksjon |

<br>

> **Nøkkelpoeng:** De fleste team er på nivå 2 (chat-koding).
> Det mest verdifulle steget akkurat nå er **2 → 3**.
> Dette repoet demonstrerer nivå 4–5.

---

# Modenhetstrapp — 5 nivåer

| Nivå | Navn | Menneskelig rolle | Eksempel |
|------|------|-------------------|----------|
| **1** | Autocomplete | Utvikler driver alt | GitHub Copilot tab-complete |
| **2** | Chat-koding | Utvikler reviewer og styrer | "Skriv denne funksjonen for meg" |
| **3** | Agentisk | Utvikler godkjenner resultat | Claude Code med tool use |
| **4** | Spekk-drevet | Produkteier, ikke programmerer | **← dette repoet** |
| **5** | Dark Factory | Definerer intensjon og kvalitet | **← dette repoet** |

### Nivå 5 i dette repoet

- Ingen linje-for-linje review — kun `doc/*-analyse.md` og satisfaction-score
- Hooks **blokkerer automatisk** avvik uten menneskelig intervensjon
- Eval-agenten ser **aldri** kildekoden

---

<!-- _class: section-break -->

<div class="num">02</div>

# Oppgaven vi løser

## Transaction Compliance Service

---

# Hva bygger vi?

**Transaction Compliance Service** — .NET 8/C# REST API for screening av finanstransaksjoner.

Typisk DNB Liv-oppgave: **veldefinert regellogikk, krever presisjon, DORA og Solvens II-relevant**.

### Fire compliance-regler

| Regel | Logikk | Resultat |
|-------|--------|----------|
| Beløpsgrense | `amount > 100 000 NOK` (10 000 000 øre) | `Flagged` |
| Sanksjonsliste | Mottaker i KP/IR/SY/CU/RU | `Rejected` |
| Kumulativ dagsgrense | Samme avsender > 500 000 NOK/dag | `Flagged` |
| PEP-sjekk | Mottaker er PEP **og** `amount > 50 000 NOK` | `PendingReview` |

> **Prioritering ved konflikt:** `Rejected` > `PendingReview` > `Flagged` > `Approved`
> Strengeste status vinner alltid.

---

# API-kontrakten

```csharp
// POST /api/v1/screen
record ScreeningRequest(
    string TransactionId,
    PartyInfo Sender,
    PartyInfo Receiver,
    long Amount,       // øre — aldri decimal
    string Currency,   // ISO 4217
    string? Description = null
);

// Respons
record ScreeningResponse(
    string RequestId,
    string TransactionId,
    ScreeningStatus Status,   // Approved | Rejected | Flagged | PendingReview
    DateTime Timestamp,
    IReadOnlyList<RuleResult> Rules
);
```

### Endepunkter

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| `POST` | `/api/v1/screen` | Screen transaksjon |
| `GET` | `/api/v1/screen/{requestId}` | Hent tidligere resultat |
| `GET` | `/health` | Helsesjekk |

---

# Pipeline-arkitekturen

```
POST /api/v1/screen
        ↓
  Request-ID middleware  (X-Request-Id header)
        ↓
  ScreeningPipeline
    ├── AmountThresholdRule      → Pass / Flag
    ├── SanctionedCountryRule   → Pass / Reject
    ├── CumulativeDailyRule     → Pass / Flag
    └── PepCheckRule            → Pass / PendingReview
        ↓
  DetermineScreeningStatus()   (strengeste vinner)
        ↓
  Audit-logging (ILogger<T>)   (alle beslutninger)
        ↓
  ScreeningResponse { requestId, status, rules[] }
```

> `IScreeningRule`-kontrakten er hellig. Ny regel = ny klasse.
> Pipeline endres aldri.

---

# Tech-stack

| Komponent | Valg | Begrunnelse |
|-----------|------|-------------|
| Runtime | .NET 8, ASP.NET Core | DNB-standard |
| API-stil | Minimal API | Lavt ceremony, testbar |
| Språk | C# 12, nullable RTs | Type-sikkerhet |
| Testing | xUnit + FluentAssertions + WAF | Full integrasjonstest |
| Beløp | `long` (øre) | Ingen presisjonsfeil |
| CI/CD | GitHub Actions | Automatisk self-healing |
| PEP-tjeneste | Interface + mock | Produksjonsimpl. utelates |

### Solution-struktur (bygges av agentene)

```
src/Api          ← ASP.NET Core Minimal API
src/Core         ← Domenelogikk, IScreeningRule, pipeline
src/Infrastructure ← PEP-mock, lagring
tests/Api.Tests  ← WebApplicationFactory-integrasjonstester
tests/Core.Tests ← Unit-tester for regler og pipeline
```

---

<!-- _class: section-break -->

<div class="num">03</div>

# Dark Software Factory-arkitekturen

## Fem agenter, én pipeline

---

# Fabrikken — oversikt

```
┌─────────────────────────────────────────┐
│  👤 MENNESKER DEFINERER (én gang)        │
│  CLAUDE.md · scenarios/ · skills/        │
└───────────────┬─────────────────────────┘
                │ én setning per feature
                ▼
┌─────────────────────────────────────────┐
│  ⚙️  DARK SOFTWARE FACTORY               │
│                                         │
│  architect  →  developer                │
│                    ↕                    │
│              reviewer  tester  (par.)   │
│                    ↓                    │
│                  eval                   │
└───────────────┬─────────────────────────┘
                │ satisfaction ≥ 80%
                ▼
┌─────────────────────────────────────────┐
│  ✅ LEVERANSE                            │
│  Commit · PR · analyse.md               │
└─────────────────────────────────────────┘
```

---

# Agent 1 — Architect

**Rolle:** Løsningsarkitekt. Tekniske valg + faset implementeringsplan.

**Input:** Feature-beskrivelse + eksisterende kodestruktur

**Output:** `doc/[feature]-plan.md`

### Hva arkitekten leverer

- Tekniske valg med begrunnelse (tabell)
- Nye typer, interfaces, API-endepunkter (kode)
- Avhengighetsgraf (brukes til parallellisering)
- 3–5 faser med akseptansekriterier per fase

### Viktige prinsipper

- **Pipeline-status-mapping er eksplisitt** — hvilken `ScreeningStatus` mapper til hvilken regel er spesifisert i planen, ikke overlatt til developer-agenten
- **Regel-navn er snake_case** — `amount_threshold`, `sanctioned_country`, `cumulative_daily_limit`, `pep_check`
- **Ingen alternativanalyser** — tar beslutninger, forklarer ikke bort

---

# Agent 2 — Developer

**Rolle:** Senior .NET/C# utvikler, 15+ års erfaring.

**Input:** Fasebeskrivelse + akseptansekriterier fra plan

**Output:** `src/` + `tests/` (all kode og tester)

### Sjekkliste (utvalg)

| Kategori | Krav |
|----------|------|
| **C#-kvalitet** | `long` for beløp, `record` for DTOer, nullable RTs, async/await |
| **Domenelogikk** | Grenseverdier eksakte, kumulativ grense per UTC-dag, PEP krever begge |
| **Testing** | WebApplicationFactory, FluentAssertions, grenseverdier 9 999 999 / 10 000 000 / 10 000 001 øre |
| **ASP.NET** | Schema-validering, global exception handler, health endpoint |
| **CI** | `dotnet format --verify-no-changes`, test-output som .trx |

> **Byggemiljø:** Alt via Docker — ingen lokal dotnet-installasjon nødvendig.

---

# Agent 3 — Reviewer

**Rolle:** Streng code-reviewer, 20 år sikkerhet + .NET + finans.

**Input:** Endrede filer etter implementering

**Output:** `GODKJENT` / `BETINGET GODKJENT` / `AVVIST` + funn-tabell

### Alvorlighetsgrader

| Nivå | Eksempel | Konsekvens |
|------|---------|------------|
| **K — Kritisk** | Feil compliance-logikk, sikkerhetshull | MÅ fikses — pipeline blokkeres |
| **A — Alvorlig** | Manglende validering, logiske feil | BØR fikses |
| **M — Moderat** | Race conditions, testmangel | Vurder |
| **L — Lavt** | Stil, informativt | Kan ignoreres |

### Hva reviewer fokuserer på
- Ingen `.Result`/`.Wait()` (deadlock-risiko)
- Audit-logg inneholder **alle** beslutninger, ikke bare avviste
- Sanksjonsliste-match er case-insensitive
- Stack traces **aldri** i klientresponser

---

# Agent 4 — Tester

**Rolle:** QA-ingeniør — kjenner ikke implementasjonsdetaljer, tester krav.

**Input:** Akseptansekriterier + endrede filer

**Output:** Testresultat-rapport

### To moduser

**Modus 1 — Kodeevaluering (uten kjørende API)**
- Dekker testene akseptansekriteriene?
- Er grenseverdier testet? (`9 999 999` / `10 000 000` / `10 000 001` øre)
- Negative scenarier? (400 Bad Request)

**Modus 2 — API Smoke test**
```bash
curl -X POST /api/v1/screen -d '{ amount: 10000001 }'  # → Flagged
curl -X POST /api/v1/screen -d '{ country: "KP" }'     # → Rejected
curl -X POST /api/v1/screen -d '{ amount: 1000000 }'   # → Approved
```

> Holdout-evaluering er **eval-agentens** ansvar — ikke tester-agentens.

---

# Agent 5 — Eval (Holdout Evaluator)

**Rolle:** Uavhengig QA-evaluator. Har **aldri** sett kildekoden.

**Input:** URL til kjørende API + `scenarios/`-mappen

**Output:** Satisfaction-score (antall bestått / totalt)

### Hva eval-agenten gjør

1. Leser alle scenario-filer
2. Konstruerer HTTP-requests med `curl`
3. Evaluerer faktisk respons mot forventet
4. Rapporterer score — foreslår **ingen** fikser

```
Satisfaction: 4/5 scenarier tilfredsstilt (80%)

✅ 01-amount-threshold     → Flagged       (forventet: Flagged)
✅ 02-sanctioned-country   → Rejected      (forventet: Rejected)
✅ 03-normal-payment       → Approved      (forventet: Approved)
❌ 04-cumulative-daily-limit → Approved    (forventet: Flagged)
✅ 05-pep-check            → PendingReview (forventet: PendingReview)
```

**≥ 80%:** Pipeline fortsetter → commit
**< 80%:** Dev-agenten fikser → ny eval-runde

---

<!-- _class: section-break -->

<div class="num">04</div>

# Kjerneprinsippene

## Train/test-separasjon · Self-healing · Self-evolution

---

# Train/Test-separasjon — det viktigste prinsippet

```
┌─────────────────────────────┐    ┌─────────────────────────────┐
│  🏋️  TRENING (dev-agenten)   │    │  🎯 TEST (eval-agenten)      │
│                             │    │                             │
│  CLAUDE.md                  │    │  scenarios/ ←── ALDRI sett  │
│      ↓                      │    │      ↓         av dev       │
│  Developer-agent            │    │  Eval-agent                 │
│      ↓                      │    │      ↓                      │
│  Kode + unit-tester         │───→│  Kjørende API               │
│                             │    │      ↓                      │
│                             │    │  Satisfaction-score         │
└─────────────────────────────┘    └─────────────────────────────┘
```

### Hvorfor?

StrongDM oppdaget at agenter som **kan se testene** skriver kode som gamer dem — inkludert `return true` for alle checks.

**`.claudeignore`** gjør `scenarios/`-mappen usynlig for alle dev-agenter.

---

# Satisfaction-metrikk — ikke binær pass/fail

### Gammelt

```
Alle tester grønne ✅   (men tester skrevet av dev-agenten selv)
```

### Nytt

```
Satisfaction: 4/5 holdout-scenarier tilfredsstilt (80%)
```

### Hvorfor dette er bedre

| | Unit-tester (dev-skrevet) | Holdout-scenarier |
|--|--|--|
| Distribusjon | In-distribution | Out-of-distribution |
| Risiko | Gamer systemet | Uavhengig fasit |
| Evaluert av | Dev-agenten selv | Separat eval-agent |
| Formål | Implementeringskvalitet | Kravoppfyllelse |

> "Tester skrevet av dev-agenten tester det dev-agenten **trodde** den implementerte.
> Holdout-scenarier tester at tjenesten **faktisk** oppfyller kravene."

---

# CI Self-Healing

Agenten er koblet til **hele utviklingssyklusen** — ikke bare kodegenerering.

```
git push
    ↓
GitHub Actions
  dotnet build + dotnet test + dotnet format
    ↓ feil?
gh run view --log-failed   →   [feilmelding]
    ↓
Dev-agent: "CI feiler. Output: [...]. Analyser og fiks."
    ↓
Fix + ny commit
    ↓
git push → ✅ CI grønn
```

### I praksis

```bash
gh run list --limit 5          # Se CI-status
gh run view --log-failed       # Hent feildetaljer
# Lim inn output til dev-agenten
```

---

# Self-Evolving Skills

Etter hver ferdigstilte feature oppdateres skills basert på funn:

```
Feature fullført
    ↓
doc/[feature]-analyse.md  ←  OBLIGATORISK (hook blokkerer uten den)
    ↓
Review-funn analysert:
  Bug i grenseverdi?       →  developer/SKILL.md + nytt punkt
  Review glemte X?         →  reviewer/SKILL.md  + nytt punkt
  Testdekning-hull?        →  tester/SKILL.md    + nytt punkt
    ↓
Neste feature fanger feilen automatisk
```

### Resultater fra parallelt prosjekt

> Etter 9 iterasjoner har `developer/SKILL.md` **80+ prosjektspesifikke gotchas** akkumulert fra reelle bugs — dette er den sterkeste mekanismen i oppsettet.

Samme prinsipp som **Memento-Skills** (forskning 2026): AI-agenter som utvikler egne skills over tid uten å retrene modellen.

---

<!-- _class: section-break -->

<div class="num">05</div>

# Kvalitetsporter og repostruktur

## Hva stopper dårlig kode fra å nå prod

---

# Fem kvalitetsporter

| Port | Hva kontrolleres | Blokkerer |
|------|-----------------|-----------|
| **1. Plan** | Orkestrator evaluerer arkitektplan | Steg 2 (ingen kode uten plan) |
| **2. Code review** | Review-agent: kritiske og alvorlige funn | Neste fase |
| **3. Unit + integrasjonstester** | Test-agent: dekning og grenseverdier | Neste fase |
| **4. Holdout-evaluering** | Eval-agent: satisfaction ≥ 80% | Commit |
| **5. Sluttrapport** | `require-analyse.sh` hook | Commit (automatisk) |

### Hooks i `.claude/settings.json`

```
require-plan.sh    ← Blokkerer Write/Edit til src/ uten plandokument
require-review.sh  ← Blokkerer git commit uten GODKJENT-verdikt
require-analyse.sh ← Blokkerer git commit uten doc/[feature]-analyse.md
```

> Hooks kjøres av Claude Code-harness — ikke av Claude selv.
> De kan **ikke** omgås ved å glemme dem.

---

# Repostruktur

```
dnb-compliance-dsf/
│
├── CLAUDE.md               ← Kontrakten med agentene (les dette først)
├── .claudeignore            ← scenarios/ usynlig for alle dev-agenter
│
├── .claude/
│   ├── settings.json       ← Tillatelser og hook-konfigurasjon
│   ├── hooks/
│   │   ├── require-plan.sh
│   │   ├── require-review.sh
│   │   └── require-analyse.sh
│   └── skills/
│       ├── architect/SKILL.md
│       ├── developer/SKILL.md
│       ├── reviewer/SKILL.md
│       ├── tester/SKILL.md
│       └── eval/SKILL.md    ← Holdout evaluator
│
├── scenarios/              ← HOLDOUT — dev-agenten ser ALDRI dette
│   ├── 01-amount-threshold.md
│   ├── 02-sanctioned-country.md
│   ├── 03-normal-payment.md
│   ├── 04-cumulative-daily-limit.md
│   └── 05-pep-check.md
│
└── doc/                    ← Plan- og analysedokumenter (auto-generert)
```

---

# Hva mennesker skriver — og hva de aldri berører

### Mennesker skriver én gang (stabilt)

| Fil | Hva | Frekvens |
|-----|-----|----------|
| `CLAUDE.md` | Arkitektur, regler, API-kontrakt | Én gang per tjeneste |
| `scenarios/` | Holdout-fasit | Per feature-set |
| `.claude/skills/*.md` | Rolledefinisjon per agent | Oppdateres auto etter funn |
| `.claude/hooks/` | Kvalitetsporter | Én gang |

### Per iterasjon — menneskelig input

```
"Ny compliance-regel: velocity check. Samme sender > 10 transaksjoner
 siste 60 min → flagged."
```

### Aldri menneskelig ansvar

`src/` · `tests/` · `.github/workflows/` · `doc/*-plan.md` · `doc/*-analyse.md`

> **Hvis et menneske skriver disse, er fabrikken ikke mørk.**

---

<!-- _class: section-break -->

<div class="num">06</div>

# Kom i gang

## Workshop-guide og neste steg

---

# Kjøre fabrikken — trinn for trinn

### 1. Start

```bash
cd dnb-compliance-dsf
claude
```

### 2. Dispatch architect-agent

```
Les CLAUDE.md.
Dispatcher architect-agent for Transaction Compliance Service.
Les .claude/skills/architect/SKILL.md og lag implementeringsplan.
Lagre i doc/compliance-service-plan.md.
```

### 3. Implementer fase for fase (protokollen i CLAUDE.md)

```
For hver fase:
  1. Dev-agent implementerer
  2. Review-agent + test-agent parallelt
  3. Evaluer, fiks ved avvik, oppdater plan
```

### 4. Holdout-evaluering

```bash
docker run -p 5000:8080 compliance-api   # start API
# Ny agent-sesjon:
# "Les .claude/skills/eval/SKILL.md. Les scenarios/. API på http://localhost:5000."
```

---

# Workshop-spor

| Spor | Oppgave | Nivå |
|------|---------|------|
| **Spor 1** | Kjør ferdig Transaction Compliance Service fra scratch | Begynner |
| **Spor 2** | Legg til én ny compliance-regel (velocity check) | Middels |
| **Spor 3** | Skriv din egen `CLAUDE.md` for en annen tjeneste | Avansert |

### Tips til fasilitator

- Demo-sekvensen tar ~20 min med ferdigkonfigurert repo
- Ha backup: pre-bygget branch hvis nettverk svikter
- Kjør **alltid** gjennom agent-protokollen én gang på forhånd
- Vis hooks i aksjon — la en agent forsøke å committe uten plan

---

<!-- _class: dark -->

# Nøkkelpunkter

<br>

**1. CLAUDE.md er alt** — Kvaliteten på input bestemmer kvaliteten på output. Dette er briefingen til en senior-utvikler som starter dag én.

**2. Train/test-separasjon** — `.claudeignore` holder `scenarios/` borte fra dev-agenten. Det er det som gjør autonom koding trygg.

**3. Satisfaction > pass/fail** — Holdout-scenarier tester faktisk kravoppfyllelse, ikke bare at agenten implementerte det den trodde.

**4. Skills selvjusterer** — `doc/*-analyse.md` → skill-oppdatering → neste feature fanger feilen automatisk.

**5. Erfaring er verdifull** — Den som vet hva "riktig" betyr i et forsikringssystem, **definerer kvaliteten**. Det krever domeneforståelse — nøyaktig det DNB Liv har.

**6. Hooks er ufravikelige** — Kvalitetsporter som ikke kan glemmes fordi de kjøres av harness.

---

<!-- _class: lead -->

# Spørsmål?

## Dark Software Factory · DNB Liv

**Start her:**

1. Velg én tjeneste i DNB Liv
2. Skriv `CLAUDE.md` for den tjenesten
3. Kjør første agent-syklus

> *"Ikke hopp til nivå 5 — bygg tillit inkrementelt.
> Nivå 3 alene er en 10x-forbedring for de fleste team."*
