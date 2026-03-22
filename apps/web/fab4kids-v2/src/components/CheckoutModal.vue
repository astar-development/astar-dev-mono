<template>
  <Teleport to="body">
    <div class="modal-wrap" v-if="modelValue" @click.self="$emit('update:modelValue', false)">
      <div class="modal">
        <div class="modal-header">
          <h2>{{ orderPlaced ? '🎉 Order Placed!' : '🔒 Checkout' }}</h2>
          <p>{{ orderPlaced ? 'Happy learning!' : 'Secure & simple' }}</p>
        </div>

        <div class="modal-body">
          <!-- SUCCESS STATE -->
          <div v-if="orderPlaced" class="success-state">
            <div class="success-icon">🌟</div>
            <h3>Thank you, {{ firstName }}!</h3>
            <p>Your resources are on their way. Check your inbox at <strong>{{ form.email }}</strong> for download links and delivery details.</p>
            <button class="place-order-btn" @click="finish">✓ Continue Shopping</button>
          </div>

          <!-- FORM -->
          <div v-else>
            <div class="order-summary">
              <div class="order-summary-title">📋 Order Summary</div>
              <div v-for="item in basket" :key="item.id" class="summary-item">
                <span class="summary-item-name">{{ item.name }} × {{ item.qty }}</span>
                <span>£{{ (item.price * item.qty).toFixed(2) }}</span>
              </div>
              <div class="summary-item total-line">
                <span>Total</span>
                <span>£{{ basketTotal.toFixed(2) }}</span>
              </div>
            </div>

            <div class="form-row">
              <label class="form-label">Full Name</label>
              <input class="form-input" v-model="form.name" placeholder="Jane Smith" />
            </div>
            <div class="form-row">
              <label class="form-label">Email Address</label>
              <input class="form-input" v-model="form.email" placeholder="jane@example.com" type="email" />
            </div>
            <div class="form-row">
              <label class="form-label">Card Number</label>
              <input class="form-input" v-model="form.card" placeholder="4242 4242 4242 4242" maxlength="19" @input="formatCard" />
            </div>
            <div class="form-row form-row-2">
              <div>
                <label class="form-label">Expiry</label>
                <input class="form-input" v-model="form.expiry" placeholder="MM/YY" maxlength="5" />
              </div>
              <div>
                <label class="form-label">CVV</label>
                <input class="form-input" v-model="form.cvv" placeholder="123" maxlength="3" type="password" />
              </div>
            </div>

            <button class="place-order-btn" @click="placeOrder">
              🌟 Place Order — £{{ basketTotal.toFixed(2) }}
            </button>
            <span class="cancel-link" @click="$emit('update:modelValue', false)">← Back to basket</span>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useBasket } from '@/composables/useBasket'
import { useToast } from '@/composables/useToast'

const props = defineProps({ modelValue: Boolean })
const emit = defineEmits(['update:modelValue'])

const { basket, basketTotal, clearBasket } = useBasket()
const { showToast } = useToast()

const orderPlaced = ref(false)
const form = ref({ name: '', email: '', card: '', expiry: '', cvv: '' })

const firstName = computed(() => form.value.name.split(' ')[0] || 'Explorer')

function formatCard() {
  form.value.card = form.value.card.replace(/\D/g, '').substring(0, 16).replace(/(.{4})/g, '$1 ').trim()
}

function placeOrder() {
  if (!form.value.name || !form.value.email) {
    showToast('Please fill in your name and email!')
    return
  }
  orderPlaced.value = true
}

function finish() {
  clearBasket()
  emit('update:modelValue', false)
  orderPlaced.value = false
  form.value = { name: '', email: '', card: '', expiry: '', cvv: '' }
  showToast('✨ Thanks for your order! Happy learning!')
}
</script>

<style scoped>
.modal-wrap {
  position: fixed; inset: 0;
  z-index: 300;
  display: flex; align-items: center; justify-content: center;
  padding: 24px;
  background: rgba(0,0,0,0.5);
  backdrop-filter: blur(6px);
  animation: fadeIn 0.2s;
}
.modal {
  background: var(--surface);
  border-radius: 28px;
  width: 100%;
  max-width: 480px;
  overflow: hidden;
  animation: popIn 0.3s cubic-bezier(0.34,1.56,0.64,1);
  max-height: 90vh;
  overflow-y: auto;
}
.modal-header {
  background: var(--hero-bg);
  color: var(--hero-text);
  padding: 28px 28px 32px;
  text-align: center;
}
.modal-header h2 { font-family: var(--font-display); font-size: 1.8rem; margin-bottom: 4px; }
.modal-header p { opacity: 0.9; font-size: 0.95rem; }
.modal-body { padding: 28px; }

.success-state { text-align: center; padding: 8px 0; }
.success-icon { font-size: 4rem; margin-bottom: 12px; animation: bounce 0.5s ease; }
.success-state h3 { font-family: var(--font-display); font-size: 1.5rem; color: var(--text); margin-bottom: 8px; }
.success-state p { color: var(--text2); font-size: 0.95rem; margin-bottom: 24px; line-height: 1.5; }

.order-summary {
  background: var(--bg2);
  border-radius: 14px;
  padding: 14px 16px;
  margin-bottom: 20px;
}
.order-summary-title { font-weight: 800; font-size: 0.85rem; color: var(--text2); margin-bottom: 10px; text-transform: uppercase; letter-spacing: 0.5px; }
.summary-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
  color: var(--text);
  padding: 4px 0;
  border-bottom: 1px solid var(--border);
  gap: 8px;
}
.summary-item:last-child { border-bottom: none; }
.total-line { font-weight: 800; font-size: 1rem; color: var(--accent1); margin-top: 4px; }
.summary-item-name { flex: 1; min-width: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.form-row { margin-bottom: 14px; }
.form-label { display: block; font-weight: 700; font-size: 0.85rem; color: var(--text2); margin-bottom: 5px; text-transform: uppercase; letter-spacing: 0.5px; }
.form-input {
  width: 100%;
  padding: 12px 16px;
  border: 2px solid var(--border);
  border-radius: 12px;
  font-family: var(--font-body);
  font-size: 0.95rem;
  background: var(--bg);
  color: var(--text);
  transition: border-color 0.2s;
  outline: none;
}
.form-input:focus { border-color: var(--accent1); }
.form-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

.place-order-btn {
  width: 100%;
  background: var(--btn-bg);
  color: var(--btn-text);
  border: none;
  border-radius: 50px;
  padding: 16px;
  font-family: var(--font-body);
  font-size: 1.05rem;
  font-weight: 800;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center; gap: 8px;
  transition: transform 0.15s;
}
.place-order-btn:hover { transform: translateY(-2px); }
.cancel-link {
  display: block;
  text-align: center;
  margin-top: 12px;
  color: var(--text2);
  cursor: pointer;
  font-size: 0.88rem;
  font-weight: 600;
  transition: color 0.15s;
}
.cancel-link:hover { color: var(--accent1); }

@keyframes bounce { 0%,100%{transform:scale(1)} 50%{transform:scale(1.3)} }
</style>
