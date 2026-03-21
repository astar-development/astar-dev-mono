<script setup lang="ts">
interface Track {
  title: string
  text: string
}

const tracks: Track[] = [
  {
    title: 'Track 1: Discovery',
    text: 'Understand your context, constraints, and where the real friction lives.',
  },
  {
    title: 'Track 2: Design',
    text: 'Shape architectures, pipelines, and observability that fit your team and domain.',
  },
  {
    title: 'Track 3: Delivery',
    text: 'Pair on implementation, keep feedback loops tight, and adjust as you learn.',
  },
  {
    title: 'Track 4: Encore',
    text: 'Leave you with documentation, patterns, and confidence to keep iterating.',
  },
]
</script>

<template>
  <section id="tracks" class="section section--solid section--top-border">
    <div class="container">
      <div class="section__head">
        <h2>How engagements typically run</h2>
        <p>A structured path from understanding your context to leaving you with a system you can confidently own.</p>
      </div>

      <div class="track-grid">
        <article v-for="(track, index) in tracks" :key="track.title" class="panel">
          <span class="track__index">{{ index + 1 < 10 ? `0${index + 1}` : index + 1 }}</span>
          <h3>{{ track.title }}</h3>
          <p>{{ track.text }}</p>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
@import './section-shared.css';

.track-grid {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
}

.track__index {
  display: inline-block;
  margin-bottom: 8px;
  font-size: 0.72rem;
  letter-spacing: 0.12em;
  color: #2dd4bf;
}
</style>
