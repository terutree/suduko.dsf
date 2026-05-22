import { GameBoard } from './components/GameBoard'
import { Toolbar } from './components/Toolbar'
import { NumberPad } from './components/NumberPad'
import { WinModal } from './components/WinModal'
import { Timer } from './components/Timer'
import { useTimer } from './hooks/useTimer'
import { usePersistence } from './hooks/usePersistence'

export default function App() {
  useTimer()
  usePersistence()

  return (
    <div className="min-h-screen bg-gray-100 flex flex-col items-center justify-start sm:justify-center p-4 gap-4 sm:gap-5">
      <h1 className="text-2xl sm:text-3xl font-bold text-gray-800 tracking-tight mt-2 sm:mt-0">Sudoku</h1>

      <div className="w-full max-w-lg mx-auto flex items-center gap-2 flex-wrap justify-center">
        <Toolbar />
        <Timer />
      </div>

      <div className="w-full max-w-lg mx-auto px-0 sm:px-4">
        <GameBoard />
      </div>

      <div className="w-full max-w-lg mx-auto px-4">
        <NumberPad />
      </div>

      <WinModal />
    </div>
  )
}
