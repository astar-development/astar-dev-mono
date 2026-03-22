import { ref, computed } from 'vue'

// Shared basket state across components
const basket = ref([])

export function useBasket() {
  const basketCount = computed(() =>
    basket.value.reduce((sum, i) => sum + i.qty, 0)
  )

  const basketTotal = computed(() =>
    basket.value.reduce((sum, i) => sum + i.price * i.qty, 0)
  )

  function isInBasket(id) {
    return basket.value.some(i => i.id === id)
  }

  function addToBasket(product) {
    const existing = basket.value.find(i => i.id === product.id)
    if (existing) {
      existing.qty++
    } else {
      basket.value.push({ ...product, qty: 1 })
    }
  }

  function updateQty(id, delta) {
    const item = basket.value.find(i => i.id === id)
    if (!item) return
    item.qty += delta
    if (item.qty <= 0) {
      basket.value = basket.value.filter(i => i.id !== id)
    }
  }

  function clearBasket() {
    basket.value = []
  }

  return { basket, basketCount, basketTotal, isInBasket, addToBasket, updateQty, clearBasket }
}
