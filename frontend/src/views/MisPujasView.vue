<script setup>
import { computed, ref } from 'vue'
import { Image } from 'lucide-vue-next'
import EstadoBadge from '../components/EstadoBadge.vue'
import MenuCuenta from '../components/MenuCuenta.vue'
import { api, urlFoto } from '../api/cliente'
import { rd, tiempoEnPalabras, tiempoRestante } from '../utils/formato'
import { useAhora } from '../composables/useAhora'
import { useSondeo } from '../composables/useSondeo'
import { useListadoEnVivo } from '../composables/useTiempoReal'

const ahora = useAhora(30000)
const pestana = ref('activas')
const filas = ref([])
const cargando = ref(true)
const error = ref('')

// Una fila por subasta en la que el usuario pujó (GET /api/mis-pujas).
async function cargar() {
  try {
    filas.value = await api('/api/mis-pujas')
    error.value = ''
  } catch (e) {
    error.value = e.message
  } finally {
    cargando.value = false
  }
}
cargar()
useListadoEnVivo(cargar)
useSondeo(cargar, 60000)

const activas = computed(() => filas.value.filter(f => ['vas_ganando', 'superado', 'cerrando'].includes(f.situacion)))
const ganadas = computed(() => filas.value.filter(f => f.situacion === 'ganada'))
const perdidas = computed(() => filas.value.filter(f => f.situacion === 'perdida'))
const visibles = computed(() => ({ activas: activas.value, ganadas: ganadas.value, perdidas: perdidas.value })[pestana.value])

const ganando = computed(() => activas.value.filter(f => f.situacion === 'vas_ganando'))
const superado = computed(() => activas.value.filter(f => f.situacion === 'superado'))
const comprometido = computed(() => ganando.value.reduce((t, f) => t + f.pujaActual, 0))
const urgente = computed(() => superado.value[0])
const totalGanado = computed(() => ganadas.value.reduce((t, f) => t + f.pujaActual, 0))
const menosDeUnaHora = f => new Date(f.fechaCierre).getTime() - ahora.value < 3600000
</script>

