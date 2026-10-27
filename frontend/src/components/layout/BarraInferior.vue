<script setup>
import { computed, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { Gavel, House, Plus, Search, User } from 'lucide-vue-next'
import { sesion } from '../../stores/sesion'

const router = useRouter()

const items = computed(() => [
  { to: '/', texto: 'Inicio', icono: House },
  { to: '/publicar', texto: 'Vender', icono: Plus },
  { to: '/mis-pujas', texto: 'Mis pujas', icono: Gavel },
  { to: sesion.token ? '/mis-ventas' : '/iniciar-sesion', texto: 'Cuenta', icono: User },
])


async function buscar() {
  await router.push('/')
  await nextTick()
  const campo = document.querySelector('.buscador-movil input')
  campo?.scrollIntoView({ block: 'center' })
  campo?.focus()
}
</script>

<template>
  <nav class="barra-inferior d-lg-none" aria-label="Navegación principal">
    <RouterLink :to="items[0].to" class="item" exact-active-class="activo"><House :size="22" /><span>Inicio</span></RouterLink>
    <button type="button" class="item" @click="buscar"><Search :size="22" /><span>Buscar</span></button>
    <RouterLink v-for="i in items.slice(1)" :key="i.texto" :to="i.to" class="item" exact-active-class="activo">
      <component :is="i.icono" :size="22" />
      <span>{{ i.texto }}</span>
    </RouterLink>
  </nav>
  <div class="d-lg-none espacio" />
</template>

<style scoped>
.barra-inferior {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 1030;
  display: flex;
  justify-content: space-between;
  padding: 8px 8px 24px;
  background: #fff;
  border-top: 1px solid var(--pj-borde);
}

.item {
  width: 66px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  padding: 0;
  border: 0;
  background: none;
  font-size: 11px;
  color: var(--pj-gris);
}

.item.activo {
  color: var(--pj-primario);
  font-weight: 500;
}

.espacio {
  height: 80px;
}
</style>
