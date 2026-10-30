<script setup>
import { reactive, ref } from 'vue'
import MenuCuenta from '../components/MenuCuenta.vue'
import { api } from '../api/cliente'
import { iniciarSesion, sesion } from '../stores/sesion'

// «Mi perfil»: datos de contacto y cambio de contraseña (api/perfil).
const perfil = ref(null)
const datos = reactive({ nombre: '', apellido: '', telefono: '' })
const clave = reactive({ actual: '', nueva: '', repetir: '' })
const erroresDatos = ref({})
const erroresClave = ref({})
const mensajeDatos = ref('')
const mensajeClave = ref('')
const error = ref('')
const guardando = ref(false)

// Se conserva donde estaba guardada la sesión («Recordarme»).
const recordada = () => {
  try {
    return !!localStorage.getItem('elmejorpostor.sesion')
  } catch {
    return false
  }
}

api('/api/perfil').then(p => {
  perfil.value = p
  Object.assign(datos, { nombre: p.nombre, apellido: p.apellido, telefono: p.telefono ?? '' })
}).catch(e => { error.value = e.message })

async function guardarDatos() {
  mensajeDatos.value = ''
  const e = {}
  if (!datos.nombre.trim()) e.nombre = 'Escribe tu nombre.'
  if (!datos.apellido.trim()) e.apellido = 'Escribe tu apellido.'
  if (!/^[\d\s-]{7,20}$/.test(datos.telefono)) e.telefono = 'Escribe un teléfono válido, por ejemplo 809-555-0142.'
  erroresDatos.value = e
  if (Object.keys(e).length) return
  guardando.value = true
  try {
    iniciarSesion(await api('/api/perfil', { metodo: 'PUT', cuerpo: datos }), recordada())
    mensajeDatos.value = 'Datos guardados.'
  } catch (err) {
    erroresDatos.value = err.errores
    mensajeDatos.value = ''
    error.value = err.message
  } finally {
    guardando.value = false
  }
}

async function cambiarClave() {
  mensajeClave.value = ''
  const e = {}
  if (!clave.actual) e.actual = 'Escribe tu contraseña actual.'
  if (clave.nueva.length < 8 || !/\d/.test(clave.nueva)) e.nueva = 'Mínimo 8 caracteres, con al menos un número.'
  if (clave.repetir !== clave.nueva) e.repetir = 'Las contraseñas no coinciden.'
  erroresClave.value = e
  if (Object.keys(e).length) return
  guardando.value = true
  try {
    iniciarSesion(await api('/api/perfil/contrasena', { metodo: 'POST', cuerpo: { actual: clave.actual, nueva: clave.nueva } }), recordada())
    Object.assign(clave, { actual: '', nueva: '', repetir: '' })
    mensajeClave.value = 'Contraseña cambiada. Cerramos tus sesiones en otros equipos.'
  } catch (err) {
    erroresClave.value = { actual: err.message, ...err.errores }
  } finally {
    guardando.value = false
  }
}
</script>

<template>
  <div class="contenedor py-4">
    <div class="dos-columnas">
      <MenuCuenta />

      <div class="d-flex flex-column gap-3 gap-lg-4">
        <h1 class="fw-bold mb-0 titulo">Mi perfil</h1>
        <div v-if="error" class="alert alert-danger mb-0">{{ error }}</div>

        <form v-if="perfil" class="panel d-flex flex-column gap-3" novalidate @submit.prevent="guardarDatos">
          <h2 class="fs-5 fw-bold mb-0">Datos personales</h2>
          <div class="row g-3">
            <div class="col-sm">
              <label for="nombre" class="form-label">Nombre</label>
              <input id="nombre" v-model="datos.nombre" maxlength="60" autocomplete="given-name" class="form-control" :class="{ 'is-invalid': erroresDatos.nombre }" />
              <div class="invalid-feedback">{{ erroresDatos.nombre }}</div>
            </div>
            <div class="col-sm">
              <label for="apellido" class="form-label">Apellido</label>
              <input id="apellido" v-model="datos.apellido" maxlength="60" autocomplete="family-name" class="form-control" :class="{ 'is-invalid': erroresDatos.apellido }" />
              <div class="invalid-feedback">{{ erroresDatos.apellido }}</div>
            </div>
          </div>
          <div class="row g-3">
            <div class="col-sm">
              <label for="telefono" class="form-label">Teléfono</label>
              <input id="telefono" v-model.trim="datos.telefono" type="tel" maxlength="20" autocomplete="tel" class="form-control" :class="{ 'is-invalid': erroresDatos.telefono }" />
              <div v-if="erroresDatos.telefono" class="invalid-feedback">{{ erroresDatos.telefono }}</div>
              <div v-else class="form-text">Lo ve la otra parte cuando ganas o vendes una subasta.</div>
            </div>
            <div class="col-sm">
              <span class="form-label d-block">Correo y cédula</span>
              <div class="pt-1">{{ perfil.correo }}</div>
              <small class="texto-gris">{{ perfil.cedula ?? 'Sin cédula' }} · no se pueden cambiar</small>
            </div>
          </div>
          <div class="d-flex align-items-center gap-3">
            <button type="submit" class="btn btn-primary" :disabled="guardando">Guardar datos</button>
            <span v-if="mensajeDatos" class="text-success texto-pequeno">{{ mensajeDatos }}</span>
          </div>
        </form>

        <form v-if="perfil" class="panel d-flex flex-column gap-3" novalidate @submit.prevent="cambiarClave">
          <h2 class="fs-5 fw-bold mb-0">Contraseña</h2>
          <input :value="sesion.usuario?.correo" type="email" autocomplete="username" class="visually-hidden" tabindex="-1" aria-hidden="true" readonly />
          <div class="row g-3">
            <div class="col-md">
              <label for="actual" class="form-label">Contraseña actual</label>
              <input id="actual" v-model="clave.actual" type="password" autocomplete="current-password" class="form-control" :class="{ 'is-invalid': erroresClave.actual }" />
              <div class="invalid-feedback">{{ erroresClave.actual }}</div>
            </div>
            <div class="col-md">
              <label for="nueva" class="form-label">Contraseña nueva</label>
              <input id="nueva" v-model="clave.nueva" type="password" autocomplete="new-password" class="form-control" :class="{ 'is-invalid': erroresClave.nueva }" />
              <div class="invalid-feedback">{{ erroresClave.nueva }}</div>
            </div>
            <div class="col-md">
              <label for="repetir" class="form-label">Repite la nueva</label>
              <input id="repetir" v-model="clave.repetir" type="password" autocomplete="new-password" class="form-control" :class="{ 'is-invalid': erroresClave.repetir }" />
              <div class="invalid-feedback">{{ erroresClave.repetir }}</div>
            </div>
          </div>
          <div class="d-flex align-items-center gap-3">
            <button type="submit" class="btn btn-primary" :disabled="guardando">Cambiar contraseña</button>
            <span v-if="mensajeClave" class="text-success texto-pequeno">{{ mensajeClave }}</span>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dos-columnas {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  gap: 24px;
  align-items: start;
}

.titulo {
  font-size: 30px;
}

@media (max-width: 991.98px) {
  .dos-columnas {
    grid-template-columns: 1fr;
  }

  .titulo {
    font-size: 24px;
  }
}
</style>
