<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { AvailabilityService } from '@/services/AvailabilityService'
import { ProvidersService } from '@/services/ProvidersService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t } = useI18n()

const selectedDate = ref('')
const providerId = ref('')
const { data: slots, isLoading, error, execute: loadSlots } = useApi(() =>
  AvailabilityService.getMine({ date: selectedDate.value || undefined }),
)

const addDate = ref('')
const addStart = ref('09:00')
const addEnd = ref('10:00')
const addErrors = ref<string[]>([])
const addLoading = ref(false)
const deleteError = ref<string | null>(null)

onMounted(async () => {
  const result = await ProvidersService.getMine()
  if (result.data) providerId.value = result.data.id
  loadSlots()
})

watch(selectedDate, loadSlots)

async function deleteSlot(id: string) {
  if (!confirm(t('availability.confirmDelete'))) return
  deleteError.value = null
  const result = await AvailabilityService.remove(id)
  if (result.errors) deleteError.value = result.errors.join(', ')
  else loadSlots()
}

async function addSlot() {
  addErrors.value = []
  addLoading.value = true
  const result = await AvailabilityService.create({
    providerId: providerId.value,
    date: addDate.value,
    startTime: addStart.value + ':00',
    endTime: addEnd.value + ':00',
  })
  addLoading.value = false
  if (result.errors) addErrors.value = result.errors
  else { addDate.value = ''; loadSlots() }
}
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('availability.title') }}</h2>

    <div class="mb-3" style="max-width:220px">
      <label class="form-label">{{ t('availability.date') }}</label>
      <input v-model="selectedDate" type="date" class="form-control" />
    </div>

    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <ErrorAlert v-if="deleteError" :errors="[deleteError]" />
    <div v-if="!isLoading && !error" class="mb-4">
      <p v-if="!(slots ?? []).length" class="text-muted">{{ t('availability.noSlots') }}</p>
      <div class="list-group" style="max-width:500px">
        <div
          v-for="s in slots ?? []"
          :key="s.id"
          class="list-group-item d-flex justify-content-between align-items-center"
        >
          <div>
            <span>{{ s.startTime.slice(0, 5) }} – {{ s.endTime.slice(0, 5) }}</span>
            <span class="ms-2 badge" :class="s.isBooked ? 'bg-danger' : 'bg-success'">
              {{ s.isBooked ? t('availability.booked') : t('availability.free') }}
            </span>
          </div>
          <button v-if="!s.isBooked" class="btn btn-sm btn-outline-danger" @click="deleteSlot(s.id)">
            {{ t('common.delete') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Add slot -->
    <h5>{{ t('availability.addSlot') }}</h5>
    <ErrorAlert v-if="addErrors.length" :errors="addErrors" />
    <form @submit.prevent="addSlot" class="row g-2 align-items-end" style="max-width:500px">
      <div class="col">
        <label class="form-label small">{{ t('availability.date') }}</label>
        <input v-model="addDate" type="date" class="form-control" required />
      </div>
      <div class="col">
        <label class="form-label small">{{ t('availability.startTime') }}</label>
        <input v-model="addStart" type="time" class="form-control" required />
      </div>
      <div class="col">
        <label class="form-label small">{{ t('availability.endTime') }}</label>
        <input v-model="addEnd" type="time" class="form-control" required />
      </div>
      <div class="col-auto">
        <button type="submit" class="btn btn-primary" :disabled="addLoading">
          <span v-if="addLoading" class="spinner-border spinner-border-sm me-1" />
          {{ t('common.add') }}
        </button>
      </div>
    </form>
  </div>
</template>
