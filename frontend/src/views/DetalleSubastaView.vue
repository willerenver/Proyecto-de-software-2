<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ChevronLeft, CircleCheck, Clock, Eye, Heart, MapPin, MessageSquare, ShieldCheck, Star } from 'lucide-vue-next'
import EstadoBadge from '../components/EstadoBadge.vue'
import ContadorRegresivo from '../components/ContadorRegresivo.vue'
import AvatarIniciales from '../components/AvatarIniciales.vue'
import ModalConfirmarPuja from '../components/ModalConfirmarPuja.vue'
import ToastAviso from '../components/ToastAviso.vue'
import { buscarSubasta, montoMinimo, pujaActual, registrarPuja, usuarioActual } from '../data/subastas'
import { fechaLarga, fechaRelativa, haceTiempo, miles, postorOculto, rd } from '../utils/formato'
import { useAhora } from '../composables/useAhora'

const route = useRoute()
const router = useRouter()
const ahora = useAhora()

const subasta = computed(() => buscarSubasta(route.params.id))
watch(subasta, s => { if (!s) router.replace('/') }, { immediate: true })

const fotos = computed(() => (subasta.value?.fotos ?? []).map(f => (typeof f === 'string' ? { src: f } : f)))
const fotoActiva = ref(0)
const pestana = ref('descripcion')
const monto = ref(null)
const error = ref('')
const confirmando = ref(false)
const aviso = ref(null)
const pujaAutomatica = ref(false)
const siguiendo = ref(false)
const verTodas = ref(false)
const pregunta = ref('')

const minimo = computed(() => (subasta.value ? montoMinimo(subasta.value) : 0))
const cerrada = computed(() => subasta.value && new Date(subasta.value.fechaCierre).getTime() <= ahora.value)
const historial = computed(() => (subasta.value ? [...subasta.value.pujas].reverse() : []))
const historialVisible = computed(() => (verTodas.value ? historial.value : historial.value.slice(0, 5)))
const reservaAlcanzada = computed(() => subasta.value?.precioReserva && pujaActual(subasta.value) >= subasta.value.precioReserva)
const rapidas = [500, 1000, 2500]
const seguidores = computed(() => (subasta.value?.siguiendo ?? 0) + (siguiendo.value ? 1 : 0))

watch(minimo, m => { monto.value = m }, { immediate: true })

