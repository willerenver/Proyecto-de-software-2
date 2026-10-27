<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { CircleCheck } from 'lucide-vue-next'
import { api } from '../api/cliente'

// Se llega desde el enlace del correo: /restablecer-contrasena?codigo=…
const route = useRoute()
const codigo = String(route.query.codigo ?? '')
const contrasena = ref('')
const repetir = ref('')
const errores = ref({})
const error = ref(codigo ? '' : 'El enlace está incompleto. Ábrelo de nuevo desde el correo o pide uno nuevo.')
const enviando = ref(false)
const listo = ref(false)

async function guardar() {
  error.value = ''
  const e = {}
  if (contrasena.value.length < 8 || !/\d/.test(contrasena.value)) e.contrasena = 'Mínimo 8 caracteres, con al menos un número.'
  if (repetir.value !== contrasena.value) e.repetir = 'Las contraseñas no coinciden.'
  errores.value = e
  if (Object.keys(e).length) return

  enviando.value = true
  try {
    await api('/api/auth/restablecer', { metodo: 'POST', cuerpo: { codigo, contrasena: contrasena.value } })
    listo.value = true
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
    <div v-if="listo" class="panel tarjeta d-flex flex-column gap-3">
      <CircleCheck :size="32" class="text-success" />
      <h1 class="fw-bold mb-0 titulo">Contraseña cambiada</h1>
      <p class="mb-0">Ya puedes entrar con tu contraseña nueva.</p>
      <RouterLink to="/iniciar-sesion" class="btn btn-primary btn-lg">Iniciar sesión</RouterLink>
    </div>
    <form v-else class="panel tarjeta d-flex flex-column gap-3" novalidate @submit.prevent="guardar">
      <div>
        <h1 class="fw-bold mb-1 titulo">Elige una contraseña nueva</h1>
        <p class="texto-gris mb-0">Mínimo 8 caracteres, con al menos un número.</p>
      </div>
      <div>
        <label for="contrasena" class="form-label">Contraseña nueva</label>
        <input id="contrasena" v-model="contrasena" type="password" autocomplete="new-password" class="form-control" :class="{ 'is-invalid': errores.contrasena }" />
        <div class="invalid-feedback">{{ errores.contrasena }}</div>
      </div>
      <div>
        <label for="repetir" class="form-label">Repite la contraseña</label>
        <input id="repetir" v-model="repetir" type="password" autocomplete="new-password" class="form-control" :class="{ 'is-invalid': errores.repetir }" />
        <div class="invalid-feedback">{{ errores.repetir }}</div>
      </div>
      <div v-if="error" class="alert alert-danger mb-0 py-2" role="alert">
        {{ error }} <RouterLink to="/olvide-contrasena" class="alert-link">Pedir otro enlace</RouterLink>
      </div>
      <button type="submit" class="btn btn-primary btn-lg" :disabled="enviando || !codigo">{{ enviando ? 'Guardando…' : 'Cambiar contraseña' }}</button>
    </form>
  </div>
</template>

<style scoped>
.tarjeta {
  width: 440px;
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
