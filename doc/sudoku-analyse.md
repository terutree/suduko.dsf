# Sudoku SPA: Final Analysis

**Date:** 2026-05-22

## Timing Table

| Phase | Activity | Duration (approx) |
|-------|----------|-------------------|
| Plan  | Architect agent | ~1 min |
| F1    | Dev agent | ~4 min |
| F1    | QC round 1 (review CONDITIONAL + test FAIL) | ~2 min |
| F1    | Dev agent fix | ~3 min |
| F1    | QC round 2 (review CONDITIONAL + test PASS) | ~1 min |
| F1    | Dev agent fix (stale closure) | ~1 min |
| F1    | QC round 3 (review APPROVED) | ~1 min |
| F2    | Dev agent | ~3 min |
| F2    | QC round 1 (review CONDITIONAL + test PASS) | ~2 min |
| F2    | Dev agent fix (accessibility) | ~11 min |
| F2    | QC round 2 (review APPROVED) | ~1 min |
| F3    | Dev agent | ~4 min |
| F3    | QC round 1 (review CONDITIONAL + test FAIL) | ~2 min |
| F3    | Dev agent fix | ~4 min |
| F3    | QC round 2 (review APPROVED + test PASS) | ~1 min |
| F4    | Dev agent | ~2 min |
| F4    | QC round 1 (review CONDITIONAL + test PASS) | ~2 min |
| F4    | Dev agent fix | ~1 min |
| F4    | QC round 2 (review APPROVED) | ~1 min |
| **Total** | | **~51 min** |

## Review Analysis

### Findings per phase

| Phase | Round | Severity | Finding | Outcome |
|-------|-------|----------|---------|---------|
| F1 | 1 | M | validator.ts: unsafe `!` on Map.get() | Fixed |
| F1 | 1 | M | gameStore: notes edits not added to undo history | Fixed |
| F1 | 1 | Mo | gameStore: timer race — get() inside set() | Fixed |
| F1 | 1 | — | Missing gameStore.test.ts (store untested) | Fixed |
| F1 | 1 | — | countSolutions >1 not tested | Fixed |
| F1 | 1 | — | Generator upper-bound assertion too weak | Fixed |
| F1 | 2 | Mi | board/history still from get() outside set(state=>) | Fixed (round 3) |
| F2 | 1 | Mo | Cell: no tabIndex — board unreachable by Tab | Fixed |
| F2 | 1 | Mo | WinModal: no autoFocus/Escape handling | Fixed |
| F3 | 1 | Mi | useTimer: unnecessary elapsedTime subscription (re-renders) | Fixed |
| F3 | 1 | Mo | SAVE_KEY duplicated in usePersistence + gameStore | Fixed |
| F3 | 1 | Mi | loadSave timerRunning heuristic undocumented | Fixed |
| F3 | 1 | — | localStorage write on move untested | Fixed |
| F3 | 1 | — | newGame clears localStorage untested | Fixed |
| F3 | 1 | — | Timer interval tick not exercised with fake timers | Fixed |
| F4 | 1 | Mi | Cell: double border on row 0/col 0 | Fixed |
| F4 | 1 | Mi | aria-label "empty" ambiguous | Fixed |
| F4 | 1 | Mi | Duplicate polling config in vite.config | Fixed |

### Most common finding types

1. **Missing tests** (5 instances) — dev agent consistently omitted tests for React hooks and LocalStorage side effects. Tests for store were also skipped in F1.
2. **Accessibility gaps** (3 instances) — tabIndex, focus management, ARIA structure. Often deprioritised without explicit AC.
3. **Shared mutable state / race conditions** (3 instances) — Zustand get() inside set(), duplicate constants.
4. **Minor visual defects** (2 instances) — double border, aria wording.

### Takeaways

- Dev agent needs explicit AC for "test the React hook with fake timers" — it defaults to testing only the store layer.
- Accessibility criteria must be spelled out precisely (tabIndex, autoFocus, role="row") — the agent ships without them if not explicitly listed.
- Duplicate constants across files should be a standard review checklist item.

## Test Analysis

| Phase | New tests | Total | Passed |
|-------|-----------|-------|--------|
| F1 (engine) | 28 → 39 (added store + solver gap tests) | 39 | 39 |
| F2 (UI) | 0 new (build+regression only) | 39 | 39 |
| F3 (timer/persistence) | +13 (timer tick, localStorage mock) | 52 | 52 |
| F4 (docker/a11y) | 0 new | 52 | 52 |
| **Final** | **52 total** | **52** | **52 (100%)** |

No holdout eval agent was run (no scenario files exist for a game UI — the eval pattern applies to REST APIs). Manual browser test: game opens, puzzle renders, keyboard + click input works, win modal appears on completion, timer ticks, LocalStorage persists across reload.

## Process Improvements

### For developer SKILL.md

1. **React hooks always need a hook-level test, not just a store test.** Use `renderHook` + `vi.useFakeTimers()` when the hook uses `setInterval`. Acceptance criteria must call this out explicitly.
2. **LocalStorage side effects must be mocked at test level.** Use `vi.fn()` on `globalThis.localStorage` — never assume it's available in the Node test env.
3. **Test store actions by calling them through the store**, not by testing Zustand internals directly. Write `gameStore.test.ts` for every phase that touches the store.

### For architect SKILL.md

1. **Acceptance criteria must include explicit accessibility items** for any component with keyboard interaction: `tabIndex` strategy, focus management for modals, ARIA role hierarchy. If not in AC, dev agent omits them.
2. **Shared constants belong in `types.ts` or `constants.ts` from day one.** Plan should name the file when a constant is used by more than one module.
3. **React hook test strategy belongs in acceptance criteria**, not just as a checklist item in the dev skill. For hooks with side effects (timers, localStorage), state explicitly: "use renderHook + fake timers to test interval ticks."
