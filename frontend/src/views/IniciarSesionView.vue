<script setup>
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '../api/cliente'
import { iniciarSesion, rutaDeRegreso } from '../stores/sesion'

const route = useRoute()
const router = useRouter()
const form = reactive({ correo: route.query.correo ?? '', contrasena: '', recordar: false })
const errores = ref({})
const error = ref('')
const enviando = ref(false)

async function entrar() {
  error.value = ''
  const e = {}
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.correo)) e.correo = 'Escribe un correo válido, por ejemplo nombre@correo.com.'
  if (!form.contrasena) e.contrasena = 'Escribe tu contraseña.'
  errores.value = e
  if (Object.keys(e).length) return

  enviando.value = true
  try {
    const datos = await api('/api/auth/login', { metodo: 'POST', cuerpo: { correo: form.correo, contrasena: form.contrasena } })
    iniciarSesion(datos, form.recordar)
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
    <form class="panel tarjeta d-flex flex-column gap-3" novalidate @submit.prevent="entrar">
      <div>
        <h1 class="fw-bold mb-1 titulo">Inicia sesión</h1>
        <p class="texto-gris mb-0">Entra para pujar y seguir tus subastas.</p>
      </div>
      <div>
        <label for="correo" class="form-label">Correo electrónico</label>
        <input id="correo" v-model.trim="form.correo" type="email" maxlength="120" autocomplete="email" class="form-control" :class="{ 'is-invalid': errores.correo }" placeholder="nombre@correo.com" />
        <div class="invalid-feedback">{{ errores.correo }}</div>
      </div>
      <div>
        <label for="contrasena" class="form-label">Contraseña</label>
        <input id="contrasena" v-model="form.contrasena" type="password" autocomplete="current-password" class="form-control" :class="{ 'is-invalid': errores.contrasena }" />
        <div class="invalid-feedback">{{ errores.contrasena }}</div>
      </div>
      <div class="d-flex justify-content-between align-items-center">
        <div class="form-check mb-0">
          <input id="recordar" v-model="form.recordar" type="checkbox" class="form-check-input" />
          <label for="recordar" class="form-check-label">Recordarme</label>
        </div>
        <RouterLink :to="{ name: 'olvide-contrasena', query: form.correo ? { correo: form.correo } : {} }" class="texto-pequeno">Olvidé mi contraseña</RouterLink>
      </div>
      <div v-if="error" class="alert alert-danger mb-0 py-2" role="alert">{{ error }}</div>
      <button type="submit" class="btn btn-primary btn-lg" :disabled="enviando">{{ enviando ? 'Entrando…' : 'Entrar' }}</button>
      <p class="mb-0">¿No tienes cuenta? <RouterLink :to="{ name: 'crear-cuenta', query: route.query }" class="fw-medium">Crea una aquí</RouterLink></p>
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
