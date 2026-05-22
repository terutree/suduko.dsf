import { describe, it, expect } from 'vitest'
import { generatePuzzle } from './generator'
import { countSolutions } from './solver'

type Difficulty = 'easy' | 'medium' | 'hard'

const CLUE_RANGES: Record<Difficulty, { min: number; max: number }> = {
  easy:   { min: 36, max: 45 },
  medium: { min: 32, max: 35 },
  hard:   { min: 28, max: 31 },
}

function countClues(puzzle: number[][]): number {
  return puzzle.flat().filter(v => v !== 0).length
}

function isValidSudoku(board: number[][]): boolean {
  // Check rows
  for (let r = 0; r < 9; r++) {
    const vals = board[r].filter(v => v !== 0)
    if (new Set(vals).size !== vals.length) return false
  }
  // Check columns
  for (let c = 0; c < 9; c++) {
    const vals = board.map(row => row[c]).filter(v => v !== 0)
    if (new Set(vals).size !== vals.length) return false
  }
  // Check boxes
  for (let br = 0; br < 3; br++) {
    for (let bc = 0; bc < 3; bc++) {
      const vals: number[] = []
      for (let r = br * 3; r < br * 3 + 3; r++) {
        for (let c = bc * 3; c < bc * 3 + 3; c++) {
          const v = board[r][c]
          if (v !== 0) vals.push(v)
        }
      }
      if (new Set(vals).size !== vals.length) return false
    }
  }
  return true
}

describe('generatePuzzle', () => {
  const difficulties: Difficulty[] = ['easy', 'medium', 'hard']

  for (const diff of difficulties) {
    it(`generates an ${diff} puzzle with correct clue count`, () => {
      const { puzzle } = generatePuzzle(diff)
      const clues = countClues(puzzle)
      const { min, max } = CLUE_RANGES[diff]
      expect(clues).toBeGreaterThanOrEqual(min)
      expect(clues).toBeLessThanOrEqual(max)
    })
  }

  it('generates a puzzle with a unique solution', () => {
    const { puzzle } = generatePuzzle('easy')
    const solutions = countSolutions(puzzle, 2)
    expect(solutions).toBe(1)
  })

  it('generates a valid solution (no row/col/box conflicts)', () => {
    const { solution } = generatePuzzle('easy')
    expect(isValidSudoku(solution)).toBe(true)
  })

  it('solution is fully filled (no zeros)', () => {
    const { solution } = generatePuzzle('medium')
    for (const row of solution) {
      for (const val of row) {
        expect(val).toBeGreaterThan(0)
        expect(val).toBeLessThanOrEqual(9)
      }
    }
  })

  it('puzzle cells match solution at non-empty positions', () => {
    const { puzzle, solution } = generatePuzzle('hard')
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (puzzle[r][c] !== 0) {
          expect(puzzle[r][c]).toBe(solution[r][c])
        }
      }
    }
  })

  it('puzzle has at least one empty cell', () => {
    const { puzzle } = generatePuzzle('hard')
    const empty = puzzle.flat().filter(v => v === 0).length
    expect(empty).toBeGreaterThan(0)
  })
})
