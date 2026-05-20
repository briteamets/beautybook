<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ProvidersService } from '@/services/ProvidersService'
import { CategoriesService } from '@/services/CategoriesService'
import { ServiceTypesService } from '@/services/ServiceTypesService'
import { FavoritesService } from '@/services/FavoritesService'
import { useApi } from '@/composables/useApi'
import { useAuthStore } from '@/stores/auth'
import ProviderCard from '@/components/ProviderCard.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import type { IServiceType } from '@/types/IServiceType'
import type { IFavorite } from '@/types/IFavorite'

const { t } = useI18n()
const auth = useAuthStore()

const search = ref('')
const categoryId = ref('')
const serviceTypeId = ref('')
const providerType = ref('')

const { data: categories, execute: loadCats } = useApi(() => CategoriesService.getAll())
const { data: serviceTypes, execute: loadTypes } = useApi(() => ServiceTypesService.getAll())
const { data: providers, isLoading, error, execute: loadProviders } = useApi(() =>
  ProvidersService.getAll({
    search: search.value || undefined,
    serviceTypeId: serviceTypeId.value || undefined,
    providerType: providerType.value || undefined,
  }),
)

const filteredTypes = computed<IServiceType[]>(() =>
  (serviceTypes.value ?? []).filter(
    (t) => !categoryId.value || t.serviceCategoryId === categoryId.value,
  ),
)

let debounceTimer: ReturnType<typeof setTimeout>
watch(search, () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => loadProviders(), 300)
})
watch([categoryId, serviceTypeId, providerType], () => loadProviders())

const favorites = ref<IFavorite[]>([])

async function loadFavorites() {
  if (!auth.isAuthenticated) return
  const result = await FavoritesService.getAll()
  if (result.data) favorites.value = result.data
}

function isFavorited(providerId: string): boolean {
  return favorites.value.some((f) => f.providerId === providerId)
}

async function toggleFavorite(providerId: string) {
  if (!auth.isAuthenticated) return
  const fav = favorites.value.find((f) => f.providerId === providerId)
  if (fav) {
    await FavoritesService.remove(fav.id)
  } else {
    await FavoritesService.create({ beautyProviderId: providerId })
  }
  await loadFavorites()
}

onMounted(async () => {
  await Promise.all([loadCats(), loadTypes(), loadProviders(), loadFavorites()])
})
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('providers.title') }}</h2>

    <!-- Filters -->
    <div class="row g-2 mb-4">
      <div class="col-md-4">
        <input
          v-model="search"
          type="search"
          class="form-control"
          :placeholder="t('providers.searchPlaceholder')"
        />
      </div>
      <div class="col-md-2">
        <select v-model="categoryId" class="form-select">
          <option value="">{{ t('providers.category') }}: {{ t('common.all') }}</option>
          <option v-for="c in categories ?? []" :key="c.id" :value="c.id">
            {{ c.nameEn || c.name }}
          </option>
        </select>
      </div>
      <div class="col-md-3">
        <select v-model="serviceTypeId" class="form-select">
          <option value="">{{ t('providers.serviceType') }}: {{ t('common.all') }}</option>
          <option v-for="st in filteredTypes" :key="st.id" :value="st.id">
            {{ st.nameEn }}
          </option>
        </select>
      </div>
      <div class="col-md-3">
        <div class="d-flex gap-2 align-items-center">
          <div class="form-check">
            <input id="anyType" v-model="providerType" type="radio" value="" class="form-check-input" />
            <label for="anyType" class="form-check-label">{{ t('providers.any') }}</label>
          </div>
          <div class="form-check">
            <input id="indType" v-model="providerType" type="radio" value="independent" class="form-check-input" />
            <label for="indType" class="form-check-label">{{ t('providers.independent') }}</label>
          </div>
          <div class="form-check">
            <input id="salonType" v-model="providerType" type="radio" value="salon" class="form-check-input" />
            <label for="salonType" class="form-check-label">{{ t('providers.salonBased') }}</label>
          </div>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else class="row g-3">
      <div v-for="p in providers ?? []" :key="p.id" class="col-sm-6 col-lg-4">
        <ProviderCard :provider="p" :is-favorited="isFavorited(p.id)" @favorite="toggleFavorite" />
      </div>
      <div v-if="!(providers ?? []).length" class="text-muted">{{ t('providers.noProviders') }}</div>
    </div>
  </div>
</template>
