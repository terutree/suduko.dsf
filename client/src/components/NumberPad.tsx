import { useGameStore } from '../store/gameStore'

export function NumberPad() {
  const enterNumber = useGameStore(s => s.enterNumber)
  const clearCell = useGameStore(s => s.clearCell)
  const status = useGameStore(s => s.status)

  const disabled = status === 'won'

  return (
    <div className="flex gap-2 flex-wrap justify-center" role="group" aria-label="Number pad">
      {[1, 2, 3, 4, 5, 6, 7, 8, 9].map(n => (
        <button
          key={n}
          onClick={() => enterNumber(n)}
          disabled={disabled}
          aria-label={`Enter ${n}`}
          className="w-10 h-10 sm:w-11 sm:h-11 rounded bg-white border border-gray-300
                     text-gray-800 font-semibold text-base
                     hover:bg-blue-50 hover:border-blue-400 active:bg-blue-100
                     disabled:opacity-40 disabled:cursor-not-allowed
                     transition-colors"
        >
          {n}
        </button>
      ))}
      <button
        onClick={clearCell}
        disabled={disabled}
        aria-label="Erase cell"
        className="w-10 h-10 sm:w-11 sm:h-11 rounded bg-white border border-gray-300
                   text-gray-600 font-medium text-sm
                   hover:bg-red-50 hover:border-red-400 active:bg-red-100
                   disabled:opacity-40 disabled:cursor-not-allowed
                   transition-colors"
      >
        ✕
      </button>
    </div>
  )
}
