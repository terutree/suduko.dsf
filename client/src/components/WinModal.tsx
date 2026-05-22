import { useEffect } from 'react'
import { useGameStore } from '../store/gameStore'

export function WinModal() {
  const status = useGameStore(s => s.status)
  const difficulty = useGameStore(s => s.difficulty)
  const newGame = useGameStore(s => s.newGame)

  useEffect(() => {
    if (status !== 'won') return
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        newGame(difficulty)
      }
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [status, difficulty, newGame])

  if (status !== 'won') return null

  return (
    <div
      className="fixed inset-0 flex items-center justify-center z-50"
      role="dialog"
      aria-modal="true"
      aria-label="You won!"
      aria-describedby="win-modal-description"
    >
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" />

      {/* Modal card */}
      <div className="relative bg-white rounded-2xl shadow-2xl px-10 py-8 flex flex-col items-center gap-5 max-w-sm w-full mx-4">
        <div className="text-5xl" role="img" aria-label="trophy">🏆</div>
        <h2 className="text-2xl font-bold text-gray-900 text-center">
          Congratulations!
        </h2>
        <p id="win-modal-description" className="text-gray-600 text-center text-sm">
          You solved the {difficulty} puzzle!
        </p>

        <button
          autoFocus
          onClick={() => newGame(difficulty)}
          className="mt-2 px-8 py-2.5 rounded-lg bg-blue-500 hover:bg-blue-600 active:bg-blue-700
                     text-white font-semibold text-base transition-colors"
        >
          New Game
        </button>
      </div>
    </div>
  )
}
