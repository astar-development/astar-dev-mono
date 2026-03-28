<script setup lang="ts">
import { ref, onMounted } from 'vue';

type Theme = 'dark' | 'light' | 'metal' | 'polished';

const VALID_THEMES: Theme[] = ['dark', 'light', 'metal', 'polished'];
const STORAGE_KEY = 'theme';

const current = ref<Theme>('dark');

function applyTheme(theme: Theme) {
  current.value = theme;
  document.documentElement.className = `theme-${theme}`;
  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    // localStorage unavailable — continue without persisting
  }
}

onMounted(() => {
  const stored = localStorage.getItem(STORAGE_KEY) as Theme | null;
  current.value = stored && VALID_THEMES.includes(stored) ? stored : 'dark';
});

const themes: { id: Theme; label: string; ariaLabel: string }[] = [
  {
    id: 'dark',
    label: '🌙',
    ariaLabel: 'Switch to dark theme',
  },
  {
    id: 'light',
    label: '☀',
    ariaLabel: 'Switch to light theme',
  },
  {
    id: 'metal',
    label: '⚡',
    ariaLabel: 'Switch to metal theme',
  },
  {
    id: 'polished',
    label: '◆',
    ariaLabel: 'Switch to polished theme',
  },
];
</script>

<template>
  <div
    class="switcher"
    role="group"
    aria-label="Choose colour theme"
  >
    <button
      v-for="theme in themes"
      :key="theme.id"
      type="button"
      class="theme-btn"
      :class="{ 'theme-btn--active': current === theme.id }"
      :aria-label="theme.ariaLabel"
      :aria-pressed="current === theme.id"
      @click="applyTheme(theme.id)"
    >
      <span aria-hidden="true">{{ theme.label }}</span>
    </button>
  </div>
</template>

<style scoped>
.switcher {
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 3px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: transparent;
  transition: border-color 0.25s;
}

.theme-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  padding: 0;
  border: 1px solid transparent;
  border-radius: 5px;
  background: transparent;
  cursor: pointer;
  font-size: 0.85rem;
  line-height: 1;
  transition: background-color 0.15s, border-color 0.15s;
}

.theme-btn:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
}

.theme-btn--active {
  background: var(--surface-raised);
  border-color: var(--border);
}

.theme-btn:not(.theme-btn--active):hover {
  background: var(--copy-btn-hover-bg);
}
</style>