function irA(seccion) {
  pestana.value = seccion
  document.getElementById(seccion)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function sumar(cantidad) {
  monto.value = (pujaActual(subasta.value) ?? subasta.value.precioSalida) + cantidad
}

function pujar() {
  error.value = ''
  if (!(monto.value >= minimo.value)) {
    error.value = `Ingresa ${rd(minimo.value)} o más.`
    return
  }
  confirmando.value = true
}

function confirmar() {
  confirmando.value = false
  const montoPujado = monto.value
  const fallo = registrarPuja(subasta.value, montoPujado)
  if (fallo) {
    error.value = fallo
    return
  }
  aviso.value = `Vas ganando con ${rd(montoPujado)}. Te avisamos si alguien te supera.`
  setTimeout(() => { aviso.value = null }, 6000)
}

function preguntar() {
  if (!pregunta.value.trim()) return
  subasta.value.preguntas = [{ pregunta: pregunta.value.trim(), respuesta: null, respondidaEn: null }, ...(subasta.value.preguntas ?? [])]
  pregunta.value = ''
}

const esMia = postor => postor === usuarioActual.alias
</script>

<template>
  <div v-if="subasta">
    <!-- 10 Móvil · Subasta: cabecera propia -->
    <div class="d-lg-none cabecera-movil">
      <RouterLink to="/" aria-label="Volver"><ChevronLeft :size="24" /></RouterLink>
      <span class="fw-medium">Subasta #{{ subasta.id }}</span>
      <button type="button" class="icono-boton" :class="{ activo: siguiendo }" aria-label="Seguir esta subasta" @click="siguiendo = !siguiendo"><Heart :size="22" /></button>
    </div>

    <div class="contenedor pt-lg-4 pb-lg-5">
      <nav class="d-none d-lg-flex gap-2 migas mb-4" aria-label="Ruta">
        <RouterLink to="/">Inicio</RouterLink><span>/</span>
        <a href="#" @click.prevent>{{ subasta.categoria }}</a><span>/</span>
        <template v-if="subasta.subcategoria"><a href="#" @click.prevent>{{ subasta.subcategoria }}</a><span>/</span></template>
        <span class="actual">Subasta #{{ subasta.id }}</span>
      </nav>

      <div class="dos-columnas">
        <!-- Columna izquierda -->
        <div class="izquierda">
          <div class="foto-principal">
            <img v-if="fotos.length" :src="fotos[fotoActiva].src" :alt="subasta.titulo" :style="{ objectPosition: fotos[fotoActiva].posicion, transform: fotos[fotoActiva].zoom ? `scale(${fotos[fotoActiva].zoom})` : null, transformOrigin: fotos[fotoActiva].posicion, objectFit: fotos[fotoActiva].zoom ? 'cover' : 'contain' }" />
          </div>
          <div v-if="fotos.length > 1" class="miniaturas d-none d-lg-flex">
            <button v-for="(f, i) in fotos" :key="i" type="button" :class="{ activa: i === fotoActiva }" :aria-label="`Foto ${i + 1}`" @click="fotoActiva = i">
              <img :src="f.src" alt="" :style="{ objectFit: f.encaje ?? 'cover', objectPosition: f.posicion, transform: f.zoom ? `scale(${f.zoom})` : null, transformOrigin: f.posicion }" />
            </button>
          </div>

          <div class="d-none d-lg-flex flex-column gap-4">
            <ul class="nav nav-tabs">
              <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'descripcion' }" @click="irA('descripcion')">Descripción</button></li>
              <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'historial' }" @click="irA('historial')">Historial de pujas ({{ subasta.pujas.length }})</button></li>
              <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'envio' }" @click="irA('envio')">Envío y pago</button></li>
            </ul>

            <section id="descripcion" class="d-flex flex-column gap-3">
              <p v-for="(p, i) in subasta.descripcion ?? ['El vendedor todavía no agregó una descripción.']" :key="i" class="mb-0 parrafo">{{ p }}</p>
              <div v-if="subasta.ficha" id="envio" class="tabla-pj">
                <div v-for="[a, b] in subasta.ficha" :key="a" class="fila"><span class="clave">{{ a }}</span><span>{{ b }}</span></div>
              </div>
            </section>

            <section id="historial" class="d-flex flex-column gap-3">
              <h2 class="subtitulo">Historial de pujas</h2>
              <div class="tabla-pj">
                <div class="fila encabezado"><span class="flex-grow-1">Postor</span><span class="col-monto">Monto</span><span class="col-fecha">Fecha y hora</span></div>
                <div v-for="(p, i) in historialVisible" :key="p.id" class="fila">
                  <span class="flex-grow-1 d-flex align-items-center gap-2">
                    <span :class="{ 'fw-medium': esMia(p.postor) }">{{ esMia(p.postor) ? 'Tú' : postorOculto(p.postor) }}</span>
                    <EstadoBadge v-if="i === 0 && esMia(p.postor)" estado="Vas ganando" />
                  </span>
                  <span class="col-monto fw-medium">{{ rd(p.monto) }}</span>
                  <span class="col-fecha texto-gris">{{ fechaRelativa(p.creadaEn, ahora) }}</span>
                </div>
                <div v-if="!historial.length" class="fila texto-gris">Todavía no hay pujas. La primera puja empieza en {{ rd(subasta.precioSalida) }}.</div>
              </div>
              <button v-if="historial.length > 5" type="button" class="btn btn-link p-0 align-self-start texto-pequeno" @click="verTodas = !verTodas">{{ verTodas ? 'Ver menos' : `Ver las ${historial.length} pujas` }}</button>
            </section>

            <section class="d-flex flex-column gap-3">
              <h2 class="subtitulo">Preguntas al vendedor</h2>
              <form class="d-flex gap-2" @submit.prevent="preguntar">
                <input v-model="pregunta" class="form-control" maxlength="300" placeholder="Escribe tu pregunta…" aria-label="Tu pregunta" />
                <button type="submit" class="btn btn-primary">Preguntar</button>
              </form>
              <div v-for="(q, i) in subasta.preguntas ?? []" :key="i" class="pregunta">
                <div class="fw-medium">{{ q.pregunta }}</div>
                <div v-if="q.respuesta" class="d-flex gap-2 texto-gris respuesta"><MessageSquare :size="16" class="flex-shrink-0 mt-1" /> {{ q.respuesta }}</div>
                <small class="tenue">{{ q.respuesta ? `Respondida ${haceTiempo(q.respondidaEn, ahora)}` : 'Esperando respuesta del vendedor' }}</small>
              </div>
            </section>
          </div>
        </div>

        <!-- Columna derecha -->
        <aside class="derecha">
          <section class="panel caja-puja">
            <div class="d-flex gap-2">
              <EstadoBadge :estado="cerrada ? 'Neutra' : 'Activa'" :texto="cerrada ? 'Cerrada' : 'Activa'" />
              <EstadoBadge estado="Neutra" :texto="subasta.condicion" />
            </div>
            <h1 class="titulo">{{ subasta.titulo }}</h1>
            <div class="d-none d-lg-flex align-items-center gap-3 meta">
              <span>Subasta #{{ subasta.id }}</span>
              <span v-if="subasta.visitas" class="d-inline-flex align-items-center gap-1"><Eye :size="14" /> {{ miles(subasta.visitas) }} visitas</span>
              <span class="d-inline-flex align-items-center gap-1"><Heart :size="14" /> {{ seguidores }} siguiendo</span>
            </div>
            <hr class="my-0 d-none d-lg-block" />
            <div class="d-flex justify-content-between align-items-end">
              <div>
                <div class="etiqueta">{{ subasta.pujas.length ? 'Puja actual' : 'Precio de salida' }}</div>
                <div class="monto">{{ rd(pujaActual(subasta) ?? subasta.precioSalida) }}</div>
              </div>
              <div class="text-end">
                <button type="button" class="btn btn-link p-0 fw-medium enlace-pujas" @click="irA('historial')">{{ subasta.pujas.length }} pujas</button>
                <div class="meta d-none d-lg-block">Precio de salida: {{ rd(subasta.precioSalida) }}</div>
              </div>
            </div>
            <div v-if="subasta.precioReserva" class="d-flex align-items-center gap-2 reserva" :class="{ alcanzada: reservaAlcanzada }">
              <CircleCheck :size="16" /> {{ reservaAlcanzada ? 'Se alcanzó el precio de reserva' : 'Aún no se alcanza el precio de reserva' }}
            </div>
            <div class="d-flex flex-column gap-2">
              <span class="etiqueta">{{ cerrada ? 'Cerró el' : 'Termina en' }}</span>
              <ContadorRegresivo v-if="!cerrada" :fecha="subasta.fechaCierre" :ahora="ahora" />
              <span class="meta d-none d-lg-block">{{ fechaLarga(subasta.fechaCierre) }}</span>
            </div>
            <hr class="my-0 d-none d-lg-block" />

            <template v-if="!cerrada">
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
              <div class="d-flex gap-2">
                <button v-for="r in rapidas" :key="r" type="button" class="btn btn-light btn-sm flex-fill" @click="sumar(r)">+ <span class="d-none d-lg-inline">RD$ </span>{{ miles(r) }}</button>
              </div>
              <div class="form-check d-none d-lg-block mb-0">
                <input id="automatica" v-model="pujaAutomatica" type="checkbox" class="form-check-input" />
                <label for="automatica" class="form-check-label">Puja automática: subir por mí hasta un máximo</label>
              </div>
              <button type="button" class="btn btn-outline-primary d-none d-lg-flex align-items-center justify-content-center gap-2" @click="siguiendo = !siguiendo">
                <Heart :size="16" :fill="siguiendo ? 'currentColor' : 'none'" /> {{ siguiendo ? 'Siguiendo esta subasta' : 'Seguir esta subasta' }}
              </button>
              <p v-if="subasta.extensionFinal" class="meta mb-0 d-none d-lg-block">Si alguien puja en los últimos 2 minutos, el cierre se extiende 2 minutos más.</p>
            </template>
            <p v-else class="mb-0 fw-medium">Esta subasta ya cerró y no acepta más pujas.</p>
          </section>

          <!-- Vendedor: escritorio -->
          <section class="panel vendedor d-none d-lg-flex">
            <span class="etiqueta">Vendedor</span>
            <div class="d-flex align-items-center gap-3">
              <AvatarIniciales :iniciales="subasta.vendedor.iniciales" :tam="48" />
              <div class="flex-grow-1">
                <div class="fw-bold nombre">{{ subasta.vendedor.nombre }}</div>
                <div class="meta d-flex align-items-center gap-1"><Star :size="14" class="estrella" /> {{ subasta.vendedor.calificacion }} · {{ subasta.vendedor.ventas }} ventas</div>
              </div>
              <EstadoBadge v-if="subasta.vendedor.verificado" estado="Activa" texto="Identidad verificada" />
            </div>
            <div class="d-flex align-items-center gap-2 texto-14"><MapPin :size="16" class="texto-gris" /> {{ subasta.vendedor.ubicacion }}</div>
            <div class="d-flex align-items-center gap-2 texto-14"><Clock :size="16" class="texto-gris" /> {{ subasta.vendedor.responde }}</div>
            <div class="d-flex gap-2">
              <button type="button" class="btn btn-light flex-fill">Ver perfil</button>
              <button type="button" class="btn btn-light flex-fill">Enviar mensaje</button>
            </div>
          </section>
          <div class="proteccion d-none d-lg-flex">
            <ShieldCheck :size="22" class="flex-shrink-0" />
            <span>Compra protegida: si el artículo no es lo que se describe, te devolvemos el dinero.</span>
          </div>

          <!-- Vendedor y descripción: móvil -->
          <section class="d-lg-none bloque-movil">
            <hr class="my-0" />
            <div class="d-flex align-items-center gap-3">
              <AvatarIniciales :iniciales="subasta.vendedor.iniciales" :tam="40" />
              <div class="flex-grow-1">
                <div class="fw-bold">{{ subasta.vendedor.nombre }}</div>
                <div class="meta">{{ subasta.vendedor.calificacion }} · {{ subasta.vendedor.ventas }} ventas · {{ subasta.vendedor.zona }}</div>
              </div>
              <button type="button" class="btn btn-light btn-sm">Mensaje</button>
            </div>
            <hr class="my-0" />
            <h2 class="fs-6 fw-bold mb-0">Descripción</h2>
            <p class="mb-0">{{ subasta.descripcionCorta ?? subasta.descripcion?.[0] ?? 'El vendedor todavía no agregó una descripción.' }}</p>
          </section>
        </aside>
      </div>
    </div>

    <!-- 10 Móvil: barra de puja fija -->
    <form v-if="!cerrada" class="barra-puja d-lg-none" @submit.prevent="pujar">
      <div class="campo-puja" :class="{ error }">
        <span>RD$</span>
        <input v-model.number="monto" type="number" :min="minimo" :step="subasta.incrementoMinimo" aria-label="Tu puja" />
      </div>
      <button type="submit" class="btn btn-pujar btn-lg">Pujar</button>
    </form>

    <ModalConfirmarPuja v-if="confirmando" :subasta="subasta" :monto="monto" :ahora="ahora" @cancelar="confirmando = false" @confirmar="confirmar" />
    <ToastAviso v-if="aviso" titulo="Puja registrada" :texto="aviso" @cerrar="aviso = null" />
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

.foto-principal img,
.miniaturas img {
  width: 100%;
  height: 100%;
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
  flex-direction: column;
  gap: 14px;
  padding: 20px;
}

.vendedor .nombre {
  font-size: 16px;
}

.estrella {
  color: var(--pj-puja);
  fill: var(--pj-puja);
}

.proteccion {
  gap: 12px;
  padding: 16px;
  background: var(--pj-primario-suave);
  border-radius: 8px;
  color: var(--pj-primario);
  font-size: 14px;
  line-height: 20px;
}

.proteccion span {
  color: var(--pj-texto);
}

.cabecera-movil {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  background: #fff;
  border-bottom: 1px solid var(--pj-borde);
}

.cabecera-movil a,
.icono-boton {
  color: var(--pj-texto);
  border: 0;
  background: none;
  padding: 0;
}

.icono-boton.activo {
  color: var(--pj-peligro);
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

  .bloque-movil {
    display: flex;
    flex-direction: column;
    gap: 14px;
    margin-top: 14px;
    line-height: 23px;
  }
}
</style>
