<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppNavbar from './components/layout/AppNavbar.vue'
import AppFooter from './components/layout/AppFooter.vue'
import EncabezadoSimple from './components/layout/EncabezadoSimple.vue'
import BarraInferior from './components/layout/BarraInferior.vue'

const route = useRoute()
const simple = computed(() => route.meta.simple === true)
</script>

<template>
  <EncabezadoSimple v-if="simple" />
  <AppNavbar v-else />
  <main class="flex-grow-1">
    <RouterView />
  </main>
  <AppFooter v-if="!simple" />
  <BarraInferior v-if="!simple && !route.meta.cabeceraMovilPropia" />
</template>
