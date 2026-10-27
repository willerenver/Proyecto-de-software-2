<script setup>
import { computed, onBeforeUnmount, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Check, Info, Upload, X } from 'lucide-vue-next'
import SubastaCard from '../components/SubastaCard.vue'
import { api, urlFoto } from '../api/cliente'
import { categorias, cargarCategorias } from '../stores/categorias'
import { CONDICIONES, diaCorto, fechaConHora, rd } from '../utils/formato'

// La misma pantalla publica (/publicar) y corrige una subasta sin pujas (/subastas/:id/editar).
const route = useRoute()
const router = useRouter()
const editandoId = route.params.id ? Number(route.params.id) : null
const duraciones = [1, 3, 5, 7]
const incrementos = reactive([100, 250, 500, 1000])
const MAX_FOTOS = 10
const MAX_MB = 10

const enUnaHora = new Date(Date.now() + 3600000)
const local = d => new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16)

const form = reactive({
  titulo: '',
  categoriaId: '',
  condicion: 'buen_estado',
  descripcion: '',
  precioSalida: null,
  incrementoMinimo: 250,
  precioReserva: null,
  empiezaAhora: true,
  fechaInicio: local(enUnaHora),
  duracion: 3,
  extenderAlFinal: true,
  retiroEnPersona: true,
  envioADomicilio: false,
})
const COMISION = 0.08
const arrastrando = ref(false)
const fotos = ref([])
const errores = ref({})
const errorFotos = ref('')
const errorGeneral = ref('')
const publicando = ref(false)
const creada = ref(null)
// Al editar: fotos ya guardadas que el vendedor quitó y fecha de inicio si la subasta ya empezó.
const quitadas = ref([])
const yaEmpezo = ref(null)
const cargandoEdicion = ref(!!editandoId)

cargarCategorias().catch(e => { errorGeneral.value = e.message })
onBeforeUnmount(() => fotos.value.forEach(f => f.archivo && URL.revokeObjectURL(f.url)))

if (editandoId) {
  api(`/api/subastas/${editandoId}/edicion`).then(d => {
    Object.assign(form, {
      titulo: d.titulo, categoriaId: d.categoriaId, condicion: d.condicion, descripcion: d.descripcion,
      precioSalida: d.precioSalida, incrementoMinimo: d.incrementoMinimo, precioReserva: d.precioReserva,
      empiezaAhora: d.estado !== 'Programada', fechaInicio: local(new Date(d.fechaInicio)), duracion: d.duracionDias,
      extenderAlFinal: d.extenderAlFinal, retiroEnPersona: d.permiteRetiro, envioADomicilio: d.permiteEnvio,
    })
    if (!incrementos.includes(d.incrementoMinimo)) incrementos.push(d.incrementoMinimo)
    if (d.estado !== 'Programada') yaEmpezo.value = d.fechaInicio
    fotos.value = d.fotos.map((ruta, i) => ({ nombre: `Foto ${i + 1}`, url: urlFoto(ruta), ruta }))
  }).catch(e => { errorGeneral.value = e.message }).finally(() => { cargandoEdicion.value = false })
}

function agregarFotos(evento) {
  agregarArchivos(evento.target.files)
  evento.target.value = ''
}

function soltarFotos(evento) {
  arrastrando.value = false
  agregarArchivos(evento.dataTransfer.files)
}

function agregarArchivos(archivos) {
  errorFotos.value = ''
  for (const archivo of archivos) {
    if (fotos.value.length >= MAX_FOTOS) { errorFotos.value = `Puedes subir hasta ${MAX_FOTOS} fotos.`; break }
    if (!['image/jpeg', 'image/png'].includes(archivo.type)) { errorFotos.value = 'Solo se aceptan fotos JPG o PNG.'; continue }
    if (archivo.size > MAX_MB * 1024 * 1024) { errorFotos.value = `Cada foto puede pesar hasta ${MAX_MB} MB.`; continue }
    fotos.value.push({ nombre: archivo.name, url: URL.createObjectURL(archivo), archivo })
  }
}

