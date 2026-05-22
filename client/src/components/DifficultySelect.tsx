import type { Difficulty } from '../types'

interface DifficultySelectProps {
  value: Difficulty
  onChange: (d: Difficulty) => void
}

export function DifficultySelect({ value, onChange }: DifficultySelectProps) {
  return (
    <select
      value={value}
      onChange={e => onChange(e.target.value as Difficulty)}
      className="px-3 py-1.5 rounded border border-gray-300 bg-white text-gray-700 text-sm font-medium
                 focus:outline-none focus:ring-2 focus:ring-blue-400 cursor-pointer"
    >
      <option value="easy">Easy</option>
      <option value="medium">Medium</option>
      <option value="hard">Hard</option>
    </select>
  )
}
