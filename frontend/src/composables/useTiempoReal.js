import { onBeforeUnmount, watch } from 'vue'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'

// Una sola conexión SignalR para toda la aplicación (api: Servicios/TiempoReal.cs). El servidor solo avisa
// «esta subasta cambió»; cada pantalla vuelve a pedir sus datos. Si la conexión falla, la consulta
// periódica de cada pantalla (useSondeo) sigue funcionando.
const BASE = import.meta.env.VITE_API_URL ?? ''
const seguidas = new Map()
const listado = new Set()
let conexion = null
let arranque = null

function llamar(funciones) {
  funciones.forEach(f => f())
}

function obtener() {
  if (conexion) return conexion
  conexion = new HubConnectionBuilder()
    .withUrl(`${BASE}/api/tiempo-real`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
  conexion.on('cambio', id => llamar(seguidas.get(id) ?? []))
  conexion.on('listado', () => llamar(listado))
  // Al reconectar se vuelve a entrar a los grupos y se refrescan las pantallas, por si algo pasó mientras tanto.
  conexion.onreconnected(async () => {
    for (const id of seguidas.keys()) await conexion.invoke('Seguir', id)
    if (listado.size) await conexion.invoke('SeguirListado')
    seguidas.forEach(llamar)
    llamar(listado)
  })
  return conexion
}

async function conectada() {
  const c = obtener()
  if (c.state === HubConnectionState.Disconnected) {
    arranque ??= c.start().finally(() => { arranque = null })
  }
  if (arranque) await arranque
  return c
}

// Llama a alCambiar cada vez que la subasta cambia en el servidor. idRef puede cambiar (otra subasta);
// al salir de la pantalla, Vue ejecuta la limpieza del watch y se deja de seguir.
export function useSubastaEnVivo(idRef, alCambiar) {
  watch(idRef, async (nuevo, _viejo, alLimpiar) => {
    const id = Number(nuevo)
    if (!id) return
    if (!seguidas.has(id)) seguidas.set(id, new Set())
    seguidas.get(id).add(alCambiar)
    alLimpiar(() => {
      seguidas.get(id)?.delete(alCambiar)
      if (!seguidas.get(id)?.size) {
        seguidas.delete(id)
        if (conexion?.state === HubConnectionState.Connected) conexion.invoke('Dejar', id).catch(() => {})
      }
    })
    try {
      const c = await conectada()
      await c.invoke('Seguir', id)
    } catch {
      // Sin tiempo real: queda la consulta periódica.
    }
  }, { immediate: true })
}

// Llama a alCambiar (como máximo una vez por segundo) cuando cambia cualquier subasta.
export function useListadoEnVivo(alCambiar) {
  let espera = null
  const conPausa = () => {
    espera ??= setTimeout(() => { espera = null; alCambiar() }, 1000)
  }
  listado.add(conPausa)
  conectada().then(c => c.invoke('SeguirListado')).catch(() => {})
  onBeforeUnmount(() => {
    listado.delete(conPausa)
    clearTimeout(espera)
  })
}
