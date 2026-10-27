import { onBeforeUnmount, onMounted } from 'vue'

// Vuelve a pedir los datos cada cierto tiempo mientras la pantalla está abierta y visible,
// para ver las pujas de los demás sin recargar.
export function useSondeo(funcion, cadaMs) {
  let reloj
  onMounted(() => {
    reloj = setInterval(() => {
      if (document.visibilityState === 'visible') funcion()
    }, cadaMs)
  })
  onBeforeUnmount(() => clearInterval(reloj))
}
