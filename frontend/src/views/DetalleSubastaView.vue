<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ChevronLeft, CircleCheck, Image, Package, Truck } from 'lucide-vue-next'
import EstadoBadge from '../components/EstadoBadge.vue'
import ContadorRegresivo from '../components/ContadorRegresivo.vue'
import AvatarIniciales from '../components/AvatarIniciales.vue'
import ModalConfirmarPuja from '../components/ModalConfirmarPuja.vue'
import ToastAviso from '../components/ToastAviso.vue'
import { api, urlFoto } from '../api/cliente'
import { esAdministrador, sesion } from '../stores/sesion'
import { condicion, fechaConHora, fechaLarga, fechaRelativa, miles, rd } from '../utils/formato'
import { useAhora } from '../composables/useAhora'
import { useSondeo } from '../composables/useSondeo'

const route = useRoute()
const router = useRouter()
const ahora = useAhora()

const subasta = ref(null)
const noExiste = ref(false)
const errorCarga = ref('')
const fotoActiva = ref(0)
const pestana = ref('descripcion')
const monto = ref(null)
const error = ref('')
const confirmando = ref(false)
const enviando = ref(false)
const aviso = ref(null)
const verTodas = ref(false)
const confirmarCancelacion = ref(false)
const confirmarRetiro = ref(false)
const automaticaAbierta = ref(false)
const maximo = ref(null)
const errorAutomatica = ref('')

async function cargar() {
  const id = route.params.id
  try {
    const datos = await api(`/api/subastas/${id}`)
    if (route.params.id === id) {
      subasta.value = datos
      errorCarga.value = ''
    }
  } catch (e) {
    if (e.estado === 404) noExiste.value = true
    else errorCarga.value = e.message
  }
}

watch(() => route.params.id, id => {
  if (!id) return
  subasta.value = null
  noExiste.value = false
  fotoActiva.value = 0
  monto.value = null
  cargar()
}, { immediate: true })

// Las pujas de los demás aparecen sin recargar la página.
useSondeo(cargar, 5000)

const fotos = computed(() => (subasta.value?.fotos ?? []).map(urlFoto))
const minimo = computed(() => subasta.value?.montoMinimo ?? 0)
const historial = computed(() => subasta.value?.historial ?? [])
const historialVisible = computed(() => (verTodas.value ? historial.value : historial.value.slice(0, 5)))
const esVendedor = computed(() => !!sesion.usuario && subasta.value?.vendedor.id === sesion.usuario.id)
const programada = computed(() => !!subasta.value && new Date(subasta.value.fechaInicio).getTime() > ahora.value)
const final = computed(() => ['Finalizada', 'Desierta', 'Cancelada'].includes(subasta.value?.estado))
const vencida = computed(() => !!subasta.value && new Date(subasta.value.fechaCierre).getTime() <= ahora.value)
const cerrada = computed(() => final.value || vencida.value)
const aceptaPujas = computed(() => !!subasta.value && !cerrada.value && !programada.value)
const puedePujar = computed(() => aceptaPujas.value && !esVendedor.value)
const voyGanando = computed(() => historial.value[0]?.esMia === true)
const rapidas = [500, 1000, 2500]
const parrafos = computed(() => (subasta.value?.descripcion ?? '').split(/\n+/).map(p => p.trim()).filter(Boolean))
const ficha = computed(() => {
  const s = subasta.value
  if (!s) return []
  const entrega = [s.permiteRetiro && 'Retiro en persona', s.permiteEnvio && 'Envío a domicilio'].filter(Boolean).join(' o ')
  return [
    ['Estado', condicion(s.condicion)],
    ['Categoría', s.subcategoria ? `${s.categoria} › ${s.subcategoria}` : s.categoria],
    ['Entrega', entrega],
    ['Incremento mínimo', rd(s.incrementoMinimo)],
    ['Empezó', fechaLarga(s.fechaInicio)],
  ]
})

// Lo que el usuario escribió se respeta mientras siga siendo válido; si otra puja lo dejó atrás, se sube al mínimo.
watch(minimo, m => { if (!(monto.value >= m)) monto.value = m }, { immediate: true })

// Al llegar a cero el contador, se consulta de nuevo para mostrar el resultado.
watch(vencida, (ahoraVencida, antes) => { if (ahoraVencida && antes === false) setTimeout(cargar, 1500) })

