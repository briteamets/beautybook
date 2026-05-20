<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { SalonsService } from '@/services/SalonsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t } = useI18n()
const { data: salons, isLoading, error, execute } = useApi(() => SalonsService.getAll())

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('salons.title') }}</h2>
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else class="row g-3">
      <p v-if="!(salons ?? []).length" class="text-muted">{{ t('salons.noSalons') }}</p>
      <div v-for="s in salons ?? []" :key="s.id" class="col-md-6 col-lg-4">
        <div class="card h-100 shadow-sm">
          <img v-if="s.profilePictureUrl" :src="s.profilePictureUrl" class="card-img-top" style="height:160px;object-fit:cover" :alt="s.name" />
          <div class="card-body">
            <h5 class="card-title">{{ s.name }}</h5>
            <p class="card-text text-muted small">{{ s.address }}</p>
            <RouterLink :to="{ name: 'SalonDetail', params: { id: s.id } }" class="btn btn-outline-primary btn-sm">
              {{ t('common.view') }}
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
