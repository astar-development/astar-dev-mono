<script setup lang="ts">
interface Testimonial {
  quote: string
  name: string
  role: string
  company: string
}

const testimonials: Testimonial[] = [
  {
    quote:
      'We expected a pipeline review. We got a complete reframe of how we think about delivery. Calm, sharp, and relentlessly practical.',
    name: 'Hannah Lee',
    role: 'Engineering Manager',
    company: 'Vectorline Systems',
  },
  {
    quote:
      'Our deploys used to feel like a live show with no soundcheck. Now they are tight, predictable, and fully instrumented.',
    name: 'Owen Carter',
    role: 'Platform Lead',
    company: 'Nimbus Data',
  },
  {
    quote:
      'The mix of deep technical discipline and human, honest communication made it easy for the team to engage.',
    name: 'Isabella Rossi',
    role: 'CTO',
    company: 'Aurora Grid',
  },
]
</script>

<template>
  <section id="testimonials" class="section section--solid section--top-border">
    <div class="container">
      <div class="section__head">
        <h2>What teams say</h2>
        <p>Feedback from teams who wanted fewer surprises and more confidence in production.</p>
      </div>

      <div class="testimonial-grid">
        <article v-for="entry in testimonials" :key="entry.name" class="panel">
          <p class="quote">"{{ entry.quote }}"</p>
          <p class="panel__meta">{{ entry.name }}, {{ entry.role }}, {{ entry.company }}</p>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
@import './section-shared.css';

.testimonial-grid {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
}

.quote {
  color: #d6e4f1;
  font-size: 0.93rem;
}
</style>
