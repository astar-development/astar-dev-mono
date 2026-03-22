<template>
  <div :class="`theme-${currentTheme}`" class="app-root">

    <AppNav
      v-model="currentTheme"
      @open-basket="basketOpen = true"
    />

    <HeroSection @browse="scrollToShop" />

    <ProductGrid @added="onAdded" />

    <AppFooter />

    <BasketDrawer
      v-model="basketOpen"
      @checkout="openCheckout"
    />

    <CheckoutModal v-model="checkoutOpen" />

    <!-- Global toast -->
    <div class="toast" :class="{ show: toastVisible }">{{ toastMessage }}</div>

  </div>
</template>

<script setup>
import { ref } from 'vue'
import AppNav        from '@/components/AppNav.vue'
import HeroSection   from '@/components/HeroSection.vue'
import ProductGrid   from '@/components/ProductGrid.vue'
import AppFooter     from '@/components/AppFooter.vue'
import BasketDrawer  from '@/components/BasketDrawer.vue'
import CheckoutModal from '@/components/CheckoutModal.vue'
import { useToast }  from '@/composables/useToast'

const currentTheme = ref('playful')
const basketOpen   = ref(false)
const checkoutOpen = ref(false)

const { toastVisible, toastMessage, showToast } = useToast()

function scrollToShop() {
  document.getElementById('shop')?.scrollIntoView({ behavior: 'smooth' })
}

function onAdded(product) {
  showToast(`🎉 "${product.name}" added to basket!`)
}

function openCheckout() {
  basketOpen.value = false
  setTimeout(() => { checkoutOpen.value = true }, 200)
}
</script>

<style>
.app-root {
  font-family: var(--font-body);
  background: var(--bg);
  color: var(--text);
  min-height: 100vh;
  transition: background 0.4s, color 0.4s;
}
</style>
