<script setup lang="ts">
import { ref, watch, nextTick, onMounted, onUnmounted } from 'vue';
import ThemeSwitcher from './ThemeSwitcher.vue';

interface NavLink {
  href: string;
  label: string;
}

const props = defineProps<{
  links: NavLink[];
  githubUrl: string;
  nugetUrl: string;
  currentPathname: string;
}>();

const isOpen = ref(false);
const drawerRef = ref<HTMLElement | null>(null);
const triggerRef = ref<HTMLButtonElement | null>(null);

const DRAWER_ID = 'mobile-nav-drawer';

function open() {
  isOpen.value = true;
}

function close() {
  isOpen.value = false;
  nextTick(() => triggerRef.value?.focus());
}

function toggle() {
  if (isOpen.value) close();
  else open();
}

function onKeydown(e: KeyboardEvent) {
  if (!isOpen.value) return;

  if (e.key === 'Escape') {
    e.preventDefault();
    close();
    return;
  }

  // Trap focus inside drawer
  if (e.key === 'Tab') {
    const focusable = drawerRef.value?.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])'
    );
    if (!focusable || focusable.length === 0) return;

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  }
}

// Move focus into drawer on open
watch(isOpen, async (val) => {
  if (val) {
    document.body.style.overflow = 'hidden';
    await nextTick();
    const first = drawerRef.value?.querySelector<HTMLElement>(
      'a[href], button:not([disabled])'
    );
    first?.focus();
  } else {
    document.body.style.overflow = '';
  }
});

onMounted(() => {
  document.addEventListener('keydown', onKeydown);
});

onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown);
  document.body.style.overflow = '';
});

function isActive(href: string): boolean {
  const pathname = props.currentPathname;
  if (href === '/') return pathname === '/';
  return pathname === href || pathname.startsWith(href + '/');
}
</script>

<template>
  <!-- Hamburger trigger -->
  <button
    ref="triggerRef"
    type="button"
    class="hamburger"
    :aria-label="isOpen ? 'Close navigation menu' : 'Open navigation menu'"
    :aria-expanded="isOpen"
    :aria-controls="DRAWER_ID"
    @click="toggle"
  >
    <svg
      width="22"
      height="22"
      viewBox="0 0 22 22"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      aria-hidden="true"
      focusable="false"
    >
      <template v-if="!isOpen">
        <line x1="3" y1="6" x2="19" y2="6"/>
        <line x1="3" y1="11" x2="19" y2="11"/>
        <line x1="3" y1="16" x2="19" y2="16"/>
      </template>
      <template v-else>
        <line x1="4" y1="4" x2="18" y2="18"/>
        <line x1="18" y1="4" x2="4" y2="18"/>
      </template>
    </svg>
  </button>

  <!-- Scrim overlay -->
  <div
    v-if="isOpen"
    class="scrim"
    aria-hidden="true"
    @click="close"
  ></div>

  <!-- Drawer -->
  <div
    :id="DRAWER_ID"
    ref="drawerRef"
    class="drawer"
    :class="{ 'drawer--open': isOpen }"
    role="dialog"
    aria-label="Navigation menu"
    aria-modal="true"
  >
    <!-- Close button inside drawer -->
    <button
      type="button"
      class="drawer-close"
      aria-label="Close navigation menu"
      @click="close"
    >
      <svg
        width="20"
        height="20"
        viewBox="0 0 20 20"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        aria-hidden="true"
        focusable="false"
      >
        <line x1="3" y1="3" x2="17" y2="17"/>
        <line x1="17" y1="3" x2="3" y2="17"/>
      </svg>
    </button>

    <!-- Page links -->
    <nav aria-label="Mobile navigation">
      <ul class="drawer-links" role="list">
        <li v-for="link in links" :key="link.href">
          <a
            :href="link.href"
            class="drawer-link"
            :class="{ 'drawer-link--active': isActive(link.href) }"
            :aria-current="isActive(link.href) ? 'page' : undefined"
            @click="close"
          >
            {{ link.label }}
          </a>
        </li>
      </ul>
    </nav>

    <div class="drawer-divider" aria-hidden="true"></div>

    <!-- Theme switcher row -->
    <div class="drawer-theme">
      <ThemeSwitcher />
    </div>

    <div class="drawer-divider" aria-hidden="true"></div>

    <!-- External links -->
    <div class="drawer-external">
      <a
        :href="githubUrl"
        target="_blank"
        rel="noopener noreferrer"
        aria-label="GitHub profile, opens in new tab"
        class="drawer-icon-link"
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" focusable="false">
          <path d="M12 2C6.477 2 2 6.477 2 12c0 4.418 2.865 8.166 6.839 9.489.5.092.682-.217.682-.482 0-.237-.008-.866-.013-1.7-2.782.604-3.369-1.34-3.369-1.34-.454-1.156-1.11-1.463-1.11-1.463-.908-.62.069-.608.069-.608 1.003.07 1.531 1.03 1.531 1.03.892 1.529 2.341 1.087 2.91.831.092-.646.35-1.086.636-1.336-2.22-.253-4.555-1.11-4.555-4.943 0-1.091.39-1.984 1.029-2.683-.103-.253-.446-1.27.098-2.647 0 0 .84-.269 2.75 1.025A9.578 9.578 0 0 1 12 6.836a9.59 9.59 0 0 1 2.504.337c1.909-1.294 2.747-1.025 2.747-1.025.546 1.377.202 2.394.1 2.647.64.699 1.028 1.592 1.028 2.683 0 3.842-2.339 4.687-4.566 4.935.359.309.678.919.678 1.852 0 1.336-.012 2.415-.012 2.743 0 .267.18.578.688.48C19.138 20.163 22 16.418 22 12c0-5.523-4.477-10-10-10z"/>
        </svg>
        <span>GitHub</span>
      </a>

      <a
        :href="nugetUrl"
        target="_blank"
        rel="noopener noreferrer"
        aria-label="NuGet profile, opens in new tab"
        class="drawer-icon-link"
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" focusable="false">
          <path d="M12 2a5 5 0 1 0 0 10A5 5 0 0 0 12 2zm-9 9.5A2.5 2.5 0 0 0 .5 14v7A2.5 2.5 0 0 0 3 23.5h7A2.5 2.5 0 0 0 12.5 21v-7A2.5 2.5 0 0 0 10 11.5H3zm11 0A2.5 2.5 0 0 0 11.5 14v7A2.5 2.5 0 0 0 14 23.5h7A2.5 2.5 0 0 0 23.5 21v-7A2.5 2.5 0 0 0 21 11.5h-7z"/>
        </svg>
        <span>NuGet</span>
      </a>
    </div>
  </div>