function quitarFoto(i) {
  const foto = fotos.value[i]
  if (foto.ruta) quitadas.value.push(foto.ruta)
  else URL.revokeObjectURL(foto.url)
  fotos.value.splice(i, 1)
}

const inicio = computed(() => (yaEmpezo.value ? new Date(yaEmpezo.value) : form.empiezaAhora ? new Date() : new Date(form.fechaInicio)))
const paso1Listo = computed(() => fotos.value.length > 0 && form.titulo.trim().length >= 10 && !!form.categoriaId && !!form.descripcion.trim())
const paso2Listo = computed(() => form.precioSalida > 0 && (form.empiezaAhora || !!form.fechaInicio))

const fechaCierre = computed(() => new Date(inicio.value.getTime() + form.duracion * 86400000))
const empiezaDespues = computed(() => !form.empiezaAhora && inicio.value.getTime() > Date.now())
const ejemploVenta = computed(() => form.precioReserva || form.precioSalida || 9000)

const vistaPrevia = computed(() => ({
  id: 0,
  titulo: form.titulo || 'Título de tu artículo',
  foto: fotos.value[0]?.url ?? null,
  precioSalida: form.precioSalida || 0,
  pujaActual: null,
  numeroPujas: 0,
  fechaInicio: new Date().toISOString(),
  fechaCierre: fechaCierre.value.toISOString(),
}))

// Las mismas reglas que valida la API; aquí solo evitan un viaje al servidor.
function validar() {
  const e = {}
  if (!fotos.value.length) e.fotos = 'Agrega al menos una foto.'
  if (form.titulo.trim().length < 10) e.titulo = 'Escribe un título de al menos 10 caracteres.'
  if (!form.categoriaId) e.categoriaId = 'Elige una categoría.'
  if (!form.descripcion.trim()) e.descripcion = 'Describe el artículo.'
  if (!(form.precioSalida > 0)) e.precioSalida = 'El precio de salida debe ser mayor que 0.'
  if (form.precioReserva && form.precioReserva < form.precioSalida) e.precioReserva = 'La reserva no puede ser menor que el precio de salida.'
  if (!yaEmpezo.value && !form.empiezaAhora && !(new Date(form.fechaInicio).getTime() >= Date.now() - 60000)) e.fechaInicio = 'La fecha de inicio no puede estar en el pasado.'
  if (fechaCierre.value.getTime() <= Date.now()) e.duracion = 'Con esa duración la subasta ya habría terminado. Elige más días.'
  if (!form.retiroEnPersona && !form.envioADomicilio) e.entrega = 'Elige al menos una forma de entrega.'
  errores.value = e
  return !Object.keys(e).length
}

function datosDeLaSubasta() {
  return {
    titulo: form.titulo.trim(),
    descripcion: form.descripcion.trim(),
    condicion: form.condicion,
    categoriaId: Number(form.categoriaId),
    precioSalida: form.precioSalida,
    incrementoMinimo: form.incrementoMinimo,
    precioReserva: form.precioReserva || null,
    fechaInicio: inicio.value.toISOString(),
    duracionDias: form.duracion,
    extenderAlFinal: form.extenderAlFinal,
    permiteRetiro: form.retiroEnPersona,
    permiteEnvio: form.envioADomicilio,
  }
}

function subirNuevas(id) {
  const nuevas = fotos.value.filter(f => f.archivo)
  if (!nuevas.length) return null
  const datos = new FormData()
  nuevas.forEach(f => datos.append('fotos', f.archivo, f.nombre))
  return api(`/api/subastas/${id}/imagenes`, { metodo: 'POST', formulario: datos })
}

