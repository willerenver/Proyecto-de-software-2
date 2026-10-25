import { onBeforeUnmount, onMounted, ref } from 'vue'

export function useAhora(cadaMs = 1000) {
  const ahora = ref(Date.now())
  let reloj
  onMounted(() => { reloj = setInterval(() => { ahora.value = Date.now() }, cadaMs) })
  onBeforeUnmount(() => clearInterval(reloj))
  return ahora
}