<template>
  <div class="contenedor py-4">
    <div class="dos-columnas">
      <MenuCuenta />

      <div class="d-flex flex-column gap-3 gap-lg-4">
        <div class="d-flex justify-content-between align-items-center">
          <h1 class="fw-bold mb-0 titulo">Mis pujas</h1>
          <RouterLink to="/" class="btn btn-primary">Explorar subastas</RouterLink>
        </div>

        <div v-if="error" class="alert alert-danger mb-0">{{ error }}</div>

        <div v-if="urgente" class="alert alert-danger mb-0" role="alert">
          <strong class="d-block">Te superaron en «{{ urgente.titulo }}»</strong>
          La puja actual es {{ rd(urgente.pujaActual) }} y termina en {{ tiempoEnPalabras(urgente.fechaCierre, ahora) }}.
          <RouterLink :to="`/subastas/${urgente.subastaId}`" class="alert-link">Puja {{ rd(urgente.montoMinimo) }} o más</RouterLink> para volver a ganar.
        </div>

        <div class="resumen">
          <div class="panel"><small class="texto-gris">Pujas activas</small><div class="cifra">{{ activas.length }}</div><small class="texto-gris">subastas abiertas</small></div>
          <div class="panel"><small class="texto-gris">Vas ganando</small><div class="cifra text-success">{{ ganando.length }}</div><small class="texto-gris">{{ rd(comprometido) }} comprometidos</small></div>
          <div class="panel"><small class="texto-gris">Te superaron</small><div class="cifra text-danger">{{ superado.length }}</div><small class="texto-gris">{{ urgente ? `una termina en ${tiempoRestante(urgente.fechaCierre, ahora)}` : 'ninguna' }}</small></div>
          <div class="panel"><small class="texto-gris">Ganadas</small><div class="cifra">{{ ganadas.length }}</div><small class="texto-gris">{{ rd(totalGanado) }} en total</small></div>
        </div>

        <section class="panel p-0 overflow-hidden">
          <ul class="nav nav-tabs px-3 pt-3">
            <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'activas' }" @click="pestana = 'activas'">Activas ({{ activas.length }})</button></li>
            <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'ganadas' }" @click="pestana = 'ganadas'">Ganadas ({{ ganadas.length }})</button></li>
            <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'perdidas' }" @click="pestana = 'perdidas'">Perdidas ({{ perdidas.length }})</button></li>
          </ul>
          <div class="table-responsive">
            <table class="table align-middle mb-0">
              <thead>
                <tr><th>Artículo</th><th class="text-end">Mi puja</th><th class="text-end">Puja actual</th><th>Termina en</th><th>Estado</th><th /></tr>
              </thead>
              <tbody>
                <tr v-for="f in visibles" :key="f.subastaId">
                  <td>
                    <RouterLink :to="`/subastas/${f.subastaId}`" class="d-flex align-items-center gap-3 text-body">
                      <img v-if="f.foto" :src="urlFoto(f.foto)" alt="" class="miniatura" />
                      <span v-else class="miniatura sin-foto"><Image :size="20" /></span>
                      <div><div class="fw-medium">{{ f.titulo }}</div><small class="texto-gris">Subasta #{{ f.subastaId }}</small></div>
                    </RouterLink>
                  </td>
                  <td class="text-end">{{ rd(f.miPuja) }}</td>
                  <td class="text-end fw-bold">{{ rd(f.pujaActual) }}</td>
                  <td :class="{ 'text-danger fw-medium': pestana === 'activas' && menosDeUnaHora(f) }">{{ tiempoRestante(f.fechaCierre, ahora) }}</td>
                  <td>
                    <EstadoBadge v-if="f.situacion === 'vas_ganando'" estado="Vas ganando" />
                    <EstadoBadge v-else-if="f.situacion === 'superado'" estado="Superado" />
                    <EstadoBadge v-else-if="f.situacion === 'cerrando'" estado="Pendiente" texto="Cerrando" />
                    <EstadoBadge v-else-if="f.situacion === 'ganada'" estado="Activa" texto="Ganada" />
                    <EstadoBadge v-else estado="Neutra" texto="Perdida" />
                  </td>
                  <td class="text-end">
                    <RouterLink v-if="f.situacion === 'ganada'" :to="`/subastas/${f.subastaId}/pagar`" class="btn btn-sm btn-primary">Coordinar</RouterLink>
                    <RouterLink v-else-if="f.situacion === 'superado'" :to="`/subastas/${f.subastaId}`" class="btn btn-sm btn-pujar">Pujar</RouterLink>
                    <RouterLink v-else :to="`/subastas/${f.subastaId}`" class="btn btn-sm btn-light">Ver</RouterLink>
                  </td>
                </tr>
                <tr v-if="!visibles.length">
                  <td colspan="6" class="texto-gris py-4 text-center">{{ cargando ? 'Cargando…' : 'No tienes subastas en esta pestaña.' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dos-columnas {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  gap: 24px;
  align-items: start;
}

.titulo {
  font-size: 30px;
}

.resumen {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.resumen .panel {
  padding: 18px;
}

.cifra {
  font-size: 28px;
  font-weight: 700;
}

thead th {
  background: var(--mp-claro);
  color: var(--mp-gris);
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
}

.sin-foto {
  display: grid;
  place-items: center;
  background: #e9ecef;
  color: var(--mp-gris);
}

.miniatura {
  width: 48px;
  height: 48px;
  border-radius: 6px;
  object-fit: cover;
}

@media (max-width: 991.98px) {
  .dos-columnas {
    grid-template-columns: 1fr;
  }

  .titulo {
    font-size: 24px;
  }

  .resumen {
    grid-template-columns: repeat(2, 1fr);
    gap: 12px;
  }
}
</style>
