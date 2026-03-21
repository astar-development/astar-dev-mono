<script setup lang="ts">
import { computed } from 'vue'
import { useTheme } from '../composables/useTheme'
import ThemeSwitcher from '../components/ThemeSwitcher.vue'

const { theme } = useTheme()

const logoUrl = computed(() => {
  switch (theme.value) {
    case 'metal':
      return '/assets/metal-logo.png'
    case 'polished':
      return '/assets/polished-logo.png'
    case 'light':
      return '/assets/astar.png'
    case 'dark':
    default:
      return '/assets/astar.png'
  }
})

</script>

<template>
  <header class="hero">
    <div class="topbar">
      
      <div class="topbar__brand">
        <img :src="logoUrl + '?v=' + theme" :key="theme" class="logo" alt="Logo" />
        <div>
          <div class="topbar__title">AStar Development</div>
          <div class="topbar__subtitle">Software Architecture & Branding</div>
        </div>
      </div>
      
    </div>

    <div class="hero__content">
      <div class="hero__eyebrow-row">
        <p class="hero__eyebrow">PRODUCTION // CLARITY</p>
        <ThemeSwitcher />
      </div>

      <h1 class="hero__title">
        Production‑grade .NET systems, designed to stay calm under load.
      </h1>

      <p class="hero__subtitle">
        Architecture, software, pipelines, and observability shaped for real teams and real constraints.
        Enough energy to move fast, enough discipline to stay reliable.
        AStar Development blends TDD discipline with the energy of a stadium show. Reliability with real
        personality.
      </p>

      <p class="hero__subtitle">
        AStar Development blends TDD discipline with the energy of a stadium show.
        Metallica-level reliability and speed. Blackpink-level polish and precision.
      </p>

      <div class="hero__tags">
        <span class="hero__tag">TDD - Tests that shape design</span>
        <span class="hero__tag">Clean code - Boundaries you can reason about</span>
        <span class="hero__tag">Observability as a lifestyle - Signals over guesswork</span>
      </div>

      <div class="hero__actions">
        <a href="#services" class="btn btn--secondary">Explore services</a>
        <a href="#contact" class="btn btn--secondary">Start a conversation</a>
      </div>
    </div>
  </header>
</template>

<style>
.topbar {
  max-width: 1120px;
  margin: 0 auto;
  padding: var(--topbar-padding);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.hero__content {
  max-width: 1120px;
  margin: 0 auto;
  padding: var(--hero-content-padding-top) 24px 0;
}

.hero__actions {
  margin-top: 0px;
  display: flex;
  gap: 16px;
  justify-content: center;
  flex-wrap: wrap;
}

.hero__tags {
  margin-top: 24px;
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
  margin-bottom: 32px;
}

.topbar__title {
  color: var(--hero-title);
  transition: color var(--transition-speed);
}

.topbar__nav select {
  color: var(--text);
  background: var(--panel-bg);
  border: 1px solid var(--panel-border);
}

.topbar__subtitle {
  color: var(--hero-subtitle);
  transition: color var(--transition-speed);
  width: max-content;
}

.topbar__link {
  color: var(--hero-link);
  transition: color var(--transition-speed);
}

.topbar__link:hover {
  color: var(--hero-link-hover);
}

.hero__title {
  color: var(--hero-title);
  transition: color var(--transition-speed);
  font-size: clamp(2.1rem,5vw,3.6rem);
  font-weight: 700;
  text-align: center;
  margin-bottom: 32px;
}

.hero__subtitle {
  color: var(--hero-subtitle);
  transition: color var(--transition-speed);
  text-align: center;
}

.hero__eyebrow {
  color: var(--hero-eyebrow);
  transition: color var(--transition-speed);
  margin: 0px;
  letter-spacing: .25em;
  font-size: .65rem;
  font-weight: 700;
  text-transform: uppercase;
  text-align: center;
  line-height: 1.2;
}

.hero__tag {
  border-radius: 999px;
  padding: 6px 12px;
  font-size: .78rem;
  border: 1px solid var(--hero-tag-border);
  background: var(--hero-tag-bg);
  color: var(--hero-title);
  transition: background var(--transition-speed), border-color var(--transition-speed), color var(--transition-speed);
}

.logo {
  width: var(--logo-size);
  height: auto;
  object-fit: contain;
  transition: width var(--transition-speed), height var(--transition-speed);
}

.theme-metal .logo,
.theme-polished .logo {
  margin-top: -0px;
}

.theme-metal .topbar,
.theme-polished .topbar {
  align-items: flex-start;
}

/* Default: traditional themes */
.topbar__brand > div {
  min-width: 0;
  text-align: left;
}

/* Me themes only */
.theme-metal .topbar__brand > div,
.theme-polished .topbar__brand > div {
  text-align: center;
}

.topbar__nav {
  flex-shrink: 0;
}

.topbar__brand {
  display: flex;
  flex-direction: var(--logo-align);
  align-items: center;
  gap: 12px;

  /* Centering logic */
  flex: calc(var(--logo-center) * 1);
  margin-left: calc(var(--logo-center) * auto);
  margin-right: calc(var(--logo-center) * auto);
}

.btn {
  display: inline-block;
  padding: 11px 20px;
  border-radius: 999px;
  font-size: 0.88rem;
  font-weight: 600;
  text-decoration: none;
  border: 1px solid var(--btn-border);
  background: var(--btn-bg);
  color: var(--btn-text);
  box-shadow: var(--btn-shadow);
  transition:
    background var(--transition-speed),
    color var(--transition-speed),
    border-color var(--transition-speed),
    box-shadow var(--transition-speed),
    transform 0.15s ease,
    opacity 0.15s ease;
}

.btn:hover {
  border-color: var(--btn-hover-border);
  background: var(--btn-hover-bg);
  color: var(--btn-hover-text);
  box-shadow: var(--btn-shadow);
  transform: translateY(-1px);
  opacity: 0.95;
}

.hero {
  border-bottom: 1px solid var(--panel-border);
  padding: var(--hero-padding-top) 24px 84px;
  justify-content: center;

  background:
    var(--hero-gradient),
    var(--hero-bg);

  transition: background var(--transition-speed), border-color var(--transition-speed);
}

.hero * {
  transition:
    color var(--transition-speed),
    background var(--transition-speed),
    border-color var(--transition-speed);
}

.theme-metal .topbar,
.theme-polished .topbar {
  align-items: flex-start !important;
}
.hero__eyebrow-row {
  display: flex;
  justify-content: center;   /* centers the whole row under the logo */
  align-items: center;
  gap: 24px;
  margin-bottom: 12px;
}

.hero__eyebrow-row .theme-select {
  margin: 0;  
  margin-left: 24px; /* small spacing so it doesn't crowd the eyebrow */
}

.hero__eyebrow-row .theme-select {
  position: relative;
}

</style>
