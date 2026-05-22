import type { Cell as CellType } from '../types'

interface CellProps {
  cell: CellType
  row: number
  col: number
  isSelected: boolean
  isHighlighted: boolean  // same row/col/box as selected
  isSameValue: boolean    // same numeric value as selected (and value != null)
  onClick: () => void
  onFocus?: () => void
}

export function Cell({ cell, row, col, isSelected, isHighlighted, isSameValue, onClick, onFocus }: CellProps) {
  // Border classes — thicker borders create the 3×3 box visual separation
  const borderRight = col === 2 || col === 5 ? 'border-r-2 border-r-gray-700' : 'border-r border-r-gray-300'
  const borderBottom = row === 2 || row === 5 ? 'border-b-2 border-b-gray-700' : 'border-b border-b-gray-300'
  const borderLeft = ''
  const borderTop = ''

  // Background priority: selected > invalid > same-value > highlighted > fixed > default
  let bgClass: string
  if (isSelected) {
    bgClass = 'bg-blue-300'
  } else if (cell.invalid) {
    bgClass = 'bg-red-100'
  } else if (isSameValue) {
    bgClass = 'bg-blue-100'
  } else if (isHighlighted) {
    bgClass = 'bg-blue-50'
  } else if (cell.fixed) {
    bgClass = 'bg-slate-50'
  } else {
    bgClass = 'bg-white'
  }

  // Text styling
  const textClass = cell.fixed
    ? 'font-bold text-gray-900'
    : cell.invalid
      ? 'font-semibold text-red-600'
      : 'font-medium text-blue-700'

  return (
    <div
      role="gridcell"
      aria-label={`Row ${row + 1}, Column ${col + 1}: ${cell.value !== null ? cell.value : 'empty cell'}${cell.fixed ? ', given' : ', editable'}`}
      aria-selected={isSelected}
      tabIndex={isSelected ? 0 : -1}
      onClick={onClick}
      onFocus={onFocus}
      className={`
        relative flex flex-1 items-center justify-center
        cursor-pointer select-none
        transition-colors duration-75
        ${bgClass}
        ${borderRight} ${borderBottom} ${borderLeft} ${borderTop}
      `}
    >
      {cell.value !== null ? (
        <span className={`text-lg sm:text-xl leading-none ${textClass}`}>
          {cell.value}
        </span>
      ) : cell.notes.length > 0 ? (
        <NotesGrid notes={cell.notes} />
      ) : null}
    </div>
  )
}

function NotesGrid({ notes }: { notes: number[] }) {
  return (
    <div className="grid grid-cols-3 grid-rows-3 w-full h-full p-0.5">
      {[1, 2, 3, 4, 5, 6, 7, 8, 9].map(n => (
        <span
          key={n}
          className="flex items-center justify-center text-[0.45rem] sm:text-[0.5rem] leading-none text-gray-500 font-medium"
        >
          {notes.includes(n) ? n : ''}
        </span>
      ))}
    </div>
  )
}
