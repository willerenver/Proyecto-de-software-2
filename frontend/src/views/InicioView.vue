<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bike, Camera, Gamepad2, Gem, House, Monitor, Music, Smartphone, Watch } from 'lucide-vue-next'
import SubastaCard from '../components/SubastaCard.vue'
import SubastaMini from '../components/SubastaMini.vue'
import { api } from '../api/cliente'
import { categorias, cargarCategorias, nombreCategoria } from '../stores/categorias'
import { useAhora } from '../composables/useAhora'
import { useSondeo } from '../composables/useSondeo'
import bannerFoto from '../assets/fotos/ps5.jpg'

// Íconos por id de categoría (Datos/CatalogoCategorias.cs en la API).
const Iconos = { 1: Smartphone, 2: Monitor, 3: Gamepad2, 4: Music, 5: House, 6: Bike, 7: Watch, 8: Camera, 9: Gem }
const ordenes = { cierre: 'cierre más próximo', precio: 'precio más bajo', pujas: 'más pujas' }

const route = useRoute()
const router = useRouter()
const ahora = useAhora(30000)
const subastas = ref([])
const cargando = ref(true)
const error = ref('')

const filtro = computed(() => ({
  categoria: route.query.categoria ?? '',
  buscar: route.query.buscar ?? '',
  orden: ordenes[route.query.orden] ? route.query.orden : 'cierre',
}))
const filtrando = computed(() => !!(filtro.value.categoria || filtro.value.buscar))

async function cargar() {
  const q = new URLSearchParams({ orden: filtro.value.orden })
  if (filtro.value.categoria) q.set('categoriaId', filtro.value.categoria)
  if (filtro.value.buscar) q.set('buscar', filtro.value.buscar)
  try {
    subastas.value = await api(`/api/subastas?${q}`)
    error.value = ''
  } catch (e) {
    error.value = e.message
  } finally {
    cargando.value = false
  }
}

watch(filtro, () => { cargando.value = true; cargar() }, { immediate: true })
useSondeo(cargar, 30000)
cargarCategorias().catch(() => {})

// Las que ya vencieron desaparecen sin esperar a la próxima consulta.
const activas = computed(() => subastas.value.filter(s => new Date(s.fechaCierre).getTime() > ahora.value))
const terminanEnUnaHora = computed(() =>
  [...activas.value].sort((a, b) => new Date(a.fechaCierre) - new Date(b.fechaCierre))
    .filter(s => new Date(s.fechaCierre).getTime() - ahora.value < 3600000).slice(0, 3))

const tituloLista = computed(() => {
  if (filtro.value.buscar) return `Resultados para «${filtro.value.buscar}»`
  if (filtro.value.categoria) return nombreCategoria(filtro.value.categoria) || 'Categoría'
  return filtro.value.orden === 'cierre' ? 'Terminan pronto' : 'Subastas activas'
})

function filtrar(cambios) {
  const query = { ...route.query, ...cambios }
  for (const k of Object.keys(query)) if (!query[k] || (k === 'orden' && query[k] === 'cierre')) delete query[k]
  router.push({ name: 'inicio', query })
}

const pasos = [
  ['Publica tu artículo', 'Sube fotos reales, describe el estado y elige el precio de salida y cuántos días dura la subasta.'],
  ['Recibe pujas', 'Cada puja debe superar a la anterior por el incremento mínimo. Si alguien puja al final, el cierre se extiende 2 minutos.'],
  ['Coordina la entrega', 'Al terminar el tiempo gana la puja más alta. El ganador y el vendedor ven los datos de contacto del otro para acordar el pago.'],
]
</script>

