import type { Difficulty } from '../types'
import { countSolutions } from './solver'

/** Target clue counts per difficulty (inclusive ranges) */
const CLUE_RANGES: Record<Difficulty, { min: number; max: number }> = {
  easy:   { min: 36, max: 45 },
  medium: { min: 32, max: 35 },
  hard:   { min: 28, max: 31 },
}

function shuffle<T>(array: T[]): T[] {
  const arr = [...array]
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[arr[i], arr[j]] = [arr[j], arr[i]]
  }
  return arr
}

/**
 * Generates a random fully-solved board by solving an empty board
 * with a randomised digit order.
 */
function generateSolvedBoard(): number[][] {
  const board: number[][] = Array.from({ length: 9 }, () => Array(9).fill(0))

  function solve(board: number[][]): boolean {
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (board[r][c] !== 0) continue

        const digits = shuffle([1, 2, 3, 4, 5, 6, 7, 8, 9])

        for (const num of digits) {
          if (isValidPlacement(board, r, c, num)) {
            board[r][c] = num
            if (solve(board)) return true
            board[r][c] = 0
          }
        }

        return false
      }
    }
    return true // no empty cell found — solved
  }

  solve(board)
  return board
}

function isValidPlacement(board: number[][], row: number, col: number, num: number): boolean {
  for (let c = 0; c < 9; c++) {
    if (board[row][c] === num) return false
  }
  for (let r = 0; r < 9; r++) {
    if (board[r][col] === num) return false
  }
  const br = Math.floor(row / 3) * 3
  const bc = Math.floor(col / 3) * 3
  for (let r = br; r < br + 3; r++) {
    for (let c = bc; c < bc + 3; c++) {
      if (board[r][c] === num) return false
    }
  }
  return true
}

/**
 * Generates a Sudoku puzzle for the given difficulty.
 * Returns the puzzle (0 = empty) and the full solution.
 */
export function generatePuzzle(difficulty: Difficulty): {
  puzzle: number[][]
  solution: number[][]
} {
  const solution = generateSolvedBoard()
  const { min: targetMin, max: targetMax } = CLUE_RANGES[difficulty]

  // Pick a target clue count in the difficulty range
  const targetClues = targetMin + Math.floor(Math.random() * (targetMax - targetMin + 1))

  // Build list of all 81 positions and shuffle them
  const positions = shuffle(
    Array.from({ length: 81 }, (_, i) => [Math.floor(i / 9), i % 9] as [number, number])
  )

  const puzzle: number[][] = solution.map(row => [...row])
  let filledCount = 81

  for (const [r, c] of positions) {
    if (filledCount <= targetClues) break

    const saved = puzzle[r][c]
    puzzle[r][c] = 0
    filledCount--

    // Verify the puzzle still has exactly one solution
    if (countSolutions(puzzle, 2) !== 1) {
      // Restore and move on
      puzzle[r][c] = saved
      filledCount++
    }
  }

  // If we couldn't reach the minimum (uniqueness constraint), that's fine —
  // just return what we have (it will always have a unique solution).
  return { puzzle, solution }
}
