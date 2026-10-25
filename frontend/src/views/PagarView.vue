<script setup>
import { computed, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { CircleCheck, CreditCard, Landmark, MapPin, ShieldCheck, Truck } from 'lucide-vue-next'
import { buscarSubasta, pujaActual } from '../data/subastas'
import { fechaConHora, rd } from '../utils/formato'

// Pantalla 08 del Figma. Es una maqueta: no se conecta con ninguna pasarela ni guarda datos de tarjeta.
const route = useRoute()
const subasta = computed(() => buscarSubasta(route.params.id))
const ganadora = computed(() => (subasta.value ? pujaActual(subasta.value) ?? 0 : 0))
const limitePago = computed(() => new Date(new Date(subasta.value.fechaCierre).getTime() + 48 * 3600000))

const provincias = ['Distrito Nacional', 'Santo Domingo', 'Santiago', 'La Vega', 'Puerto Plata', 'San Cristóbal', 'La Romana', 'San Pedro de Macorís']
const form = reactive({ entrega: 'envio', direccion: '', provincia: 'Distrito Nacional', pago: 'tarjeta', tarjeta: '', vence: '', cvv: '', nombre: '' })
const errores = ref({})
const demostracion = ref(false)

const envio = computed(() => (form.entrega === 'envio' ? 350 : 0))
const cargoServicio = 0
const total = computed(() => ganadora.value + envio.value + cargoServicio)

function formatearTarjeta(v) {
  return v.replace(/\D/g, '').slice(0, 16).replace(/(\d{4})(?=\d)/g, '$1 ')
}

function formatearVence(v) {
  const d = v.replace(/\D/g, '').slice(0, 4)
  return d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` : d
}

function pagar() {
  const e = {}
  if (form.entrega === 'envio' && !form.direccion.trim()) e.direccion = 'Escribe la dirección de entrega.'
  if (form.pago === 'tarjeta') {
    if (form.tarjeta.replace(/\D/g, '').length !== 16) e.tarjeta = 'El número de tarjeta tiene 16 dígitos.'
    if (!/^(0[1-9]|1[0-2])\/\d{2}$/.test(form.vence)) e.vence = 'Usa el formato MM/AA.'
    if (!/^\d{3,4}$/.test(form.cvv)) e.cvv = 'De 3 o 4 dígitos.'
    if (!form.nombre.trim()) e.nombre = 'Como aparece en la tarjeta.'
  }
  errores.value = e
  demostracion.value = !Object.keys(e).length
}
</script>

<template>
  <div v-if="subasta" class="contenedor py-4 d-flex flex-column gap-4">
    <div class="alerta-exito">
      <CircleCheck :size="20" class="flex-shrink-0" />
      <div>
        <strong class="d-block">¡Ganaste la subasta de «{{ subasta.titulo }}»!</strong>
        Tienes hasta el {{ fechaConHora(limitePago) }} para pagar. Si no pagas a tiempo, la subasta pasa al segundo postor.
      </div>
    </div>
    <h1 class="fw-bold mb-0 titulo">Completa tu compra</h1>

    <div class="dos-columnas">
      <form class="d-flex flex-column gap-4" novalidate @submit.prevent="pagar">
        <section class="panel d-flex flex-column gap-3">
          <h2 class="subtitulo">1. Entrega</h2>
          <div class="opciones">
            <label class="opcion" :class="{ activa: form.entrega === 'retiro' }">
              <input v-model="form.entrega" type="radio" value="retiro" class="form-check-input mt-0" />
              <MapPin :size="22" class="icono" />
              <span class="flex-grow-1"><span class="fw-medium d-block">Retiro en persona</span><small class="texto-gris">{{ subasta.vendedor.ubicacion }}. Coordinas la hora con el vendedor.</small></span>
              <strong>Gratis</strong>
            </label>
            <label class="opcion" :class="{ activa: form.entrega === 'envio' }">
              <input v-model="form.entrega" type="radio" value="envio" class="form-check-input mt-0" />
              <Truck :size="22" class="icono" />
              <span class="flex-grow-1"><span class="fw-medium d-block">Envío a domicilio</span><small class="texto-gris">Llega en 1 a 2 días laborables.</small></span>
              <strong>RD$ 350</strong>
            </label>
          </div>
          <div v-if="form.entrega === 'envio'" class="row g-3">
            <div class="col-md">
              <label for="direccion" class="form-label">Dirección</label>
              <input id="direccion" v-model="form.direccion" maxlength="150" autocomplete="street-address" class="form-control" :class="{ 'is-invalid': errores.direccion }" placeholder="Calle El Conde 152, apto. 3B" />
              <div class="invalid-feedback">{{ errores.direccion }}</div>
            </div>
            <div class="col-md">
              <label for="provincia" class="form-label">Provincia</label>
              <select id="provincia" v-model="form.provincia" class="form-select">
                <option v-for="p in provincias" :key="p">{{ p }}</option>
              </select>
            </div>
          </div>
        </section>

        <section class="panel d-flex flex-column gap-3">
          <h2 class="subtitulo">2. Pago</h2>
          <div class="opciones">
            <label class="opcion" :class="{ activa: form.pago === 'tarjeta' }">
              <input v-model="form.pago" type="radio" value="tarjeta" class="form-check-input mt-0" />
              <CreditCard :size="22" class="icono" />
              <span class="flex-grow-1"><span class="fw-medium d-block">Tarjeta de crédito o débito</span><small class="texto-gris">Visa o Mastercard</small></span>
            </label>
            <label class="opcion" :class="{ activa: form.pago === 'transferencia' }">
              <input v-model="form.pago" type="radio" value="transferencia" class="form-check-input mt-0" />
              <Landmark :size="22" class="icono" />
              <span class="flex-grow-1"><span class="fw-medium d-block">Transferencia bancaria</span><small class="texto-gris">Se confirma en 1 a 24 horas</small></span>
            </label>
          </div>
          <template v-if="form.pago === 'tarjeta'">
            <div>
              <label for="tarjeta" class="form-label">Número de tarjeta</label>
              <input id="tarjeta" :value="form.tarjeta" inputmode="numeric" autocomplete="off" class="form-control" :class="{ 'is-invalid': errores.tarjeta }" placeholder="0000 0000 0000 0000" @input="form.tarjeta = formatearTarjeta($event.target.value)" />
              <div class="invalid-feedback">{{ errores.tarjeta }}</div>
            </div>
            <div class="row g-3">
              <div class="col-md-3">
                <label for="vence" class="form-label">Vence</label>
                <input id="vence" :value="form.vence" inputmode="numeric" autocomplete="off" class="form-control" :class="{ 'is-invalid': errores.vence }" placeholder="MM/AA" @input="form.vence = formatearVence($event.target.value)" />
                <div class="invalid-feedback">{{ errores.vence }}</div>
              </div>
              <div class="col-md-3">
                <label for="cvv" class="form-label">CVV</label>
                <input id="cvv" v-model="form.cvv" type="password" inputmode="numeric" maxlength="4" autocomplete="off" class="form-control" :class="{ 'is-invalid': errores.cvv }" />
                <div class="invalid-feedback">{{ errores.cvv }}</div>
              </div>
              <div class="col-md">
                <label for="nombre" class="form-label">Nombre en la tarjeta</label>
                <input id="nombre" v-model="form.nombre" maxlength="60" autocomplete="off" class="form-control text-uppercase" :class="{ 'is-invalid': errores.nombre }" />
                <div class="invalid-feedback">{{ errores.nombre }}</div>
              </div>
            </div>
          </template>
          <p v-else class="texto-gris mb-0">Al confirmar, te mostramos la cuenta del vendedor para hacer la transferencia.</p>
        </section>
        <button type="submit" class="d-none" />
      </form>

      <aside class="panel d-flex flex-column gap-3 resumen">
        <h2 class="subtitulo">Resumen</h2>
        <div class="d-flex gap-3 align-items-center">
          <img v-if="subasta.foto" :src="subasta.foto" alt="" class="miniatura" />
          <div><div class="fw-medium texto-14">{{ subasta.titulo }}</div><small class="texto-gris">Vendedor: {{ subasta.vendedor.nombre }}</small></div>
        </div>
        <hr class="my-0" />
        <div class="d-flex justify-content-between"><span class="texto-gris">Puja ganadora</span><span class="fw-medium">{{ rd(ganadora) }}</span></div>
        <div class="d-flex justify-content-between"><span class="texto-gris">Envío</span><span class="fw-medium">{{ rd(envio) }}</span></div>
        <div class="d-flex justify-content-between"><span class="texto-gris">Cargo por servicio</span><span class="fw-medium">{{ rd(cargoServicio) }}</span></div>
        <hr class="my-0" />
        <div class="d-flex justify-content-between fs-5 fw-bold"><span>Total</span><span>{{ rd(total) }}</span></div>
        <button type="button" class="btn btn-primary btn-lg" @click="pagar">Pagar {{ rd(total) }}</button>
        <div v-if="demostracion" class="alert alert-info mb-0 py-2 texto-14">Maqueta: no se procesó ningún pago.</div>
        <div class="d-flex gap-2 texto-gris nota"><ShieldCheck :size="16" class="flex-shrink-0 mt-1" /> El vendedor recibe el dinero cuando confirmes que te llegó el artículo.</div>
      </aside>
    </div>
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
  border: 1px solid var(--pj-borde);
  border-radius: 8px;
  background: #fff;
  cursor: pointer;
}

.opcion .icono {
  color: var(--pj-gris);
  flex-shrink: 0;
}

.opcion.activa {
  border: 2px solid var(--pj-primario);
  padding: 15px;
  background: #f5f9fe;
}

.opcion.activa .icono {
  color: var(--pj-primario);
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
