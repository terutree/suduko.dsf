import { describe, it, expect } from 'vitest'
import { solveSudoku, countSolutions } from './solver'

// A well-known valid Sudoku puzzle (0 = empty)
const EASY_PUZZLE = [
  [5, 3, 0, 0, 7, 0, 0, 0, 0],
  [6, 0, 0, 1, 9, 5, 0, 0, 0],
  [0, 9, 8, 0, 0, 0, 0, 6, 0],
  [8, 0, 0, 0, 6, 0, 0, 0, 3],
  [4, 0, 0, 8, 0, 3, 0, 0, 1],
  [7, 0, 0, 0, 2, 0, 0, 0, 6],
  [0, 6, 0, 0, 0, 0, 2, 8, 0],
  [0, 0, 0, 4, 1, 9, 0, 0, 5],
  [0, 0, 0, 0, 8, 0, 0, 7, 9],
]

const EASY_SOLUTION = [
  [5, 3, 4, 6, 7, 8, 9, 1, 2],
  [6, 7, 2, 1, 9, 5, 3, 4, 8],
  [1, 9, 8, 3, 4, 2, 5, 6, 7],
  [8, 5, 9, 7, 6, 1, 4, 2, 3],
  [4, 2, 6, 8, 5, 3, 7, 9, 1],
  [7, 1, 3, 9, 2, 4, 8, 5, 6],
  [9, 6, 1, 5, 3, 7, 2, 8, 4],
  [2, 8, 7, 4, 1, 9, 6, 3, 5],
  [3, 4, 5, 2, 8, 6, 1, 7, 9],
]

// An unsolvable puzzle — two 5s in the same row
const UNSOLVABLE_PUZZLE = [
  [5, 5, 0, 0, 7, 0, 0, 0, 0],
  [6, 0, 0, 1, 9, 5, 0, 0, 0],
  [0, 9, 8, 0, 0, 0, 0, 6, 0],
  [8, 0, 0, 0, 6, 0, 0, 0, 3],
  [4, 0, 0, 8, 0, 3, 0, 0, 1],
  [7, 0, 0, 0, 2, 0, 0, 0, 6],
  [0, 6, 0, 0, 0, 0, 2, 8, 0],
  [0, 0, 0, 4, 1, 9, 0, 0, 5],
  [0, 0, 0, 0, 8, 0, 0, 7, 9],
]

describe('solveSudoku', () => {
  it('solves a known puzzle correctly', () => {
    const result = solveSudoku(EASY_PUZZLE)
    expect(result).not.toBeNull()
    expect(result).toEqual(EASY_SOLUTION)
  })

  it('returns null for an unsolvable puzzle', () => {
    const result = solveSudoku(UNSOLVABLE_PUZZLE)
    expect(result).toBeNull()
  })

  it('does not mutate the input board', () => {
    const copy = EASY_PUZZLE.map(row => [...row])
    solveSudoku(EASY_PUZZLE)
    expect(EASY_PUZZLE).toEqual(copy)
  })

  it('solves an empty board (returns some valid solution)', () => {
    const empty = Array.from({ length: 9 }, () => Array(9).fill(0))
    const result = solveSudoku(empty)
    expect(result).not.toBeNull()
    // Verify each row, column, and box contains 1-9 exactly once
    for (let r = 0; r < 9; r++) {
      const rowVals = result!.map(row => row[r]).sort()
      expect(rowVals).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9])
    }
  })

  it('returns a fully filled board (no zeros)', () => {
    const result = solveSudoku(EASY_PUZZLE)
    expect(result).not.toBeNull()
    for (const row of result!) {
      for (const val of row) {
        expect(val).toBeGreaterThan(0)
        expect(val).toBeLessThanOrEqual(9)
      }
    }
  })
})

describe('countSolutions', () => {
  it('returns 1 for a puzzle with a unique solution', () => {
    expect(countSolutions(EASY_PUZZLE, 2)).toBe(1)
  })

  it('returns 0 for an unsolvable puzzle', () => {
    expect(countSolutions(UNSOLVABLE_PUZZLE, 2)).toBe(0)
  })

  it('respects the limit — stops early', () => {
    const empty = Array.from({ length: 9 }, () => Array(9).fill(0))
    // An empty board has many solutions; with limit=2 we get 2
    const count = countSolutions(empty, 2)
    expect(count).toBe(2)
  })

  it('returns exactly 1 for the known solution board (already filled)', () => {
    expect(countSolutions(EASY_SOLUTION, 2)).toBe(1)
  })

  it('returns 2 for a board with exactly 2 solutions (limit=3)', () => {
    // Deadly pattern: rows 0 and 3, columns 3 and 4.
    //   EASY_SOLUTION[0][3]=6, EASY_SOLUTION[0][4]=7
    //   EASY_SOLUTION[3][3]=7, EASY_SOLUTION[3][4]=6
    // Swapping the two pairs produces a second valid completed board,
    // so emptying those four cells creates exactly 2 solutions.
    const twoSolBoard = EASY_SOLUTION.map(row => [...row])
    twoSolBoard[0][3] = 0
    twoSolBoard[0][4] = 0
    twoSolBoard[3][3] = 0
    twoSolBoard[3][4] = 0
    expect(countSolutions(twoSolBoard, 3)).toBe(2)
  })

  it('returns 2 for a board with exactly 2 solutions (limit=2)', () => {
    const twoSolBoard = EASY_SOLUTION.map(row => [...row])
    twoSolBoard[0][3] = 0
    twoSolBoard[0][4] = 0
    twoSolBoard[3][3] = 0
    twoSolBoard[3][4] = 0
    expect(countSolutions(twoSolBoard, 2)).toBe(2)
  })
})