// Guarda los datos, borra las fotos quitadas y sube las nuevas. La API exige siempre al menos
// una foto y como máximo 10, así que se borra antes de subir salvo la última foto guardada.
async function guardarCambios() {
  await api(`/api/subastas/${editandoId}`, { metodo: 'PUT', cuerpo: datosDeLaSubasta() })
  const guardadas = fotos.value.filter(f => f.ruta).length + quitadas.value.length
  const pendientes = [...quitadas.value]
  const despues = guardadas === pendientes.length ? [pendientes.pop()] : []
  for (const ruta of pendientes) await api(`/api/subastas/${editandoId}/imagenes?ruta=${encodeURIComponent(ruta)}`, { metodo: 'DELETE' })
  quitadas.value = despues
  const subidas = await subirNuevas(editandoId)
  if (subidas) fotos.value = subidas.map((ruta, i) => ({ nombre: `Foto ${i + 1}`, url: urlFoto(ruta), ruta }))
  for (const ruta of despues) await api(`/api/subastas/${editandoId}/imagenes?ruta=${encodeURIComponent(ruta)}`, { metodo: 'DELETE' })
  quitadas.value = []
  router.push(`/subastas/${editandoId}`)
}

async function publicar() {
  errorGeneral.value = ''
  if (publicando.value || !validar()) return
  publicando.value = true
  if (editandoId) {
    try {
      await guardarCambios()
    } catch (e) {
      errores.value = { ...errores.value, ...e.errores }
      errorGeneral.value = e.message
    } finally {
      publicando.value = false
    }
    return
  }
  try {
    // Si la subasta ya se creó y fallaron las fotos, se reintenta solo la subida.
    creada.value ??= await api('/api/subastas', { metodo: 'POST', cuerpo: datosDeLaSubasta() })
    await subirNuevas(creada.value.id)
    router.push(`/subastas/${creada.value.id}`)
  } catch (e) {
    errores.value = { ...errores.value, ...e.errores }
    errorGeneral.value = creada.value
      ? `La subasta #${creada.value.id} se publicó, pero las fotos no se subieron: ${e.message} Vuelve a intentarlo.`
      : e.message
  } finally {
    publicando.value = false
  }
}
</script>

