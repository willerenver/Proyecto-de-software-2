<script setup>
import LogoElMejorPostor from '../LogoElMejorPostor.vue'

const columnas = [
  { titulo: 'Comprar', enlaces: ['Cómo pujar', 'Subastas que terminan hoy', 'Categorías'] },
  { titulo: 'Vender', enlaces: ['Publicar un artículo', 'Comisiones', 'Consejos para tus fotos'] },
  { titulo: 'Ayuda', enlaces: ['Preguntas frecuentes', 'Reglas de las subastas', 'Contacto'] },
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
          <a v-for="e in c.enlaces" :key="e" href="#" @click.prevent>{{ e }}</a>
        </div>
      </div>
      <div class="legal d-flex justify-content-between pt-4">
        <span>© 2026 El Mejor Postor · Proyecto universitario</span>
        <span>Términos · Privacidad</span>
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
