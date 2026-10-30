import { expect, test } from '@playwright/test'

test('en el móvil se navega con la barra inferior y los chips', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('navigation', { name: 'Navegación principal' })).toBeVisible()
  await page.locator('.chips').getByRole('button', { name: 'Videojuegos' }).click()
  await expect(page).toHaveURL(/categoria=3/)
  await expect(page.locator('.card-subasta').first()).toBeVisible()
  await page.locator('.card-subasta .titulo').first().click()
  await expect(page.locator('.barra-puja')).toBeVisible()
})
