<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { CircleCheck, Mail, MapPin, MessageCircle, Phone, Truck } from 'lucide-vue-next'
import AvatarIniciales from '../components/AvatarIniciales.vue'
import { api, urlFoto } from '../api/cliente'
import { fechaConHora, rd } from '../utils/formato'

// Pantalla 08 del Figma, adaptada: El Mejor Postor no cobra en línea. El ganador y el vendedor ven los datos
// de contacto del otro (GET /api/subastas/{id}/contacto) y acuerdan el pago y la entrega.
const route = useRoute()
const subasta = ref(null)
const contacto = ref(null)
const error = ref('')

watch(() => route.params.id, async id => {
  subasta.value = contacto.value = null
  error.value = ''
  try {
    ;[subasta.value, contacto.value] = await Promise.all([api(`/api/subastas/${id}`), api(`/api/subastas/${id}/contacto`)])
  } catch (e) {
    error.value = e.message
  }
}, { immediate: true })

const soyComprador = computed(() => contacto.value?.rol === 'comprador')
const limitePago = computed(() => contacto.value?.plazoPago)
const confirmando = ref(false)

async function confirmarPago() {
  try {
    await api(`/api/subastas/${route.params.id}/confirmar-pago`, { metodo: 'POST' })
    contacto.value = await api(`/api/subastas/${route.params.id}/contacto`)
    confirmando.value = false
  } catch (e) {
    error.value = e.message
  }
}
const otro = computed(() => contacto.value?.contraparte)
const iniciales = computed(() => (otro.value?.nombre ?? '').split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase())

// Los teléfonos dominicanos (809, 829, 849) tienen 10 dígitos; WhatsApp los espera con el 1 delante.
const whatsapp = computed(() => {
  const digitos = (otro.value?.telefono ?? '').replace(/\D/g, '')
  if (digitos.length === 10) return `https://wa.me/1${digitos}`
  return digitos.length >= 11 ? `https://wa.me/${digitos}` : null
})
const mensaje = computed(() => (soyComprador.value
  ? `Hola, gané tu subasta «${subasta.value?.titulo}» en El Mejor Postor por ${rd(contacto.value?.monto ?? 0)}. ¿Cómo coordinamos el pago y la entrega?`
  : `Hola, soy el vendedor de «${subasta.value?.titulo}» en El Mejor Postor. Ganaste con ${rd(contacto.value?.monto ?? 0)}. ¿Cómo coordinamos el pago y la entrega?`))
</script>

