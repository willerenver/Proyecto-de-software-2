import { defineConfig, devices } from '@playwright/test'

// Pruebas de punta a punta contra la aplicación levantada (por defecto, docker compose en el puerto 8080).
// Cambian datos: corre «docker compose down -v && docker compose up -d» para empezar de cero.
export default defineConfig({
  testDir: './e2e',
  workers: 1,
  timeout: 30000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.PUJALO_URL ?? 'http://localhost:8080',
    locale: 'es-DO',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'sesiones', testMatch: /sesiones\.setup\.js/ },
    { name: 'escritorio', use: { ...devices['Desktop Chrome'] }, dependencies: ['sesiones'], testIgnore: /movil\.spec\.js/ },
    { name: 'movil', use: { ...devices['Pixel 7'] }, dependencies: ['sesiones'], testMatch: /movil\.spec\.js/ },
  ],
})
