<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { FavoritesService } from '@/services/FavoritesService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t } = useI18n()
const { data: favorites, isLoading, error, execute } = useApi(() => FavoritesService.getAll())
const removeError = ref<string | null>(null)

async function remove(id: string) {
  if (!confirm(t('favorites.confirmRemove'))) return
  removeError.value = null
  const result = await FavoritesService.remove(id)
  if (result.errors) removeError.value = result.errors.join(', ')
  else execute()
}

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('favorites.title') }}</h2>
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <ErrorAlert v-if="removeError" :errors="[removeError]" />
    <div v-if="!isLoading && !error">
      <p v-if="!(favorites ?? []).length" class="text-muted">{{ t('favorites.noFavorites') }}</p>
      <div class="list-group">
        <div
          v-for="f in favorites ?? []"
          :key="f.id"
          class="list-group-item d-flex justify-content-between align-items-center"
        >
          <div>
            <RouterLink :to="{ name: 'ProviderDetail', params: { id: f.providerId } }" class="fw-bold">
              {{ f.profileName }}
            </RouterLink>
            <div class="text-muted small">{{ f.address }} · ⭐ {{ f.averageRating.toFixed(1) }}</div>
          </div>
          <button class="btn btn-sm btn-outline-danger" @click="remove(f.id)">
            {{ t('favorites.remove') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
