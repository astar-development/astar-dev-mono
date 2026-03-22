<template>
  <section class="hero">
    <div class="hero-blob blob1"></div>
    <div class="hero-blob blob2"></div>
    <div class="hero-blob blob3"></div>

    <div class="hero-emoji-row">📚 ✏️ 🎨 🔬 🌍</div>
    <h1>Learning is the<br>Greatest Adventure!</h1>
    <p>Brilliant resources for home-educating families. Worksheets, crafts, games and more — handpicked with love for curious minds aged 5–11.</p>

    <div class="hero-btns">
      <button class="btn-primary" @click="$emit('browse')">🛍 Browse Resources</button>
      <button class="btn-secondary">📖 About Us</button>
    </div>
  </section>
</template>

<script setup>
defineEmits(['browse'])
</script>

<style scoped>
.hero {
  background: var(--hero-bg);
  color: var(--hero-text);
  padding: 64px 24px 80px;
  text-align: center;
  position: relative;
  overflow: hidden;
}
.hero::before {
  content: '';
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 20% 50%, rgba(255,255,255,0.15) 0%, transparent 60%),
    radial-gradient(circle at 80% 30%, rgba(255,255,255,0.1)  0%, transparent 50%);
  pointer-events: none;
}
.hero-blob {
  position: absolute;
  border-radius: 50%;
  opacity: 0.15;
  pointer-events: none;
  background: white;
}
.blob1 { width: 300px; height: 300px; top: -80px; left: -80px; }
.blob2 { width: 200px; height: 200px; bottom: -50px; right: 60px; }
.blob3 { width: 120px; height: 120px; top: 30px; right: 200px; }

.hero-emoji-row {
  font-size: 2.2rem;
  margin-bottom: 16px;
  animation: float 3s ease-in-out infinite alternate;
  position: relative;
  z-index: 1;
}
h1 {
  font-family: var(--font-display);
  font-size: clamp(2.2rem, 5vw, 3.8rem);
  line-height: 1.15;
  margin-bottom: 16px;
  position: relative;
  z-index: 1;
  text-shadow: 0 2px 12px rgba(0,0,0,0.15);
}
p {
  font-size: 1.15rem;
  opacity: 0.92;
  max-width: 560px;
  margin: 0 auto 32px;
  line-height: 1.6;
  position: relative;
  z-index: 1;
}
.hero-btns {
  display: flex;
  gap: 12px;
  justify-content: center;
  flex-wrap: wrap;
  position: relative;
  z-index: 1;
}
.btn-primary {
  background: var(--surface);
  color: var(--accent1);
  border: none;
  border-radius: 50px;
  padding: 14px 32px;
  font-family: var(--font-body);
  font-size: 1rem;
  font-weight: 800;
  cursor: pointer;
  transition: transform 0.15s, box-shadow 0.15s;
  box-shadow: 0 4px 20px rgba(0,0,0,0.15);
}
.btn-primary:hover { transform: translateY(-3px) scale(1.03); box-shadow: 0 8px 30px rgba(0,0,0,0.2); }
.btn-secondary {
  background: rgba(255,255,255,0.25);
  color: var(--hero-text);
  border: 2px solid rgba(255,255,255,0.7);
  border-radius: 50px;
  padding: 12px 28px;
  font-family: var(--font-body);
  font-size: 1rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s, transform 0.15s;
}
.btn-secondary:hover { background: rgba(255,255,255,0.35); transform: translateY(-2px); }

@keyframes float {
  from { transform: translateY(0); }
  to   { transform: translateY(-8px); }
}
</style>
