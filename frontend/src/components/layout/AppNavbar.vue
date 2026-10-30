<script setup>
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ChevronDown, LogOut, Search } from 'lucide-vue-next'
import LogoElMejorPostor from '../LogoElMejorPostor.vue'
import AvatarIniciales from '../AvatarIniciales.vue'
import { categorias, cargarCategorias } from '../../stores/categorias'
import { cerrarSesion, esAdministrador, sesion } from '../../stores/sesion'

const route = useRoute()
const router = useRouter()
const texto = ref('')
const categoria = ref('')
const menuAbierto = ref(false)

onMounted(() => cargarCategorias().catch(() => {}))

// El buscador refleja lo que se está viendo en el inicio.
watch(() => route.query, q => {
  texto.value = q.buscar ?? ''
  categoria.value = q.categoria ?? ''
}, { immediate: true })

watch(() => route.fullPath, () => { menuAbierto.value = false })

function buscar() {
  const query = {}
  if (texto.value.trim()) query.buscar = texto.value.trim()
  if (categoria.value) query.categoria = categoria.value
  router.push({ name: 'inicio', query })
}

function salir() {
  cerrarSesion()
  router.push('/')
}
</script>

<template>
  <header class="navbar-elmejorpostor">
    <!-- Escritorio -->
    <div class="barra-superior d-none d-lg-block">
      <div class="contenedor d-flex align-items-center gap-4 py-3">
        <LogoElMejorPostor />
        <form class="buscador flex-grow-1" role="search" @submit.prevent="buscar">
          <select v-model="categoria" class="categoria" aria-label="Categoría">
            <option value="">Todas</option>
            <option v-for="c in categorias" :key="c.id" :value="String(c.id)">{{ c.nombre }}</option>
          </select>
          <input v-model="texto" type="search" class="campo" maxlength="80" placeholder="Buscar artículos, marcas y más…" aria-label="Buscar" />
          <button type="submit" class="boton-buscar" aria-label="Buscar"><Search :size="18" /></button>
        </form>
        <nav class="acciones d-flex align-items-center gap-4">
          <RouterLink to="/publicar" class="fw-medium">Vender</RouterLink>
          <RouterLink to="/mis-pujas" class="fw-medium text-body">Mis pujas</RouterLink>
          <div v-if="sesion.usuario" class="position-relative">
            <button type="button" class="cuenta" :aria-expanded="menuAbierto" aria-label="Mi cuenta" @click="menuAbierto = !menuAbierto">
              <AvatarIniciales :iniciales="sesion.usuario.iniciales" />
              <ChevronDown :size="14" />
            </button>
            <div v-if="menuAbierto" class="menu-cuenta">
              <div class="px-3 py-2 border-bottom">
                <div class="fw-bold">{{ sesion.usuario.nombre }} {{ sesion.usuario.apellido }}</div>
                <small class="texto-gris">{{ sesion.usuario.correo }}</small>
              </div>
              <RouterLink to="/mis-pujas">Mis pujas</RouterLink>
              <RouterLink to="/mis-ventas">Mis ventas</RouterLink>
              <RouterLink to="/perfil">Mi perfil</RouterLink>
              <RouterLink v-if="esAdministrador()" to="/admin">Administración</RouterLink>
              <button type="button" class="salir" @click="salir"><LogOut :size="16" /> Cerrar sesión</button>
            </div>
          </div>
          <RouterLink v-else :to="{ name: 'iniciar-sesion', query: { volver: route.fullPath } }" class="btn btn-primary">Iniciar sesión</RouterLink>
        </nav>
      </div>
    </div>
    <nav class="barra-categorias d-none d-lg-block">
      <div class="contenedor d-flex align-items-center gap-4 py-2">
        <RouterLink to="/" class="fw-medium text-white">Todas las categorías</RouterLink>
        <RouterLink v-for="c in categorias" :key="c.id" :to="{ name: 'inicio', query: { categoria: c.id } }">{{ c.nombre }}</RouterLink>
      </div>
    </nav>

    <!-- Móvil (09 y 10) -->
    <div v-if="!route.meta.cabeceraMovilPropia" class="d-lg-none bg-white px-3 pt-3 pb-2">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <LogoElMejorPostor />
        <RouterLink v-if="!sesion.usuario" :to="{ name: 'iniciar-sesion', query: { volver: route.fullPath } }" class="fw-medium">Entrar</RouterLink>
        <AvatarIniciales v-else :iniciales="sesion.usuario.iniciales" :tam="32" />
      </div>
      <form class="buscador-movil" role="search" @submit.prevent="buscar">
        <Search :size="18" class="texto-gris" />
        <input v-model="texto" type="search" maxlength="80" placeholder="Buscar en El Mejor Postor" aria-label="Buscar" />
      </form>
    </div>
  </header>
</template>

<style scoped>
.barra-superior {
  background: #fff;
  border-bottom: 1px solid var(--mp-borde);
}

.buscador {
  display: flex;
  height: 44px;
  border: 1px solid var(--mp-borde);
  border-radius: 6px;
  overflow: hidden;
  background: #fff;
}

.categoria {
  border: 0;
  background: var(--mp-claro);
  padding: 0 14px;
  font-size: 14px;
}

.campo {
  flex: 1;
  border: 0;
  padding: 0 14px;
  outline: none;
}

.boton-buscar {
  width: 56px;
  border: 0;
  background: var(--mp-primario);
  color: #fff;
}

.cuenta {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: 0;
  background: none;
  padding: 0;
  color: var(--mp-gris);
}

.menu-cuenta {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  z-index: 1040;
  width: 240px;
  background: #fff;
  border: 1px solid var(--mp-borde);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  overflow: hidden;
}

.menu-cuenta a,
.menu-cuenta .salir {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 10px 16px;
  border: 0;
  background: none;
  color: var(--mp-texto);
  font-size: 14px;
  text-align: left;
}

.menu-cuenta a:hover,
.menu-cuenta .salir:hover {
  background: var(--mp-claro);
}

.menu-cuenta .salir {
  border-top: 1px solid var(--mp-borde);
  color: var(--mp-gris);
}

.barra-categorias {
  background: var(--mp-primario);
  font-size: 14px;
}

.barra-categorias a {
  color: #dce6f3;
}

.barra-categorias a:hover,
.barra-categorias a.router-link-exact-active {
  color: #fff;
}

.buscador-movil {
  display: flex;
  align-items: center;
  gap: 8px;
  height: 42px;
  padding: 0 12px;
  border: 1px solid var(--mp-borde);
  border-radius: 6px;
  background: var(--mp-claro);
}

.buscador-movil input {
  flex: 1;
  border: 0;
  background: none;
  outline: none;
}
</style>
