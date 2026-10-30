<script setup>
import { computed } from 'vue'

const props = defineProps({
  fecha: { type: String, required: true },
  ahora: { type: Number, required: true },
})

const bloques = computed(() => {
  const s = Math.max(0, Math.floor((new Date(props.fecha).getTime() - props.ahora) / 1000))
  const dos = n => String(n).padStart(2, '0')
  return [
    [dos(Math.floor(s / 86400)), 'días'],
    [dos(Math.floor((s % 86400) / 3600)), 'horas'],
    [dos(Math.floor((s % 3600) / 60)), 'min'],
    [dos(s % 60), 'seg'],
  ]
})
</script>

<template>
  <div class="contador" role="timer">
    <div v-for="[n, l] in bloques" :key="l" class="bloque">
      <strong>{{ n }}</strong>
      <span>{{ l }}</span>
    </div>
  </div>
</template>

<style scoped>
.contador {
  display: flex;
  gap: 8px;
}

.bloque {
  width: 70px;
  padding: 8px 0;
  background: var(--mp-claro);
  border: 1px solid var(--mp-borde);
  border-radius: 6px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.bloque strong {
  font-size: 24px;
  line-height: 1.1;
}

.bloque span {
  font-size: 11px;
  color: var(--mp-gris);
}
</style>
