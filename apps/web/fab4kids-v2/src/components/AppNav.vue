<template>
  <nav class="nav">
    <a class="nav-logo">
      <span class="logo-icon">🌟</span>
      <span class="logo-text">LearnLand</span>
    </a>

    <div class="nav-right">
      <!-- Theme switcher -->
      <div class="theme-switcher">
        <button
          v-for="t in themes"
          :key="t.id"
          class="theme-btn"
          :class="[`theme-btn-${t.id}`, { active: modelValue === t.id }]"
          :title="t.label"
          @click="$emit('update:modelValue', t.id)"
        >{{ t.emoji }}</button>
      </div>

      <!-- Basket button -->
      <button class="basket-btn" @click="$emit('open-basket')">
        🛒
        <span class="basket-count">{{ basketCount }}</span>
        £{{ basketTotal.toFixed(2) }}
      </button>
    </div>
  </nav>
</template>

<script setup>
import { useBasket } from '@/composables/useBasket'

defineProps({ modelValue: String })
defineEmits(['update:modelValue', 'open-basket'])

const { basketCount, basketTotal } = useBasket()

const themes = [
  { id: 'playful', label: 'Playful',  emoji: '🎈' },
  { id: 'pastel',  label: 'Pastel',   emoji: '🌸' },
  { id: 'nature',  label: 'Nature',   emoji: '🌿' },
]
</script>

<style scoped>
.nav {
  background: var(--nav-bg);
  color: var(--nav-text);
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 64px;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 4px 20px rgba(0,0,0,0.12);
}
.nav-logo {
  font-family: var(--font-display);
  font-size: 1.7rem;
  color: var(--nav-text);
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: default;
}
.nav-right {
  display: flex;
  align-items: center;
  gap: 16px;
}
.theme-switcher {
  display: flex;
  gap: 6px;
  background: rgba(255,255,255,0.2);
  border-radius: 30px;
  padding: 4px;
}
.theme-btn {
  width: 26px; height: 26px;
  border-radius: 50%;
  border: 2px solid transparent;
  cursor: pointer;
  transition: transform 0.2s, border-color 0.2s;
  font-size: 14px;
  display: flex; align-items: center; justify-content: center;
}
.theme-btn:hover { transform: scale(1.2); }
.theme-btn.active { border-color: white; transform: scale(1.15); }
.theme-btn-playful { background: #ff6b6b; }
.theme-btn-pastel  { background: #b39ddb; }
.theme-btn-nature  { background: #5a8a3c; }

.basket-btn {
  background: rgba(255,255,255,0.25);
  border: 2px solid rgba(255,255,255,0.5);
  color: var(--nav-text);
  border-radius: 30px;
  padding: 6px 16px 6px 12px;
  cursor: pointer;
  font-family: var(--font-body);
  font-weight: 700;
  font-size: 0.9rem;
  display: flex; align-items: center; gap: 6px;
  transition: background 0.2s, transform 0.1s;
}
.basket-btn:hover { background: rgba(255,255,255,0.35); transform: translateY(-1px); }
.basket-count {
  background: white;
  color: var(--accent1);
  border-radius: 50%;
  width: 22px; height: 22px;
  display: flex; align-items: center; justify-content: center;
  font-size: 0.75rem; font-weight: 800;
  min-width: 22px;
}

@media (max-width: 600px) {
  .logo-text { display: none; }
}
</style>
