<script setup>
import { useRoute } from 'vue-router'
import { Bell, ChevronDown, Menu, Search } from 'lucide-vue-next'
import LogoPujalo from '../LogoPujalo.vue'
import AvatarIniciales from '../AvatarIniciales.vue'
import { categorias, usuarioActual } from '../../data/subastas'

const route = useRoute()
</script>

<template>
  <header class="navbar-pujalo">
    <!-- Escritorio -->
    <div class="barra-superior d-none d-lg-block">
      <div class="contenedor d-flex align-items-center gap-4 py-3">
        <LogoPujalo />
        <form class="buscador flex-grow-1" role="search" @submit.prevent>
          <select class="categoria" aria-label="Categoría">
            <option>Todas</option>
            <option v-for="c in categorias" :key="c.id">{{ c.nombre }}</option>
          </select>
          <input type="search" class="campo" placeholder="Buscar artículos, marcas y más…" aria-label="Buscar" />
          <button type="submit" class="boton-buscar" aria-label="Buscar"><Search :size="18" /></button>
        </form>
        <nav class="acciones d-flex align-items-center gap-4">
          <RouterLink to="/publicar" class="fw-medium">Vender</RouterLink>
          <RouterLink to="/mis-pujas" class="fw-medium text-body">Mis pujas</RouterLink>
          <button class="campana" aria-label="Notificaciones"><Bell :size="22" :stroke-width="1.8" /><span class="punto" /></button>
          <AvatarIniciales :iniciales="usuarioActual.iniciales" />
        </nav>
      </div>
    </div>
    <nav class="barra-categorias d-none d-lg-block">
      <div class="contenedor d-flex align-items-center gap-4 py-2">
        <span class="fw-medium text-white d-inline-flex align-items-center gap-1">Todas las categorías <ChevronDown :size="14" /></span>
        <a v-for="c in categorias" :key="c.id" href="#" @click.prevent>{{ c.nombre }}</a>
      </div>
    </nav>

    <!-- Móvil (09 y 10) -->
    <div v-if="!route.meta.cabeceraMovilPropia" class="d-lg-none bg-white px-3 pt-3 pb-2">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <LogoPujalo />
        <div class="d-flex gap-3">
          <Bell :size="22" />
          <Menu :size="22" />
        </div>
      </div>
      <label class="buscador-movil">
        <Search :size="18" class="texto-gris" />
        <input type="search" placeholder="Buscar en Pújalo" aria-label="Buscar" />
      </label>
    </div>
  </header>
</template>

<style scoped>
.barra-superior {
  background: #fff;
  border-bottom: 1px solid var(--pj-borde);
}

.buscador {
  display: flex;
  height: 44px;
  border: 1px solid var(--pj-borde);
  border-radius: 6px;
  overflow: hidden;
  background: #fff;
}

.categoria {
  border: 0;
  background: var(--pj-claro);
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
  background: var(--pj-primario);
  color: #fff;
}

.campana {
  position: relative;
  border: 0;
  background: none;
  padding: 0;
}

.punto {
  position: absolute;
  top: 0;
  right: -2px;
  width: 9px;
  height: 9px;
  border-radius: 50%;
  background: var(--pj-peligro);
}

.barra-categorias {
  background: var(--pj-primario);
  font-size: 14px;
}

.barra-categorias a {
  color: #dce6f3;
}

.barra-categorias a:hover {
  color: #fff;
}

.buscador-movil {
  display: flex;
  align-items: center;
  gap: 8px;
  height: 42px;
  padding: 0 12px;
  border: 1px solid var(--pj-borde);
  border-radius: 6px;
  background: var(--pj-claro);
}

.buscador-movil input {
  flex: 1;
  border: 0;
  background: none;
  outline: none;
}
</style>
