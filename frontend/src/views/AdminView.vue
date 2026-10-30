<script setup>
import { computed, reactive, ref } from 'vue'
import { Image } from 'lucide-vue-next'
import EstadoBadge from '../components/EstadoBadge.vue'
import MenuCuenta from '../components/MenuCuenta.vue'
import { api, paginas as pedirPaginas, urlFoto } from '../api/cliente'
import { sesion } from '../stores/sesion'
import { diaCorto, rd } from '../utils/formato'

// RF-15 y RF-16: el administrador activa o desactiva usuarios, cancela subastas abiertas y mantiene las categorías.
// No está en el Figma; usa los mismos componentes que «Mis pujas».
const pestana = ref('subastas')
const subastas = ref([])
const usuarios = ref([])
const error = ref('')
const porCancelar = ref(null)
const categorias = ref([])
const POR_PAGINA = 24
const buscar = ref('')
const totales = reactive({ subastas: 0, usuarios: 0 })
const paginas = reactive({ subastas: 1, usuarios: 1 })

// Subastas y usuarios llegan por páginas de 24; al refrescar se piden todas las que ya están en pantalla.
async function cargarLista(cual) {
  const q = new URLSearchParams()
  if (buscar.value.trim()) q.set('buscar', buscar.value.trim())
  const r = await pedirPaginas(`/api/admin/${cual}?${q}`, paginas[cual], POR_PAGINA)
  totales[cual] = r.total
  return r.datos
}

async function cargar() {
  try {
    ;[subastas.value, usuarios.value, categorias.value] = await Promise.all([
      cargarLista('subastas'), cargarLista('usuarios'), api('/api/admin/categorias'),
    ])
    error.value = ''
  } catch (e) {
    error.value = e.message
  }
}
cargar()

function verMas(cual) {
  paginas[cual]++
  cargar()
}

let esperaBusqueda = null
function alBuscar() {
  clearTimeout(esperaBusqueda)
  esperaBusqueda = setTimeout(() => { paginas.subastas = paginas.usuarios = 1; cargar() }, 300)
}

