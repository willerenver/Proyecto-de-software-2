import { expect, test } from '@playwright/test'
import { fileURLToPath } from 'node:url'
import { sesionDe, subastaActivaAjena } from './ayuda'

const foto = fileURLToPath(new URL('../src/assets/fotos/camara.jpg', import.meta.url))

test('cualquiera ve el listado y el detalle sin entrar', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Terminan pronto' })).toBeVisible()
  await page.locator('.card-subasta .titulo').first().click()
  await expect(page).toHaveURL(/\/subastas\/\d+$/)
  await expect(page.getByRole('heading', { name: 'Historial de pujas' })).toBeVisible()
})

test('pujar sin sesión lleva a iniciar sesión', async ({ page, request }) => {
  const s = await subastaActivaAjena(request, '')
  await page.goto(`/subastas/${s.id}`)
  await page.locator('.btn-pujar.px-4').click()
  await expect(page).toHaveURL(new RegExp(`iniciar-sesion\\?volver=(%2F|/)subastas(%2F|/)${s.id}`))
})

test('la búsqueda filtra el listado', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('searchbox', { name: 'Buscar' }).first().fill('guitarra')
  await page.getByRole('searchbox', { name: 'Buscar' }).first().press('Enter')
  await expect(page.getByRole('heading', { name: 'Resultados para «guitarra»' })).toBeVisible()
  await expect(page.locator('.card-subasta')).toHaveCount(1)
})

test.describe('con la sesión de María', () => {
  test.use({ storageState: sesionDe('maria') })

  test('publicar con foto, editar y retirar', async ({ page }) => {
    await page.goto('/publicar')
    await page.locator('input[type=file]').setInputFiles(foto)
    await page.locator('#titulo').fill('Cámara de prueba automática con estuche')
    await page.locator('#categoria').selectOption({ label: 'Fotografía › Cámaras de rollo' })
    await page.locator('#descripcion').fill('Publicada por las pruebas automáticas.')
    await page.locator('#salida').fill('5000')
    await page.getByRole('button', { name: 'Publicar subasta' }).click()

    await expect(page).toHaveURL(/\/subastas\/\d+$/)
    await expect(page.getByRole('heading', { name: 'Cámara de prueba automática con estuche' })).toBeVisible()
    await expect(page.locator('.foto-principal img')).toBeVisible()

    await page.getByRole('link', { name: 'Editar' }).click()
    await page.locator('#salida').fill('4500')
    await page.getByRole('button', { name: 'Guardar cambios' }).click()
    await expect(page.getByText('RD$ 4,500').first()).toBeVisible()

    await page.getByRole('button', { name: 'Retirar' }).click()
    await page.getByRole('button', { name: 'Sí, retirar' }).click()
    await expect(page.getByText('Esta subasta fue cancelada')).toBeVisible()
  })

  test('pujar con confirmación', async ({ page, request }) => {
    const s = await subastaActivaAjena(request, 'María Rodríguez')
    await page.goto(`/subastas/${s.id}`)
    await page.locator('.btn-pujar.px-4').click()
    await expect(page.getByRole('dialog', { name: 'Confirma tu puja' })).toBeVisible()
    await page.locator('.modal-pj .btn-pujar').click()

    await expect(page.getByRole('status')).toContainText(/Puja registrada|Te superaron al instante/)
    await expect(page.locator('#historial').getByText('Tú').first()).toBeVisible()
  })

  test('programar y desactivar una puja automática', async ({ page, request }) => {
    const s = await subastaActivaAjena(request, 'María Rodríguez')
    await page.goto(`/subastas/${s.id}`)
    // Si quedó activa de una corrida anterior, el botón dice «Cambiar».
    await page.getByRole('button', { name: /Puja automática: subir por mí|Cambiar/ }).click()
    const maximo = s.montoMinimo + s.incrementoMinimo * 10
    await page.locator('#maximo').fill(String(maximo))
    await page.getByRole('button', { name: 'Activar' }).click()

    // Si María no iba ganando, la automática puja por ella enseguida; si ya ganaba, no hace falta pujar.
    await expect(page.getByText(`Puja automática activa hasta RD$ ${maximo.toLocaleString('en-US')}`)).toBeVisible()
    await expect(page.getByText('Vas ganando').first()).toBeVisible()

    await page.getByRole('button', { name: 'Desactivar' }).click()
    await expect(page.getByText('Puja automática activa hasta')).toBeHidden()
  })

  test('la subasta ganada muestra el contacto del vendedor', async ({ page }) => {
    await page.goto('/mis-pujas')
    await page.getByRole('button', { name: /Ganadas/ }).click()
    await page.getByRole('link', { name: 'Coordinar' }).first().click()

    await expect(page.getByRole('heading', { name: 'Coordina el pago y la entrega' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Vendedor' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'WhatsApp' })).toHaveAttribute('href', /^https:\/\/wa\.me\/1809/)
  })

  test('un usuario normal no entra a administración', async ({ page }) => {
    await page.goto('/admin')
    await expect(page).toHaveURL('/')
  })
})

test.describe('con la sesión del administrador', () => {
  test.use({ storageState: sesionDe('admin') })

  test('administración muestra subastas, usuarios y categorías', async ({ page }) => {
    await page.goto('/admin')
    await expect(page.getByRole('heading', { name: 'Administración' })).toBeVisible()
    await page.getByRole('button', { name: /Usuarios/ }).click()
    await expect(page.getByRole('cell', { name: 'maria.r@elmejorpostor.test' })).toBeVisible()
    await page.getByRole('button', { name: /Categorías/ }).click()
    await expect(page.getByRole('cell', { name: 'Fotografía' })).toBeVisible()
  })
})