function irA(seccion) {
  pestana.value = seccion
  document.getElementById(seccion)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function sumar(cantidad) {
  monto.value = Math.max(minimo.value, (subasta.value.pujaActual ?? subasta.value.precioSalida) + cantidad)
}

function pujar() {
  error.value = ''
  if (!sesion.token) {
    router.push({ name: 'iniciar-sesion', query: { volver: route.fullPath } })
    return
  }
  if (!(monto.value >= minimo.value)) {
    error.value = `Ingresa ${rd(minimo.value)} o más.`
    return
  }
  confirmando.value = true
}

async function confirmar() {
  const montoPujado = monto.value
  enviando.value = true
  try {
    const r = await api(`/api/subastas/${subasta.value.id}/pujas`, { metodo: 'POST', cuerpo: { monto: montoPujado } })
    aviso.value = r.vasGanando
      ? { titulo: 'Puja registrada', texto: `Vas ganando con ${rd(montoPujado)}. Si alguien te supera, te avisamos por correo.` }
      : { titulo: 'Te superaron al instante', texto: `Otro postor tenía una puja automática más alta. La puja actual es ${rd(r.pujaActual)}.` }
    setTimeout(() => { aviso.value = null }, 6000)
  } catch (e) {
    error.value = e.errores.monto ?? e.message
  } finally {
    enviando.value = false
    confirmando.value = false
    await cargar()
  }
}

function abrirAutomatica() {
  if (!sesion.token) {
    router.push({ name: 'iniciar-sesion', query: { volver: route.fullPath } })
    return
  }
  errorAutomatica.value = ''
  maximo.value = subasta.value.miPujaAutomatica ?? minimo.value + subasta.value.incrementoMinimo * 4
  automaticaAbierta.value = true
}

async function programarAutomatica() {
  errorAutomatica.value = ''
  try {
    await api(`/api/subastas/${subasta.value.id}/puja-automatica`, { metodo: 'PUT', cuerpo: { maximo: maximo.value } })
    automaticaAbierta.value = false
    aviso.value = { titulo: 'Puja automática activa', texto: `Pujaremos por ti lo necesario para que vayas ganando, hasta ${rd(maximo.value)}.` }
    setTimeout(() => { aviso.value = null }, 6000)
    await cargar()
  } catch (e) {
    errorAutomatica.value = e.errores.maximo ?? e.message
  }
}

async function cancelarAutomatica() {
  try {
    await api(`/api/subastas/${subasta.value.id}/puja-automatica`, { metodo: 'DELETE' })
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}

async function retirar() {
  try {
    await api(`/api/subastas/${subasta.value.id}/retirar`, { metodo: 'POST' })
    confirmarRetiro.value = false
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}

async function cancelar() {
  try {
    await api(`/api/admin/subastas/${subasta.value.id}/cancelar`, { metodo: 'POST' })
    confirmarCancelacion.value = false
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}
</script>

<template>
  <div v-if="noExiste" class="contenedor py-5 text-center">
    <h1 class="fs-4 fw-bold">Esta subasta no existe</h1>
    <p class="texto-gris">Puede que el enlace esté mal escrito.</p>
    <RouterLink to="/" class="btn btn-primary">Ver subastas activas</RouterLink>
  </div>
  <div v-else-if="!subasta" class="contenedor py-5 text-center texto-gris">
    <p v-if="errorCarga" class="text-danger">{{ errorCarga }}</p>
    <p v-else>Cargando subasta…</p>
  </div>
  <div v-else>
    <!-- 10 Móvil · Subasta: cabecera propia -->
    <div class="d-lg-none cabecera-movil">
      <RouterLink to="/" aria-label="Volver"><ChevronLeft :size="24" /></RouterLink>
      <span class="fw-medium">Subasta #{{ subasta.id }}</span>
      <span style="width: 24px" />
    </div>

    <div class="contenedor pt-lg-4 pb-lg-5">
      <nav class="d-none d-lg-flex gap-2 migas mb-4" aria-label="Ruta">
        <RouterLink to="/">Inicio</RouterLink><span>/</span>
        <span>{{ subasta.categoria }}</span><span>/</span>
        <template v-if="subasta.subcategoria"><span>{{ subasta.subcategoria }}</span><span>/</span></template>
        <span class="actual">Subasta #{{ subasta.id }}</span>
      </nav>

      <div class="dos-columnas">
        <!-- Columna izquierda -->
        <div class="izquierda">
          <div class="foto-principal">
            <img v-if="fotos.length" :src="fotos[fotoActiva]" :alt="subasta.titulo" />
            <span v-else class="sin-foto"><Image :size="48" /></span>
          </div>
          <div v-if="fotos.length > 1" class="miniaturas">
            <button v-for="(f, i) in fotos" :key="f" type="button" :class="{ activa: i === fotoActiva }" :aria-label="`Foto ${i + 1}`" @click="fotoActiva = i">
              <img :src="f" alt="" />
            </button>
          </div>

          <div class="d-none d-lg-flex flex-column gap-4">
            <ul class="nav nav-tabs">
              <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'descripcion' }" @click="irA('descripcion')">Descripción</button></li>
              <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'historial' }" @click="irA('historial')">Historial de pujas ({{ subasta.numeroPujas }})</button></li>
            </ul>

            <section id="descripcion" class="d-flex flex-column gap-3">
              <p v-for="(p, i) in parrafos" :key="i" class="mb-0 parrafo">{{ p }}</p>
              <div class="tabla-pj">
                <div v-for="[a, b] in ficha" :key="a" class="fila"><span class="clave">{{ a }}</span><span>{{ b }}</span></div>
              </div>
            </section>

            <section id="historial" class="d-flex flex-column gap-3">
              <h2 class="subtitulo">Historial de pujas</h2>
              <div class="tabla-pj">
                <div class="fila encabezado"><span class="flex-grow-1">Postor</span><span class="col-monto">Monto</span><span class="col-fecha">Fecha y hora</span></div>
                <div v-for="(p, i) in historialVisible" :key="p.id" class="fila">
                  <span class="flex-grow-1 d-flex align-items-center gap-2">
                    <span :class="{ 'fw-medium': p.esMia, descartada: p.descartada }">{{ p.postor }}</span>
                    <EstadoBadge v-if="i === 0 && p.esMia && !cerrada" estado="Vas ganando" />
                    <EstadoBadge v-if="p.descartada" estado="Neutra" texto="No pagó a tiempo" />
                    <small v-if="p.automatica" class="texto-gris">automática</small>
                  </span>
                  <span class="col-monto fw-medium" :class="{ descartada: p.descartada }">{{ rd(p.monto) }}</span>
                  <span class="col-fecha texto-gris">{{ fechaRelativa(p.creadaEn, ahora) }}</span>
                </div>
                <div v-if="!historial.length" class="fila texto-gris">Todavía no hay pujas. La primera puja empieza en {{ rd(subasta.precioSalida) }}.</div>
              </div>
              <button v-if="historial.length > 5" type="button" class="btn btn-link p-0 align-self-start texto-pequeno" @click="verTodas = !verTodas">{{ verTodas ? 'Ver menos' : `Ver las ${historial.length} pujas` }}</button>
            </section>
          </div>
        </div>

        <!-- Columna derecha -->
        <aside class="derecha">
          <section class="panel caja-puja">
            <div class="d-flex gap-2 flex-wrap">
              <EstadoBadge v-if="programada" estado="Pendiente" texto="Programada" />
              <EstadoBadge v-else-if="!cerrada" estado="Activa" />
              <EstadoBadge v-else estado="Neutra" :texto="subasta.estado === 'Cancelada' ? 'Cancelada' : 'Cerrada'" />
              <EstadoBadge estado="Neutra" :texto="condicion(subasta.condicion)" />
              <EstadoBadge v-if="voyGanando && !cerrada" estado="Vas ganando" />
            </div>
            <h1 class="titulo">{{ subasta.titulo }}</h1>
            <div class="d-none d-lg-flex align-items-center gap-3 meta">
              <span>Subasta #{{ subasta.id }}</span>
              <span>{{ subasta.categoria }}</span>
            </div>
            <hr class="my-0 d-none d-lg-block" />
            <div class="d-flex justify-content-between align-items-end">
              <div>
                <div class="etiqueta">{{ subasta.numeroPujas ? (cerrada ? 'Puja final' : 'Puja actual') : 'Precio de salida' }}</div>
                <div class="monto">{{ rd(subasta.pujaActual ?? subasta.precioSalida) }}</div>
              </div>
              <div class="text-end">
                <button type="button" class="btn btn-link p-0 fw-medium enlace-pujas" @click="irA('historial')">{{ subasta.numeroPujas }} {{ subasta.numeroPujas === 1 ? 'puja' : 'pujas' }}</button>
                <div class="meta d-none d-lg-block">Precio de salida: {{ rd(subasta.precioSalida) }}</div>
              </div>
            </div>
            <div v-if="subasta.tienePrecioReserva" class="d-flex align-items-center gap-2 reserva" :class="{ alcanzada: subasta.reservaAlcanzada }">
              <CircleCheck :size="16" /> {{ subasta.reservaAlcanzada ? 'Se alcanzó el precio de reserva' : 'Aún no se alcanza el precio de reserva' }}
            </div>
            <div class="d-flex flex-column gap-2">
              <span class="etiqueta">{{ programada ? 'Empieza en' : cerrada ? 'Cerró el' : 'Termina en' }}</span>
              <ContadorRegresivo v-if="!cerrada" :fecha="programada ? subasta.fechaInicio : subasta.fechaCierre" :ahora="ahora" />
              <span class="meta">{{ fechaLarga(programada ? subasta.fechaInicio : subasta.fechaCierre) }}</span>
            </div>
            <hr class="my-0 d-none d-lg-block" />

            <!-- Resultado -->
            <div v-if="subasta.estado === 'Finalizada'" class="resultado" :class="{ ganaste: subasta.pujaGanadora?.esMia }">
              <template v-if="subasta.pujaGanadora?.esMia">
                <strong class="d-block">¡Ganaste esta subasta con {{ rd(subasta.pujaGanadora.monto) }}!</strong>
                <template v-if="subasta.pagoConfirmado">El vendedor confirmó que recibió tu pago.</template>
                <template v-else-if="subasta.plazoPago">Tienes hasta el {{ fechaConHora(subasta.plazoPago) }} para pagar; si no, la subasta pasa al siguiente postor.</template>
                <RouterLink :to="`/subastas/${subasta.id}/pagar`" class="btn btn-primary w-100 mt-3">Coordinar pago y entrega</RouterLink>
              </template>
              <template v-else-if="esVendedor">
                <strong class="d-block">Vendiste este artículo en {{ rd(subasta.pujaGanadora.monto) }}.</strong>
                <template v-if="subasta.pagoConfirmado">Confirmaste el pago.</template>
                <template v-else-if="subasta.plazoPago">El comprador tiene hasta el {{ fechaConHora(subasta.plazoPago) }} para pagar. Confirma el pago cuando lo recibas.</template>
                <RouterLink :to="`/subastas/${subasta.id}/pagar`" class="btn btn-primary w-100 mt-3">Ver datos del comprador</RouterLink>
              </template>
              <template v-else>
                <strong class="d-block">Ganó {{ subasta.pujaGanadora?.postor }} con {{ rd(subasta.pujaGanadora?.monto ?? 0) }}.</strong>
                Esta subasta ya terminó.
              </template>
            </div>
            <p v-else-if="subasta.estado === 'Desierta'" class="mb-0 fw-medium">Terminó sin ganador: no hubo pujas o no se alcanzó el precio de reserva.</p>
            <p v-else-if="subasta.estado === 'Cancelada'" class="mb-0 fw-medium">Esta subasta fue cancelada y no acepta pujas.</p>
            <p v-else-if="vencida" class="mb-0 fw-medium">El tiempo terminó. Estamos confirmando el resultado…</p>
            <template v-else-if="esVendedor">
              <p class="mb-0 texto-gris">Es tu subasta: aquí ves las pujas a medida que llegan. No puedes pujar en ella.</p>
              <div v-if="subasta.puedeEditar" class="d-flex flex-column gap-2">
                <div v-if="!confirmarRetiro" class="d-flex gap-2">
                  <RouterLink :to="`/subastas/${subasta.id}/editar`" class="btn btn-primary flex-fill">Editar</RouterLink>
                  <button type="button" class="btn btn-outline-danger flex-fill" @click="confirmarRetiro = true">Retirar</button>
                </div>
                <div v-else class="d-flex gap-2 align-items-center">
                  <span class="texto-pequeno flex-grow-1">¿Retirar esta subasta? Ya no se podrá pujar.</span>
                  <button type="button" class="btn btn-light btn-sm" @click="confirmarRetiro = false">No</button>
                  <button type="button" class="btn btn-danger btn-sm" @click="retirar">Sí, retirar</button>
                </div>
                <small class="texto-gris">Puedes editarla o retirarla mientras nadie haya pujado.</small>
              </div>
              <div v-if="error" class="text-danger texto-pequeno">{{ error }}</div>
            </template>
            <p v-else-if="programada" class="mb-0 texto-gris">Podrás pujar cuando empiece. La primera puja es de {{ rd(subasta.precioSalida) }} o más.</p>

            <template v-if="puedePujar">
              <form class="d-none d-lg-flex flex-column gap-2" @submit.prevent="pujar">
                <label for="monto" class="fw-medium texto-14">Tu puja</label>
                <div class="d-flex gap-2">
                  <div class="campo-puja" :class="{ error }">
                    <span>RD$</span>
                    <input id="monto" v-model.number="monto" type="number" :min="minimo" :step="subasta.incrementoMinimo" />
                  </div>
                  <button type="submit" class="btn btn-pujar btn-lg px-4">Pujar</button>
                </div>
                <div v-if="error" class="text-danger texto-pequeno">{{ error }}</div>
                <div v-else class="meta">Ingresa {{ rd(minimo) }} o más. El incremento mínimo es de {{ rd(subasta.incrementoMinimo) }}.</div>
              </form>
              <div v-if="error" class="text-danger texto-pequeno d-lg-none">{{ error }}</div>
              <div class="d-flex gap-2">
                <button v-for="r in rapidas" :key="r" type="button" class="btn btn-light btn-sm flex-fill" @click="sumar(r)">+ <span class="d-none d-lg-inline">RD$ </span>{{ miles(r) }}</button>
              </div>
              <div class="automatica">
                <div v-if="subasta.miPujaAutomatica && !automaticaAbierta" class="d-flex align-items-center gap-2 flex-wrap">
                  <span class="flex-grow-1 texto-14">Puja automática activa hasta <strong>{{ rd(subasta.miPujaAutomatica) }}</strong></span>
                  <button type="button" class="btn btn-light btn-sm" @click="abrirAutomatica">Cambiar</button>
                  <button type="button" class="btn btn-link btn-sm text-danger p-0" @click="cancelarAutomatica">Desactivar</button>
                </div>
                <button v-else-if="!automaticaAbierta" type="button" class="btn btn-link p-0 texto-14" @click="abrirAutomatica">Puja automática: subir por mí hasta un máximo</button>
                <form v-else class="d-flex flex-column gap-2" @submit.prevent="programarAutomatica">
                  <label for="maximo" class="fw-medium texto-14">Tu máximo</label>
                  <div class="d-flex gap-2">
                    <div class="campo-puja chico" :class="{ error: errorAutomatica }">
                      <span>RD$</span>
                      <input id="maximo" v-model.number="maximo" type="number" :min="minimo" :step="subasta.incrementoMinimo" />
                    </div>
                    <button type="submit" class="btn btn-primary">Activar</button>
                    <button type="button" class="btn btn-light" @click="automaticaAbierta = false">Cancelar</button>
                  </div>
                  <div v-if="errorAutomatica" class="text-danger texto-pequeno">{{ errorAutomatica }}</div>
                  <div v-else class="meta">Pujamos por ti solo lo necesario para que vayas ganando, sin pasar de este monto. Nadie más ve tu máximo.</div>
                </form>
              </div>
              <p v-if="subasta.extenderAlFinal" class="meta mb-0">Si alguien puja en los últimos 2 minutos, el cierre se extiende 2 minutos más.</p>
            </template>

            <div v-if="esAdministrador() && !cerrada" class="d-flex flex-column gap-2">
              <hr class="my-0" />
              <button v-if="!confirmarCancelacion" type="button" class="btn btn-outline-danger btn-sm" @click="confirmarCancelacion = true">Cancelar subasta (administrador)</button>
              <div v-else class="d-flex gap-2 align-items-center">
                <span class="texto-pequeno flex-grow-1">¿Cancelar esta subasta? Las pujas quedan en el historial.</span>
                <button type="button" class="btn btn-light btn-sm" @click="confirmarCancelacion = false">No</button>
                <button type="button" class="btn btn-danger btn-sm" @click="cancelar">Sí, cancelar</button>
              </div>
            </div>
          </section>

          <!-- Vendedor -->
          <section class="panel vendedor">
            <span class="etiqueta">Vendedor</span>
            <div class="d-flex align-items-center gap-3">
              <AvatarIniciales :iniciales="subasta.vendedor.iniciales" :tam="48" />
              <div class="flex-grow-1">
                <div class="fw-bold nombre">{{ subasta.vendedor.nombre }}</div>
                <div class="meta">{{ subasta.vendedor.ventas }} {{ subasta.vendedor.ventas === 1 ? 'venta' : 'ventas' }} en Pújalo</div>
              </div>
            </div>
            <div v-if="subasta.permiteRetiro" class="d-flex align-items-center gap-2 texto-14"><Package :size="16" class="texto-gris" /> Retiro en persona</div>
            <div v-if="subasta.permiteEnvio" class="d-flex align-items-center gap-2 texto-14"><Truck :size="16" class="texto-gris" /> Envío a domicilio</div>
          </section>

          <!-- Descripción e historial: móvil -->
          <section class="d-lg-none bloque-movil">
            <h2 class="fs-6 fw-bold mb-0">Descripción</h2>
            <p v-for="(p, i) in parrafos" :key="i" class="mb-0">{{ p }}</p>
            <h2 class="fs-6 fw-bold mb-0 mt-2">Últimas pujas</h2>
            <div v-for="p in historial.slice(0, 5)" :key="p.id" class="d-flex justify-content-between texto-14">
              <span :class="{ 'fw-medium': p.esMia }">{{ p.postor }}</span>
              <span><strong>{{ rd(p.monto) }}</strong> <span class="texto-gris">· {{ fechaRelativa(p.creadaEn, ahora) }}</span></span>
            </div>
            <p v-if="!historial.length" class="texto-gris mb-0">Todavía no hay pujas.</p>
          </section>
        </aside>
      </div>
    </div>

    <!-- 10 Móvil: barra de puja fija -->
    <form v-if="puedePujar" class="barra-puja d-lg-none" @submit.prevent="pujar">
      <div class="campo-puja" :class="{ error }">
        <span>RD$</span>
        <input v-model.number="monto" type="number" :min="minimo" :step="subasta.incrementoMinimo" aria-label="Tu puja" />
      </div>
      <button type="submit" class="btn btn-pujar btn-lg">Pujar</button>
    </form>

    <ModalConfirmarPuja v-if="confirmando" :subasta="subasta" :monto="monto" :ahora="ahora" :enviando="enviando" @cancelar="confirmando = false" @confirmar="confirmar" />
    <ToastAviso v-if="aviso" :titulo="aviso.titulo" :texto="aviso.texto" @cerrar="aviso = null" />
  </div>
</template>

<style scoped>
.migas {
  font-size: 14px;
  color: var(--pj-gris-claro);
}

.migas .actual {
  color: var(--pj-gris);
}

.dos-columnas {
  display: grid;
  grid-template-columns: minmax(0, 680px) minmax(0, 488px);
  gap: 32px;
  justify-content: space-between;
  align-items: start;
}

.izquierda {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.foto-principal {
  aspect-ratio: 680 / 500;
  background: #eef0f2;
  border-radius: 8px;
  overflow: hidden;
}

.foto-principal {
  display: grid;
  place-items: center;
}

.foto-principal img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.miniaturas {
  display: flex;
}

.miniaturas img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.sin-foto {
  color: var(--pj-gris-claro);
}

.automatica {
  padding-top: 4px;
}

.campo-puja.chico {
  height: 42px;
}

.campo-puja.chico input {
  font-size: 16px;
}

.descartada {
  text-decoration: line-through;
  color: var(--pj-gris);
}

.resultado {
  padding: 14px 16px;
  border-radius: 6px;
  background: var(--pj-claro);
  border: 1px solid var(--pj-borde);
  font-size: 14px;
  line-height: 20px;
}

.resultado.ganaste {
  background: #d1e7dd;
  border-color: #a3cfbb;
  color: #0a3622;
}

.resultado strong {
  font-size: 15px;
}

.miniaturas {
  gap: 12px;
}

.miniaturas button {
  flex: 1;
  aspect-ratio: 161 / 120;
  padding: 0;
  border: 0;
  border-radius: 6px;
  overflow: hidden;
  background: #eef0f2;
  outline: 2px solid transparent;
  outline-offset: -2px;
}

.miniaturas button.activa {
  outline-color: var(--pj-primario);
}

.parrafo {
  line-height: 24px;
}

.subtitulo {
  font-size: 18px;
  font-weight: 700;
  margin: 0;
}

.tabla-pj {
  background: #fff;
  border: 1px solid var(--pj-borde);
  border-radius: 8px;
  overflow: hidden;
  font-size: 14px;
}

.tabla-pj .fila {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid var(--pj-borde);
}

.tabla-pj .fila:last-child {
  border-bottom: 0;
}

.tabla-pj .clave {
  width: 200px;
  flex-shrink: 0;
  color: var(--pj-gris);
}

.tabla-pj .encabezado {
  background: var(--pj-claro);
  color: var(--pj-gris);
  font-size: 13px;
  font-weight: 500;
}

.col-monto {
  width: 160px;
  text-align: right;
}

.col-fecha {
  width: 200px;
  text-align: right;
}

.pregunta {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 4px 0;
}

.respuesta {
  line-height: 22px;
}

.tenue {
  font-size: 12px;
  color: var(--pj-gris-claro);
}

.derecha {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.caja-puja {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.titulo {
  font-size: 26px;
  font-weight: 700;
  line-height: 32px;
  margin: 0;
}

.meta {
  font-size: 13px;
  color: var(--pj-gris);
}

.etiqueta {
  font-size: 14px;
  color: var(--pj-gris);
}

.monto {
  font-size: 36px;
  font-weight: 700;
  line-height: 1.15;
}

.enlace-pujas {
  font-size: 15px;
  text-decoration: underline;
}

.reserva {
  font-size: 14px;
  font-weight: 500;
  color: var(--pj-gris);
}

.reserva.alcanzada {
  color: var(--pj-exito);
}

.texto-14 {
  font-size: 14px;
}

.campo-puja {
  flex: 1;
  display: flex;
  height: 50px;
  border: 1px solid var(--pj-borde);
  border-radius: 6px;
  overflow: hidden;
  background: #fff;
}

.campo-puja.error {
  border-color: var(--pj-peligro);
}

.campo-puja span {
  display: flex;
  align-items: center;
  padding: 0 14px;
  background: #e9ecef;
  color: var(--pj-gris);
  font-size: 16px;
}

.campo-puja input {
  flex: 1;
  min-width: 0;
  border: 0;
  padding: 0 14px;
  font-size: 18px;
  font-weight: 500;
  outline: none;
}

.vendedor {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 20px;
}

.vendedor .nombre {
  font-size: 16px;
}


.cabecera-movil {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  background: #fff;
  border-bottom: 1px solid var(--pj-borde);
}

.cabecera-movil a {
  color: var(--pj-texto);
}

.barra-puja {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1030;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px 28px;
  background: #fff;
  border-top: 1px solid var(--pj-borde);
  box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.06);
}

.barra-puja .campo-puja {
  height: 48px;
}

.barra-puja .campo-puja span {
  padding: 0 10px;
  font-size: 15px;
}

.barra-puja .campo-puja input {
  padding: 0 10px;
  font-size: 17px;
}

@media (max-width: 991.98px) {
  .contenedor {
    padding: 0;
  }

  .dos-columnas {
    grid-template-columns: 1fr;
    gap: 0;
  }

  .izquierda {
    gap: 0;
  }

  .foto-principal {
    border-radius: 0;
    aspect-ratio: 390 / 340;
  }

  .derecha {
    gap: 0;
    padding: 16px 16px 100px;
    background: #fff;
  }

  .caja-puja {
    padding: 0;
    border: 0;
    border-radius: 0;
    gap: 14px;
  }

  .titulo {
    font-size: 21px;
    line-height: 27px;
  }

  .etiqueta,
  .meta {
    font-size: 13px;
  }

  .monto {
    font-size: 30px;
  }

  .vendedor {
    padding: 14px 0 0;
    border: 0;
    border-top: 1px solid var(--pj-borde);
    border-radius: 0;
    margin-top: 14px;
  }

  .miniaturas {
    padding: 8px 16px 0;
    background: #fff;
    overflow-x: auto;
  }

  .miniaturas button {
    flex: 0 0 72px;
  }

  .bloque-movil {
    display: flex;
    flex-direction: column;
    gap: 14px;
    margin-top: 14px;
    line-height: 23px;
  }
}
</style>
