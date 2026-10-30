import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// En desarrollo, /api y /fotos van a la API de .NET (api/src/ElMejorPostor.Api, perfil http).
const API = process.env.ELMEJORPOSTOR_API ?? 'http://localhost:5230'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': { target: API, ws: true },
      '/fotos': API,
    },
  },
})
