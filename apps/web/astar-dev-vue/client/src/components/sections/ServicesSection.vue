<script setup lang="ts">
interface Service {
  title: string
  text: string
  output: string
}

const services: Service[] = [
  {
    title: 'Architecture and System Design',
    text: 'Clear boundaries, explicit contracts, and a shared mental model of how your system behaves in production.',
    output: 'Output: architecture sketches, decision records, and a roadmap that respects your reality.',
  },
  {
    title: 'Production-Grade .NET Pipelines',
    text: 'Deterministic, observable pipelines that make TDD and clean code pay off all the way to production.',
    output: 'Output: pipeline definitions, guardrails, and patterns your team can extend without fear.',
  },
  {
    title: 'Observability and Telemetry',
    text: 'Traces, metrics, and logs that make incidents understandable, and post-incident reviews genuinely useful.',
    output: 'Output: instrumentation plan, dashboards, and workflows tuned to your team.',
  },
]
</script>

<template>
  <section id="services" class="section section--solid">
    <div class="container">
      <div class="section__head">
        <h2>What You Actually Get</h2>
        <p>
          Concrete improvements to how your system behaves and how your team works — not slide decks or abstract diagrams.
        </p>
        <span>Typically 2-12 weeks, shaped around your team and constraints.</span>
      </div>

      <div class="service-grid">
        <article v-for="service in services" :key="service.title" class="panel">
          <h3>{{ service.title }}</h3>
          <p>{{ service.text }}</p>
          <p class="panel__meta">{{ service.output }}</p>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
@import './section-shared.css';

.section__head span {
  display: inline-block;
  margin-top: 10px;
  color: #8da2b7;
  font-size: 0.86rem;
}

.service-grid {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
}
</style>
