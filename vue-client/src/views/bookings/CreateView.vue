<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { BookingsService } from '@/services/BookingsService'
import { ServicesService } from '@/services/ServicesService'
import { AvailabilityService } from '@/services/AvailabilityService'
import ErrorAlert from '@/components/ErrorAlert.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type { IService } from '@/types/IService'
import type { IAvailability } from '@/types/IAvailability'

const props = defineProps<{ id: string }>()
const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const serviceId = ref((route.query.serviceId as string) ?? '')
const selectedDate = ref((route.query.date as string) ?? '')
const startTime = ref((route.query.startTime as string) ?? '')
const notes = ref('')
const errors = ref<string[]>([])
const isLoading = ref(false)

const service = ref<IService | null>(null)
const availability = ref<IAvailability | null>(null)

onMounted(async () => {
  if (serviceId.value) {
    const r = await ServicesService.getById(serviceId.value)
    if (r.data) service.value = r.data
  }
  if (props.id && selectedDate.value) {
    const r = await AvailabilityService.getAll({
      providerId: props.id,
      serviceId: serviceId.value || undefined,
      date: selectedDate.value,
    })
    if (r.data) {
      availability.value =
        r.data.find((a) => !a.isBooked && a.startTime.startsWith(startTime.value.slice(0, 5))) ??
        null
    }
  }
})

async function submit() {
  if (!serviceId.value || !availability.value) return
  errors.value = []
  isLoading.value = true
  const result = await BookingsService.create({
    serviceId: serviceId.value,
    availabilityId: availability.value.id,
    notes: notes.value || undefined,
  })
  isLoading.value = false
  if (result.errors) {
    errors.value = result.errors
  } else {
    router.push({ name: 'BookingDetail', params: { id: result.data!.id } })
  }
}
</script>

<template>
  <div class="container py-4" style="max-width: 540px">
    <h2 class="mb-4">{{ t('bookings.newBooking') }}</h2>
    <ErrorAlert v-if="errors.length" :errors="errors" />

    <LoadingSpinner v-if="!service && !availability" />

    <div v-else class="card p-4">
      <dl class="row mb-3">
        <dt class="col-5">{{ t('bookings.service') }}</dt>
        <dd class="col-7">{{ service?.nameEn || service?.name }}</dd>
        <dt class="col-5">{{ t('bookings.date') }}</dt>
        <dd class="col-7">{{ selectedDate }}</dd>
        <dt class="col-5">{{ t('bookings.selectSlot') }}</dt>
        <dd class="col-7">{{ startTime.slice(0, 5) }}</dd>
        <dt class="col-5">{{ t('bookings.price') }}</dt>
        <dd class="col-7">€{{ service?.price }}</dd>
      </dl>

      <div v-if="!availability" class="alert alert-warning">
        {{ t('bookings.noMatchingSlot') }}
      </div>

      <form @submit.prevent="submit">
        <div class="mb-3">
          <label class="form-label">{{ t('bookings.notes') }}</label>
          <textarea
            v-model="notes"
            class="form-control"
            rows="3"
            :placeholder="t('bookings.notesPlaceholder')"
          />
        </div>
        <button type="submit" class="btn btn-primary w-100" :disabled="isLoading || !availability">
          <span v-if="isLoading" class="spinner-border spinner-border-sm me-2" />
          {{ t('bookings.confirmBooking') }}
        </button>
      </form>
    </div>
  </div>
</template>
