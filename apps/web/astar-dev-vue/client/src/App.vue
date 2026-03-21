<script setup lang="ts">
import Hero from './components/Hero.vue'
import Features from './components/Features.vue'
import Footer from './components/Footer.vue'
</script>

<template>
  <div class="app">
    <Hero />
    <Features />
    <Footer />
  </div>
</template>

<style>
*,
*::before,
*::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  background-color: var(--background);
  color: var(--text);
}

html {
  scroll-behavior: smooth;
}

.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
</style>
