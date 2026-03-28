<script setup lang="ts">
import { ref, onMounted } from 'vue';

const props = defineProps<{
  text: string;
  packageId: string;
}>();

const copied = ref(false);
const supported = ref(false);
const liveRegion = ref('');

onMounted(() => {
  supported.value =
    typeof navigator !== 'undefined' && !!navigator.clipboard;
});

async function copyToClipboard() {
  if (!navigator.clipboard) return;
  try {
    await navigator.clipboard.writeText(props.text);
    copied.value = true;
    liveRegion.value = 'Copied to clipboard';
    setTimeout(() => {
      copied.value = false;
      liveRegion.value = '';
    }, 2000);
  } catch {
    // Clipboard write failed — silently ignore
  }
}
</script>

<template>
  <button
    v-if="supported"
    type="button"
    class="copy-btn"
    :aria-label="copied ? 'Copied!' : 'Copy install command'"
    @click="copyToClipboard"
  >
    <!-- Default: clipboard icon -->
    <svg
      v-if="!copied"
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <rect x="9" y="9" width="13" height="13" rx="2" ry="2"/>
      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
    </svg>

    <!-- Copied: checkmark icon -->
    <svg
      v-else
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2.5"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
      class="icon-check"
    >
      <polyline points="20 6 9 17 4 12"/>
    </svg>
  </button>

  <!-- Screen reader live region -->
  <span
    role="status"
    aria-live="polite"
    aria-atomic="true"
    class="sr-only"
  >{{ liveRegion }}</span>
</template>

<style scoped>
.copy-btn {
  position: absolute;
  top: 8px;
  right: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  background: transparent;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  color: var(--text-muted);
  padding: 0;
  transition: color 0.15s, background-color 0.15s;
}

.copy-btn:hover {
  color: var(--accent);
  background: var(--copy-btn-hover-bg);
}

.copy-btn:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
}

.icon-check {
  color: var(--terminal-accent);
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
