/**
 * Sudoku solver using backtracking.
 * All inputs are 9x9 arrays of numbers where 0 represents an empty cell.
 */

function copyBoard(board: number[][]): number[][] {
  return board.map(row => [...row])
}

function isValid(board: number[][], row: number, col: number, num: number): boolean {
  // Check row
  for (let c = 0; c < 9; c++) {
    if (board[row][c] === num) return false
  }

  // Check column
  for (let r = 0; r < 9; r++) {
    if (board[r][col] === num) return false
  }

  // Check 3x3 box
  const boxRow = Math.floor(row / 3) * 3
  const boxCol = Math.floor(col / 3) * 3
  for (let r = boxRow; r < boxRow + 3; r++) {
    for (let c = boxCol; c < boxCol + 3; c++) {
      if (board[r][c] === num) return false
    }
  }

  return true
}

function findEmpty(board: number[][]): [number, number] | null {
  for (let r = 0; r < 9; r++) {
    for (let c = 0; c < 9; c++) {
      if (board[r][c] === 0) return [r, c]
    }
  }
  return null
}

/**
 * Solves a Sudoku board using backtracking.
 * Does NOT mutate the input board.
 * Returns the solved board, or null if no solution exists.
 */
export function solveSudoku(board: number[][]): number[][] | null {
  const working = copyBoard(board)

  function solve(): boolean {
    const empty = findEmpty(working)
    if (empty === null) return true // all cells filled — solved

    const [row, col] = empty

    for (let num = 1; num <= 9; num++) {
      if (isValid(working, row, col, num)) {
        working[row][col] = num
        if (solve()) return true
        working[row][col] = 0
      }
    }

    return false
  }

  return solve() ? working : null
}

/**
 * Counts the number of solutions for a board, up to `limit`.
 * Stops early once `limit` solutions are found.
 * Used by the generator to verify puzzle uniqueness.
 */
export function countSolutions(board: number[][], limit: number): number {
  const working = copyBoard(board)
  let count = 0

  function solve(): void {
    if (count >= limit) return

    const empty = findEmpty(working)
    if (empty === null) {
      count++
      return
    }

    const [row, col] = empty

    for (let num = 1; num <= 9; num++) {
      if (count >= limit) return
      if (isValid(working, row, col, num)) {
        working[row][col] = num
        solve()
        working[row][col] = 0
      }
    }
  }

  solve()
  return count
}
