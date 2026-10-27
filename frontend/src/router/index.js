import { createRouter, createWebHistory } from 'vue-router'
import InicioView from '../views/InicioView.vue'
import { esAdministrador, sesion } from '../stores/sesion'

const routes = [
  { path: '/', name: 'inicio', component: InicioView },
  { path: '/subastas/:id', name: 'subasta', component: () => import('../views/DetalleSubastaView.vue'), meta: { cabeceraMovilPropia: true } },
  { path: '/publicar', name: 'publicar', component: () => import('../views/PublicarView.vue'), meta: { requiereSesion: true } },
  { path: '/subastas/:id/editar', name: 'editar-subasta', component: () => import('../views/PublicarView.vue'), meta: { requiereSesion: true } },
  { path: '/mis-pujas', name: 'mis-pujas', component: () => import('../views/MisPujasView.vue'), meta: { requiereSesion: true } },
  { path: '/mis-ventas', name: 'mis-ventas', component: () => import('../views/MisVentasView.vue'), meta: { requiereSesion: true } },
  { path: '/admin', name: 'admin', component: () => import('../views/AdminView.vue'), meta: { requiereSesion: true, soloAdministrador: true } },
  { path: '/iniciar-sesion', name: 'iniciar-sesion', component: () => import('../views/IniciarSesionView.vue'), meta: { simple: true } },
  { path: '/olvide-contrasena', name: 'olvide-contrasena', component: () => import('../views/OlvideContrasenaView.vue'), meta: { simple: true } },
  { path: '/restablecer-contrasena', name: 'restablecer-contrasena', component: () => import('../views/RestablecerContrasenaView.vue'), meta: { simple: true } },
  { path: '/crear-cuenta', name: 'crear-cuenta', component: () => import('../views/CrearCuentaView.vue'), meta: { simple: true } },
  { path: '/subastas/:id/pagar', name: 'pagar', component: () => import('../views/PagarView.vue'), meta: { requiereSesion: true } },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: (to, from) => (to.path === from.path ? false : { top: 0 }),
})

router.beforeEach(to => {
  if (to.meta.requiereSesion && !sesion.token) return { name: 'iniciar-sesion', query: { volver: to.fullPath } }
  if (to.meta.soloAdministrador && !esAdministrador()) return { name: 'inicio' }
})

export default router
