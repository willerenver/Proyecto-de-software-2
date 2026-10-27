<script setup>
import { ref } from 'vue'
import { X } from 'lucide-vue-next'
import { urlFoto } from '../api/cliente'
import { rd, tiempoRestante } from '../utils/formato'

// Recibe la subasta como la devuelve GET /api/subastas/{id}.
defineProps({
  subasta: { type: Object, required: true },
  monto: { type: Number, required: true },
  ahora: { type: Number, required: true },
  enviando: { type: Boolean, default: false },
})
const emit = defineEmits(['cancelar', 'confirmar'])
const acepta = ref(true)
</script>

<template>
  <div class="velo" @click.self="emit('cancelar')">
    <div class="modal-pj" role="dialog" aria-modal="true" aria-labelledby="titulo-confirmar">
      <header>
        <h2 id="titulo-confirmar">Confirma tu puja</h2>
        <button type="button" class="cerrar" aria-label="Cerrar" @click="emit('cancelar')"><X :size="20" /></button>
      </header>
      <div class="cuerpo">
        <div class="d-flex gap-3 align-items-center">
          <img v-if="subasta.fotos.length" :src="urlFoto(subasta.fotos[0])" alt="" />
          <div>
            <div class="fw-medium">{{ subasta.titulo }}</div>
            <div class="texto-pequeno texto-gris">Subasta #{{ subasta.id }} · termina en {{ tiempoRestante(subasta.fechaCierre, ahora) }}</div>
          </div>
        </div>
        <div class="montos">
          <div class="d-flex justify-content-between"><span class="texto-gris">Puja actual</span><span class="fw-medium">{{ rd(subasta.pujaActual ?? subasta.precioSalida) }}</span></div>
          <div class="d-flex justify-content-between align-items-center"><span class="texto-gris">Tu puja</span><strong class="fs-5">{{ rd(monto) }}</strong></div>
        </div>
        <p class="texto-gris mb-0">Si ganas, te comprometes a pagar dentro de las 48 horas siguientes al cierre. Una puja no se puede retirar.</p>
        <div class="form-check">
          <input id="acepta" v-model="acepta" class="form-check-input" type="checkbox" />
          <label class="form-check-label" for="acepta">Leí y acepto las reglas de las subastas</label>
        </div>
      </div>
      <footer>
        <button type="button" class="btn btn-light" @click="emit('cancelar')">Cancelar</button>
        <button type="button" class="btn btn-pujar" :disabled="!acepta || enviando" @click="emit('confirmar')">{{ enviando ? 'Registrando…' : `Confirmar puja de ${rd(monto)}` }}</button>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.velo {
  position: fixed;
  inset: 0;
  z-index: 1050;
  background: rgba(0, 0, 0, 0.5);
  display: grid;
  place-items: center;
  padding: 16px;
}

.modal-pj {
  width: 500px;
  max-width: 100%;
  background: #fff;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.25);
}

header,
footer {
  display: flex;
  align-items: center;
  padding: 16px 20px;
}

header {
  justify-content: space-between;
  border-bottom: 1px solid var(--pj-borde);
}

header h2 {
  font-size: 20px;
  font-weight: 700;
  margin: 0;
}

.cerrar {
  border: 0;
  background: none;
  color: var(--pj-gris);
}

.cuerpo {
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  font-size: 15px;
}

.cuerpo img {
  width: 64px;
  height: 64px;
  border-radius: 6px;
  object-fit: cover;
}

.montos {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 16px;
  background: var(--pj-claro);
  border-radius: 6px;
}

footer {
  justify-content: flex-end;
  gap: 10px;
  border-top: 1px solid var(--pj-borde);
}
</style>
