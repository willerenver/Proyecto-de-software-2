<script setup>
import { CircleCheck, X } from 'lucide-vue-next'

defineProps({
  titulo: { type: String, required: true },
  texto: { type: String, required: true },
})
const emit = defineEmits(['cerrar'])
</script>

<template>
  <div class="toast-pj" role="status">
    <CircleCheck :size="20" class="icono" />
    <div class="flex-grow-1">
      <strong>{{ titulo }}</strong>
      <p class="mb-0">{{ texto }}</p>
    </div>
    <button type="button" aria-label="Cerrar" @click="emit('cerrar')"><X :size="18" /></button>
  </div>
</template>

<style scoped>
.toast-pj {
  position: fixed;
  top: 130px;
  right: max(16px, calc((100vw - 1440px) / 2 + 120px));
  z-index: 1060;
  width: 380px;
  max-width: calc(100vw - 32px);
  display: flex;
  gap: 12px;
  padding: 14px 16px;
  background: var(--pj-texto);
  color: #fff;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
}

.icono {
  color: #75b798;
  flex-shrink: 0;
}

p {
  color: #ced4da;
  font-size: 14px;
  line-height: 20px;
}

button {
  border: 0;
  background: none;
  color: #adb5bd;
  align-self: flex-start;
}

@media (max-width: 991.98px) {
  .toast-pj {
    top: 16px;
    right: 16px;
  }
}
</style>
