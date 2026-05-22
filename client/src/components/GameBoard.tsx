import { useEffect, useCallback } from 'react'
import { useGameStore } from '../store/gameStore'
import { Cell } from './Cell'

export function GameBoard() {
  const board = useGameStore(s => s.board)
  const selectedCell = useGameStore(s => s.selectedCell)
  const selectCell = useGameStore(s => s.selectCell)
  const enterNumber = useGameStore(s => s.enterNumber)
  const clearCell = useGameStore(s => s.clearCell)
  const undo = useGameStore(s => s.undo)
  const status = useGameStore(s => s.status)

  // Keyboard handler on document so it works without any element being focused
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (status === 'won') return

    // Number input 1-9
    if (e.key >= '1' && e.key <= '9') {
      e.preventDefault()
      enterNumber(parseInt(e.key, 10))
      return
    }

    // Clear cell
    if (e.key === 'Backspace' || e.key === 'Delete') {
      e.preventDefault()
      clearCell()
      return
    }

    // Undo
    if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
      e.preventDefault()
      undo()
      return
    }

    // Arrow key navigation
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.key)) {
      e.preventDefault()
      if (!selectedCell) {
        selectCell(0, 0)
        return
      }
      const [r, c] = selectedCell
      switch (e.key) {
        case 'ArrowUp':    selectCell(Math.max(0, r - 1), c); break
        case 'ArrowDown':  selectCell(Math.min(8, r + 1), c); break
        case 'ArrowLeft':  selectCell(r, Math.max(0, c - 1)); break
        case 'ArrowRight': selectCell(r, Math.min(8, c + 1)); break
      }
    }
  }, [selectedCell, enterNumber, clearCell, undo, selectCell, status])

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])

  const [selRow, selCol] = selectedCell ?? [-1, -1]
  const selBoxRow = selRow >= 0 ? Math.floor(selRow / 3) : -1
  const selBoxCol = selCol >= 0 ? Math.floor(selCol / 3) : -1
  const selValue = selectedCell ? board[selRow][selCol].value : null

  return (
    <div
      role="grid"
      aria-label="Sudoku board"
      className="w-full max-w-[min(90vw,90vh,480px)] aspect-square
                 border-2 border-gray-700 rounded shadow-md bg-white
                 flex flex-col"
    >
      {board.map((row, r) => (
        <div key={r} role="row" className="flex flex-1">
          {row.map((cell, c) => {
            const isSelected = r === selRow && c === selCol
            const isHighlighted =
              !isSelected &&
              selectedCell !== null &&
              (r === selRow || c === selCol ||
                (Math.floor(r / 3) === selBoxRow && Math.floor(c / 3) === selBoxCol))
            const isSameValue =
              !isSelected &&
              selValue !== null &&
              cell.value === selValue

            return (
              <Cell
                key={`${r}-${c}`}
                cell={cell}
                row={r}
                col={c}
                isSelected={isSelected}
                isHighlighted={isHighlighted}
                isSameValue={isSameValue}
                onClick={() => selectCell(r, c)}
                onFocus={() => selectCell(r, c)}
              />
            )
          })}
        </div>
      ))}
    </div>
  )
}
