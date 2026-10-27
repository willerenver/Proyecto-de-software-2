<script setup>
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '../api/cliente'
import { iniciarSesion, rutaDeRegreso } from '../stores/sesion'

const route = useRoute()
const router = useRouter()
const form = reactive({ nombre: '', apellido: '', correo: '', telefono: '', cedula: '', contrasena: '', acepta: false })
const errores = ref({})
const error = ref('')
const enviando = ref(false)

// «00112345678» → «001-1234567-8».
function formatearCedula(valor) {
  const d = valor.replace(/\D/g, '').slice(0, 11)
  return [d.slice(0, 3), d.slice(3, 10), d.slice(10)].filter(Boolean).join('-')
}

// Mismas reglas que el diccionario de datos (largos de columna) y el diseño (contraseña).
async function crear() {
  error.value = ''
  const e = {}
  if (!form.nombre.trim()) e.nombre = 'Escribe tu nombre.'
  if (!form.apellido.trim()) e.apellido = 'Escribe tu apellido.'
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.correo)) e.correo = 'Escribe un correo válido, por ejemplo nombre@correo.com.'
  if (!/^[\d\s-]{7,20}$/.test(form.telefono)) e.telefono = 'Escribe un teléfono válido, por ejemplo 809-555-0142.'
  if (form.cedula.replace(/\D/g, '').length !== 11) e.cedula = 'La cédula tiene 11 dígitos. Ejemplo: 001-1234567-8.'
  if (form.contrasena.length < 8 || !/\d/.test(form.contrasena)) e.contrasena = 'Mínimo 8 caracteres, con al menos un número.'
  if (!form.acepta) e.acepta = 'Debes aceptar los términos para crear la cuenta.'
  errores.value = e
  if (Object.keys(e).length) return

  enviando.value = true
  try {
    const { acepta, ...cuerpo } = form
    const datos = await api('/api/auth/registro', { metodo: 'POST', cuerpo })
    iniciarSesion(datos, true)
    router.replace(rutaDeRegreso(route.query.volver))
  } catch (err) {
    errores.value = err.errores
    error.value = err.message
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="d-flex justify-content-center px-3 py-5">
    <form class="panel tarjeta d-flex flex-column gap-3" novalidate @submit.prevent="crear">
      <div>
        <h1 class="fw-bold mb-1 titulo">Crea tu cuenta</h1>
        <p class="texto-gris mb-0">Con una cuenta puedes pujar, vender y guardar favoritos.</p>
      </div>
      <div class="row g-3">
        <div class="col-sm">
          <label for="nombre" class="form-label">Nombre</label>
          <input id="nombre" v-model="form.nombre" maxlength="60" autocomplete="given-name" class="form-control" :class="{ 'is-invalid': errores.nombre }" />
          <div class="invalid-feedback">{{ errores.nombre }}</div>
        </div>
        <div class="col-sm">
          <label for="apellido" class="form-label">Apellido</label>
          <input id="apellido" v-model="form.apellido" maxlength="60" autocomplete="family-name" class="form-control" :class="{ 'is-invalid': errores.apellido }" />
          <div class="invalid-feedback">{{ errores.apellido }}</div>
        </div>
      </div>
      <div>
        <label for="correo" class="form-label">Correo electrónico</label>
        <input id="correo" v-model.trim="form.correo" type="email" maxlength="120" autocomplete="email" class="form-control" :class="{ 'is-invalid': errores.correo }" placeholder="nombre@correo.com" />
        <div class="invalid-feedback">{{ errores.correo }}</div>
      </div>
      <div>
        <label for="telefono" class="form-label">Teléfono</label>
        <input id="telefono" v-model.trim="form.telefono" type="tel" maxlength="20" autocomplete="tel" class="form-control" :class="{ 'is-invalid': errores.telefono }" placeholder="809-555-0142" />
        <div v-if="errores.telefono" class="invalid-feedback">{{ errores.telefono }}</div>
        <div v-else class="form-text">Te escribimos por WhatsApp cuando ganes una subasta.</div>
      </div>
      <div>
        <label for="cedula" class="form-label">Cédula</label>
        <input id="cedula" v-model.trim="form.cedula" inputmode="numeric" maxlength="13" class="form-control" :class="{ 'is-invalid': errores.cedula }" placeholder="001-1234567-8" @input="form.cedula = formatearCedula(form.cedula)" />
        <div class="invalid-feedback">{{ errores.cedula }}</div>
      </div>
      <div>
        <label for="contrasena" class="form-label">Contraseña</label>
        <input id="contrasena" v-model="form.contrasena" type="password" autocomplete="new-password" class="form-control" :class="{ 'is-invalid': errores.contrasena }" />
        <div v-if="errores.contrasena" class="invalid-feedback">{{ errores.contrasena }}</div>
        <div v-else class="form-text">Mínimo 8 caracteres, con al menos un número.</div>
      </div>
      <div class="form-check">
        <input id="acepta" v-model="form.acepta" type="checkbox" class="form-check-input" :class="{ 'is-invalid': errores.acepta }" />
        <label for="acepta" class="form-check-label">Acepto los términos y las reglas de las subastas</label>
        <div class="invalid-feedback">{{ errores.acepta }}</div>
      </div>
      <div v-if="error" class="alert alert-danger mb-0 py-2" role="alert">{{ error }}</div>
      <button type="submit" class="btn btn-primary btn-lg" :disabled="enviando">{{ enviando ? 'Creando la cuenta…' : 'Crear cuenta' }}</button>
      <p class="mb-0">¿Ya tienes cuenta? <RouterLink :to="{ name: 'iniciar-sesion', query: route.query }" class="fw-medium">Inicia sesión</RouterLink></p>
    </form>
  </div>
</template>

<style scoped>
.tarjeta {
  width: 540px;
  max-width: 100%;
  padding: 40px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

.titulo {
  font-size: 28px;
}

@media (max-width: 575.98px) {
  .tarjeta {
    padding: 24px;
  }
}
</style>
