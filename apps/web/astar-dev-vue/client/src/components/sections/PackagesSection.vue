<script setup lang="ts">
interface Package {
  name: string
  description: string
  nugetUrl: string
  docsUrl: string
}

const packages: Package[] = [
  {
    name: 'AStar.Pipelines',
    description:
      'Deterministic, observable .NET pipelines that behave like a rehearsed setlist: tight, predictable, and loud when needed.',
    nugetUrl: 'https://www.nuget.org/packages/AStar.Pipelines',
    docsUrl: '/docs/astar.pipelines.md',
  },
  {
    name: 'AStar.Observability',
    description:
      'Traces, metrics, and logs that turn production from noise into a mix you can hear and tune.',
    nugetUrl: 'https://www.nuget.org/packages/AStar.Observability',
    docsUrl: '/docs/astar.observability.md',
  },
  {
    name: 'AStar.Resilience',
    description:
      'Resilience primitives that keep your system playing through retries, timeouts, and partial failures.',
    nugetUrl: 'https://www.nuget.org/packages/AStar.Resilience',
    docsUrl: '/docs/astar.resilience.md',
  },
]
</script>

<template>
  <section id="packages" class="section section--solid">
    <div class="container">
      <div class="section__head">
        <h2>NuGet Rack</h2>
        <p>Packages that capture patterns proven in production.</p>
      </div>

      <div class="package-row">
        <article v-for="pkg in packages" :key="pkg.name" class="panel package">
          <div class="package__head">
            <h3>{{ pkg.name }}</h3>
            <span>NuGet</span>
          </div>

          <p>{{ pkg.description }}</p>

          <div class="package__links">
            <a :href="pkg.nugetUrl" target="_blank" rel="noopener noreferrer">View on NuGet</a>
            <a :href="pkg.docsUrl">View markdown docs</a>
          </div>

          <pre><code>dotnet add package {{ pkg.name }}</code></pre>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
@import './section-shared.css';

.package-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 16px;
}

.package__head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}

.package__head h3 {
  margin-bottom: 0;
}

.package__head span {
  font-size: 0.7rem;
  color: #99aec2;
  border: 1px solid #32485f;
  border-radius: 999px;
  padding: 2px 8px;
}

.package p {
  margin-top: 8px;
}

.package__links {
  margin-top: 12px;
  display: grid;
  gap: 4px;
}

.package__links a {
  color: #2dd4bf;
  text-decoration: none;
  font-size: 0.85rem;
}

.package__links a:hover {
  text-decoration: underline;
}

.package pre {
  margin-top: 14px;
  border: 1px solid #274056;
  border-radius: 10px;
  background: #0a1a2a;
  padding: 10px;
  overflow: auto;
}

.package code {
  font-size: 0.8rem;
  color: #d5e3f0;
}
</style>
