import { useGameStore } from '../store/gameStore'

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}

export function Timer() {
  const elapsedTime = useGameStore(s => s.elapsedTime)

  return (
    <span
      className="font-mono text-lg font-semibold text-gray-700 tabular-nums"
      aria-label={`Elapsed time: ${formatTime(elapsedTime)}`}
    >
      {formatTime(elapsedTime)}
    </span>
  )
}
