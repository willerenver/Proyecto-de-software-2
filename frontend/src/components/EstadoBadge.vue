<script setup>
// Variantes del componente «Badge» del Figma.
defineProps({
  estado: { type: String, required: true },
  texto: { type: String, default: '' },
})
</script>

<template>
  <span class="badge-pj" :class="estado.toLowerCase().replaceAll(' ', '-')">{{ texto || estado }}</span>
</template>

<style scoped>
.badge-pj {
  display: inline-block;
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
  white-space: nowrap;
  background: #e9ecef;
  color: #495057;
}

.activa,
.vas-ganando {
  background: #d1e7dd;
  color: #0a3622;
}

.cierra-pronto {
  background: var(--mp-peligro);
  color: #fff;
}

.superado {
  background: #f8d7da;
  color: #842029;
}

.nueva {
  background: var(--mp-primario);
  color: #fff;
}

.pendiente {
  background: #fff3cd;
  color: #664d03;
}
</style>
