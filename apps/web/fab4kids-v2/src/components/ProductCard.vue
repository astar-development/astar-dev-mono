<template>
  <div class="product-card">
    <div class="product-thumb" :style="{ background: product.bg }">
      <span>{{ product.emoji }}</span>
      <span v-if="product.badge" class="product-badge">{{ product.badge }}</span>
    </div>
    <div class="product-body">
      <div class="product-type-tag">{{ product.typeEmoji }} {{ product.type }}</div>
      <div class="product-title">{{ product.name }}</div>
      <div class="product-desc">{{ product.desc }}</div>
      <div class="product-stars">{{ product.stars }}</div>
      <div class="product-footer">
        <span class="product-price">£{{ product.price.toFixed(2) }}</span>
        <button
          class="add-btn"
          :class="{ added: inBasket }"
          @click="handleAdd"
        >
          {{ inBasket ? '✓ Added' : '+ Add' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useBasket } from '@/composables/useBasket'

const props = defineProps({ product: Object })
const emit = defineEmits(['added'])

const { isInBasket, addToBasket } = useBasket()
const inBasket = computed(() => isInBasket(props.product.id))

function handleAdd() {
  if (!inBasket.value) {
    addToBasket(props.product)
    emit('added', props.product)
  }
}
</script>

<style scoped>
.product-card {
  background: var(--surface);
  border-radius: var(--card-radius);
  border: 2px solid var(--border);
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;
  cursor: pointer;
  position: relative;
}
.product-card:hover { transform: translateY(-6px) rotate(-0.5deg); box-shadow: var(--shadow); }

.product-thumb {
  width: 100%;
  aspect-ratio: 4/3;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 4rem;
  position: relative;
  overflow: hidden;
}
.product-badge {
  position: absolute;
  top: 10px; right: 10px;
  background: var(--badge-bg);
  color: white;
  border-radius: 20px;
  padding: 3px 10px;
  font-size: 0.72rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.product-body { padding: 14px 16px 16px; }
.product-type-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: var(--tag-bg);
  color: var(--tag-text);
  border-radius: 20px;
  padding: 2px 10px;
  font-size: 0.72rem;
  font-weight: 800;
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.4px;
}
.product-title {
  font-family: var(--font-body);
  font-weight: 800;
  font-size: 1rem;
  color: var(--text);
  margin-bottom: 6px;
  line-height: 1.3;
}
.product-desc {
  font-size: 0.82rem;
  color: var(--text2);
  line-height: 1.5;
  margin-bottom: 10px;
}
.product-stars { color: var(--star); font-size: 0.8rem; margin-bottom: 10px; }
.product-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.product-price {
  font-family: var(--font-display);
  font-size: 1.3rem;
  color: var(--accent1);
  font-weight: 700;
}
.add-btn {
  background: var(--btn-bg);
  color: var(--btn-text);
  border: none;
  border-radius: 50px;
  padding: 8px 16px;
  font-family: var(--font-body);
  font-size: 0.85rem;
  font-weight: 800;
  cursor: pointer;
  transition: transform 0.15s, opacity 0.15s;
  display: flex; align-items: center; gap: 4px;
}
.add-btn:hover { transform: scale(1.07); }
.add-btn.added { opacity: 0.6; cursor: default; }
</style>
