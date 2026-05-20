<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import ErrorAlert from '@/components/ErrorAlert.vue'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)

async function submit() {
  errors.value = []
  isLoading.value = true
  const result = await auth.loginAsync(email.value, password.value)
  isLoading.value = false
  if (result.errors) {
    errors.value = result.errors
  } else {
    const redirect = route.query.redirect as string | undefined
    router.push(redirect ?? { name: 'Home' })
  }
}
</script>

<template>
  <div class="container py-5" style="max-width: 420px">
    <h2 class="mb-4">{{ $t('auth.loginTitle') }}</h2>
    <ErrorAlert v-if="errors.length" :errors="errors" />
    <form @submit.prevent="submit">
      <div class="form-floating mb-3">
        <input
          id="email"
          v-model="email"
          type="email"
          class="form-control"
          placeholder="email"
          required
        />
        <label for="email">{{ $t('auth.email') }}</label>
      </div>
      <div class="form-floating mb-3">
        <input
          id="password"
          v-model="password"
          type="password"
          class="form-control"
          placeholder="password"
          required
        />
        <label for="password">{{ $t('auth.password') }}</label>
      </div>
      <button type="submit" class="btn btn-primary w-100" :disabled="isLoading">
        <span v-if="isLoading" class="spinner-border spinner-border-sm me-2" />
        {{ $t('auth.loginBtn') }}
      </button>
    </form>
    <p class="mt-3 text-center text-muted small">
      {{ $t('auth.noAccount') }}
      <RouterLink :to="{ name: 'Register' }">{{ $t('nav.register') }}</RouterLink>
    </p>
  </div>
</template>
