# sudoku.dsf

A single-page Sudoku app built with React, TypeScript, and Docker. Runs fully in the browser — no backend required.

---

## Features

- 9×9 board with four difficulty levels: Easy, Medium, Hard, Expert
- Real-time conflict highlighting (row, column, 3×3 box)
- Pencil/notes mode — toggle candidate numbers per cell
- Undo (up to 50 moves)
- Elapsed timer that pauses on page blur
- Auto-save to localStorage — resume where you left off after refresh
- Win detection with completion modal
- Keyboard navigation (arrows, 1–9, Backspace, `n` for notes mode)
- Mobile-friendly number pad
- Dark/light mode

---

## Stack

| Layer | Technology |
|-------|-----------|
| UI | React 18 + TypeScript |
| Build | Vite |
| State | Zustand |
| Styling | Tailwind CSS |
| Puzzle engine | Pure TypeScript (backtracking solver + cell removal) |
| Persistence | localStorage |
| Container | Docker + docker-compose |
| Tests | Vitest + React Testing Library |

---

## Running Locally

**With Docker (recommended):**
```bash
docker compose up
```
Open http://localhost:5173

**Without Docker:**
```bash
cd client
npm install
npm run dev
```

---

## Project Structure

```
client/
├── src/
│   ├── components/   GameBoard, Cell, Toolbar, Timer, DifficultySelect, NumberPad, WinModal
│   ├── engine/       generator.ts, solver.ts, validator.ts
│   ├── store/        gameStore.ts (Zustand)
│   ├── hooks/        useTimer.ts, usePersistence.ts
│   └── types.ts
├── vite.config.ts
└── package.json
docker-compose.yml
Dockerfile
```

---

## Tests

```bash
cd client
npm run test:run
```

---

## How It Was Built

Built end-to-end by AI agents using the [Dark Software Factory](https://hackernoon.com/the-dark-factory-pattern-moving-from-ai-assisted-to-fully-autonomous-coding) pattern — an orchestrator dispatches specialist agents (architect, developer, reviewer, tester, evaluator) with quality gates enforced by shell hooks between each phase. No human wrote any application code.

| Phase | Delivered |
|-------|-----------|
| F1 | Puzzle engine (generator, solver, validator) + Zustand store |
| F2 | Board UI, cell input, keyboard/mobile interaction |
| F3 | Timer, localStorage persistence, undo |
| F4 | Polish, dark mode, Docker setup |
