<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ProvidersService } from '@/services/ProvidersService'
import { CategoriesService } from '@/services/CategoriesService'
import { useApi } from '@/composables/useApi'
import ProviderCard from '@/components/ProviderCard.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t } = useI18n()
const { data: providers, isLoading, error, execute } = useApi(() => ProvidersService.getAll())
const { data: categories, execute: loadCategories } = useApi(() => CategoriesService.getAll())

onMounted(async () => {
  await Promise.all([execute(), loadCategories()])
})
</script>

<template>
  <div>
    <div class="bg-dark text-white py-5 mb-4">
      <div class="container text-center">
        <h1 class="display-5 fw-bold">{{ t('home.hero') }}</h1>
        <p class="lead">{{ t('home.sub') }}</p>
        <RouterLink :to="{ name: 'Providers' }" class="btn btn-primary btn-lg">
          {{ t('home.browse') }}
        </RouterLink>
      </div>
    </div>

    <div class="container pb-5">
      <h2 class="mb-3">{{ t('home.featured') }}</h2>
      <LoadingSpinner v-if="isLoading" />
      <ErrorAlert v-else-if="error" :errors="[error]" />
      <div v-else class="row g-3">
        <div v-for="p in (providers ?? []).slice(0, 6)" :key="p.id" class="col-sm-6 col-lg-4">
          <ProviderCard :provider="p" />
        </div>
        <div v-if="!(providers ?? []).length" class="text-muted">{{ t('providers.noProviders') }}</div>
      </div>

      <h2 class="mt-5 mb-3">{{ t('home.categories') }}</h2>
      <div class="d-flex flex-wrap gap-2">
        <RouterLink
          v-for="cat in categories ?? []"
          :key="cat.id"
          :to="{ name: 'Providers' }"
          class="btn btn-outline-secondary"
        >
          {{ cat.nameEn || cat.name }}
        </RouterLink>
      </div>
    </div>
  </div>
</template>
