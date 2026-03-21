<template>
  <section class="section section--stripe">
    <div class="container strip">
      <div>
        <h2>Discipline with room for momentum</h2>
        <p>
          Good engineering balances clarity with pace. The goal isn't perfection — it's systems that stay understandable and resilient as they evolve.
        </p>
      </div>

      <div class="strip__cards">
        <article class="panel panel--small">
          <h3>TDD and Clean Code</h3>
          <p>Tests as design, not afterthought. Code your future team can live with.</p>
        </article>
        <article class="panel panel--small">
          <h3>Eclectic Energy</h3>
          <p>Metallica for focus, Blackpink for momentum. Serious work, human atmosphere.</p>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
@import './section-shared.css';

.section--stripe {
  border-bottom: 1px solid #26303d;
  background: linear-gradient(90deg, #08131f 0%, #11243a 50%, #08131f 100%);
}

.strip {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 20px;
  align-items: start;
}

.strip h2 {
  color: #f3f6fb;
  margin-bottom: 10px;
}

.strip p {
  color: #cbdae9;
}

.strip__cards {
  display: grid;
  gap: 12px;
  grid-template-columns: 1fr 1fr;
}

.panel--small h3 {
  font-size: 0.88rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: #2dd4bf;
  margin-bottom: 8px;
}

.panel--small p {
  color: #bfd0e0;
  font-size: 0.93rem;
}

@media (max-width: 960px) {
  .strip {
    grid-template-columns: 1fr;
  }

  .strip__cards {
    grid-template-columns: 1fr;
  }
}
</style>
