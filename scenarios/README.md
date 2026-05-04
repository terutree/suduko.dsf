# Holdout Scenarios

> **CRITICAL: These files are in `.claudeignore`.**
> The dev agent implementing the code NEVER reads this content.
> Only the eval agent (`eval/SKILL.md`) uses these — and only against a running API.

## What This Is

Acceptance criteria written as plain-text HTTP scenarios.

Inspired by train/test separation in machine learning: the code is trained (implemented) without knowing the answer key. Evaluation happens separately, by a different agent, against a running API.

StrongDM discovered that agents who can see the tests write code that games them — including `return true`. The separation is what makes autonomous coding safe.

## Structure

Each scenario is a Markdown file with:
- **Request**: HTTP method, endpoint, payload
- **Expected**: status code, status field, active rules
- **Rationale**: which compliance rule is relevant

## Who Creates the Scenarios?

Scenarios are created as part of the planning process — **not** by the dev agent:

1. Architect agent creates a plan with a `## Holdout Scenario` section (content + filename)
2. The orchestrator writes the scenario file using the `Write` tool based on the plan
3. The orchestrator commits the file **BEFORE** implementation starts
4. The dev agent never sees `scenarios/` — `.claudeignore` blocks indexing

## Running Holdout Evaluation

```bash
# 1. Start API
docker build -t compliance-api . && docker run -p 5000:8080 compliance-api

# 2. Dispatch eval agent
# Prompt to orchestrator:
#   "Read .claude/skills/eval/SKILL.md
#    Read the scenarios/ folder
#    API is running at http://localhost:5000
#    Deliver satisfaction report."
```
