<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { MailCheck } from 'lucide-vue-next'
import { api } from '../api/cliente'

const route = useRoute()
const correo = ref(route.query.correo ?? '')
const error = ref('')
const enviando = ref(false)
const enviado = ref(false)

async function enviar() {
  error.value = ''
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo.value)) {
    error.value = 'Escribe un correo válido, por ejemplo nombre@correo.com.'
    return
  }
  enviando.value = true
  try {
    await api('/api/auth/recuperar', { metodo: 'POST', cuerpo: { correo: correo.value } })
    enviado.value = true
  } catch (e) {
    error.value = e.message
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="d-flex justify-content-center px-3 py-5">
    <div v-if="enviado" class="panel tarjeta d-flex flex-column gap-3">
      <MailCheck :size="32" class="text-success" />
      <h1 class="fw-bold mb-0 titulo">Revisa tu correo</h1>
      <p class="mb-0">Si <strong>{{ correo }}</strong> tiene una cuenta en El Mejor Postor, te enviamos un enlace para elegir una contraseña nueva. El enlace vence en 1 hora.</p>
      <p class="texto-gris mb-0">¿No llegó? Revisa la carpeta de correo no deseado o pide otro enlace.</p>
      <div class="d-flex gap-2">
        <button type="button" class="btn btn-light" @click="enviado = false">Pedir otro enlace</button>
        <RouterLink :to="{ name: 'iniciar-sesion', query: { correo } }" class="btn btn-primary">Volver a iniciar sesión</RouterLink>
      </div>
    </div>
    <form v-else class="panel tarjeta d-flex flex-column gap-3" novalidate @submit.prevent="enviar">
      <div>
        <h1 class="fw-bold mb-1 titulo">¿Olvidaste tu contraseña?</h1>
        <p class="texto-gris mb-0">Escribe el correo de tu cuenta y te enviamos un enlace para cambiarla.</p>
      </div>
      <div>
        <label for="correo" class="form-label">Correo electrónico</label>
        <input id="correo" v-model.trim="correo" type="email" maxlength="120" autocomplete="email" class="form-control" :class="{ 'is-invalid': error }" placeholder="nombre@correo.com" />
        <div class="invalid-feedback">{{ error }}</div>
      </div>
      <button type="submit" class="btn btn-primary btn-lg" :disabled="enviando">{{ enviando ? 'Enviando…' : 'Enviar enlace' }}</button>
      <p class="mb-0"><RouterLink to="/iniciar-sesion" class="fw-medium">Volver a iniciar sesión</RouterLink></p>
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
