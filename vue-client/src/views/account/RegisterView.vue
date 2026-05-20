<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import ErrorAlert from '@/components/ErrorAlert.vue'

const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const firstname = ref('')
const lastname = ref('')
const isProviderApplicant = ref(false)
const errors = ref<string[]>([])
const isLoading = ref(false)

async function submit() {
  errors.value = []
  isLoading.value = true
  const result = await auth.registerAsync({
    email: email.value,
    password: password.value,
    firstname: firstname.value,
    lastname: lastname.value,
    isProviderApplicant: isProviderApplicant.value,
  })
  isLoading.value = false
  if (result.errors) {
    errors.value = result.errors
  } else {
    router.push({ name: 'Home' })
  }
}
</script>

<template>
  <div class="container py-5" style="max-width: 480px">
    <h2 class="mb-4">{{ $t('auth.registerTitle') }}</h2>
    <ErrorAlert v-if="errors.length" :errors="errors" />
    <form @submit.prevent="submit">
      <div class="row g-3 mb-3">
        <div class="col-6">
          <div class="form-floating">
            <input id="firstname" v-model="firstname" type="text" class="form-control" placeholder="First name" required />
            <label for="firstname">{{ $t('auth.firstName') }}</label>
          </div>
        </div>
        <div class="col-6">
          <div class="form-floating">
            <input id="lastname" v-model="lastname" type="text" class="form-control" placeholder="Last name" required />
            <label for="lastname">{{ $t('auth.lastName') }}</label>
          </div>
        </div>
      </div>
      <div class="form-floating mb-3">
        <input id="email" v-model="email" type="email" class="form-control" placeholder="email" required />
        <label for="email">{{ $t('auth.email') }}</label>
      </div>
      <div class="form-floating mb-3">
        <input id="password" v-model="password" type="password" class="form-control" placeholder="password" required />
        <label for="password">{{ $t('auth.password') }}</label>
      </div>
      <div class="form-check mb-3">
        <input id="isProvider" v-model="isProviderApplicant" type="checkbox" class="form-check-input" />
        <label for="isProvider" class="form-check-label">{{ $t('auth.registerAsProvider') }}</label>
      </div>
      <button type="submit" class="btn btn-primary w-100" :disabled="isLoading">
        <span v-if="isLoading" class="spinner-border spinner-border-sm me-2" />
        {{ $t('auth.registerBtn') }}
      </button>
    </form>
    <p class="mt-3 text-center text-muted small">
      {{ $t('auth.haveAccount') }}
      <RouterLink :to="{ name: 'Login' }">{{ $t('nav.login') }}</RouterLink>
    </p>
  </div>
</template>
