<template>
  <Teleport to="body">
    <template v-if="modelValue">
      <div class="overlay" @click="$emit('update:modelValue', false)"></div>
      <div class="drawer">
        <div class="drawer-header">
          <h2>🛒 Your Basket</h2>
          <button class="close-btn" @click="$emit('update:modelValue', false)">✕</button>
        </div>

        <div class="drawer-body">
          <div v-if="basket.length === 0" class="basket-empty">
            <div class="big-emoji">🧺</div>
            <p>Your basket is empty!</p>
            <p class="sub">Browse our resources above to get started</p>
          </div>

          <div v-for="item in basket" :key="item.id" class="basket-item">
            <div class="basket-item-thumb">{{ item.emoji }}</div>
            <div class="basket-item-info">
              <div class="basket-item-name">{{ item.name }}</div>
              <div class="basket-item-price">£{{ (item.price * item.qty).toFixed(2) }}</div>
            </div>
            <div class="qty-ctrl">
              <button class="qty-btn" @click="updateQty(item.id, -1)">−</button>
              <span class="qty-num">{{ item.qty }}</span>
              <button class="qty-btn" @click="updateQty(item.id, 1)">+</button>
            </div>
          </div>
        </div>

        <div class="drawer-footer" v-if="basket.length > 0">
          <div class="total-row">
            <span class="total-label">Total</span>
            <span class="total-amount">£{{ basketTotal.toFixed(2) }}</span>
          </div>
          <button class="checkout-btn" @click="$emit('checkout')">🎉 Proceed to Checkout</button>
        </div>
      </div>
    </template>
  </Teleport>
</template>

<script setup>
import { useBasket } from '@/composables/useBasket'

defineProps({ modelValue: Boolean })
defineEmits(['update:modelValue', 'checkout'])

const { basket, basketTotal, updateQty } = useBasket()
</script>

<style scoped>
.overlay {
  position: fixed; inset: 0;
  background: rgba(0,0,0,0.4);
  z-index: 200;
  backdrop-filter: blur(4px);
  animation: fadeIn 0.2s;
}
.drawer {
  position: fixed;
  top: 0; right: 0; bottom: 0;
  width: min(420px, 100vw);
  background: var(--surface);
  z-index: 201;
  display: flex;
  flex-direction: column;
  box-shadow: -8px 0 40px rgba(0,0,0,0.2);
  animation: slideIn 0.3s ease;
}
.drawer-header {
  background: var(--nav-bg);
  color: var(--nav-text);
  padding: 20px 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.drawer-header h2 { font-family: var(--font-display); font-size: 1.4rem; }
.close-btn {
  background: rgba(255,255,255,0.25);
  border: none;
  color: var(--nav-text);
  width: 36px; height: 36px;
  border-radius: 50%;
  font-size: 1.2rem;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: background 0.15s;
}
.close-btn:hover { background: rgba(255,255,255,0.4); }

.drawer-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.basket-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: var(--text2);
  gap: 8px;
  font-size: 1rem;
  font-weight: 600;
  text-align: center;
}
.basket-empty .big-emoji { font-size: 3.5rem; }
.basket-empty .sub { font-size: 0.85rem; font-weight: 400; opacity: 0.7; }

.basket-item {
  background: var(--surface2);
  border: 2px solid var(--border);
  border-radius: 14px;
  padding: 12px;
  display: flex;
  align-items: center;
  gap: 12px;
}
.basket-item-thumb {
  font-size: 2.2rem;
  width: 52px; height: 52px;
  background: var(--bg2);
  border-radius: 12px;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.basket-item-info { flex: 1; min-width: 0; }
.basket-item-name {
  font-weight: 800;
  font-size: 0.9rem;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.basket-item-price { color: var(--accent1); font-weight: 700; font-size: 0.95rem; margin-top: 2px; }
.qty-ctrl { display: flex; align-items: center; gap: 8px; }
.qty-btn {
  width: 28px; height: 28px;
  border-radius: 50%;
  border: 2px solid var(--border);
  background: var(--surface);
  color: var(--text);
  font-size: 1rem; font-weight: 800;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: background 0.15s;
}
.qty-btn:hover { background: var(--accent1); color: white; border-color: var(--accent1); }
.qty-num { font-weight: 800; font-size: 0.95rem; min-width: 16px; text-align: center; }

.drawer-footer {
  border-top: 2px solid var(--border);
  padding: 20px 24px;
  background: var(--surface2);
}
.total-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.total-label { font-weight: 700; font-size: 1rem; color: var(--text2); }
.total-amount { font-family: var(--font-display); font-size: 1.6rem; color: var(--accent1); }
.checkout-btn {
  width: 100%;
  background: var(--btn-bg);
  color: var(--btn-text);
  border: none;
  border-radius: 50px;
  padding: 16px;
  font-family: var(--font-body);
  font-size: 1.1rem;
  font-weight: 800;
  cursor: pointer;
  transition: transform 0.15s;
  display: flex; align-items: center; justify-content: center; gap: 8px;
}
.checkout-btn:hover { transform: translateY(-2px); }
</style>
