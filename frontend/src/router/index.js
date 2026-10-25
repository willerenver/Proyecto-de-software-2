import { createRouter, createWebHistory } from 'vue-router'
import InicioView from '../views/InicioView.vue'

const routes = [
  { path: '/', name: 'inicio', component: InicioView }, 
  { path: '/subastas/:id', name: 'subasta', component: () => import('../views/DetalleSubastaView.vue'), meta: { cabeceraMovilPropia: true } },
  { path: '/publicar', name: 'publicar', component: () => import('../views/PublicarView.vue') }, 
  { path: '/mis-pujas', name: 'mis-pujas', component: () => import('../views/MisPujasView.vue') },
  { path: '/iniciar-sesion', name: 'iniciar-sesion', component: () => import('../views/IniciarSesionView.vue'), meta: { simple: true } },
  { path: '/crear-cuenta', name: 'crear-cuenta', component: () => import('../views/CrearCuentaView.vue'), meta: { simple: true } }, 
  { path: '/subastas/:id/pagar', name: 'pagar', component: () => import('../views/PagarView.vue') },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

export default createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})
