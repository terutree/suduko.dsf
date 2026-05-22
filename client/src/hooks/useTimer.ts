import { useEffect } from 'react'
import { useGameStore } from '../store/gameStore'

/**
 * Reads timerRunning from the store.
 * While running, increments elapsedTime by 1 every second.
 * Cleans up on unmount or when timerRunning becomes false.
 */
export function useTimer(): void {
  const timerRunning = useGameStore(s => s.timerRunning)
  const setElapsedTime = useGameStore(s => s.setElapsedTime)

  useEffect(() => {
    if (!timerRunning) return

    const id = setInterval(() => {
      setElapsedTime(useGameStore.getState().elapsedTime + 1)
    }, 1000)

    return () => clearInterval(id)
  }, [timerRunning, setElapsedTime])
}
