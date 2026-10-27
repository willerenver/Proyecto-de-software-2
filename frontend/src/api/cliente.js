import { cerrarSesion, sesion } from '../stores/sesion'

// En desarrollo queda vacío y Vite reenvía /api y /fotos a la API (vite.config.js).
const BASE = import.meta.env.VITE_API_URL ?? ''

export class ErrorApi extends Error {
  constructor(mensaje, estado, errores = {}) {
    super(mensaje)
    this.estado = estado
    // Errores por campo, con el nombre del campo en camelCase: { correo: 'Ya existe…' }.
    this.errores = errores
  }
}

// Con conTotal devuelve { datos, total }, con el total de la cabecera X-Total de los listados paginados.
export async function api(ruta, { metodo = 'GET', cuerpo, formulario, conTotal = false } = {}) {
  const headers = {}
  if (sesion.token) headers.Authorization = `Bearer ${sesion.token}`
  let body
  if (formulario) {
    body = formulario
  } else if (cuerpo !== undefined) {
    headers['Content-Type'] = 'application/json'
    body = JSON.stringify(cuerpo)
  }

  let respuesta
  try {
    respuesta = await fetch(`${BASE}${ruta}`, { method: metodo, headers, body })
  } catch {
    throw new ErrorApi('No pudimos conectar con el servidor. Revisa tu conexión e intenta de nuevo.', 0)
  }

  if (respuesta.status === 401 && sesion.token) {
    cerrarSesion()
    throw new ErrorApi('Tu sesión venció. Inicia sesión de nuevo.', 401)
  }
  if (respuesta.status === 204) return null

  const datos = await respuesta.json().catch(() => null)
  if (!respuesta.ok) {
    const errores = {}
    for (const [campo, mensajes] of Object.entries(datos?.errors ?? {})) {
      const nombre = campo.replace(/^\$\./, '')
      errores[nombre.charAt(0).toLowerCase() + nombre.slice(1)] = mensajes[0]
    }
    throw new ErrorApi(datos?.title ?? 'Ocurrió un error inesperado. Intenta de nuevo en unos minutos.', respuesta.status, errores)
  }
  if (conTotal) return { datos, total: Number(respuesta.headers.get('X-Total') ?? datos?.length ?? 0) }
  return datos
}

// Pide las páginas 1..paginas de un listado paginado y las une sin repetir (por id). Así, al refrescar,
// no se pierde lo que ya se cargó con «Ver más».
export async function paginas(ruta, cuantas, porPagina = 24) {
  const separador = ruta.includes('?') ? '&' : '?'
  const respuestas = await Promise.all(Array.from({ length: cuantas }, (_, i) =>
    api(`${ruta}${separador}pagina=${i + 1}&porPagina=${porPagina}`, { conTotal: true })))
  const vistos = new Set()
  const datos = respuestas.flatMap(r => r.datos).filter(x => !vistos.has(x.id) && vistos.add(x.id))
  return { datos, total: respuestas[0]?.total ?? 0 }
}

// Las rutas de las fotos llegan como «/fotos/abc.jpg»; las vistas previas locales son «blob:…».
export function urlFoto(ruta) {
  if (!ruta) return null
  return ruta.startsWith('/') ? `${BASE}${ruta}` : ruta
}
