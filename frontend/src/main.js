import { createApp } from 'vue'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'
import 'bootstrap/dist/css/bootstrap.min.css'
import './styles/elmejorpostor.css'
import App from './App.vue'
import router from './router'

createApp(App).use(router).mount('#app')