async function cancelar(id) {
  try {
    await api(`/api/admin/subastas/${id}/cancelar`, { metodo: 'POST' })
    porCancelar.value = null
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}

async function cambiarActivo(usuario) {
  try {
    await api(`/api/admin/usuarios/${usuario.id}`, { metodo: 'PATCH', cuerpo: { activo: !usuario.activo } })
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}

// Categorías: un formulario sirve para crear (id null) y para editar.
const principales = computed(() => categorias.value.filter(c => c.categoriaPadreId === null))
const hijas = id => categorias.value.filter(c => c.categoriaPadreId === id)
const vacia = () => ({ id: null, nombre: '', categoriaPadreId: null, activa: true })
const formCategoria = reactive(vacia())
const errorCategoria = ref('')

function editarCategoria(c) {
  Object.assign(formCategoria, { id: c.id, nombre: c.nombre, categoriaPadreId: c.categoriaPadreId, activa: c.activa })
  errorCategoria.value = ''
}

async function guardarCategoria() {
  errorCategoria.value = ''
  if (!formCategoria.nombre.trim()) { errorCategoria.value = 'Escribe el nombre.'; return }
  const { id, ...cuerpo } = formCategoria
  try {
    await api(id ? `/api/admin/categorias/${id}` : '/api/admin/categorias', { metodo: id ? 'PUT' : 'POST', cuerpo })
    Object.assign(formCategoria, vacia())
    await cargar()
  } catch (e) {
    errorCategoria.value = e.errores.nombre ?? e.message
  }
}

async function cambiarActiva(c) {
  try {
    await api(`/api/admin/categorias/${c.id}`, { metodo: 'PUT', cuerpo: { nombre: c.nombre, categoriaPadreId: c.categoriaPadreId, activa: !c.activa } })
    await cargar()
  } catch (e) {
    error.value = e.message
  }
}

const ESTADOS = {
  Programada: ['Pendiente', 'Programada'],
  Activa: ['Activa', 'Activa'],
  Finalizada: ['Vas ganando', 'Vendida'],
  Desierta: ['Neutra', 'Sin ganador'],
  Cancelada: ['Superado', 'Cancelada'],
}
const abierta = s => ['Programada', 'Activa'].includes(s.estado)
</script>

<template>
  <div class="contenedor py-4">
    <div class="dos-columnas">
      <MenuCuenta />

      <div class="d-flex flex-column gap-3 gap-lg-4">
        <h1 class="fw-bold mb-0 titulo">Administración</h1>
        <div v-if="error" class="alert alert-danger mb-0">{{ error }}</div>

        <section class="panel p-0 overflow-hidden">
          <ul class="nav nav-tabs px-3 pt-3">
            <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'subastas' }" @click="pestana = 'subastas'">Subastas ({{ totales.subastas }})</button></li>
            <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'usuarios' }" @click="pestana = 'usuarios'">Usuarios ({{ totales.usuarios }})</button></li>
            <li class="nav-item"><button class="nav-link" :class="{ active: pestana === 'categorias' }" @click="pestana = 'categorias'">Categorías ({{ categorias.length }})</button></li>
          </ul>

          <div v-if="pestana === 'categorias'" class="p-3 d-flex flex-column gap-3">
            <form class="d-flex flex-wrap gap-2 align-items-start" novalidate @submit.prevent="guardarCategoria">
              <div class="flex-grow-1">
                <input v-model="formCategoria.nombre" class="form-control" maxlength="60" :class="{ 'is-invalid': errorCategoria }" :placeholder="formCategoria.id ? 'Nuevo nombre' : 'Nombre de la categoría'" aria-label="Nombre" />
                <div class="invalid-feedback">{{ errorCategoria }}</div>
              </div>
              <select v-model="formCategoria.categoriaPadreId" class="form-select w-auto" aria-label="Dentro de">
                <option :value="null">Categoría principal</option>
                <option v-for="c in principales.filter(c => c.id !== formCategoria.id)" :key="c.id" :value="c.id">Dentro de {{ c.nombre }}</option>
              </select>
              <button type="submit" class="btn btn-primary">{{ formCategoria.id ? 'Guardar' : 'Agregar' }}</button>
              <button v-if="formCategoria.id" type="button" class="btn btn-light" @click="Object.assign(formCategoria, vacia())">Cancelar</button>
            </form>
            <div class="table-responsive">
              <table class="table align-middle mb-0">
                <thead>
                  <tr><th>Categoría</th><th class="text-end">Artículos</th><th>Estado</th><th /></tr>
                </thead>
                <tbody>
                  <template v-for="c in principales" :key="c.id">
                    <tr v-for="(x, i) in [c, ...hijas(c.id)]" :key="x.id">
                      <td :class="i ? 'ps-4' : 'fw-medium'">{{ i ? '› ' : '' }}{{ x.nombre }}</td>
                      <td class="text-end">{{ x.articulos }}</td>
                      <td><EstadoBadge :estado="x.activa ? 'Activa' : 'Neutra'" :texto="x.activa ? 'Visible' : 'Oculta'" /></td>
                      <td class="text-end text-nowrap">
                        <button type="button" class="btn btn-sm btn-light me-1" @click="editarCategoria(x)">Editar</button>
                        <button type="button" class="btn btn-sm" :class="x.activa ? 'btn-outline-danger' : 'btn-outline-primary'" @click="cambiarActiva(x)">{{ x.activa ? 'Ocultar' : 'Mostrar' }}</button>
                      </td>
                    </tr>
                  </template>
                </tbody>
              </table>
            </div>
            <small class="texto-gris">Una categoría oculta no se ofrece al publicar; las subastas que ya tiene siguen visibles.</small>
          </div>

          <div v-if="pestana !== 'categorias'" class="px-3 pt-3">
            <input v-model="buscar" type="search" maxlength="80" class="form-control" :placeholder="pestana === 'subastas' ? 'Buscar por título' : 'Buscar por nombre o correo'" aria-label="Buscar" @input="alBuscar" />
          </div>

          <div v-if="pestana === 'subastas'" class="table-responsive">
            <table class="table align-middle mb-0">
              <thead>
                <tr><th>Artículo</th><th class="text-end">Puja actual</th><th class="text-end">Pujas</th><th>Cierre</th><th>Estado</th><th /></tr>
              </thead>
              <tbody>
                <tr v-for="s in subastas" :key="s.id">
                  <td>
                    <RouterLink :to="`/subastas/${s.id}`" class="d-flex align-items-center gap-3 text-body">
                      <img v-if="s.foto" :src="urlFoto(s.foto)" alt="" class="miniatura" />
                      <span v-else class="miniatura sin-foto"><Image :size="20" /></span>
                      <div><div class="fw-medium">{{ s.titulo }}</div><small class="texto-gris">Subasta #{{ s.id }} · {{ s.categoria }}</small></div>
                    </RouterLink>
                  </td>
                  <td class="text-end fw-bold">{{ rd(s.pujaActual ?? s.precioSalida) }}</td>
                  <td class="text-end">{{ s.numeroPujas }}</td>
                  <td class="text-nowrap">{{ diaCorto(s.fechaCierre) }}</td>
                  <td><EstadoBadge :estado="ESTADOS[s.estado]?.[0] ?? 'Neutra'" :texto="ESTADOS[s.estado]?.[1] ?? s.estado" /></td>
                  <td class="text-end text-nowrap">
                    <template v-if="abierta(s)">
                      <button v-if="porCancelar !== s.id" type="button" class="btn btn-sm btn-outline-danger" @click="porCancelar = s.id">Cancelar</button>
                      <span v-else class="d-inline-flex gap-1">
                        <button type="button" class="btn btn-sm btn-light" @click="porCancelar = null">No</button>
                        <button type="button" class="btn btn-sm btn-danger" @click="cancelar(s.id)">Sí, cancelar</button>
                      </span>
                    </template>
                  </td>
                </tr>
                <tr v-if="!subastas.length"><td colspan="6" class="texto-gris py-4 text-center">No hay subastas.</td></tr>
              </tbody>
            </table>
            <div v-if="subastas.length < totales.subastas" class="text-center p-3">
              <button type="button" class="btn btn-light" @click="verMas('subastas')">Ver más ({{ totales.subastas - subastas.length }})</button>
            </div>
          </div>

          <div v-else-if="pestana === 'usuarios'" class="table-responsive">
            <table class="table align-middle mb-0">
              <thead>
                <tr><th>Nombre</th><th>Correo</th><th>Rol</th><th>Desde</th><th>Estado</th><th /></tr>
              </thead>
              <tbody>
                <tr v-for="u in usuarios" :key="u.id">
                  <td class="fw-medium">{{ u.nombre }}</td>
                  <td>{{ u.correo }}</td>
                  <td class="text-capitalize">{{ u.rol }}</td>
                  <td class="text-nowrap">{{ diaCorto(u.creadoEn) }} {{ new Date(u.creadoEn).getFullYear() }}</td>
                  <td><EstadoBadge :estado="u.activo ? 'Activa' : 'Superado'" :texto="u.activo ? 'Activo' : 'Desactivado'" /></td>
                  <td class="text-end">
                    <button v-if="u.id !== sesion.usuario?.id" type="button" class="btn btn-sm" :class="u.activo ? 'btn-outline-danger' : 'btn-outline-primary'" @click="cambiarActivo(u)">{{ u.activo ? 'Desactivar' : 'Activar' }}</button>
                  </td>
                </tr>
                <tr v-if="!usuarios.length"><td colspan="6" class="texto-gris py-4 text-center">No hay usuarios con esa búsqueda.</td></tr>
              </tbody>
            </table>
            <div v-if="usuarios.length < totales.usuarios" class="text-center p-3">
              <button type="button" class="btn btn-light" @click="verMas('usuarios')">Ver más ({{ totales.usuarios - usuarios.length }})</button>
            </div>
          </div>
        </section>
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

thead th {
  background: var(--mp-claro);
  color: var(--mp-gris);
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
}

.sin-foto {
  display: grid;
  place-items: center;
  background: #e9ecef;
  color: var(--mp-gris);
}

.miniatura {
  width: 48px;
  height: 48px;
  border-radius: 6px;
  object-fit: cover;
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
