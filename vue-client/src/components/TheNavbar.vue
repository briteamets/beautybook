<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useNotificationsStore } from '@/stores/notifications'
import { useRouter } from 'vue-router'
import { onMounted } from 'vue'
import LanguageSwitcher from './LanguageSwitcher.vue'

const auth = useAuthStore()
const notif = useNotificationsStore()
const router = useRouter()

onMounted(async () => {
  if (auth.isAuthenticated) await notif.fetchUnreadCount()
})

async function logout() {
  await auth.logoutAsync()
  router.push({ name: 'Home' })
}
</script>

<template>
  <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
    <div class="container">
      <RouterLink class="navbar-brand fw-bold" :to="{ name: 'Home' }">✨ BeautyBook</RouterLink>
      <button
        class="navbar-toggler"
        type="button"
        data-bs-toggle="collapse"
        data-bs-target="#navbarMain"
      >
        <span class="navbar-toggler-icon"></span>
      </button>

      <div class="collapse navbar-collapse" id="navbarMain">
        <ul class="navbar-nav me-auto">
          <li class="nav-item">
            <RouterLink class="nav-link" :to="{ name: 'Home' }">{{ $t('nav.home') }}</RouterLink>
          </li>
          <li class="nav-item">
            <RouterLink class="nav-link" :to="{ name: 'Providers' }">{{ $t('nav.providers') }}</RouterLink>
          </li>
          <li class="nav-item">
            <RouterLink class="nav-link" :to="{ name: 'Salons' }">{{ $t('nav.salons') }}</RouterLink>
          </li>
          <template v-if="auth.isAuthenticated">
            <li class="nav-item">
              <RouterLink class="nav-link" :to="{ name: 'MyBookings' }">{{ $t('nav.bookings') }}</RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink class="nav-link" :to="{ name: 'Favorites' }">{{ $t('nav.favorites') }}</RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink class="nav-link" :to="{ name: 'Notifications' }">
                {{ $t('nav.notifications') }}
                <span v-if="notif.hasUnread" class="badge bg-danger ms-1">{{ notif.unreadCount }}</span>
              </RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink class="nav-link" :to="{ name: 'Payments' }">{{ $t('nav.payments') }}</RouterLink>
            </li>
            <li v-if="auth.hasRole('ServiceProvider')" class="nav-item dropdown">
              <a class="nav-link dropdown-toggle" href="#" data-bs-toggle="dropdown">
                {{ $t('nav.provider') }}
              </a>
              <ul class="dropdown-menu dropdown-menu-dark">
                <li>
                  <RouterLink class="dropdown-item" :to="{ name: 'ProviderProfile' }">{{ $t('nav.profile') }}</RouterLink>
                </li>
                <li>
                  <RouterLink class="dropdown-item" :to="{ name: 'ProviderServices' }">{{ $t('nav.services') }}</RouterLink>
                </li>
                <li>
                  <RouterLink class="dropdown-item" :to="{ name: 'ProviderSchedule' }">{{ $t('nav.schedule') }}</RouterLink>
                </li>
                <li>
                  <RouterLink class="dropdown-item" :to="{ name: 'ProviderAvailability' }">{{ $t('nav.availability') }}</RouterLink>
                </li>
                <li>
                  <RouterLink class="dropdown-item" :to="{ name: 'ProviderIncoming' }">{{ $t('nav.incoming') }}</RouterLink>
                </li>
              </ul>
            </li>
          </template>
        </ul>

        <ul class="navbar-nav align-items-center gap-2">
          <li class="nav-item">
            <LanguageSwitcher />
          </li>
          <template v-if="auth.isAuthenticated">
            <li class="nav-item text-light small">
              {{ auth.firstName || auth.email }}
            </li>
            <li class="nav-item">
              <button class="btn btn-outline-light btn-sm" @click="logout">{{ $t('nav.logout') }}</button>
            </li>
          </template>
          <template v-else>
            <li class="nav-item">
              <RouterLink class="btn btn-outline-light btn-sm" :to="{ name: 'Login' }">{{ $t('nav.login') }}</RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink class="btn btn-primary btn-sm" :to="{ name: 'Register' }">{{ $t('nav.register') }}</RouterLink>
            </li>
          </template>
        </ul>
      </div>
    </div>
  </nav>
</template>
