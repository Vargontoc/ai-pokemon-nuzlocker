/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      animation: {
        'neon-pulse': 'neon-pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
      keyframes: {
        'neon-pulse': {
          '0%, 100%': {
            boxShadow: '0 0 5px theme("colors.blue.400"), 0 0 20px theme("colors.blue.600")'
          },
          '50%': {
            boxShadow: '0 0 10px theme("colors.blue.400"), 0 0 30px theme("colors.blue.500")'
          },
        }
      }
    },
  },
  plugins: [],
}

