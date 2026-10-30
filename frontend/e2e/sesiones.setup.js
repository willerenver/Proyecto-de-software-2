import { test as setup } from '@playwright/test'
import { readFile } from 'node:fs/promises'
import { entrar, sesionDe } from './ayuda'

// La API limita los inicios de sesión por minuto: se reutiliza la sesión guardada mientras siga vigente
// y solo se vuelve a entrar si venció o no existe.
async function sesionVigente(request, archivo) {
  try {
    const estado = JSON.parse(await readFile(archivo, 'utf8'))
    const guardada = estado.origins.flatMap(o => o.localStorage).find(x => x.name === 'elmejorpostor.sesion')
    const { token } = JSON.parse(guardada.value)
    return (await request.get('/api/auth/yo', { headers: { Authorization: `Bearer ${token}` } })).ok()
  } catch {
    return false
  }
}

for (const [usuario, correo] of [['maria', 'maria.r@elmejorpostor.test'], ['admin', 'admin@elmejorpostor.test']]) {
  setup(`sesión de ${usuario}`, async ({ page, request }) => {
    if (await sesionVigente(request, sesionDe(usuario))) return
    await entrar(page, correo)
    await page.context().storageState({ path: sesionDe(usuario) })
  })
}