<template>
  <!-- Chips de categorías (móvil) -->
  <div class="chips d-lg-none">
    <button type="button" class="chip" :class="{ activo: !filtro.categoria }" @click="filtrar({ categoria: '' })">Todas</button>
    <button v-for="c in categorias" :key="c.id" type="button" class="chip" :class="{ activo: filtro.categoria === String(c.id) }" @click="filtrar({ categoria: String(c.id) })">{{ c.nombre }}</button>
  </div>

  <div class="contenedor py-3 py-lg-4 d-flex flex-column gap-4">
    <template v-if="!filtrando">
      <section class="banner-movil d-lg-none">
        <h1>Subasta lo que ya no usas</h1>
        <p>Publicar es gratis. Solo pagas si se vende.</p>
        <RouterLink to="/publicar" class="btn btn-pujar">Publicar un artículo</RouterLink>
      </section>

      <section class="d-none d-lg-flex gap-4 destacados">
        <div class="banner flex-grow-1">
          <div class="texto">
            <h1>Subasta lo que ya no usas.<br />Alguien lo está buscando.</h1>
            <p>Publicar es gratis. Pones el precio de salida, la gente puja y solo pagas comisión si se vende.</p>
            <div class="d-flex gap-3">
              <RouterLink to="/publicar" class="btn btn-pujar btn-lg">Publicar un artículo</RouterLink>
              <a href="#como-funciona" class="btn btn-light btn-lg">Ver cómo funciona</a>
            </div>
          </div>
          <img :src="bannerFoto" alt="" />
        </div>
        <aside class="panel terminan">
          <div class="d-flex justify-content-between align-items-center">
            <strong class="fs-6">Terminan en menos de 1 hora</strong>
            <a href="#lista" class="texto-pequeno">Ver todas</a>
          </div>
          <template v-for="(s, i) in terminanEnUnaHora" :key="s.id">
            <hr v-if="i" class="my-0" />
            <SubastaMini :subasta="s" :ahora="ahora" />
          </template>
          <p v-if="!terminanEnUnaHora.length" class="texto-gris mb-0">No hay subastas por cerrar en la próxima hora.</p>
        </aside>
      </section>

      <section class="d-none d-lg-block">
        <h2 class="fs-5 fw-bold mb-3">Explora por categoría</h2>
        <div class="categorias">
          <RouterLink v-for="c in categorias.filter(c => c.id !== 9)" :key="c.id" :to="{ name: 'inicio', query: { categoria: c.id } }" class="categoria">
            <span class="icono"><component :is="Iconos[c.id] ?? Gem" :size="22" /></span>
            <span class="fw-medium texto-pequeno">{{ c.nombre }}</span>
          </RouterLink>
        </div>
      </section>
    </template>

    <section id="lista">
      <div class="d-flex justify-content-between align-items-end gap-3 mb-3 flex-wrap">
        <div>
          <h2 class="fw-bold mb-0 titulo-lista">{{ tituloLista }}</h2>
          <span class="texto-gris texto-pequeno">{{ activas.length }} {{ activas.length === 1 ? 'subasta activa' : 'subastas activas' }}</span>
        </div>
        <div class="d-flex align-items-center gap-3">
          <label class="d-flex align-items-center gap-2 texto-pequeno">
            <span class="texto-gris d-none d-lg-inline">Ordenar:</span>
            <select class="form-select form-select-sm ordenar" :value="filtro.orden" aria-label="Ordenar" @change="filtrar({ orden: $event.target.value })">
              <option v-for="(t, valor) in ordenes" :key="valor" :value="valor">{{ t }}</option>
            </select>
          </label>
          <RouterLink v-if="filtrando" to="/" class="fw-medium ver-todas">Ver todas</RouterLink>
        </div>
      </div>
      <div v-if="error" class="alert alert-danger d-flex justify-content-between align-items-center">
        <span>{{ error }}</span>
        <button type="button" class="btn btn-sm btn-light" @click="cargar">Reintentar</button>
      </div>
      <p v-else-if="cargando && !subastas.length" class="texto-gris">Cargando subastas…</p>
      <div v-else-if="!activas.length" class="panel p-4 text-center">
        <p class="fw-medium mb-1">No hay subastas activas{{ filtrando ? ' con esos filtros' : '' }}.</p>
        <p class="texto-gris mb-3">¿Tienes algo que ya no usas? Publícalo y recibe pujas.</p>
        <RouterLink to="/publicar" class="btn btn-pujar">Publicar un artículo</RouterLink>
      </div>
      <div v-else class="grilla">
        <SubastaCard v-for="s in activas" :key="s.id" :subasta="s" :ahora="ahora" />
      </div>
    </section>

    <section v-if="!filtrando" id="como-funciona" class="panel p-4 p-lg-5 d-none d-lg-block">
      <h2 class="fs-5 fw-bold mb-4">Cómo funciona</h2>
      <div class="row g-5">
        <div v-for="([t, d], i) in pasos" :key="t" class="col">
          <span class="numero">{{ i + 1 }}</span>
          <h3 class="fs-6 fw-bold mt-3">{{ t }}</h3>
          <p class="texto-gris mb-0">{{ d }}</p>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.banner {
  display: flex;
  min-height: 300px;
  background: var(--pj-primario);
  border-radius: 10px;
  overflow: hidden;
}

.banner .texto {
  flex: 1;
  padding: 0 44px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 16px;
}

.banner h1 {
  color: #fff;
  font-size: 34px;
  font-weight: 700;
  line-height: 42px;
  margin: 0;
}

.banner p {
  color: #d6e4f5;
  font-size: 16px;
  line-height: 24px;
  margin: 0;
}

.banner img {
  width: 360px;
  object-fit: cover;
}

.banner-movil {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 12px;
  padding: 20px;
  background: var(--pj-primario);
  border-radius: 10px;
}

.banner-movil h1 {
  color: #fff;
  font-size: 22px;
  font-weight: 700;
  line-height: 28px;
  margin: 0;
}

.banner-movil p {
  color: #d6e4f5;
  font-size: 14px;
  margin: 0;
}

.ordenar {
  width: auto;
  font-size: 14px;
}

.ver-todas {
  font-size: 15px;
}

.terminan {
  width: 384px;
  flex-shrink: 0;
  flex-direction: column;
  gap: 14px;
  padding: 20px;
}

.categorias {
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 16px;
}

.categoria {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 18px 8px;
  background: #fff;
  border: 1px solid var(--pj-borde);
  border-radius: 8px;
  color: var(--pj-texto);
}

.categoria .icono {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: var(--pj-primario-suave);
  color: var(--pj-primario);
}

.titulo-lista {
  font-size: 22px;
}

.grilla {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 24px;
}

.numero {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: inline-grid;
  place-items: center;
  background: var(--pj-primario);
  color: #fff;
  font-weight: 700;
}

.chips {
  display: flex;
  gap: 8px;
  padding: 12px 16px;
  overflow-x: auto;
  background: #fff;
  border-bottom: 1px solid var(--pj-borde);
}

.chip {
  background: #fff;
  color: var(--pj-texto);
  padding: 6px 14px;
  border-radius: 16px;
  border: 1px solid var(--pj-borde);
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
}

.chip.activo {
  background: var(--pj-primario);
  border-color: var(--pj-primario);
  color: #fff;
}

@media (max-width: 1199.98px) {
  .grilla {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 991.98px) {
  .titulo-lista {
    font-size: 18px;
  }

  .ver-todas {
    font-size: 14px;
    font-weight: 400 !important;
  }

  .grilla {
    grid-template-columns: repeat(2, 1fr);
    gap: 12px;
  }
}
</style>
