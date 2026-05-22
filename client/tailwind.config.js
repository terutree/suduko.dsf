/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        sudoku: {
          cell: 'var(--cell-bg)',
          selected: 'var(--cell-selected)',
          highlight: 'var(--cell-highlight)',
          fixed: 'var(--cell-fixed)',
          invalid: 'var(--cell-invalid)',
        },
      },
      gridTemplateColumns: {
        '9': 'repeat(9, minmax(0, 1fr))',
      },
      gridTemplateRows: {
        '9': 'repeat(9, minmax(0, 1fr))',
      },
    },
  },
  plugins: [],
}
