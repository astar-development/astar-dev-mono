import { createApp } from 'vue'
import App from './App.vue'
import './theme.css'
import { useTheme } from './composables/useTheme'

const { loadTheme } = useTheme()
loadTheme()

createApp(App).mount('#app')
