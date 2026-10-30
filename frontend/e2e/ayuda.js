import { expect } from '@playwright/test'

export const CONTRASENA = 'Demo-ElMejorPostor-2026'
export const sesionDe = usuario => `e2e/.sesiones/${usuario}.json`

// Entra con «Recordarme» para que la sesión quede en localStorage y se pueda guardar.
export async function entrar(page, correo, contrasena = CONTRASENA) {
  await page.goto('/iniciar-sesion')
  await page.locator('#correo').fill(correo)
  await page.locator('#contrasena').fill(contrasena)
  await page.getByLabel('Recordarme').check()
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page).not.toHaveURL(/iniciar-sesion/)
}

// Primera subasta activa que no es de ese vendedor.
export async function subastaActivaAjena(request, vendedor) {
  const subastas = await (await request.get('/api/subastas?porPagina=50')).json()
  for (const s of subastas) {
    const detalle = await (await request.get(`/api/subastas/${s.id}`)).json()
    if (detalle.vendedor.nombre !== vendedor && detalle.aceptaPujas) return detalle
  }
  throw new Error('No hay subastas activas de otro vendedor')
}
