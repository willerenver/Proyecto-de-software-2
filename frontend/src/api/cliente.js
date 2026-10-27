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

export async function api(ruta, { metodo = 'GET', cuerpo, formulario } = {}) {
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
  return datos
}

// Las rutas de las fotos llegan como «/fotos/abc.jpg»; las vistas previas locales son «blob:…».
export function urlFoto(ruta) {
  if (!ruta) return null
  return ruta.startsWith('/') ? `${BASE}${ruta}` : ruta
}
