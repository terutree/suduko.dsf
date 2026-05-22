export const SUDOKU_SAVE_KEY = 'sudoku-save'

export type Difficulty = 'easy' | 'medium' | 'hard'

export type Cell = {
  value: number | null    // 1-9 or null
  notes: number[]         // pencil marks
  fixed: boolean          // clue cell — immutable
  invalid: boolean        // conflicts with another cell
}

export type Board = Cell[][]  // 9x9

export type GameStatus = 'playing' | 'won'

export type SavedState = {
  board: Board
  solution: Board
  difficulty: Difficulty
  elapsedTime: number
  notesMode: boolean
  history: Board[]
}

export type GameState = {
  board: Board
  solution: Board          // the full solved board — for win detection
  selectedCell: [number, number] | null
  difficulty: Difficulty
  notesMode: boolean
  history: Board[]         // undo stack (max 50)
  status: GameStatus
  elapsedTime: number      // seconds
  timerRunning: boolean
  // actions
  selectCell: (row: number, col: number) => void
  enterNumber: (num: number) => void
  clearCell: () => void
  newGame: (difficulty: Difficulty) => void
  toggleNotesMode: () => void
  undo: () => void
  setElapsedTime: (t: number) => void
  setTimerRunning: (running: boolean) => void
  loadSave: (saved: SavedState) => void
}
