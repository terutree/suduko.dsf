# Prosessforbedring: QC-håndhevelse mellom faser

**Dato:** 2026-04-29
**Bakgrunn:** Orkestratoren brøt protokollen under første byggerunde ved å starte F2 uten å kjøre review+test på F1.

---

## Problemet

CLAUDE.md beskriver en klar protokoll:

```
2a. IMPLEMENTER
2b. KVALITETSKONTROLL (review + test, parallelt)
2c. EVALUER
2d. OPPDATER PLAN
→ neste fase
```

I praksis ble F1 og F2 implementert uten at steg 2b ble kjørt mellom dem. Orkestratoren gikk direkte F1 → F2 → startet F3.

**Rotårsak:** Ingenting *blokkerte* overgangen. Prosessen var dokumentert, men ikke håndhevet. Hooks sjekket kun at plan fantes (ved kodeskriving) og at review-verdikt fantes i plan (ved commit) — ikke om gjeldende fase var QC-godkjent *mellom* fasene.

---

## Endringer

### 1. Ny hook: `require-phase-qc.sh`

**Hva:** PreToolUse-hook som kjøres på alle Write/Edit-kall til `src/` og `tests/`.

**Mekanisme:** Leser plandokumentet og søker etter `**QC-status:** IMPLEMENTERT`. Hvis funnet: avbryt med exit 2 og feilmelding som viser hvilken fase som blokkerer og nøyaktig hva som må gjøres.

**Hvorfor:** Uten maskinell blokkering er protokollen en henstilling, ikke en port. Orkestratoren kan ikke (selv utilsiktet) starte neste fase uten at QC er fullført — Claude Code vil avvise selve skriveoperasjonen.

```
F1 dev-agent ferdig
  → orkestrator setter **QC-status:** IMPLEMENTERT i plan
  → hook BLOKKERER all videre skriving til src/
  → orkestrator kjører review + test parallelt
  → orkestrator setter **QC-status:** GODKJENT
  → hook tillater F2 dev-agent å skrive
```

### 2. QC-status-konvensjon i plandokumentet

**Hva:** Hvert faseavsnitt i `*-plan.md` får nå et obligatorisk felt:

```markdown
### F1: Domenetyper og solution-scaffold
**QC-status:** IKKE STARTET | IMPLEMENTERT | GODKJENT | AVVIST
**Review:** —
**Test:** —
```

| Verdi | Tilstand | Hook |
|-------|----------|------|
| `IKKE STARTET` | Fase ikke begynt | Tillatt |
| `IMPLEMENTERT` | Dev-agent kjørt — venter QC | **BLOKKERT** |
| `GODKJENT` | Review+test OK | Tillatt |
| `AVVIST` | Feil funnet — dev-agent fikser | **BLOKKERT** |

**Hvorfor:** Hooken trenger et konsistent ankerpunkt i plandokumentet. Feltet tjener dobbelt formål: maskinell håndhevelse og menneskelig sporbarhet (hvem godkjente, hva var funnene).

### 3. settings.json oppdatert

**Hva:** `require-phase-qc.sh` lagt til i `PreToolUse`-hooks for `Write|Edit`, parallelt med eksisterende `require-plan.sh`.

**Hvorfor:** Hooken må kjøre *før* agenten skriver til disk — ikke etter.

### 4. CLAUDE.md: steg 2b med BLOKKERT-boks og QC-status-tabell

**Hva:** Steg 2b fikk en synlig advarselsboks:

```
╔══════════════════════════════════════════════════════════╗
║  STOPP. Kjør review + test FØR neste fase. Uten unntak. ║
╚══════════════════════════════════════════════════════════╝
```

Steg 2a fikk eksplisitt påminnelse: oppdater QC-status til IMPLEMENTERT etter dev-agent er ferdig. Kvalitetsporter-tabellen ble oppdatert med begge nye porter. Ufravikelige regler fikk `require-phase-qc.sh`-referanse.

**Hvorfor:** Protokollteksten var korrekt men ikke fremtredende nok. Visuell vekt og eksplisitte hook-referanser gjør det vanskeligere å overse steget.

### 5. architect/SKILL.md: QC-status i fase-mal

**Hva:** Fase-malen nå inkluderer `**QC-status:** IKKE STARTET` som standardfelt.

**Hvorfor:** Architect-agenten genererer plandokumentet. Uten feltet i malen vil nye planer mangle ankerpunktet hooken leser.

### 6. developer/SKILL.md: to rettelser

**a) RuleStatus-enum korrigert**

`RuleStatus.Pass`/`Flag` → `RuleStatus.Passed`/`Triggered`

Skill-filen var utdatert og stemte ikke med faktisk implementert kode (`src/Core/Models/Enums.cs`). En reviewer-agent ville fanget dette — presist det feilen i prosessen kostet oss.

**b) Rapportplikt tydeliggjort**

Lagt til: dev-agenten skal rapportere eksakt filliste slik at orkestratoren kan sette QC-status og starte QC-prosessen.

### 7. architect/SKILL.md: default-mapping rettet

`default→Flagged` → `default→Approved`

Feil default-status i pipeline-mapping-dokumentasjonen. Ingen triggerede regler skal gi `Approved`, ikke `Flagged`.

---

## Hva som nå er i plan (compliance-service-plan.md)

| Fase | QC-status |
|------|-----------|
| F1 | IMPLEMENTERT ← blokkerer |
| F2 | IMPLEMENTERT ← blokkerer |
| F3 | IKKE STARTET |
| F4 | IKKE STARTET |
| F5 | IKKE STARTET |

**Neste steg:** Kjøre review-agent + test-agent parallelt på F1+F2. Etter GODKJENT: sette begge til GODKJENT og fortsette med F3.

---

## Hva dette ikke løser

- Hooken leser `find doc/ -name "*-plan.md" | head -1` — fungerer kun hvis ett plandokument eksisterer. Flere samtidige features krever mer robust plan-identifikasjon.
- Ingen håndhevelse av *hvilken* fase som er IMPLEMENTERT vs. hvilke src/-filer som tilhører den fasen. En agent som skriver til en annen fase sine filer vil også bli blokkert (konservativt — dette er riktig adferd).
- Eval-agentens satisfaction-score håndheves fortsatt kun av prosess, ikke av hook.
