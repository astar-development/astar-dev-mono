<script setup lang="ts">
import { useTheme } from '../composables/useTheme'

const { theme, setTheme } = useTheme()

const themes = [
  { value: 'dark', label: 'Traditional Dark' },
  { value: 'light', label: 'Traditional Light' },
  { value: 'metal', label: 'Volume = 11' },
  { value: 'polished', label: 'Polished' },
]
</script>

<template>
  <select v-model="theme" @change="setTheme(theme)" class="theme-select">
    <option v-for="t in themes" :key="t.value" :value="t.value">
      {{ t.label }}
    </option>
  </select>
</template>

<style scoped>
.theme-select {
  padding: 6px 10px;
  border-radius: 6px;
  border: 1px solid var(--panel-border);
  background: var(--panel-bg);
  color: var(--text);
}
</style>
