# Sudoku SPA: Architecture and Plan

**Date:** 2026-05-22

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | React 18 + TypeScript + Vite | Fast HMR, modern tooling, type safety |
| State | Zustand | Minimal boilerplate, no context drilling |
| Styling | Tailwind CSS | Utility-first, responsive without custom CSS |
| Puzzle engine | Pure client-side TypeScript | No backend needed, fast, offline-capable |
| Generator strategy | Backtracking solver + cell removal | Reliable unique-solution guarantee |
| Persistence | LocalStorage | No backend, works offline, simple API |
| Containerization | Docker + docker-compose (Vite dev server) | Single command startup |
| Testing | Vitest + React Testing Library | Native Vite integration |

## Project Structure

```
sudoku-app/
├── client/
│   ├── src/
│   │   ├── components/   GameBoard, Cell, Toolbar, Timer, DifficultySelect, NumberPad, WinModal
│   │   ├── engine/       generator.ts, solver.ts, validator.ts
│   │   ├── store/        gameStore.ts
│   │   ├── hooks/        useTimer.ts, usePersistence.ts
│   │   └── main.tsx
│   ├── index.html
│   ├── vite.config.ts
│   ├── tailwind.config.js
│   └── package.json
├── docker-compose.yml
└── Dockerfile
```

## Dependency Graph

```
F1 (Engine + Store) ──► F2 (Board UI + Input) ──► F3 (Timer + Persistence) ──► F4 (Polish + Docker)
```

## Implementation Plan

### F1: Sudoku Engine and State Store
**QC-status:** APPROVED
**Review:** APPROVED (round 3) — all actions use functional `set(state=>...)` form, no stale closures, no findings
**Test:** PASS — 39/39 pass
**Delivers:** Core puzzle logic (generator, solver, validator) and Zustand store. No UI yet — engine is fully testable in isolation.
**Files:**
- `client/package.json`
- `client/vite.config.ts`
- `client/tailwind.config.js`
- `client/src/engine/solver.ts`
- `client/src/engine/generator.ts`
- `client/src/engine/validator.ts`
- `client/src/store/gameStore.ts`
- `client/src/types.ts`

**Acceptance Criteria:**
- [ ] `solver.ts` solves a valid 9×9 board and returns `null` for unsolvable input
- [ ] `generator.ts` produces a valid puzzle with a unique solution for Easy (36–45 clues), Medium (32–35), Hard (28–31)
- [ ] `validator.ts` identifies all cells that conflict in their row, column, or 3×3 box
- [ ] Zustand store holds: `board`, `selectedCell`, `difficulty`, `notesMode`, `history`, `status` (`playing` | `won`)
- [ ] Store actions: `selectCell`, `enterNumber`, `clearCell`, `newGame(difficulty)`, `toggleNotesMode`, `undo`
- [ ] `newGame` replaces the board and resets status; difficulty is passed as argument
- [ ] Win condition detected automatically after every `enterNumber` call when board is complete and valid

---

### F2: Game Board UI and Number Input
**QC-status:** APPROVED
**Review:** APPROVED (round 2) — all accessibility fixes confirmed, no new issues
**Test:** PASS — 39/39 pass
**Delivers:** Playable board in browser. User can click cells, type numbers, see validation highlights, and win the game.
**Files:**
- `client/src/components/GameBoard.tsx`
- `client/src/components/Cell.tsx`
- `client/src/components/NumberPad.tsx`
- `client/src/components/WinModal.tsx`
- `client/src/components/Toolbar.tsx`
- `client/src/components/DifficultySelect.tsx`
- `client/src/App.tsx`
- `client/index.html`
- `client/src/main.tsx`

**Acceptance Criteria:**
- [ ] 9×9 grid renders with visible 3×3 box borders
- [ ] Selected cell is highlighted; cells sharing the same number or same row/column/box are subtly highlighted
- [ ] Fixed (clue) cells are visually distinct and cannot be edited
- [ ] Invalid cells (duplicate in row/column/box) are highlighted in red
- [ ] Keyboard input 1–9 enters a number; Backspace/Delete clears; arrow keys navigate
- [ ] NumberPad renders below the board on mobile and mirrors keyboard input
- [ ] DifficultySelect dropdown triggers `newGame(difficulty)` on change
- [ ] New Game button in Toolbar generates a new puzzle at selected difficulty
- [ ] WinModal appears when `status === 'won'`, shows congratulations, offers New Game

---

### F3: Timer and LocalStorage Persistence
**QC-status:** APPROVED
**Review:** APPROVED (round 2) — all three fixes confirmed, no new issues
**Test:** PASS — 52/52 pass, timer tick + localStorage mock tests added
**Delivers:** Elapsed timer and autosave. User can close and reopen the browser and continue from where they left off.
**Files:**
- `client/src/hooks/useTimer.ts`
- `client/src/hooks/usePersistence.ts`
- `client/src/components/Timer.tsx`
- `client/src/store/gameStore.ts` (extend with `elapsedTime`, `timerRunning`)

**Acceptance Criteria:**
- [ ] Timer starts on first move, counts up in seconds, displays as `MM:SS`
- [ ] Timer stops when puzzle is won
- [ ] Timer resets to 0 when New Game is started
- [ ] Board state (cells, notes, difficulty, elapsed time) is saved to LocalStorage on every move
- [ ] On page load, saved state is restored if present and `status !== 'won'`
- [ ] Starting a New Game clears the saved state and begins fresh

---

### F4: Docker, Responsive Polish, and Accessibility
**QC-status:** APPROVED
**Review:** APPROVED (round 2) — all three fixes confirmed, no new issues
**Test:** PASS — 52/52
**Delivers:** Fully containerized app with a polished mobile layout and basic accessibility. `docker compose up` opens a playable game.
**Files:**
- `Dockerfile`
- `docker-compose.yml`
- `client/src/App.tsx` (responsive layout adjustments)
- `client/src/components/Cell.tsx` (ARIA labels)
- `client/src/components/GameBoard.tsx` (ARIA grid role)
- `client/tailwind.config.js` (dark mode token if needed)

**Acceptance Criteria:**
- [ ] `docker compose up` starts the app; game is accessible at `http://localhost:5173`
- [ ] Layout is fully usable on a 375px mobile viewport (no horizontal scroll, NumberPad visible)
- [ ] Board cells have `aria-label` describing row, column, value, and fixed/editable state
- [ ] GameBoard has `role="grid"` with appropriate `aria-rowcount`/`aria-colcount`
- [ ] Hot reload works inside the container (Vite `--host` flag, volume mount)

---

## Risks

| Risk | Mitigation |
|------|-----------|
| Puzzle generator too slow for Hard (uniqueness check via full solve per removal) | Cap removals with a step limit; accept near-minimal clue counts rather than exact target |
| LocalStorage serialization of `Board[]` history grows unbounded | Cap undo history to 50 moves in the store |
| Vite HMR unreliable inside Docker on macOS (polling) | Set `CHOKIDAR_USEPOLLING=true` or `server.watch: { usePolling: true }` in vite.config |
