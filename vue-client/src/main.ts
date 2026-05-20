import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import i18n from './i18n'
import { useAuthStore } from './stores/auth'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(i18n)

// Hydrate auth store from localStorage before mounting
const auth = useAuthStore()
if (auth.jwt && !auth.roles.length) {
  // roles are parsed in the store constructor via parseRoles, already done
}
void auth // suppress unused warning

app.mount('#app')