<template>
  <div class="contenedor py-4 d-flex flex-column gap-4">
    <div class="d-flex justify-content-between align-items-end">
      <div>
        <h1 class="fw-bold mb-1 titulo">{{ editandoId ? 'Editar subasta' : 'Publicar un artículo' }}</h1>
        <p v-if="editandoId" class="texto-gris mb-0">Puedes corregir la subasta mientras nadie haya pujado.</p>
        <p v-else class="texto-gris mb-0">Completa los datos y publica. La subasta queda visible en cuanto empieza.</p>
      </div>
    </div>

    <div class="panel pasos d-none d-lg-flex">
      <div class="paso">
        <span class="circulo" :class="paso1Listo ? 'hecho' : 'actual'"><Check v-if="paso1Listo" :size="16" :stroke-width="3" /><template v-else>1</template></span>
        <div><small class="texto-gris">Paso 1</small><div class="fw-medium">Fotos y detalles</div></div>
      </div>
      <span class="conector" :class="{ hecho: paso1Listo }" />
      <div class="paso">
        <span class="circulo" :class="{ hecho: paso1Listo && paso2Listo, actual: paso1Listo && !paso2Listo }"><Check v-if="paso1Listo && paso2Listo" :size="16" :stroke-width="3" /><template v-else>2</template></span>
        <div><small class="texto-gris">Paso 2</small><div class="fw-medium">Configurar la subasta</div></div>
      </div>
      <span class="conector" :class="{ hecho: paso1Listo && paso2Listo }" />
      <div class="paso">
        <span class="circulo" :class="{ actual: paso1Listo && paso2Listo }">3</span>
        <div><small class="texto-gris">Paso 3</small><div class="fw-medium">Revisar y publicar</div></div>
      </div>
    </div>

    <div class="dos-columnas">
      <form class="d-flex flex-column gap-4" novalidate @submit.prevent="publicar">
        <section class="panel d-flex flex-column gap-3">
          <div class="d-flex justify-content-between"><h2 class="fs-5 fw-bold mb-0">Fotos</h2><span class="texto-gris texto-pequeno">{{ fotos.length }} de {{ MAX_FOTOS }}</span></div>
          <div class="fotos">
            <div v-for="(f, i) in fotos" :key="f.url" class="foto">
              <img :src="f.url" :alt="f.nombre" />
              <span v-if="i === 0" class="portada">Portada</span>
              <button type="button" class="quitar" :aria-label="`Quitar ${f.nombre}`" @click="quitarFoto(i)"><X :size="14" /></button>
            </div>
            <label v-if="fotos.length < MAX_FOTOS" class="soltar" :class="{ arrastrando }" @dragover.prevent="arrastrando = true" @dragleave="arrastrando = false" @drop.prevent="soltarFotos">
              <Upload :size="22" class="texto-gris" />
              <span class="fw-medium texto-14">Arrastra fotos o haz clic para elegirlas</span>
              <small class="texto-gris">JPG o PNG, hasta {{ MAX_MB }} MB cada una</small>
              <input type="file" accept="image/jpeg,image/png" multiple class="visually-hidden" @change="agregarFotos" />
            </label>
          </div>
          <div v-if="errorFotos || errores.fotos" class="text-danger texto-pequeno">{{ errorFotos || errores.fotos }}</div>
          <div v-else class="form-text mt-0">Usa luz natural y fotografía también los detalles y los defectos. La primera foto es la portada.</div>
        </section>

        <section class="panel d-flex flex-column gap-3">
          <h2 class="fs-5 fw-bold mb-0">Detalles del artículo</h2>
          <div>
            <label for="titulo" class="form-label">Título</label>
            <input id="titulo" v-model="form.titulo" class="form-control" maxlength="80" :class="{ 'is-invalid': errores.titulo }" placeholder="Ej.: Cámara Canon AE-1 Program con lente 50 mm" />
            <div v-if="errores.titulo" class="invalid-feedback">{{ errores.titulo }}</div>
            <div v-else class="form-text">{{ form.titulo.length }} de 80 caracteres. Incluye marca, modelo y lo que trae.</div>
          </div>
          <div class="row g-3">
            <div class="col-md">
              <label for="categoria" class="form-label">Categoría</label>
              <select id="categoria" v-model="form.categoriaId" class="form-select" :class="{ 'is-invalid': errores.categoriaId }">
                <option value="" disabled>Elige una categoría</option>
                <template v-for="c in categorias" :key="c.id">
                  <optgroup v-if="c.subcategorias.length" :label="c.nombre">
                    <option v-for="sc in c.subcategorias" :key="sc.id" :value="sc.id">{{ c.nombre }} › {{ sc.nombre }}</option>
                  </optgroup>
                  <option v-else :value="c.id">{{ c.nombre }}</option>
                </template>
              </select>
              <div class="invalid-feedback">{{ errores.categoriaId }}</div>
            </div>
            <div class="col-md">
              <label for="condicion" class="form-label">Estado</label>
              <select id="condicion" v-model="form.condicion" class="form-select">
                <option v-for="(texto, valor) in CONDICIONES" :key="valor" :value="valor">{{ texto }}</option>
              </select>
            </div>
          </div>
          <div>
            <label for="descripcion" class="form-label">Descripción</label>
            <textarea id="descripcion" v-model="form.descripcion" class="form-control" rows="5" maxlength="2000" :class="{ 'is-invalid': errores.descripcion }" />
            <div v-if="errores.descripcion" class="invalid-feedback">{{ errores.descripcion }}</div>
            <div v-else class="form-text">Cuenta cómo está, qué incluye y si tiene algún defecto.</div>
          </div>
        </section>

        <section class="panel d-flex flex-column gap-3">
          <h2 class="fs-5 fw-bold mb-0">Subasta</h2>
          <div class="row g-3">
            <div class="col-md">
              <label for="salida" class="form-label">Precio de salida</label>
              <div class="input-group has-validation">
                <span class="input-group-text">RD$</span>
                <input id="salida" v-model.number="form.precioSalida" type="number" min="1" step="50" class="form-control" :class="{ 'is-invalid': errores.precioSalida }" />
                <div class="invalid-feedback">{{ errores.precioSalida }}</div>
              </div>
              <div v-if="!errores.precioSalida" class="form-text">La primera puja empieza aquí.</div>
            </div>
            <div class="col-md">
              <label for="reserva" class="form-label">Precio de reserva (opcional)</label>
              <div class="input-group has-validation">
                <span class="input-group-text">RD$</span>
                <input id="reserva" v-model.number="form.precioReserva" type="number" min="1" step="50" class="form-control" :class="{ 'is-invalid': errores.precioReserva }" />
                <div class="invalid-feedback">{{ errores.precioReserva }}</div>
              </div>
              <div v-if="!errores.precioReserva" class="form-text">No se vende por menos de esto.</div>
            </div>
            <div class="col-md">
              <label for="incremento" class="form-label">Incremento mínimo</label>
              <select id="incremento" v-model.number="form.incrementoMinimo" class="form-select">
                <option v-for="i in incrementos" :key="i" :value="i">RD$ {{ i.toLocaleString('en-US') }}</option>
              </select>
            </div>
          </div>
          <div class="row g-3">
            <div class="col-md-5">
              <span class="form-label d-block">Inicio</span>
              <p v-if="yaEmpezo" class="mb-0 pt-1">Empezó el {{ fechaConHora(yaEmpezo) }}</p>
              <template v-else>
              <div class="form-check form-check-inline">
                <input id="inicio-ahora" v-model="form.empiezaAhora" class="form-check-input" type="radio" :value="true" />
                <label class="form-check-label" for="inicio-ahora">Al publicar</label>
              </div>
              <div class="form-check form-check-inline">
                <input id="inicio-fecha" v-model="form.empiezaAhora" class="form-check-input" type="radio" :value="false" />
                <label class="form-check-label" for="inicio-fecha">En una fecha</label>
              </div>
              <input v-if="!form.empiezaAhora" id="inicio" v-model="form.fechaInicio" type="datetime-local" aria-label="Fecha de inicio" class="form-control mt-2" :class="{ 'is-invalid': errores.fechaInicio }" />
              <div class="invalid-feedback">{{ errores.fechaInicio }}</div>
              </template>
            </div>
            <fieldset class="col-md">
              <legend class="form-label">Duración</legend>
              <div class="d-flex gap-4 pt-2">
                <div v-for="d in duraciones" :key="d" class="form-check">
                  <input :id="`dur-${d}`" v-model.number="form.duracion" class="form-check-input" type="radio" :value="d" />
                  <label class="form-check-label" :for="`dur-${d}`">{{ d }} {{ d === 1 ? 'día' : 'días' }}</label>
                </div>
              </div>
              <div v-if="errores.duracion" class="text-danger texto-pequeno">{{ errores.duracion }}</div>
            </fieldset>
          </div>
          <hr class="my-1" />
          <div class="form-check mb-0">
            <input id="extender" v-model="form.extenderAlFinal" type="checkbox" class="form-check-input" />
            <label for="extender" class="form-check-label">Extender 2 minutos si alguien puja al final</label>
          </div>
          <div class="d-flex flex-column gap-2">
            <span class="fw-medium texto-14">Entrega</span>
            <div class="d-flex gap-4">
              <div class="form-check mb-0">
                <input id="retiro" v-model="form.retiroEnPersona" type="checkbox" class="form-check-input" />
                <label for="retiro" class="form-check-label">Retiro en persona</label>
              </div>
              <div class="form-check mb-0">
                <input id="envio" v-model="form.envioADomicilio" type="checkbox" class="form-check-input" />
                <label for="envio" class="form-check-label">Envío a domicilio</label>
              </div>
            </div>
            <div v-if="errores.entrega" class="text-danger texto-pequeno">{{ errores.entrega }}</div>
          </div>
        </section>

        <div v-if="errorGeneral" class="alert alert-danger mb-0" role="alert">{{ errorGeneral }}</div>
        <div class="d-flex gap-3 flex-wrap align-items-center">
          <button type="submit" class="btn btn-primary btn-lg" :disabled="publicando || cargandoEdicion">{{ publicando ? 'Guardando…' : editandoId ? 'Guardar cambios' : 'Publicar subasta' }}</button>
          <RouterLink :to="editandoId ? `/subastas/${editandoId}` : '/'" class="btn btn-link btn-lg">Cancelar</RouterLink>
        </div>
      </form>

      <aside class="vista d-flex flex-column gap-3">
        <strong class="fs-6">Así se verá tu subasta</strong>
        <SubastaCard :subasta="vistaPrevia" :ahora="Date.now()" :texto-tiempo="empiezaDespues ? `Empieza el ${diaCorto(inicio)}` : ''" />
        <div class="alerta-info">
          <Info :size="20" class="flex-shrink-0" />
          <div>
            <strong class="d-block">Comisión de venta</strong>
            Pagas {{ COMISION * 100 }} % del precio final solo si se vende. Si se vende en {{ rd(ejemploVenta) }}, recibes {{ rd(Math.round(ejemploVenta * (1 - COMISION))) }}.
          </div>
        </div>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.titulo {
  font-size: 30px;
}

