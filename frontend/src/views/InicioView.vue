<script setup>
import { computed } from 'vue'
import { Bike, Camera, ChevronDown, Gamepad2, House, Monitor, Music, Smartphone, Watch } from 'lucide-vue-next'
import SubastaCard from '../components/SubastaCard.vue'
import SubastaMini from '../components/SubastaMini.vue'
import { categorias, subastas } from '../data/subastas'
import { useAhora } from '../composables/useAhora'
import bannerFoto from '../assets/fotos/ps5.jpg'

const chips = ['Todas', 'Celulares', 'Videojuegos', 'Instrumentos', 'Relojes']
const Iconos = { Bike, Camera, Gamepad2, House, Monitor, Music, Smartphone, Watch }
const ahora = useAhora(30000)
const activas = computed(() =>
  subastas
    .filter(s => !s.oculta && new Date(s.fechaCierre).getTime() > ahora.value)
    .sort((a, b) => new Date(a.fechaCierre) - new Date(b.fechaCierre)),
)
const terminanEnUnaHora = computed(() => activas.value.filter(s => new Date(s.fechaCierre).getTime() - ahora.value < 3600000).slice(0, 3))

const pasos = [
  ['Publica tu artículo', 'Sube fotos reales, describe el estado y elige el precio de salida y cuántos días dura la subasta.'],
  ['Recibe pujas', 'Cada puja debe superar a la anterior por el incremento mínimo. Te avisamos cada vez que alguien puja.'],
  ['Cobra y entrega', 'Al terminar el tiempo gana la puja más alta. El comprador paga en 48 horas y coordinan la entrega.'],
]
</script>

<template>
  <!-- Chips de categorías (móvil) -->
  <div class="chips d-lg-none">
    <span v-for="(c, i) in chips" :key="c" class="chip" :class="{ activo: i === 0 }">{{ c }}</span>
  </div>

  <div class="contenedor py-3 py-lg-4 d-flex flex-column gap-4">
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
        <a v-for="c in categorias.filter(c => !c.soloBarra)" :key="c.id" href="#" class="categoria" @click.prevent>
          <span class="icono"><component :is="Iconos[c.icono]" :size="22" /></span>
          <span class="fw-medium texto-pequeno">{{ c.nombre }}</span>
        </a>
      </div>
    </section>

    <section id="lista">
      <div class="d-flex justify-content-between align-items-end mb-3">
        <div>
          <h2 class="fw-bold mb-0 titulo-lista">Terminan pronto</h2>
          <span class="texto-gris texto-pequeno d-none d-lg-inline">{{ activas.length }} subastas activas, ordenadas por hora de cierre</span>
        </div>
        <div class="d-flex align-items-center gap-3">
          <button type="button" class="btn btn-light ordenar d-none d-lg-inline-flex">Ordenar: cierre más próximo <ChevronDown :size="16" /></button>
          <a href="#" class="fw-medium ver-todas" @click.prevent>Ver todas</a>
        </div>
      </div>
      <div class="grilla">
        <SubastaCard v-for="s in activas" :key="s.id" :subasta="s" :ahora="ahora" />
      </div>
    </section>

    <section id="como-funciona" class="panel p-4 p-lg-5 d-none d-lg-block">
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
  align-items: center;
  gap: 8px;
  font-size: 14px;
  padding: 7px 12px;
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

  /* En el móvil el Figma muestra solo cuatro tarjetas. */
  .grilla > :nth-child(n + 5) {
    display: none;
  }
}
</style>