</template>

<style scoped>
/* Hamburger is only visible on mobile */
.hamburger {
  display: none;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  background: transparent;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  color: var(--text);
  padding: 0;
  transition: color 0.15s;
}

.hamburger:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
}

@media (max-width: 767px) {
  .hamburger {
    display: flex;
  }
}

/* Scrim */
.scrim {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  z-index: 150;
}

/* Drawer */
.drawer {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  width: min(320px, 85vw);
  background: var(--nav-bg);
  border-left: 1px solid var(--border);
  z-index: 200;
  display: flex;
  flex-direction: column;
  padding: 16px 0;
  transform: translateX(100%);
  transition: transform 0.25s ease, background-color 0.25s, border-color 0.25s;
  overflow-y: auto;
}

.drawer--open {
  transform: translateX(0);
}

.drawer-close {
  display: flex;
  align-items: center;
  justify-content: center;
  align-self: flex-end;
  width: 40px;
  height: 40px;
  margin: 0 12px 8px;
  background: transparent;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  color: var(--text-muted);
  transition: color 0.15s;
}

.drawer-close:hover {
  color: var(--text);
}

.drawer-close:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
}

.drawer-links {
  list-style: none;
  margin: 0;
  padding: 0;
}

.drawer-link {
  display: flex;
  align-items: center;
  min-height: 48px;
  padding: 0 24px;
  color: var(--text);
  font-weight: 500;
  font-size: 1rem;
  text-decoration: none;
  border-left: 3px solid transparent;
  transition: color 0.15s, border-color 0.15s;
}

.drawer-link--active {
  border-left-color: var(--active-indicator);
  color: var(--accent);
}

.drawer-link:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: -3px;
}

.drawer-divider {
  height: 1px;
  background: var(--border);
  margin: 12px 0;
  transition: background-color 0.25s;
}

.drawer-theme {
  padding: 8px 24px;
}

.drawer-external {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px 0;
}

.drawer-icon-link {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 48px;
  padding: 0 24px;
  color: var(--text-muted);
  text-decoration: none;
  font-size: 0.9rem;
  transition: color 0.15s;
}

.drawer-icon-link:hover {
  color: var(--accent);
}

.drawer-icon-link:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: -3px;
}
</style>
