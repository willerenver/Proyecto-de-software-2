<script setup>
defineProps({
  iniciales: { type: String, required: true },
  tam: { type: Number, default: 36 },
})
</script>

<template>
  <span class="avatar" :style="{ width: `${tam}px`, height: `${tam}px`, fontSize: `${Math.round(tam * 0.38)}px` }">{{ iniciales }}</span>
</template>

<style scoped>
.avatar {
  display: inline-grid;
  place-items: center;
  flex-shrink: 0;
  border-radius: 50%;
  background: var(--mp-primario-suave);
  color: var(--mp-primario);
  font-weight: 700;
}
</style>
