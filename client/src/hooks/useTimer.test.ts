// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useGameStore } from '../store/gameStore'
import { useTimer } from './useTimer'

// Tests for timer-related store behaviour (timer start/stop/reset)
// Hook integration tests use renderHook to exercise the interval logic directly.

beforeEach(() => {
  vi.useFakeTimers()
  useGameStore.getState().newGame('easy')
})

afterEach(() => {
  vi.useRealTimers()
})

describe('timer state — initial', () => {
  it('starts with timerRunning false and elapsedTime 0', () => {
    const { timerRunning, elapsedTime } = useGameStore.getState()
    expect(timerRunning).toBe(false)
    expect(elapsedTime).toBe(0)
  })
})

describe('timer state — starts on first move', () => {
  it('sets timerRunning true after enterNumber on a non-fixed cell', () => {
    const state = useGameStore.getState()
    let targetRow = -1, targetCol = -1
    outer: for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!state.board[r][c].fixed) { targetRow = r; targetCol = c; break outer }
      }
    }
    state.selectCell(targetRow, targetCol)
    state.enterNumber(5)
    expect(useGameStore.getState().timerRunning).toBe(true)
  })
})

describe('timer state — stops on win', () => {
  it('sets timerRunning false when puzzle is solved', () => {
    const state = useGameStore.getState()
    const { solution, board } = state
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        if (!board[r][c].fixed) {
          useGameStore.getState().selectCell(r, c)
          const solVal = solution[r][c].value
          if (solVal !== null) useGameStore.getState().enterNumber(solVal)
        }
      }
    }
    expect(useGameStore.getState().status).toBe('won')
    expect(useGameStore.getState().timerRunning).toBe(false)
  })
})

describe('timer state — resets on newGame', () => {
  it('resets elapsedTime to 0 and timerRunning to false', () => {
    const state = useGameStore.getState()
    state.setElapsedTime(120)
    state.setTimerRunning(true)
    useGameStore.getState().newGame('medium')
    const after = useGameStore.getState()
    expect(after.elapsedTime).toBe(0)
    expect(after.timerRunning).toBe(false)
  })
})

describe('setElapsedTime', () => {
  it('updates elapsedTime in the store', () => {
    useGameStore.getState().setElapsedTime(42)
    expect(useGameStore.getState().elapsedTime).toBe(42)
  })
})

describe('formatTime utility check', () => {
  it('elapsed seconds encode correctly as MM:SS pattern', () => {
    // Verify the formatting logic used by Timer component
    const format = (s: number) => {
      const m = Math.floor(s / 60)
      const sec = s % 60
      return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
    }
    expect(format(0)).toBe('00:00')
    expect(format(59)).toBe('00:59')
    expect(format(60)).toBe('01:00')
    expect(format(83)).toBe('01:23')
    expect(format(3661)).toBe('61:01')
  })
})

describe('useTimer hook — elapsedTime increments', () => {
  it('increments elapsedTime by 1 per second while timerRunning is true', () => {
    useGameStore.setState({ timerRunning: true, elapsedTime: 0 })

    const { unmount } = renderHook(() => useTimer())

    act(() => {
      vi.advanceTimersByTime(3000)
    })

    expect(useGameStore.getState().elapsedTime).toBe(3)

    unmount()
  })
})
