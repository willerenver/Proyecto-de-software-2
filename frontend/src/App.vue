<script setup>
import { computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppNavbar from './components/layout/AppNavbar.vue'
import AppFooter from './components/layout/AppFooter.vue'
import EncabezadoSimple from './components/layout/EncabezadoSimple.vue'
import BarraInferior from './components/layout/BarraInferior.vue'
import { sesion } from './stores/sesion'

const route = useRoute()
const router = useRouter()
const simple = computed(() => route.meta.simple === true)

// Si la sesión vence o se cierra en una pantalla que la necesita, se pide entrar de nuevo.
watch(() => sesion.token, token => {
  if (!token && route.meta.requiereSesion) router.replace({ name: 'iniciar-sesion', query: { volver: route.fullPath } })
})
</script>

<template>
  <EncabezadoSimple v-if="simple" />
  <AppNavbar v-else />
  <main class="flex-grow-1">
    <!-- Publicar y Editar usan la misma vista: al cambiar de ruta se vuelve a montar con el formulario limpio. -->
    <RouterView :key="String(route.name)" />
  </main>
  <AppFooter v-if="!simple" />
  <BarraInferior v-if="!simple && !route.meta.cabeceraMovilPropia" />
</template>
