<template>
  <div>
    <!-- Filter pills -->
    <div class="category-bar" id="shop">
      <button
        v-for="cat in categories"
        :key="cat.id"
        class="cat-pill"
        :class="{ active: activeFilter === cat.id }"
        @click="activeFilter = cat.id"
      >
        {{ cat.emoji }} {{ cat.label }}
      </button>
    </div>

    <!-- Grid -->
    <section class="section">
      <div class="section-header">
        <h2 class="section-title">{{ activeCategoryLabel }}</h2>
        <span class="results-count">
          {{ filteredProducts.length }} resource{{ filteredProducts.length !== 1 ? 's' : '' }}
        </span>
      </div>

      <div class="product-grid" v-if="filteredProducts.length">
        <ProductCard
          v-for="product in filteredProducts"
          :key="product.id"
          :product="product"
          @added="p => $emit('added', p)"
        />
      </div>

      <div class="empty-state" v-else>
        <div class="empty-icon">🔍</div>
        <p>No resources in this category yet!</p>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { categories, products } from '@/data/products'
import ProductCard from './ProductCard.vue'

defineEmits(['added'])

const activeFilter = ref('all')

const filteredProducts = computed(() =>
  activeFilter.value === 'all'
    ? products
    : products.filter(p => p.category === activeFilter.value)
)

const activeCategoryLabel = computed(() => {
  const cat = categories.find(c => c.id === activeFilter.value)
  return cat ? `${cat.emoji} ${cat.label}` : '✨ All Resources'
})
</script>

<style scoped>
.category-bar {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: center;
  padding: 24px 24px 0;
}
.cat-pill {
  background: var(--surface);
  border: 2px solid var(--border);
  color: var(--text2);
  border-radius: 50px;
  padding: 8px 20px;
  font-family: var(--font-body);
  font-size: 0.9rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.18s;
  display: flex; align-items: center; gap: 6px;
  white-space: nowrap;
}
.cat-pill:hover { border-color: var(--accent1); color: var(--accent1); transform: translateY(-2px); }
.cat-pill.active {
  background: var(--accent1);
  border-color: var(--accent1);
  color: white;
  box-shadow: var(--shadow);
}
.section {
  max-width: 1200px;
  margin: 0 auto;
  padding: 32px 24px 64px;
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 12px;
}
.section-title {
  font-family: var(--font-display);
  font-size: 1.8rem;
  color: var(--text);
}
.results-count { font-size: 0.9rem; color: var(--text2); font-weight: 600; }
.product-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 24px;
}
.empty-state {
  text-align: center;
  padding: 64px 24px;
  color: var(--text2);
}
.empty-icon { font-size: 4rem; margin-bottom: 16px; }
.empty-state p { font-size: 1.1rem; font-weight: 600; }

@media (max-width: 600px) {
  .product-grid { grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 14px; }
}
</style>
