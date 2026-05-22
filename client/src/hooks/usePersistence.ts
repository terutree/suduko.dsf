import { useEffect } from 'react'
import { useGameStore } from '../store/gameStore'
import { SUDOKU_SAVE_KEY } from '../types'
import type { SavedState } from '../types'

/**
 * Saves the current game state to LocalStorage on every change.
 * Removes the save when the game is won.
 * On mount, restores a saved game if one exists and status !== 'won'.
 */
export function usePersistence(): void {
  const board = useGameStore(s => s.board)
  const solution = useGameStore(s => s.solution)
  const elapsedTime = useGameStore(s => s.elapsedTime)
  const difficulty = useGameStore(s => s.difficulty)
  const status = useGameStore(s => s.status)
  const notesMode = useGameStore(s => s.notesMode)
  const history = useGameStore(s => s.history)
  const loadSave = useGameStore(s => s.loadSave)

  // Save on every relevant change
  useEffect(() => {
    if (status === 'won') {
      localStorage.removeItem(SUDOKU_SAVE_KEY)
      return
    }

    const saved: SavedState = {
      board,
      solution,
      difficulty,
      elapsedTime,
      notesMode,
      history,
    }

    try {
      localStorage.setItem(SUDOKU_SAVE_KEY, JSON.stringify(saved))
    } catch {
      // Silently ignore storage errors (e.g. private mode quota exceeded)
    }
  }, [board, solution, elapsedTime, difficulty, status, notesMode, history])

  // Load once on mount
  useEffect(() => {
    try {
      const raw = localStorage.getItem(SUDOKU_SAVE_KEY)
      if (!raw) return

      const saved: SavedState = JSON.parse(raw)

      // Basic shape validation
      if (
        !saved ||
        !Array.isArray(saved.board) ||
        !Array.isArray(saved.solution) ||
        typeof saved.elapsedTime !== 'number' ||
        !saved.difficulty
      ) {
        localStorage.removeItem(SUDOKU_SAVE_KEY)
        return
      }

      loadSave(saved)
    } catch {
      localStorage.removeItem(SUDOKU_SAVE_KEY)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])
}
