<script setup>
import LogoElMejorPostor from '../LogoElMejorPostor.vue'

const columnas = [
  {
    titulo: 'Comprar',
    enlaces: [
      { texto: 'Subastas abiertas', a: '/' },
      { texto: 'Cómo pujar', a: '/ayuda#pujar' },
      { texto: 'Mis pujas', a: '/mis-pujas' },
    ],
  },
  {
    titulo: 'Vender',
    enlaces: [
      { texto: 'Publicar un artículo', a: '/publicar' },
      { texto: 'Mis ventas', a: '/mis-ventas' },
      { texto: 'Consejos para tus fotos', a: '/ayuda#vender' },
    ],
  },
  {
    titulo: 'Ayuda',
    enlaces: [
      { texto: 'Reglas de las subastas', a: '/ayuda#reglas' },
      { texto: 'Pago y entrega', a: '/ayuda#pago' },
      { texto: 'Todas las preguntas', a: '/ayuda' },
    ],
  },
]
</script>

<template>
  <footer class="pie d-none d-lg-block">
    <div class="contenedor pt-5 pb-4">
      <div class="d-flex gap-5 mb-4">
        <div class="marca">
          <LogoElMejorPostor claro />
          <p class="mt-3 mb-0">Compra y vende por subasta con gente de tu ciudad.</p>
        </div>
        <div v-for="c in columnas" :key="c.titulo" class="d-flex flex-column gap-2">
          <strong class="text-white">{{ c.titulo }}</strong>
          <RouterLink v-for="e in c.enlaces" :key="e.a" :to="e.a">{{ e.texto }}</RouterLink>
        </div>
      </div>
      <div class="legal d-flex justify-content-between pt-4">
        <span>© 2026 El Mejor Postor · Proyecto universitario</span>
        <RouterLink to="/ayuda#privacidad">Privacidad</RouterLink>
      </div>
    </div>
  </footer>
</template>

<style scoped>
.pie {
  background: var(--mp-primario-oscuro);
  color: #b8c7da;
  font-size: 14px;
}

.marca {
  width: 300px;
  margin-right: 32px;
}

.pie a {
  color: #b8c7da;
}

.pie a:hover {
  color: #fff;
}

.legal {
  border-top: 1px solid rgba(255, 255, 255, 0.15);
  color: #8ea2bc;
  font-size: 13px;
}
</style>
