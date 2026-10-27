import { expect, test } from '@playwright/test'

test('crear una cuenta deja la sesión abierta', async ({ page }) => {
  const sufijo = Date.now().toString().slice(-8)
  await page.goto('/crear-cuenta')
  await page.locator('#nombre').fill('Prueba')
  await page.locator('#apellido').fill('Automática')
  await page.locator('#correo').fill(`prueba${sufijo}@pujalo.test`)
  await page.locator('#telefono').fill('809-555-0199')
  await page.locator('#cedula').fill(`402${sufijo}`)
  await page.locator('#contrasena').fill('clave12345')
  await page.locator('#acepta').check()
  await page.getByRole('button', { name: 'Crear cuenta' }).click()

  await expect(page).toHaveURL('/')
  await expect(page.getByRole('button', { name: 'Mi cuenta' })).toBeVisible()
})

test('el registro valida los datos antes de enviarlos', async ({ page }) => {
  await page.goto('/crear-cuenta')
  await page.locator('#cedula').fill('123')
  await page.getByRole('button', { name: 'Crear cuenta' }).click()

  await expect(page.getByText('La cédula tiene 11 dígitos')).toBeVisible()
  await expect(page.getByText('Debes aceptar los términos')).toBeVisible()
})

test('una contraseña equivocada no deja entrar', async ({ page }) => {
  await page.goto('/iniciar-sesion')
  await page.locator('#correo').fill('maria.r@pujalo.test')
  await page.locator('#contrasena').fill('no-es-esta-1')
  await page.getByRole('button', { name: 'Entrar' }).click()

  await expect(page.getByRole('alert')).toContainText('El correo o la contraseña no son correctos')
})

test('las pantallas privadas piden entrar y luego regresan', async ({ page }) => {
  await page.goto('/mis-pujas')
  await expect(page).toHaveURL(/iniciar-sesion\?volver=(%2F|\/)mis-pujas/)
})
