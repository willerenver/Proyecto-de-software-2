<script setup>
import { computed } from 'vue'
import { Clock, Image } from 'lucide-vue-next'
import EstadoBadge from './EstadoBadge.vue'
import { urlFoto } from '../api/cliente'
import { cierraPronto, diaCorto, rd, tiempoRestante } from '../utils/formato'

// Componente «Card de subasta» del Figma. Recibe una subasta como la devuelve GET /api/subastas.
const props = defineProps({
  subasta: { type: Object, required: true },
  ahora: { type: Number, required: true },
  // Para la vista previa de «Publicar»: «Empieza el 20 oct».
  textoTiempo: { type: String, default: '' },
})

const programada = computed(() => new Date(props.subasta.fechaInicio).getTime() > props.ahora)
const urgente = computed(() => !props.textoTiempo && !programada.value && cierraPronto(props.subasta.fechaCierre, props.ahora))
const monto = computed(() => props.subasta.pujaActual ?? props.subasta.precioSalida)
const tiempo = computed(() => props.textoTiempo || (programada.value ? `Empieza el ${diaCorto(props.subasta.fechaInicio)}` : tiempoRestante(props.subasta.fechaCierre, props.ahora)))
const enlace = computed(() => (props.subasta.id ? `/subastas/${props.subasta.id}` : ''))
</script>

<template>
  <article class="card-subasta">
    <component :is="enlace ? 'RouterLink' : 'div'" :to="enlace || undefined" class="imagen">
      <img v-if="subasta.foto" :src="urlFoto(subasta.foto)" :alt="subasta.titulo" loading="lazy" />
      <span v-else class="sin-foto"><Image :size="32" /></span>
      <EstadoBadge v-if="urgente" estado="Cierra pronto" class="etiqueta" />
    </component>
    <div class="cuerpo">
      <component :is="enlace ? 'RouterLink' : 'div'" :to="enlace || undefined" class="titulo">{{ subasta.titulo }}</component>
      <div class="precio">
        <div class="etiqueta-precio">{{ subasta.numeroPujas ? 'Puja actual' : 'Precio de salida' }}</div>
        <div class="monto">{{ rd(monto) }}</div>
      </div>
      <div class="datos">
        <span>{{ subasta.numeroPujas }} {{ subasta.numeroPujas === 1 ? 'puja' : 'pujas' }}</span>
        <span class="tiempo" :class="{ urgente }">
          <Clock :size="14" /> {{ tiempo }}
        </span>
      </div>
      <component :is="enlace ? 'RouterLink' : 'span'" :to="enlace || undefined" class="btn btn-pujar btn-sm w-100 boton">{{ programada ? 'Ver' : 'Pujar' }}</component>
    </div>
  </article>
</template>

<style scoped>
.card-subasta {
  background: #fff;
  border: 1px solid var(--pj-borde);
  border-radius: 8px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.imagen {
  position: relative;
  display: block;
  aspect-ratio: 282 / 212;
  overflow: hidden;
  background: #e9ecef;
}

.imagen img {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.etiqueta {
  position: absolute;
  top: 10px;
  left: 10px;
}

.sin-foto {
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
  color: var(--pj-gris-claro);
}

.cuerpo {
  padding: 14px 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  flex: 1;
}

.titulo {
  color: var(--pj-texto);
  font-size: 15px;
  font-weight: 500;
  line-height: 20px;
}

.precio {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin-top: auto;
}

.etiqueta-precio {
  font-size: 12px;
  color: var(--pj-gris);
}

.monto {
  font-size: 21px;
  font-weight: 700;
  line-height: 1.2;
}

.datos {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  color: var(--pj-gris);
}

.tiempo {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-weight: 500;
}

.tiempo.urgente {
  color: var(--pj-peligro);
}

/* Versión de la pantalla 09: sin botón, textos más pequeños. */
@media (max-width: 991.98px) {
  .imagen {
    aspect-ratio: 173 / 150;
  }

  .boton {
    display: none;
  }

  .titulo {
    font-size: 14px;
  }

  .monto {
    font-size: 18px;
  }
}
</style>
