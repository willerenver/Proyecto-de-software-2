<script setup>
import { useRouter } from 'vue-router'
import AvatarIniciales from './AvatarIniciales.vue'
import { cerrarSesion, esAdministrador, sesion } from '../stores/sesion'

// Menú lateral de «Mis pujas» y «Mis ventas». En el móvil se muestra como una fila de enlaces.
const router = useRouter()

function salir() {
  cerrarSesion()
  router.push('/')
}
</script>

<template>
  <nav v-if="sesion.usuario" class="panel p-0 menu" aria-label="Mi cuenta">
    <div class="d-none d-lg-flex align-items-center gap-3 p-3 border-bottom">
      <AvatarIniciales :iniciales="sesion.usuario.iniciales" :tam="44" />
      <div class="min-w-0">
        <div class="fw-bold">{{ sesion.usuario.nombre }} {{ sesion.usuario.apellido }}</div>
        <small class="texto-gris">Miembro desde {{ sesion.usuario.miembroDesde }}</small>
      </div>
    </div>
    <div class="enlaces">
      <RouterLink to="/mis-pujas" class="item">Mis pujas</RouterLink>
      <RouterLink to="/mis-ventas" class="item">Mis ventas</RouterLink>
      <RouterLink to="/publicar" class="item">Publicar un artículo</RouterLink>
      <RouterLink to="/perfil" class="item">Mi perfil</RouterLink>
      <RouterLink v-if="esAdministrador()" to="/admin" class="item">Administración</RouterLink>
      <button type="button" class="item salir" @click="salir">Cerrar sesión</button>
    </div>
  </nav>
</template>

<style scoped>
.menu {
  overflow: hidden;
}

.enlaces {
  display: flex;
  flex-direction: column;
  padding-bottom: 8px;
}

.item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  border: 0;
  background: none;
  color: var(--mp-texto);
  text-align: left;
}

.item.router-link-exact-active {
  background: var(--mp-primario-suave);
  color: var(--mp-primario);
  font-weight: 500;
}

.item.salir {
  color: var(--mp-gris);
}

.min-w-0 {
  min-width: 0;
}

@media (max-width: 991.98px) {
  .enlaces {
    flex-direction: row;
    overflow-x: auto;
    padding: 6px;
    gap: 4px;
  }

  .item {
    padding: 8px 12px;
    border-radius: 6px;
    white-space: nowrap;
    font-size: 14px;
  }
}
</style>