<template>
  <div class="contenedor py-4 d-flex flex-column gap-4">
    <div v-if="error" class="panel p-4 d-flex flex-column gap-3 align-items-start">
      <h1 class="fs-4 fw-bold mb-0">No podemos mostrar esta compra</h1>
      <p class="texto-gris mb-0">{{ error }}</p>
      <RouterLink :to="`/subastas/${route.params.id}`" class="btn btn-primary">Volver a la subasta</RouterLink>
    </div>
    <p v-else-if="!contacto" class="texto-gris">Cargando…</p>

    <template v-else>
      <div class="alerta-exito">
        <CircleCheck :size="20" class="flex-shrink-0" />
        <div v-if="soyComprador">
          <strong class="d-block">¡Ganaste la subasta de «{{ subasta.titulo }}»!</strong>
          <template v-if="contacto.pagoConfirmado">El vendedor confirmó que recibió tu pago.</template>
          <template v-else-if="limitePago">Tienes hasta el {{ fechaConHora(limitePago) }} para ponerte de acuerdo con el vendedor y pagar. Si no, la subasta pasa al siguiente postor.</template>
        </div>
        <div v-else>
          <strong class="d-block">Vendiste «{{ subasta.titulo }}»</strong>
          <template v-if="contacto.pagoConfirmado">Confirmaste el pago. Solo falta la entrega.</template>
          <template v-else-if="limitePago">Escríbele al comprador para acordar el pago. Tiene hasta el {{ fechaConHora(limitePago) }}; si no paga, la subasta pasa al siguiente postor.</template>
        </div>
      </div>
      <h1 class="fw-bold mb-0 titulo">Coordina el pago y la entrega</h1>

      <div class="dos-columnas">
        <div class="d-flex flex-column gap-4">
          <section class="panel d-flex flex-column gap-3">
            <h2 class="subtitulo">{{ soyComprador ? 'Vendedor' : 'Comprador' }}</h2>
            <div class="d-flex align-items-center gap-3">
              <AvatarIniciales :iniciales="iniciales" :tam="48" />
              <div class="fw-bold fs-6">{{ otro.nombre }}</div>
            </div>
            <div class="d-flex flex-wrap gap-2">
              <a :href="`mailto:${otro.correo}?subject=${encodeURIComponent(`Subasta #${subasta.id} en El Mejor Postor`)}&body=${encodeURIComponent(mensaje)}`" class="btn btn-light d-inline-flex align-items-center gap-2"><Mail :size="16" /> {{ otro.correo }}</a>
              <a v-if="otro.telefono" :href="`tel:${otro.telefono}`" class="btn btn-light d-inline-flex align-items-center gap-2"><Phone :size="16" /> {{ otro.telefono }}</a>
              <a v-if="whatsapp" :href="`${whatsapp}?text=${encodeURIComponent(mensaje)}`" target="_blank" rel="noopener" class="btn btn-success d-inline-flex align-items-center gap-2"><MessageCircle :size="16" /> WhatsApp</a>
            </div>
          </section>

          <section class="panel d-flex flex-column gap-3">
            <h2 class="subtitulo">Entrega acordada en la publicación</h2>
            <div class="opciones">
              <div v-if="subasta.permiteRetiro" class="opcion">
                <MapPin :size="22" class="icono" />
                <span class="flex-grow-1"><span class="fw-medium d-block">Retiro en persona</span><small class="texto-gris">Acuerden el lugar y la hora.</small></span>
              </div>
              <div v-if="subasta.permiteEnvio" class="opcion">
                <Truck :size="22" class="icono" />
                <span class="flex-grow-1"><span class="fw-medium d-block">Envío a domicilio</span><small class="texto-gris">El costo del envío lo acuerdan entre ustedes.</small></span>
              </div>
            </div>
            <p class="texto-gris mb-0">Recomendación: revisa el artículo antes de pagar y, si es en persona, reúnanse en un lugar público.</p>
          </section>
        </div>

        <aside class="panel d-flex flex-column gap-3 resumen">
          <h2 class="subtitulo">Resumen</h2>
          <RouterLink :to="`/subastas/${subasta.id}`" class="d-flex gap-3 align-items-center text-body">
            <img v-if="subasta.fotos.length" :src="urlFoto(subasta.fotos[0])" alt="" class="miniatura" />
            <div><div class="fw-medium texto-14">{{ subasta.titulo }}</div><small class="texto-gris">Subasta #{{ subasta.id }}</small></div>
          </RouterLink>
          <hr class="my-0" />
          <div class="d-flex justify-content-between"><span class="texto-gris">Puja ganadora</span><span class="fw-medium">{{ rd(contacto.monto) }}</span></div>
          <div class="d-flex justify-content-between"><span class="texto-gris">Pujas recibidas</span><span class="fw-medium">{{ subasta.numeroPujas }}</span></div>
          <hr class="my-0" />
          <div class="d-flex justify-content-between fs-5 fw-bold"><span>{{ soyComprador ? 'A pagar' : 'A cobrar' }}</span><span>{{ rd(contacto.monto) }}</span></div>
          <small class="texto-gris">Sin contar el envío, si lo hay.</small>
          <div v-if="contacto.pagoConfirmado" class="text-success fw-medium d-flex align-items-center gap-2"><CircleCheck :size="18" /> Pago confirmado</div>
          <template v-else-if="!soyComprador">
            <button v-if="!confirmando" type="button" class="btn btn-primary btn-lg" @click="confirmando = true">Confirmar pago recibido</button>
            <div v-else class="d-flex flex-column gap-2">
              <span class="texto-14">¿Ya recibiste {{ rd(contacto.monto) }}? Después de confirmar no se puede deshacer.</span>
              <div class="d-flex gap-2">
                <button type="button" class="btn btn-light flex-fill" @click="confirmando = false">No</button>
                <button type="button" class="btn btn-primary flex-fill" @click="confirmarPago">Sí, ya me pagó</button>
              </div>
            </div>
          </template>
          <small v-else class="texto-gris">Cuando pagues, el vendedor lo confirma aquí.</small>
        </aside>
      </div>
    </template>
  </div>
</template>

<style scoped>
.titulo {
  font-size: 30px;
}

.subtitulo {
  font-size: 18px;
  font-weight: 700;
  margin: 0;
}

.texto-14 {
  font-size: 14px;
}

.alerta-exito {
  display: flex;
  gap: 12px;
  padding: 14px 16px;
  background: #d1e7dd;
  border: 1px solid #a3cfbb;
  border-radius: 6px;
  color: #0a3622;
  font-size: 14px;
  line-height: 20px;
}

.alerta-exito strong {
  font-size: 15px;
}

.dos-columnas {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 400px;
  gap: 24px;
  align-items: start;
}

.opciones {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 12px;
}

.opcion {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--mp-borde);
  border-radius: 8px;
  background: #fff;
}

.opcion .icono {
  color: var(--mp-gris);
  flex-shrink: 0;
}

.opcion.activa {
  border: 2px solid var(--mp-primario);
  padding: 15px;
  background: #f5f9fe;
}

.opcion.activa .icono {
  color: var(--mp-primario);
}

.miniatura {
  width: 64px;
  height: 64px;
  border-radius: 6px;
  object-fit: cover;
}

.nota {
  font-size: 13px;
  line-height: 19px;
}

@media (max-width: 991.98px) {
  .titulo {
    font-size: 24px;
  }

  .dos-columnas {
    grid-template-columns: 1fr;
  }
}
</style>
