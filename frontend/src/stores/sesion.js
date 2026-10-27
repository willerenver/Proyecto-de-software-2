import { reactive } from 'vue'

// «Recordarme» guarda la sesión en localStorage; si no, dura lo que la pestaña (sessionStorage).
const CLAVE = 'pujalo.sesion'

function almacenes() {
  try {
    return [localStorage, sessionStorage]
  } catch {
    return []
  }
}

function leer() {
  for (const almacen of almacenes()) {
    try {
      const guardada = JSON.parse(almacen.getItem(CLAVE))
      if (guardada?.token && new Date(guardada.expira) > new Date()) return guardada
    } catch {
      // Sesión ilegible: se ignora.
    }
  }
  return null
}

const guardada = leer()

export const sesion = reactive({
  token: guardada?.token ?? null,
  expira: guardada?.expira ?? null,
  usuario: guardada?.usuario ?? null,
})

export function iniciarSesion({ token, expira, usuario }, recordar = false) {
  Object.assign(sesion, { token, expira, usuario })
  const [local, pestana] = almacenes()
  try {
    ;(recordar ? local : pestana)?.setItem(CLAVE, JSON.stringify({ token, expira, usuario }))
  } catch {
    // Sin almacenamiento la sesión dura hasta recargar.
  }
}

export function cerrarSesion() {
  Object.assign(sesion, { token: null, expira: null, usuario: null })
  for (const almacen of almacenes()) {
    try {
      almacen.removeItem(CLAVE)
    } catch {
      // Nada que borrar.
    }
  }
}

export const esAdministrador = () => sesion.usuario?.rol === 'administrador'

// Después de entrar se vuelve a la pantalla de origen, solo si es una ruta interna.
export function rutaDeRegreso(volver) {
  return typeof volver === 'string' && volver.startsWith('/') && !volver.startsWith('//') ? volver : '/'
}
