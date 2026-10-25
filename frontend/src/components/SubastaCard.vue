<script setup>
import { computed } from 'vue'
import { Clock, Heart } from 'lucide-vue-next'
import EstadoBadge from './EstadoBadge.vue'
import { pujaActual } from '../data/subastas'
import { cierraPronto, rd, tiempoRestante } from '../utils/formato'

// Componente «Card de subasta» del Figma.
const props = defineProps({
  subasta: { type: Object, required: true },
  ahora: { type: Number, required: true },
  // Para la vista previa de «Publicar»: «Empieza el 20 oct».
  textoTiempo: { type: String, default: '' },
})

const urgente = computed(() => !props.textoTiempo && cierraPronto(props.subasta.fechaCierre, props.ahora))
const monto = computed(() => pujaActual(props.subasta) ?? props.subasta.precioSalida)
const enlace = computed(() => (props.subasta.id ? `/subastas/${props.subasta.id}` : ''))
</script>

<template>
  <article class="card-subasta">
    <component :is="enlace ? 'RouterLink' : 'div'" :to="enlace || undefined" class="imagen">
      <img v-if="subasta.foto" :src="subasta.foto" :alt="subasta.titulo" loading="lazy" />
      <EstadoBadge v-if="urgente" estado="Cierra pronto" class="etiqueta" />
      <button class="favorito" type="button" aria-label="Seguir esta subasta" @click.prevent><Heart :size="16" /></button>
    </component>
    <div class="cuerpo">
      <component :is="enlace ? 'RouterLink' : 'div'" :to="enlace || undefined" class="titulo">{{ subasta.titulo }}</component>
      <div class="precio">
        <div class="etiqueta-precio">{{ subasta.pujas.length ? 'Puja actual' : 'Precio de salida' }}</div>
        <div class="monto">{{ rd(monto) }}</div>
      </div>
      <div class="datos">
        <span>{{ subasta.pujas.length }} pujas</span>
        <span class="tiempo" :class="{ urgente }">
          <Clock :size="14" /> {{ textoTiempo || tiempoRestante(subasta.fechaCierre, ahora) }}
        </span>
      </div>
      <component :is="enlace ? 'RouterLink' : 'span'" :to="enlace || undefined" class="btn btn-pujar btn-sm w-100 boton">Pujar</component>
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

.favorito {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 32px;
  height: 32px;
  border: 0;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);
  display: grid;
  place-items: center;
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

/* Versión de la pantalla 09: sin favorito ni botón, textos más pequeños. */
@media (max-width: 991.98px) {
  .imagen {
    aspect-ratio: 173 / 150;
  }

  .favorito,
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
