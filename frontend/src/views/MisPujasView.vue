<script setup>
import { computed, ref } from 'vue'
import AvatarIniciales from '../components/AvatarIniciales.vue'
import EstadoBadge from '../components/EstadoBadge.vue'
import { pujaActual, subastas, usuarioActual } from '../data/subastas'
import { ChevronLeft, ChevronRight, Image } from 'lucide-vue-next'
import { rd, tiempoEnPalabras, tiempoRestante } from '../utils/formato'
import { useAhora } from '../composables/useAhora'

const ahora = useAhora(30000)
const pestana = ref('activas')

// Una fila por subasta en la que el usuario pujó, con su puja más alta.
const filas = computed(() =>
  subastas
    .map(s => {
      const mias = s.pujas.filter(p => p.postor === usuarioActual.alias)
      if (!mias.length) return null
      const cerrada = new Date(s.fechaCierre).getTime() <= ahora.value
      const gana = s.pujas[s.pujas.length - 1].postor === usuarioActual.alias
      return { s, miPuja: Math.max(...mias.map(p => p.monto)), actual: pujaActual(s), cerrada, gana }
    })
    .filter(Boolean)
    .sort((a, b) => new Date(a.s.fechaCierre) - new Date(b.s.fechaCierre)),
)
const activas = computed(() => filas.value.filter(f => !f.cerrada))
const ganadas = computed(() => filas.value.filter(f => f.cerrada && f.gana))
const perdidas = computed(() => filas.value.filter(f => f.cerrada && !f.gana))
const visibles = computed(() => ({ activas: activas.value, ganadas: ganadas.value, perdidas: perdidas.value })[pestana.value])

const ganando = computed(() => activas.value.filter(f => f.gana))
const superado = computed(() => activas.value.filter(f => !f.gana))
const comprometido = computed(() => ganando.value.reduce((t, f) => t + f.actual, 0))
const urgente = computed(() => superado.value[0])

const totalGanado = computed(() => ganadas.value.reduce((t, f) => t + f.actual, 0))
const nombrePestana = { activas: 'activas', ganadas: 'ganadas', perdidas: 'perdidas' }

const menu = computed(() => [
  ['Resumen'], ['Mis pujas', 'Neutra', activas.value.length], ['Subastas ganadas', 'Neutra', ganadas.value.length], ['Mis ventas'], ['Favoritos'],
  ['Mensajes', 'Cierra pronto', 3], ['Datos de pago'], ['Configuración'], ['Cerrar sesión'],
])
const menosDeUnaHora = f => new Date(f.s.fechaCierre).getTime() - ahora.value < 3600000
</script>

