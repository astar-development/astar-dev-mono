<template>
  <section id="contact" class="section section--solid section--top-border">
    <div class="container contact">
      <div>
        <h2>Let’s talk about your system</h2>
        <p>
          If your architecture, pipelines, or observability feel harder to reason about than they should, a short conversation can clarify what to change first.
        </p>
        <p>
          Share a brief outline of your context. You will receive a considered response, not a sales sequence.
        </p>
      </div>

      <form class="panel form" @submit.prevent>
        <label for="name">Name</label>
        <input id="name" type="text" />

        <label for="email">Email</label>
        <input id="email" type="email" />

        <label for="message">Message</label>
        <textarea id="message" rows="4"></textarea>

        <button type="submit">Submit (non-functional demo)</button>
      </form>
    </div>
  </section>
</template>

<style scoped>
@import './section-shared.css';

.contact {
  display: grid;
  grid-template-columns: 1.1fr 1fr;
  gap: 18px;
}

.contact h2 {
  color: #f3f6fb;
  margin-bottom: 10px;
}

.contact p {
  color: #bfd0e0;
  margin-bottom: 8px;
}

.form {
  display: grid;
  gap: 8px;
}

.form label {
  font-size: 0.8rem;
  color: #9cb1c5;
}

.form input,
.form textarea {
  width: 100%;
  border: 1px solid #32485f;
  background: #0a1a2a;
  border-radius: 10px;
  color: #e8f1fa;
  padding: 10px 11px;
  font: inherit;
}

.form button {
  margin-top: 8px;
  border: 1px solid #5de0d0;
  background: #2dd4bf;
  color: #0f1a26;
  border-radius: 999px;
  padding: 10px 14px;
  font-weight: 700;
  cursor: pointer;
}

@media (max-width: 960px) {
  .contact {
    grid-template-columns: 1fr;
  }
}
</style>
