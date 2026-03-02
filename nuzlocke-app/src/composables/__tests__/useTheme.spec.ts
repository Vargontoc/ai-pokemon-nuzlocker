import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useTheme } from '../useTheme'

describe('useTheme composable', () => {
    beforeEach(() => {
        localStorage.clear()
        document.documentElement.className = ''
        vi.stubGlobal('matchMedia', vi.fn().mockImplementation(query => ({
            matches: false,
            media: query,
            onchange: null,
            addListener: vi.fn(),
            removeListener: vi.fn(),
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            dispatchEvent: vi.fn(),
        })))
    })

    it('initializes with dark theme if localStorage specifies dark', () => {
        localStorage.setItem('theme-preference', 'dark')
        // Necesitamos aislar la ejecucion, we run the setup logic manually
        const { isDark } = useTheme()
        expect(isDark.value).toBe(true)
        expect(document.documentElement.classList.contains('dark')).toBe(true)
    })

    it('toggles theme successfully', () => {
        const { isDark, toggleTheme } = useTheme()

        expect(isDark.value).toBe(false)

        toggleTheme()

        expect(isDark.value).toBe(true)
        expect(document.documentElement.classList.contains('dark')).toBe(true)
        expect(localStorage.getItem('theme-preference')).toBe('dark')

        toggleTheme()

        expect(isDark.value).toBe(false)
        expect(document.documentElement.classList.contains('dark')).toBe(false)
        expect(localStorage.getItem('theme-preference')).toBe('light')
    })
})
