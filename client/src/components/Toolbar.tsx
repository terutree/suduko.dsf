import { useGameStore } from '../store/gameStore'
import { DifficultySelect } from './DifficultySelect'
import type { Difficulty } from '../types'

export function Toolbar() {
  const difficulty = useGameStore(s => s.difficulty)
  const notesMode = useGameStore(s => s.notesMode)
  const newGame = useGameStore(s => s.newGame)
  const toggleNotesMode = useGameStore(s => s.toggleNotesMode)
  const undo = useGameStore(s => s.undo)

  const handleDifficultyChange = (d: Difficulty) => {
    newGame(d)
  }

  return (
    <div className="flex items-center gap-3 flex-wrap justify-center">
      <DifficultySelect value={difficulty} onChange={handleDifficultyChange} />

      <button
        onClick={() => newGame(difficulty)}
        className="px-4 py-1.5 rounded bg-blue-500 hover:bg-blue-600 active:bg-blue-700
                   text-white text-sm font-semibold transition-colors"
      >
        New Game
      </button>

      <button
        onClick={toggleNotesMode}
        className={`px-4 py-1.5 rounded border text-sm font-medium transition-colors
          ${notesMode
            ? 'bg-amber-100 border-amber-400 text-amber-800'
            : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50'
          }`}
      >
        Notes {notesMode ? 'ON' : 'OFF'}
      </button>

      <button
        onClick={undo}
        className="px-4 py-1.5 rounded border border-gray-300 bg-white hover:bg-gray-50
                   text-gray-700 text-sm font-medium transition-colors"
      >
        Undo
      </button>
    </div>
  )
}
