<script setup>
import { Gavel } from 'lucide-vue-next'

defineProps({ claro: { type: Boolean, default: false } })
</script>

<template>
  <RouterLink to="/" class="logo" :class="{ claro }" aria-label="Pújalo, ir al inicio">
    <span class="marca"><Gavel :size="20" :stroke-width="2.2" /></span>
    <span class="nombre">Pújalo</span>
  </RouterLink>
</template>

<style scoped>
.logo {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.marca {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  display: grid;
  place-items: center;
  background: var(--pj-primario);
  color: #fff;
}

.nombre {
  font-size: 24px;
  font-weight: 700;
  color: var(--pj-primario);
}

.claro .marca {
  background: #fff;
  color: var(--pj-primario);
}

.claro .nombre {
  color: #fff;
}
</style>
