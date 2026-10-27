import { ref } from 'vue'
import { api } from '../api/cliente'

// Las categorías casi no cambian: se piden una vez por visita.
export const categorias = ref([])
let pedido = null

export function cargarCategorias() {
  pedido ??= api('/api/categorias')
    .then(lista => { categorias.value = lista })
    .catch(e => { pedido = null; throw e })
  return pedido
}

export const nombreCategoria = id => categorias.value.find(c => c.id === Number(id))?.nombre ?? ''
