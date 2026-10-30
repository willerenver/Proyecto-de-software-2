<script setup>
import { Clock } from 'lucide-vue-next'
import { urlFoto } from '../api/cliente'
import { rd, tiempoRestante } from '../utils/formato'

defineProps({
  subasta: { type: Object, required: true },
  ahora: { type: Number, required: true },
})
</script>

<template>
  <RouterLink :to="`/subastas/${subasta.id}`" class="mini">
    <img v-if="subasta.foto" :src="urlFoto(subasta.foto)" :alt="subasta.titulo" />
    <span v-else class="sin-foto" />
    <div class="flex-grow-1 min-w-0">
      <div class="titulo">{{ subasta.titulo }}</div>
      <div class="fw-bold">{{ rd(subasta.pujaActual ?? subasta.precioSalida) }}</div>
      <div class="restante"><Clock :size="12" /> Termina en {{ tiempoRestante(subasta.fechaCierre, ahora) }}</div>
    </div>
  </RouterLink>
</template>

<style scoped>
.mini {
  display: flex;
  align-items: center;
  gap: 12px;
  color: var(--mp-texto);
}

.mini img,
.sin-foto {
  width: 64px;
  height: 64px;
  border-radius: 6px;
  object-fit: cover;
  flex-shrink: 0;
}

.sin-foto {
  background: #e9ecef;
}

.titulo {
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.restante {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  font-weight: 500;
  color: var(--mp-peligro);
}

.min-w-0 {
  min-width: 0;
}
</style>
