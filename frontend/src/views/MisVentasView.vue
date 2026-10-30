<script setup>
import { computed, ref } from 'vue'
import { Image } from 'lucide-vue-next'
import EstadoBadge from '../components/EstadoBadge.vue'
import MenuCuenta from '../components/MenuCuenta.vue'
import { api, urlFoto } from '../api/cliente'
import { diaCorto, fechaConHora, rd, tiempoRestante } from '../utils/formato'
import { useAhora } from '../composables/useAhora'
import { useSondeo } from '../composables/useSondeo'
import { useListadoEnVivo } from '../composables/useTiempoReal'

const ahora = useAhora(30000)
const filas = ref([])
const cargando = ref(true)
const error = ref('')

// Las subastas que publicó el usuario (GET /api/mis-ventas).
async function cargar() {
  try {
    filas.value = await api('/api/mis-ventas')
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

const abiertas = computed(() => filas.value.filter(f => ['Programada', 'Activa'].includes(f.estado)))
const vendidas = computed(() => filas.value.filter(f => f.estado === 'Finalizada'))
const totalVendido = computed(() => vendidas.value.reduce((t, f) => t + f.pujaActual, 0))
const pujasRecibidas = computed(() => abiertas.value.reduce((t, f) => t + f.numeroPujas, 0))

const ESTADOS = {
  Programada: ['Pendiente', 'Programada'],
  Activa: ['Activa', 'Activa'],
  Finalizada: ['Vas ganando', 'Vendida'],
  Desierta: ['Neutra', 'Sin ganador'],
  Cancelada: ['Superado', 'Cancelada'],
}
const porConfirmar = ref(null)

async function confirmarPago(id) {
  try {
    await api(`/api/subastas/${id}/confirmar-pago`, { metodo: 'POST' })
    porConfirmar.value = null
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}

const enCurso = f => ['Programada', 'Activa'].includes(f.estado) && new Date(f.fechaCierre).getTime() > ahora.value
</script>

<template>
  <div class="contenedor py-4">
    <div class="dos-columnas">
      <MenuCuenta />

      <div class="d-flex flex-column gap-3 gap-lg-4">
        <div class="d-flex justify-content-between align-items-center">
          <h1 class="fw-bold mb-0 titulo">Mis ventas</h1>
          <RouterLink to="/publicar" class="btn btn-primary">Publicar un artículo</RouterLink>
        </div>

        <div v-if="error" class="alert alert-danger mb-0">{{ error }}</div>

        <div class="resumen">
          <div class="panel"><small class="texto-gris">En subasta</small><div class="cifra">{{ abiertas.length }}</div><small class="texto-gris">{{ pujasRecibidas }} pujas recibidas</small></div>
          <div class="panel"><small class="texto-gris">Vendidas</small><div class="cifra text-success">{{ vendidas.length }}</div><small class="texto-gris">{{ rd(totalVendido) }} en total</small></div>
          <div class="panel"><small class="texto-gris">Publicadas</small><div class="cifra">{{ filas.length }}</div><small class="texto-gris">desde que te uniste</small></div>
          <div class="panel"><small class="texto-gris">Comisión estimada</small><div class="cifra">{{ rd(Math.round(totalVendido * 0.08)) }}</div><small class="texto-gris">8 % de lo vendido</small></div>
        </div>

        <section class="panel p-0 overflow-hidden">
          <div class="table-responsive">
            <table class="table align-middle mb-0">
              <thead>
                <tr><th>Artículo</th><th class="text-end">Salida</th><th class="text-end">Puja actual</th><th class="text-end">Pujas</th><th>Cierre</th><th>Estado</th><th>Comprador</th></tr>
              </thead>
              <tbody>
                <tr v-for="f in filas" :key="f.subastaId">
                  <td>
                    <RouterLink :to="`/subastas/${f.subastaId}`" class="d-flex align-items-center gap-3 text-body">
                      <img v-if="f.foto" :src="urlFoto(f.foto)" alt="" class="miniatura" />
                      <span v-else class="miniatura sin-foto"><Image :size="20" /></span>
                      <div><div class="fw-medium">{{ f.titulo }}</div><small class="texto-gris">Subasta #{{ f.subastaId }}</small></div>
                    </RouterLink>
                  </td>
                  <td class="text-end">{{ rd(f.precioSalida) }}</td>
                  <td class="text-end fw-bold">{{ f.pujaActual ? rd(f.pujaActual) : '—' }}</td>
                  <td class="text-end">{{ f.numeroPujas }}</td>
                  <td class="text-nowrap">{{ enCurso(f) ? tiempoRestante(f.fechaCierre, ahora) : diaCorto(f.fechaCierre) }}</td>
                  <td><EstadoBadge :estado="ESTADOS[f.estado]?.[0] ?? 'Neutra'" :texto="ESTADOS[f.estado]?.[1] ?? f.estado" /></td>
                  <td>
                    <template v-if="f.ganador">
                      <div class="fw-medium">{{ f.ganador.nombre }}</div>
                      <small><a :href="`mailto:${f.ganador.correo}`">{{ f.ganador.correo }}</a></small>
                      <small v-if="f.ganador.telefono" class="d-block texto-gris">{{ f.ganador.telefono }}</small>
                      <small v-if="f.pagoConfirmado" class="d-block text-success fw-medium">Pago confirmado</small>
                      <template v-else>
                        <small v-if="f.plazoPago" class="d-block texto-gris">Paga antes del {{ fechaConHora(f.plazoPago) }}</small>
                        <button v-if="porConfirmar !== f.subastaId" type="button" class="btn btn-sm btn-outline-primary mt-1" @click="porConfirmar = f.subastaId">Confirmar pago recibido</button>
                        <span v-else class="d-inline-flex gap-1 mt-1">
                          <button type="button" class="btn btn-sm btn-light" @click="porConfirmar = null">No</button>
                          <button type="button" class="btn btn-sm btn-primary" @click="confirmarPago(f.subastaId)">Sí, ya me pagó</button>
                        </span>
                      </template>
                    </template>
                    <span v-else class="texto-gris">—</span>
                  </td>
                </tr>
                <tr v-if="!filas.length">
                  <td colspan="7" class="texto-gris py-4 text-center">{{ cargando ? 'Cargando…' : 'Todavía no has publicado artículos.' }}</td>
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