<template>
  <div class="contenedor py-4">
    <div class="dos-columnas">
      <nav class="panel p-0 menu d-none d-lg-block" aria-label="Mi cuenta">
        <div class="d-flex align-items-center gap-3 p-3 border-bottom">
          <AvatarIniciales :iniciales="usuarioActual.iniciales" :tam="44" />
          <div>
            <div class="fw-bold">{{ usuarioActual.nombre }} {{ usuarioActual.apellido }}</div>
            <small class="texto-gris">Miembro desde {{ usuarioActual.desde }}</small>
          </div>
        </div>
        <a v-for="[t, estilo, n] in menu" :key="t" href="#" class="item" :class="{ activo: t === 'Mis pujas', salir: t === 'Cerrar sesión' }" @click.prevent>
          {{ t }}
          <EstadoBadge v-if="estilo" :estado="estilo" :texto="String(n)" />
        </a>
      </nav>

      <div class="d-flex flex-column gap-3 gap-lg-4">
        <div class="d-flex justify-content-between align-items-center">
          <h1 class="fw-bold mb-0 titulo">Mis pujas</h1>
          <RouterLink to="/" class="btn btn-primary">Explorar subastas</RouterLink>
        </div>

        <div v-if="urgente" class="alert alert-danger mb-0" role="alert">
          <strong class="d-block">Te superaron en «{{ urgente.s.titulo }}»</strong>
          La puja actual es {{ rd(urgente.actual) }} y termina en {{ tiempoEnPalabras(urgente.s.fechaCierre, ahora) }}.
          Puja {{ rd(urgente.actual + urgente.s.incrementoMinimo) }} o más para volver a ganar.
        </div>

        <div class="resumen">
          <div class="panel"><small class="texto-gris">Pujas activas</small><div class="cifra">{{ activas.length }}</div><small class="texto-gris">en {{ activas.length }} subastas</small></div>
          <div class="panel"><small class="texto-gris">Vas ganando</small><div class="cifra text-success">{{ ganando.length }}</div><small class="texto-gris">{{ rd(comprometido) }} comprometidos</small></div>
          <div class="panel"><small class="texto-gris">Te superaron</small><div class="cifra text-danger">{{ superado.length }}</div><small class="texto-gris">{{ urgente ? `una termina en ${tiempoRestante(urgente.s.fechaCierre, ahora)}` : 'ninguna' }}</small></div>
          <div class="panel"><small class="texto-gris">Ganadas este mes</small><div class="cifra">{{ ganadas.length }}</div><small class="texto-gris">{{ rd(totalGanado) }} en total</small></div>
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
                <tr v-for="f in visibles" :key="f.s.id">
                  <td>
                    <RouterLink :to="`/subastas/${f.s.id}`" class="d-flex align-items-center gap-3 text-body">
                      <img v-if="f.s.foto" :src="f.s.foto" alt="" class="miniatura" />
                      <span v-else class="miniatura sin-foto"><Image :size="20" /></span>
                      <div><div class="fw-medium">{{ f.s.titulo }}</div><small class="texto-gris">Subasta #{{ f.s.id }}</small></div>
                    </RouterLink>
                  </td>
                  <td class="text-end">{{ rd(f.miPuja) }}</td>
                  <td class="text-end fw-bold">{{ rd(f.actual) }}</td>
                  <td :class="{ 'text-danger fw-medium': !f.cerrada && menosDeUnaHora(f) }">{{ tiempoRestante(f.s.fechaCierre, ahora) }}</td>
                  <td>
                    <EstadoBadge v-if="!f.cerrada" :estado="f.gana ? 'Vas ganando' : 'Superado'" />
                    <EstadoBadge v-else-if="f.gana" estado="Pendiente" texto="Pago pendiente" />
                    <EstadoBadge v-else estado="Neutra" texto="Perdida" />
                  </td>
                  <td class="text-end">
                    <RouterLink v-if="f.cerrada && f.gana" :to="`/subastas/${f.s.id}/pagar`" class="btn btn-sm btn-primary">Pagar</RouterLink>
                    <RouterLink v-else-if="f.cerrada" :to="`/subastas/${f.s.id}`" class="btn btn-sm btn-light">Ver</RouterLink>
                    <RouterLink v-else :to="`/subastas/${f.s.id}`" class="btn btn-sm" :class="f.gana ? 'btn-light' : 'btn-pujar'">{{ f.gana ? 'Ver' : 'Pujar' }}</RouterLink>
                  </td>
                </tr>
                <tr v-if="!visibles.length"><td colspan="6" class="texto-gris py-4 text-center">No tienes subastas en esta pestaña.</td></tr>
              </tbody>
            </table>
          </div>
          <div class="d-flex justify-content-between align-items-center p-3">
            <span class="texto-gris texto-14">Mostrando {{ visibles.length }} de {{ visibles.length }} subastas {{ nombrePestana[pestana] }}</span>
            <nav aria-label="Páginas">
              <ul class="pagination mb-0 gap-1">
                <li class="page-item disabled"><span class="page-link"><ChevronLeft :size="16" /></span></li>
                <li class="page-item active"><span class="page-link">1</span></li>
                <li class="page-item disabled"><span class="page-link"><ChevronRight :size="16" /></span></li>
              </ul>
            </nav>
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

.menu {
  overflow: hidden;
  padding-bottom: 8px !important;
}

.item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  color: var(--pj-texto);
}

.item.activo {
  background: var(--pj-primario-suave);
  color: var(--pj-primario);
  font-weight: 500;
}

.item.salir {
  color: var(--pj-gris);
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
  background: var(--pj-claro);
  color: var(--pj-gris);
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
}

.texto-14 {
  font-size: 14px;
}

.pagination .page-link {
  width: 38px;
  height: 38px;
  display: grid;
  place-items: center;
  padding: 0;
  border-radius: 6px !important;
}

.sin-foto {
  display: grid;
  place-items: center;
  background: #e9ecef;
  color: var(--pj-gris);
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
