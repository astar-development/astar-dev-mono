<script setup lang="ts">
import { ref, onMounted } from 'vue';

const STORAGE_KEY = 'cookie-consent';
const visible = ref(false);
const liveText = ref('');

onMounted(() => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) visible.value = true;
  } catch {
    visible.value = true;
  }
});

function accept() {
  persist({ analytics: true });
  liveText.value = 'Cookie preferences saved';
  visible.value = false;
  window.dispatchEvent(new Event('cookie-consent-accepted'));
}

function decline() {
  persist({ analytics: false });
  liveText.value = 'Cookie preferences saved';
  visible.value = false;
}

function persist(prefs: { analytics: boolean }) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
  } catch {
    // localStorage unavailable — accept silently
  }
}
</script>

<template>
  <div
    v-if="visible"
    class="consent-bar"
    role="dialog"
    aria-label="Cookie consent"
    aria-modal="false"
  >
    <div class="consent-inner">
      <p class="consent-text">
        We use cookies to remember your theme preference and collect anonymised
        usage analytics.&nbsp;
        <a href="/privacy" class="consent-link">Read our Privacy Policy.</a>
      </p>

      <div class="consent-buttons">
        <button type="button" class="btn-accept" @click="accept">Accept</button>
        <button type="button" class="btn-decline" @click="decline">Decline</button>
      </div>
    </div>
  </div>

  <!-- Screen reader announcement -->
  <span
    role="status"
    aria-live="polite"
    aria-atomic="true"
    class="sr-only"
  >{{ liveText }}</span>
</template>

<style scoped>
.consent-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 200;
  background: var(--nav-bg);
  border-top: 1px solid var(--border);
  padding: 16px 24px;
  transition: background-color 0.25s, border-color 0.25s;
}

.consent-inner {
  max-width: 1120px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
}

.consent-text {
  flex-grow: 1;
  font-size: 0.85rem;
  color: var(--text-muted);
  line-height: 1.5;
  transition: color 0.25s;
}

.consent-link {
  color: var(--accent);
  text-decoration: underline;
  transition: color 0.2s;
}

.consent-link:hover {
  opacity: 0.8;
}

.consent-link:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
  border-radius: 2px;
}

.consent-buttons {
  flex-shrink: 0;
  display: flex;
  gap: 12px;
  align-items: center;
}

.btn-accept,
.btn-decline {
  padding: 8px 20px;
  font-size: 0.85rem;
  font-weight: 600;
  border-radius: 8px;
  cursor: pointer;
  transition: transform 0.15s ease, background-color 0.25s, border-color 0.25s, color 0.25s;
}

.btn-accept:focus-visible,
.btn-decline:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 3px;
}

.btn-accept {
  background: var(--btn-primary-bg);
  color: var(--btn-primary-text);
  border: 2px solid var(--btn-primary-bg);
}

.btn-accept:hover {
  transform: translateY(-1px);
}

.btn-decline {
  background: transparent;
  color: var(--text);
  border: 1px solid var(--border);
}

.btn-decline:hover {
  border-color: var(--accent);
  transform: translateY(-1px);
}

@media (prefers-reduced-motion: reduce) {
  .btn-accept:hover,
  .btn-decline:hover {
    transform: none;
  }
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border-width: 0;
}
</style>
