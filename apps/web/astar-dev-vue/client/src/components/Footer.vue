<script setup lang="ts">
const year: number = new Date().getFullYear()
</script>

<template>
  <footer class="footer">
    <p class="footer__text">
      &copy; {{ year }} AStar Development. Production-grade architecture, pipelines, and observability.
    </p>
  </footer>
</template>

<style scoped>
.footer {
  background: #0a1a2a;
  border-top: 1px solid #26303d;
  padding: 24px;
  text-align: center;
}

.footer__text {
  color: #9db2c6;
  font-size: 0.9rem;
}
</style>
