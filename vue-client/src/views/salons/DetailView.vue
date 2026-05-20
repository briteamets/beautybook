<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { SalonsService } from '@/services/SalonsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const props = defineProps<{ id: string }>()
const { t } = useI18n()
const { data: salon, isLoading, error, execute } = useApi(() => SalonsService.getById(props.id))

onMounted(execute)
watch(() => props.id, execute)
</script>

<template>
  <div class="container py-4">
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else-if="salon">
      <div class="row mb-4">
        <div class="col-md-4">
          <img v-if="salon.profilePictureUrl" :src="salon.profilePictureUrl" class="img-fluid rounded shadow" :alt="salon.name" />
        </div>
        <div class="col-md-8">
          <h1>{{ salon.name }}</h1>
          <p class="text-muted">{{ salon.address }}</p>
          <p v-if="salon.description">{{ salon.description }}</p>
        </div>
      </div>

      <h3>{{ t('salons.staff') }}</h3>
      <div class="row g-3">
        <div v-for="m in salon.staffMembers" :key="m.providerId" class="col-sm-6 col-md-4">
          <div class="card p-3 d-flex flex-row align-items-center gap-3">
            <img
              v-if="m.profilePictureUrl"
              :src="m.profilePictureUrl"
              class="rounded-circle"
              style="width:48px;height:48px;object-fit:cover"
              :alt="m.providerName"
            />
            <div v-else class="rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center" style="width:48px;height:48px">
              {{ m.providerName.charAt(0) }}
            </div>
            <div>
              <div class="fw-bold">{{ m.providerName }}</div>
              <div class="text-muted small">{{ m.role }}</div>
              <RouterLink :to="{ name: 'ProviderDetail', params: { id: m.providerId } }" class="small">
                {{ t('salons.viewProfile') }}
              </RouterLink>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
