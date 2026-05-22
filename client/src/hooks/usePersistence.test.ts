// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useGameStore } from '../store/gameStore'
import { usePersistence } from './usePersistence'
import { SUDOKU_SAVE_KEY } from '../types'
import type { SavedState } from '../types'

// Tests for persistence-related store behaviour (loadSave action) and
// the usePersistence hook's localStorage side-effects.

// localStorage mock
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: vi.fn((key: string): string | null => store[key] ?? null),
    setItem: vi.fn((key: string, val: string) => { store[key] = val }),
    removeItem: vi.fn((key: string) => { delete store[key] }),
    clear: () => { store = {} },
  }
})()
Object.defineProperty(globalThis, 'localStorage', { value: localStorageMock, writable: true })

beforeEach(() => {
  localStorageMock.clear()
  vi.clearAllMocks()
  useGameStore.getState().newGame('easy')
})

describe('loadSave', () => {
  it('restores board, difficulty, elapsedTime, notesMode and history', () => {
    const state = useGameStore.getState()

    // Capture current board/solution as a synthetic save
    const saved: SavedState = {
      board: state.board,
      solution: state.solution,
      difficulty: 'hard',
      elapsedTime: 77,
      notesMode: true,
      history: [],
    }

    useGameStore.getState().loadSave(saved)

    const after = useGameStore.getState()
    expect(after.difficulty).toBe('hard')
    expect(after.elapsedTime).toBe(77)
    expect(after.notesMode).toBe(true)
    expect(after.status).toBe('playing')
    expect(after.selectedCell).toEqual([0, 0])
  })

  it('sets timerRunning true when elapsedTime > 0', () => {
    const state = useGameStore.getState()
    const saved: SavedState = {
      board: state.board,
      solution: state.solution,
      difficulty: 'easy',
      elapsedTime: 30,
      notesMode: false,
      history: [],
    }
    useGameStore.getState().loadSave(saved)
    expect(useGameStore.getState().timerRunning).toBe(true)
  })

  it('sets timerRunning false when elapsedTime is 0', () => {
    const state = useGameStore.getState()
    const saved: SavedState = {
      board: state.board,
      solution: state.solution,
      difficulty: 'easy',
      elapsedTime: 0,
      notesMode: false,
      history: [],
    }
    useGameStore.getState().loadSave(saved)
    expect(useGameStore.getState().timerRunning).toBe(false)
  })
})

describe('usePersistence hook — localStorage integration', () => {
  it('saves board to localStorage on move', async () => {
    const { unmount } = renderHook(() => usePersistence())

    const state = useGameStore.getState()
    let targetRow = -1, targetCol = -1
    outer: for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!state.board[r][c].fixed) { targetRow = r; targetCol = c; break outer }
      }
    }
    state.selectCell(targetRow, targetCol)

    act(() => {
      useGameStore.getState().enterNumber(5)
    })

    await waitFor(() => {
      expect(localStorageMock.setItem).toHaveBeenCalledWith(
        SUDOKU_SAVE_KEY,
        expect.any(String),
      )
    })

    unmount()
  })

  it('newGame clears localStorage', async () => {
    const { unmount } = renderHook(() => usePersistence())

    act(() => {
      useGameStore.getState().newGame('easy')
    })

    await waitFor(() => {
      expect(localStorageMock.removeItem).toHaveBeenCalledWith(SUDOKU_SAVE_KEY)
    })

    unmount()
  })

  it('restores saved state on mount', () => {
    const baseState = useGameStore.getState()
    const saved: SavedState = {
      board: baseState.board,
      solution: baseState.solution,
      difficulty: 'hard',
      elapsedTime: 55,
      notesMode: false,
      history: [],
    }
    localStorageMock.getItem.mockImplementation((key: string): string | null =>
      key === SUDOKU_SAVE_KEY ? JSON.stringify(saved) : null
    )

    renderHook(() => usePersistence())

    expect(useGameStore.getState().difficulty).toBe('hard')
  })
})