.pasos {
  align-items: center;
  gap: 20px;
  padding: 18px 24px;
}

.paso {
  display: flex;
  align-items: center;
  gap: 10px;
}

.circulo {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  border: 1px solid var(--pj-gris-claro);
  color: var(--pj-gris);
  font-weight: 700;
  font-size: 14px;
}

.circulo.hecho {
  background: var(--pj-exito);
  border-color: var(--pj-exito);
  color: #fff;
}

.circulo.actual {
  background: var(--pj-primario);
  border-color: var(--pj-primario);
  color: #fff;
}

.conector {
  flex: 1;
  height: 2px;
  background: var(--pj-borde);
}

.conector.hecho {
  background: var(--pj-exito);
}

.dos-columnas {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 384px;
  gap: 24px;
  align-items: start;
}

.fotos {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.foto {
  position: relative;
  width: 116px;
  height: 116px;
  border-radius: 8px;
  overflow: hidden;
}

.foto img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.portada {
  position: absolute;
  top: 8px;
  left: 8px;
  padding: 2px 6px;
  border-radius: 4px;
  background: #e9ecef;
  font-size: 12px;
  font-weight: 500;
}

.quitar {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 24px;
  height: 24px;
  border: 0;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.9);
  display: grid;
  place-items: center;
}

.texto-14 {
  font-size: 14px;
}

.alerta-info {
  display: flex;
  gap: 12px;
  padding: 14px 16px;
  background: #cfe2ff;
  border: 1px solid #9ec5fe;
  border-radius: 6px;
  color: #052c65;
  font-size: 14px;
  line-height: 20px;
}

.alerta-info strong {
  font-size: 15px;
}

.soltar.arrastrando {
  border-color: var(--pj-primario);
  background: var(--pj-primario-suave);
}

.soltar {
  flex: 1;
  min-width: 220px;
  height: 116px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  background: var(--pj-claro);
  border: 1px dashed var(--pj-gris-claro);
  border-radius: 8px;
  cursor: pointer;
}

@media (max-width: 991.98px) {
  .titulo {
    font-size: 24px;
  }

  .dos-columnas {
    grid-template-columns: 1fr;
  }

  .vista {
    display: none !important;
  }
}
</style>
