import { ref } from 'vue'

export function useTheme() {
    const isDark = ref(false)

    const initTheme = () => {
        // Verificamos preferencia previa en LocalStorage o preferencia del sistema
        const storedTheme = localStorage.getItem('theme-preference')
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches

        if (storedTheme === 'dark' || (!storedTheme && prefersDark)) {
            isDark.value = true
            document.documentElement.classList.add('dark')
        } else {
            isDark.value = false
            document.documentElement.classList.remove('dark')
        }
    }

    const toggleTheme = () => {
        isDark.value = !isDark.value
        if (isDark.value) {
            document.documentElement.classList.add('dark')
            localStorage.setItem('theme-preference', 'dark')
        } else {
            document.documentElement.classList.remove('dark')
            localStorage.setItem('theme-preference', 'light')
        }
    }

    initTheme()

    return { isDark, toggleTheme }
}
