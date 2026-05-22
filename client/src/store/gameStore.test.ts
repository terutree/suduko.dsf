import { describe, it, expect, beforeEach } from 'vitest'
import { useGameStore } from './gameStore'

// Reset the store to a fresh state before each test
beforeEach(() => {
  useGameStore.getState().newGame('easy')
})

describe('gameStore initialization', () => {
  it('initializes with a valid 9x9 board and status playing', () => {
    const { board, status } = useGameStore.getState()
    expect(status).toBe('playing')
    expect(board).toHaveLength(9)
    for (const row of board) {
      expect(row).toHaveLength(9)
    }
  })
})

describe('selectCell', () => {
  it('updates selectedCell', () => {
    const { selectCell } = useGameStore.getState()
    selectCell(3, 5)
    expect(useGameStore.getState().selectedCell).toEqual([3, 5])
  })
})

describe('enterNumber', () => {
  it('places a number in the selected non-fixed cell', () => {
    const state = useGameStore.getState()
    // Find a non-fixed cell
    let targetRow = -1
    let targetCol = -1
    outer: for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!state.board[r][c].fixed) {
          targetRow = r
          targetCol = c
          break outer
        }
      }
    }
    expect(targetRow).toBeGreaterThanOrEqual(0)

    state.selectCell(targetRow, targetCol)
    state.enterNumber(5)

    expect(useGameStore.getState().board[targetRow][targetCol].value).toBe(5)
  })
})

describe('clearCell', () => {
  it('sets selected cell value back to null', () => {
    const state = useGameStore.getState()
    // Find a non-fixed cell
    let targetRow = -1
    let targetCol = -1
    outer: for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!state.board[r][c].fixed) {
          targetRow = r
          targetCol = c
          break outer
        }
      }
    }
    expect(targetRow).toBeGreaterThanOrEqual(0)

    state.selectCell(targetRow, targetCol)
    state.enterNumber(7)
    expect(useGameStore.getState().board[targetRow][targetCol].value).toBe(7)

    useGameStore.getState().clearCell()
    expect(useGameStore.getState().board[targetRow][targetCol].value).toBeNull()
  })
})

describe('undo', () => {
  it('reverts the last move', () => {
    const state = useGameStore.getState()
    // Find a non-fixed cell
    let targetRow = -1
    let targetCol = -1
    outer: for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!state.board[r][c].fixed) {
          targetRow = r
          targetCol = c
          break outer
        }
      }
    }
    expect(targetRow).toBeGreaterThanOrEqual(0)

    state.selectCell(targetRow, targetCol)
    state.enterNumber(5)
    expect(useGameStore.getState().board[targetRow][targetCol].value).toBe(5)

    useGameStore.getState().undo()
    expect(useGameStore.getState().board[targetRow][targetCol].value).toBeNull()
  })
})

describe('newGame', () => {
  it('resets the board to a new medium puzzle', () => {
    useGameStore.getState().newGame('medium')

    const newState = useGameStore.getState()
    expect(newState.difficulty).toBe('medium')
    expect(newState.status).toBe('playing')
    expect(newState.history).toHaveLength(0)
    expect(newState.board).toHaveLength(9)
    // The board should be different from (or equal to, but logically fresh) the old easy board
    // What we can guarantee: it is a valid 9x9 board with some fixed cells
    const fixedCount = newState.board.flat().filter(c => c.fixed).length
    expect(fixedCount).toBeGreaterThan(0)
    // Difficulty clue range for medium: 32-35
    expect(fixedCount).toBeGreaterThanOrEqual(32)
    expect(fixedCount).toBeLessThanOrEqual(35)
  })
})

describe('win detection', () => {
  it('sets status to won when the board matches the solution', () => {
    const state = useGameStore.getState()
    const { solution, board } = state

    // Enter the solution value for every non-fixed (empty) cell
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!board[r][c].fixed) {
          useGameStore.getState().selectCell(r, c)
          const solVal = solution[r][c].value
          if (solVal !== null) {
            useGameStore.getState().enterNumber(solVal)
          }
        }
      }
    }

    expect(useGameStore.getState().status).toBe('won')
  })
})

describe('history cap', () => {
  it('caps history at 50 entries after 60 number entries', () => {
    const state = useGameStore.getState()

    // Collect all non-fixed cells
    const emptyCells: [number, number][] = []
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!state.board[r][c].fixed) {
          emptyCells.push([r, c])
        }
      }
    }

    // We need at least 60 empty cells to test the cap (easy has ~36-45 clues, so 36-45 empty)
    // If fewer than 60 available, alternate values on the same cells using clearCell
    let moveCount = 0
    let cellIndex = 0
    while (moveCount < 60 && cellIndex < emptyCells.length) {
      const [r, c] = emptyCells[cellIndex % emptyCells.length]
      useGameStore.getState().selectCell(r, c)
      // Alternate between 1 and 2 to avoid win condition prematurely
      useGameStore.getState().enterNumber((moveCount % 8) + 1)
      moveCount++
      cellIndex++
      if (cellIndex >= emptyCells.length) {
        // Wrap: clear cells to make room for more moves
        for (const [cr, cc] of emptyCells) {
          useGameStore.getState().selectCell(cr, cc)
          useGameStore.getState().clearCell()
        }
        cellIndex = 0
      }
      // Stop if game is won (shouldn't happen with random values but guard anyway)
      if (useGameStore.getState().status === 'won') break
    }

    expect(useGameStore.getState().history.length).toBeLessThanOrEqual(50)
  })
})

describe('toggleNotesMode', () => {
  it('flips notesMode', () => {
    expect(useGameStore.getState().notesMode).toBe(false)
    useGameStore.getState().toggleNotesMode()
    expect(useGameStore.getState().notesMode).toBe(true)
    useGameStore.getState().toggleNotesMode()
    expect(useGameStore.getState().notesMode).toBe(false)
  })
})
