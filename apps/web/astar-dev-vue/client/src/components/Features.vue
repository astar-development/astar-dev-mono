<script setup lang="ts">
import ServicesSection from './sections/ServicesSection.vue'
import ChaosSection from './sections/ChaosSection.vue'
import PackagesSection from './sections/PackagesSection.vue'
import TracksSection from './sections/TracksSection.vue'
import TestimonialsSection from './sections/TestimonialsSection.vue'
import ContactSection from './sections/ContactSection.vue'
</script>

<template>
  <main class="sections">
    <ServicesSection />
    <ChaosSection />
    <PackagesSection />
    <TracksSection />
    <TestimonialsSection />
    <ContactSection />
  </main>
</template>

<style scoped>
.sections {
  background: #08131f;
}
</style>
