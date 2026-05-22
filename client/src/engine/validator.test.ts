import { describe, it, expect } from 'vitest'
import { getInvalidCells, isBoardComplete } from './validator'
import type { Board, Cell } from '../types'

function makeCell(value: number | null, fixed = false): Cell {
  return { value, notes: [], fixed, invalid: false }
}

function makeBoard(values: (number | null)[][]): Board {
  return values.map(row => row.map(v => makeCell(v))) as Board
}

function emptyBoard(): Board {
  return makeBoard(Array.from({ length: 9 }, () => Array(9).fill(null)))
}

function solvedBoard(): Board {
  return makeBoard([
    [5, 3, 4, 6, 7, 8, 9, 1, 2],
    [6, 7, 2, 1, 9, 5, 3, 4, 8],
    [1, 9, 8, 3, 4, 2, 5, 6, 7],
    [8, 5, 9, 7, 6, 1, 4, 2, 3],
    [4, 2, 6, 8, 5, 3, 7, 9, 1],
    [7, 1, 3, 9, 2, 4, 8, 5, 6],
    [9, 6, 1, 5, 3, 7, 2, 8, 4],
    [2, 8, 7, 4, 1, 9, 6, 3, 5],
    [3, 4, 5, 2, 8, 6, 1, 7, 9],
  ])
}

describe('getInvalidCells', () => {
  it('returns empty set for a board with no conflicts', () => {
    const board = solvedBoard()
    expect(getInvalidCells(board).size).toBe(0)
  })

  it('returns empty set for an empty board', () => {
    expect(getInvalidCells(emptyBoard()).size).toBe(0)
  })

  it('detects a row conflict', () => {
    const values = Array.from({ length: 9 }, () => Array(9).fill(null) as (number | null)[])
    values[0][0] = 5
    values[0][4] = 5 // duplicate 5 in row 0
    const board = makeBoard(values)
    const invalid = getInvalidCells(board)
    expect(invalid.has('0,0')).toBe(true)
    expect(invalid.has('0,4')).toBe(true)
    expect(invalid.size).toBe(2)
  })

  it('detects a column conflict', () => {
    const values = Array.from({ length: 9 }, () => Array(9).fill(null) as (number | null)[])
    values[0][3] = 7
    values[6][3] = 7 // duplicate 7 in column 3
    const board = makeBoard(values)
    const invalid = getInvalidCells(board)
    expect(invalid.has('0,3')).toBe(true)
    expect(invalid.has('6,3')).toBe(true)
    expect(invalid.size).toBe(2)
  })

  it('detects a 3x3 box conflict', () => {
    const values = Array.from({ length: 9 }, () => Array(9).fill(null) as (number | null)[])
    values[0][0] = 3
    values[2][2] = 3 // both in top-left box
    const board = makeBoard(values)
    const invalid = getInvalidCells(board)
    expect(invalid.has('0,0')).toBe(true)
    expect(invalid.has('2,2')).toBe(true)
    expect(invalid.size).toBe(2)
  })

  it('marks all duplicates when three same values appear in a row', () => {
    const values = Array.from({ length: 9 }, () => Array(9).fill(null) as (number | null)[])
    values[1][0] = 4
    values[1][4] = 4
    values[1][8] = 4
    const board = makeBoard(values)
    const invalid = getInvalidCells(board)
    expect(invalid.has('1,0')).toBe(true)
    expect(invalid.has('1,4')).toBe(true)
    expect(invalid.has('1,8')).toBe(true)
  })

  it('does not flag distinct values in same row', () => {
    const values = Array.from({ length: 9 }, () => Array(9).fill(null) as (number | null)[])
    values[0][0] = 1
    values[0][1] = 2
    values[0][2] = 3
    const board = makeBoard(values)
    expect(getInvalidCells(board).size).toBe(0)
  })
})

describe('isBoardComplete', () => {
  it('returns true for a correctly solved board', () => {
    expect(isBoardComplete(solvedBoard())).toBe(true)
  })

  it('returns false for an empty board', () => {
    expect(isBoardComplete(emptyBoard())).toBe(false)
  })

  it('returns false when all cells are filled but there are conflicts', () => {
    const values = [
      [5, 3, 4, 6, 7, 8, 9, 1, 2],
      [6, 7, 2, 1, 9, 5, 3, 4, 8],
      [1, 9, 8, 3, 4, 2, 5, 6, 7],
      [8, 5, 9, 7, 6, 1, 4, 2, 3],
      [4, 2, 6, 8, 5, 3, 7, 9, 1],
      [7, 1, 3, 9, 2, 4, 8, 5, 6],
      [9, 6, 1, 5, 3, 7, 2, 8, 4],
      [2, 8, 7, 4, 1, 9, 6, 3, 5],
      [3, 4, 5, 2, 8, 6, 1, 5, 9], // last row: two 5s
    ] as (number | null)[][]
    expect(isBoardComplete(makeBoard(values))).toBe(false)
  })

  it('returns false when one cell is empty', () => {
    const values = [
      [5, 3, 4, 6, 7, 8, 9, 1, 2],
      [6, 7, 2, 1, 9, 5, 3, 4, 8],
      [1, 9, 8, 3, 4, 2, 5, 6, 7],
      [8, 5, 9, 7, 6, 1, 4, 2, 3],
      [4, 2, 6, 8, 5, 3, 7, 9, 1],
      [7, 1, 3, 9, 2, 4, 8, 5, 6],
      [9, 6, 1, 5, 3, 7, 2, 8, 4],
      [2, 8, 7, 4, 1, 9, 6, 3, 5],
      [3, 4, 5, 2, 8, 6, 1, 7, null], // one null
    ] as (number | null)[][]
    expect(isBoardComplete(makeBoard(values))).toBe(false)
  })
})
