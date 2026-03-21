import { ref } from 'vue'

export type Theme = 'dark' | 'light' | 'metal' | 'polished'

const theme = ref<Theme>('dark')

export function useTheme() {
  function setTheme(newTheme: Theme) {
    theme.value = newTheme
    document.documentElement.className = `theme-${newTheme}`
    localStorage.setItem('theme', newTheme)
  }

  function loadTheme() {
    const saved = localStorage.getItem('theme') as Theme | null
    setTheme(saved ?? 'dark')
  }

  return {
    theme,
    setTheme,
    loadTheme,
  }
}
