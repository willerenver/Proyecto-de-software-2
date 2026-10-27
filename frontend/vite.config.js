import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// En desarrollo, /api y /fotos van a la API de .NET (api/src/Pujalo.Api, perfil http).
const API = process.env.PUJALO_API ?? 'http://localhost:5230'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': API,
      '/fotos': API,
    },
  },
})
