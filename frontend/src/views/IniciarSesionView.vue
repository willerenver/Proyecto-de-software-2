<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { User } from 'lucide-vue-next'

const router = useRouter()
const form = reactive({ correo: '', contrasena: '', recordar: false })
const errores = ref({})

function entrar() {
  const e = {}
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.correo)) e.correo = 'Escribe un correo válido, por ejemplo nombre@correo.com.'
  if (!form.contrasena) e.contrasena = 'Escribe tu contraseña.'
  errores.value = e
  if (!Object.keys(e).length) router.push('/')
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
        <a href="#" class="texto-pequeno" @click.prevent>Olvidé mi contraseña</a>
      </div>
      <button type="submit" class="btn btn-primary btn-lg">Entrar</button>
      <div class="separador"><span>o</span></div>
      <button type="button" class="btn btn-light btn-lg d-flex align-items-center justify-content-center gap-2 google"><User :size="16" /> Continuar con Google</button>
      <p class="mb-0">¿No tienes cuenta? <RouterLink to="/crear-cuenta" class="fw-medium">Crea una aquí</RouterLink></p>
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

.separador {
  display: flex;
  align-items: center;
  gap: 12px;
  color: var(--pj-gris);
  font-size: 14px;
}

.separador::before,
.separador::after {
  content: '';
  flex: 1;
  height: 1px;
  background: var(--pj-borde);
}

.google {
  font-size: 16px;
}

@media (max-width: 575.98px) {
  .tarjeta {
    padding: 24px;
  }
}
</style>
