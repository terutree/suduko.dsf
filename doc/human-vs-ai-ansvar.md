# Human vs. AI — Division of Responsibility in Dark Software Factory

The factory has one principle: humans define **what** and **why**, AI produces **how**.

---

## Humans Write Once — The Intent Layer

These files are stable between iterations. They define the contract with the factory.

| File | Content | Why human |
|-----|---------|-----------------|
| `CLAUDE.md` | Architecture, compliance rules, API contract, agent protocol, code standards | Domain understanding and architecture responsibility cannot be delegated |
| `scenarios/*.md` | Holdout acceptance criteria in plain text | Can be AI-generated from `CLAUDE.md`. What matters is not *who* writes them, but that the dev agent never sees them during implementation — train/test separation |
| `.claude/skills/*.md` | Role definition per agent (architect, developer, reviewer, tester, eval) | Defines quality level and working method per role |
| `.claude/hooks/` | `require-plan`, `require-review`, `require-analyse` — blocks commit on violations | Quality gates are non-negotiable and cannot be overridden by the agent itself |
| `.claudeignore` | Excludes `scenarios/` from the dev agent's context | Guarantees train/test separation — without this file the holdout pattern is broken |
| `.claude/settings.json` | Permissions and Claude Code configuration | Security boundary — what the factory is allowed to do |

---

## Updated Per Iteration

### Human Input Per Iteration: One Sentence

```
New compliance rule: PEP check. Receiver PEP + amount > 50,000 NOK → pending_review.
Fetch PEP list via HttpClient (mock interface). Log PEP match in audit log.
```

That is it. The rest is the factory.

### What Gets Updated

| Input | Who | When |
|-------|------|-----|
| Feature description (natural language) | **Human** | New task / Jira ticket |
| `scenarios/` — new scenarios | **Human** | New rule, new edge case, or gap identified in eval report |
| `CLAUDE.md` — architecture changes | **Human** | New established decision after discussion (not routine changes) |
| `.claude/skills/*.md` — skill adjustment | **Human** | Based on findings in `doc/*-analysis.md` — see improvement loop below |
| `doc/*-plan.md` | AI (architect agent) | Automatic — human reads, does not approve |
| `doc/*-analysis.md` | AI (orchestrator) | Automatic after final phase — this is the key document |

---

## The Improvement Loop

```
doc/*-analysis.md
      │
      ▼
Human reads: patterns in review findings, test failures, satisfaction score
      │
      ├── Recurring review findings → update skills/reviewer/SKILL.md
      ├── Skill gap in dev agent → update skills/developer/SKILL.md
      ├── Scenario gaps → add to scenarios/
      └── Architecture change → update CLAUDE.md
      │
      ▼
Next iteration produces higher quality without model retraining
```

This is the closest a Dark Software Factory gets to Memento Skills — evolving quality through human feedback on AI-generated analysis reports, not via new training data.

---

## What Is Never Human Responsibility

- Code (all files under `src/`)
- Tests (all files under `tests/`)
- CI configuration (`.github/workflows/`)
- Plan documents (`doc/*-plan.md`)
- Analysis reports (`doc/*-analysis.md`)
- PR texts and commit messages

If a human writes these, the factory is not dark.

---

## Practical Checklist — New Service from Scratch

```
[ ] Write CLAUDE.md (architecture, rules, API contract, stack decisions)
[ ] Write scenarios/ (3–5 holdout scenarios covering critical rules)
[ ] Verify .claudeignore excludes scenarios/
[ ] Configure .claude/skills/ (copy from this repo, adapt domain language)
[ ] Configure .claude/hooks/ (require-plan, require-review, require-analyse)
[ ] Start the factory with one feature description
```

After first delivery: read `doc/*-analysis.md` and update skills based on findings.
