import type { Board } from '../types'

/**
 * Returns a Set of "row,col" strings for every cell that conflicts with
 * another cell in the same row, column, or 3x3 box.
 * Only non-null values are checked.
 */
export function getInvalidCells(board: Board): Set<string> {
  const invalid = new Set<string>()

  // Check rows
  for (let r = 0; r < 9; r++) {
    const seen = new Map<number, number>() // value → first col index seen
    for (let c = 0; c < 9; c++) {
      const val = board[r][c].value
      if (val === null) continue
      if (seen.has(val)) {
        invalid.add(`${r},${seen.get(val)}`)
        invalid.add(`${r},${c}`)
      } else {
        seen.set(val, c)
      }
    }
  }

  // Check columns
  for (let c = 0; c < 9; c++) {
    const seen = new Map<number, number>() // value → first row index seen
    for (let r = 0; r < 9; r++) {
      const val = board[r][c].value
      if (val === null) continue
      if (seen.has(val)) {
        invalid.add(`${seen.get(val)},${c}`)
        invalid.add(`${r},${c}`)
      } else {
        seen.set(val, r)
      }
    }
  }

  // Check 3x3 boxes
  for (let boxR = 0; boxR < 3; boxR++) {
    for (let boxC = 0; boxC < 3; boxC++) {
      const seen = new Map<number, string>() // value → "row,col" of first occurrence
      for (let r = boxR * 3; r < boxR * 3 + 3; r++) {
        for (let c = boxC * 3; c < boxC * 3 + 3; c++) {
          const val = board[r][c].value
          if (val === null) continue
          const key = `${r},${c}`
          if (seen.has(val)) {
            const prev = seen.get(val)
            if (prev !== undefined) {
              invalid.add(prev)
            }
            invalid.add(key)
          } else {
            seen.set(val, key)
          }
        }
      }
    }
  }

  return invalid
}

/**
 * Returns true when every cell has a non-null value and there are no conflicts.
 */
export function isBoardComplete(board: Board): boolean {
  for (let r = 0; r < 9; r++) {
    for (let c = 0; c < 9; c++) {
      if (board[r][c].value === null) return false
    }
  }
  return getInvalidCells(board).size === 0
}
