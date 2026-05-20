<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ServicesService } from '@/services/ServicesService'
import { useApi } from '@/composables/useApi'
import { useLocaleText } from '@/composables/useLocaleText'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t } = useI18n()
const localeText = useLocaleText()
const { data: services, isLoading, error, execute } = useApi(() => ServicesService.getMine())

async function remove(id: string) {
  if (!confirm(t('providerDashboard.confirmDelete'))) return
  await ServicesService.remove(id)
  execute()
}

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>{{ t('providerDashboard.services') }}</h2>
      <RouterLink :to="{ name: 'ProviderServiceNew' }" class="btn btn-primary">
        + {{ t('providerDashboard.newService') }}
      </RouterLink>
    </div>
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else>
      <p v-if="!(services ?? []).length" class="text-muted">{{ t('common.noData') }}</p>
      <table v-else class="table table-hover align-middle">
        <thead class="table-light">
          <tr>
            <th>{{ t('providerDashboard.name') }}</th>
            <th>{{ t('providerDashboard.price') }}</th>
            <th>{{ t('providerDashboard.duration') }}</th>
            <th>{{ t('providerDashboard.isActive') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in services ?? []" :key="s.id">
            <td>{{ localeText(s.nameEn, s.nameEt) || s.name }}</td>
            <td>€{{ s.price }}</td>
            <td>{{ s.durationMinutes }} min</td>
            <td>
              <span class="badge" :class="s.isActive ? 'bg-success' : 'bg-secondary'">
                {{ s.isActive ? t('common.active') : t('common.inactive') }}
              </span>
            </td>
            <td class="d-flex gap-1">
              <RouterLink :to="{ name: 'ProviderServiceEdit', params: { id: s.id } }" class="btn btn-sm btn-outline-secondary">
                {{ t('common.edit') }}
              </RouterLink>
              <button class="btn btn-sm btn-outline-danger" @click="remove(s.id)">
                {{ t('common.delete') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
