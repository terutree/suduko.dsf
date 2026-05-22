import { create } from 'zustand'
import { SUDOKU_SAVE_KEY } from '../types'
import type { Board, Cell, Difficulty, GameState, SavedState } from '../types'
import { generatePuzzle } from '../engine/generator'
import { getInvalidCells, isBoardComplete } from '../engine/validator'

const MAX_HISTORY = 50

/** Convert a 9x9 number array (0 = empty) to a Board. */
function numbersToCells(puzzle: number[][]): Board {
  return puzzle.map(row =>
    row.map((val): Cell => ({
      value: val === 0 ? null : val,
      notes: [],
      fixed: val !== 0,
      invalid: false,
    }))
  ) as Board
}

/** Apply invalid flags from the conflict set to a board (immutably). */
function applyInvalid(board: Board, invalidSet: Set<string>): Board {
  return board.map((row, r) =>
    row.map((cell, c): Cell => ({
      ...cell,
      invalid: invalidSet.has(`${r},${c}`),
    }))
  )
}

function safeLocalStorageRemove(key: string): void {
  try {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(key)
    }
  } catch {
    // ignore
  }
}

function buildInitialState(difficulty: Difficulty): Omit<
  GameState,
  | 'selectCell'
  | 'enterNumber'
  | 'clearCell'
  | 'newGame'
  | 'toggleNotesMode'
  | 'undo'
  | 'setElapsedTime'
  | 'setTimerRunning'
  | 'loadSave'
> {
  const { puzzle, solution } = generatePuzzle(difficulty)
  const rawBoard = numbersToCells(puzzle)
  const invalidSet = getInvalidCells(rawBoard)
  const board = applyInvalid(rawBoard, invalidSet)

  const solutionBoard: Board = solution.map(row =>
    row.map((val): Cell => ({
      value: val,
      notes: [],
      fixed: true,
      invalid: false,
    }))
  )

  return {
    board,
    solution: solutionBoard,
    selectedCell: [0, 0],
    difficulty,
    notesMode: false,
    history: [],
    status: 'playing',
    elapsedTime: 0,
    timerRunning: false,
  }
}

export const useGameStore = create<GameState>((set) => ({
  ...buildInitialState('easy'),

  selectCell: (row, col) => {
    set({ selectedCell: [row, col] })
  },

  enterNumber: (num) =>
    set((state) => {
      const { board, selectedCell, notesMode, history } = state
      if (!selectedCell) return {}
      const [r, c] = selectedCell
      if (board[r][c].fixed) return {}

      if (notesMode) {
        // Push current board to history before notes edit (so notes are undoable)
        const newHistory = [...history, board].slice(-MAX_HISTORY)

        // Toggle the note
        const currentNotes = board[r][c].notes
        const newNotes = currentNotes.includes(num)
          ? currentNotes.filter(n => n !== num)
          : [...currentNotes, num].sort((a, b) => a - b)

        const newBoard = board.map((row, ri) =>
          row.map((cell, ci): Cell =>
            ri === r && ci === c ? { ...cell, notes: newNotes } : cell
          )
        )

        return { board: newBoard, history: newHistory, timerRunning: true }
      }

      // Push current board to history (cap at MAX_HISTORY)
      const newHistory = [...history, board].slice(-MAX_HISTORY)

      const updatedBoard = board.map((row, ri) =>
        row.map((cell, ci): Cell =>
          ri === r && ci === c
            ? { ...cell, value: num, notes: [] }
            : cell
        )
      )

      const invalidSet = getInvalidCells(updatedBoard)
      const validatedBoard = applyInvalid(updatedBoard, invalidSet)

      const won = isBoardComplete(validatedBoard)

      return {
        board: validatedBoard,
        history: newHistory,
        status: won ? 'won' : state.status,
        timerRunning: won ? false : true,
      }
    }),

  clearCell: () =>
    set((state) => {
      const { board, selectedCell, history } = state
      if (!selectedCell) return {}
      const [r, c] = selectedCell
      if (board[r][c].fixed) return {}

      const newHistory = [...history, board].slice(-MAX_HISTORY)

      const updatedBoard = board.map((row, ri) =>
        row.map((cell, ci): Cell =>
          ri === r && ci === c
            ? { ...cell, value: null, notes: [], invalid: false }
            : cell
        )
      )

      const invalidSet = getInvalidCells(updatedBoard)
      const validatedBoard = applyInvalid(updatedBoard, invalidSet)

      return {
        board: validatedBoard,
        history: newHistory,
      }
    }),

  newGame: (difficulty) => {
    safeLocalStorageRemove(SUDOKU_SAVE_KEY)
    set(buildInitialState(difficulty))
  },

  toggleNotesMode: () => {
    set(state => ({ notesMode: !state.notesMode }))
  },

  undo: () =>
    set((state) => {
      const { history } = state
      if (history.length === 0) return {}

      const previousBoard = history[history.length - 1]
      const newHistory = history.slice(0, -1)

      const invalidSet = getInvalidCells(previousBoard)
      const validatedBoard = applyInvalid(previousBoard, invalidSet)

      return {
        board: validatedBoard,
        history: newHistory,
        status: 'playing',
      }
    }),

  setElapsedTime: (t) => {
    set({ elapsedTime: t })
  },

  setTimerRunning: (running) => {
    set({ timerRunning: running })
  },

  loadSave: (saved: SavedState) => {
    set({
      board: saved.board,
      solution: saved.solution,
      difficulty: saved.difficulty,
      elapsedTime: saved.elapsedTime,
      notesMode: saved.notesMode,
      history: saved.history,
      status: 'playing',
      selectedCell: [0, 0],
      // Restore timer immediately if there was prior elapsed time, so the
      // game continues ticking after reload without requiring a move.
      timerRunning: saved.elapsedTime > 0,
    })
  },
}))
