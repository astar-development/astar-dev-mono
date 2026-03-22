import { ref } from 'vue'

const toastVisible = ref(false)
const toastMessage = ref('')
let toastTimer = null

export function useToast() {
  function showToast(msg) {
    toastMessage.value = msg
    toastVisible.value = true
    clearTimeout(toastTimer)
    toastTimer = setTimeout(() => { toastVisible.value = false }, 2500)
  }

  return { toastVisible, toastMessage, showToast }
}
