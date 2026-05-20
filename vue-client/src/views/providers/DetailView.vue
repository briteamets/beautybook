<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ProvidersService } from '@/services/ProvidersService'
import { ServicesService } from '@/services/ServicesService'
import { ReviewsService } from '@/services/ReviewsService'
import { FavoritesService } from '@/services/FavoritesService'
import { useApi } from '@/composables/useApi'
import { useAuthStore } from '@/stores/auth'
import { useLocaleText } from '@/composables/useLocaleText'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import RatingStars from '@/components/RatingStars.vue'
import type { IService } from '@/types/IService'
import type { IAvailableSlot } from '@/types/IAvailableSlot'

const props = defineProps<{ id: string }>()
const router = useRouter()
const { t, locale } = useI18n()
const auth = useAuthStore()
const localeText = useLocaleText()

const { data: provider, isLoading, error, execute: loadProvider } = useApi(() =>
  ProvidersService.getById(props.id),
)
const { data: services, execute: loadServices } = useApi(() =>
  ServicesService.getAll({ providerId: props.id }),
)
const { data: reviews, execute: loadReviews } = useApi(() =>
  ReviewsService.getByProvider(props.id),
)

const selectedService = ref<IService | null>(null)
const selectedDate = ref('')
const slots = ref<IAvailableSlot[]>([])
const slotsLoading = ref(false)
const slotsError = ref<string | null>(null)

async function loadSlots() {
  if (!selectedService.value || !selectedDate.value) return
  slotsLoading.value = true
  slotsError.value = null
  // Use availabilityId-based booking path via availabilities
  const { AvailabilityService } = await import('@/services/AvailabilityService')
  const result = await AvailabilityService.getAll({
    providerId: props.id,
    serviceId: selectedService.value.id,
    date: selectedDate.value,
  })
  slotsLoading.value = false
  if (result.errors) {
    slotsError.value = result.errors.join(', ')
  } else {
    slots.value = (result.data ?? [])
      .filter((a) => !a.isBooked)
      .map((a) => ({ startTime: a.startTime, endTime: a.endTime }))
  }
}

watch([selectedService, selectedDate], loadSlots)

function selectSlot(slot: IAvailableSlot) {
  if (!auth.isAuthenticated) {
    router.push({ name: 'Login', query: { redirect: `/providers/${props.id}/book` } })
    return
  }
  router.push({
    name: 'BookProvider',
    params: { id: props.id },
    query: {
      serviceId: selectedService.value?.id,
      startTime: slot.startTime,
      date: selectedDate.value,
    },
  })
}

async function addFavorite() {
  if (!auth.isAuthenticated) return
  await FavoritesService.create({ beautyProviderId: props.id })
}

onMounted(async () => {
  await Promise.all([loadProvider(), loadServices(), loadReviews()])
})

watch(() => props.id, async () => {
  await Promise.all([loadProvider(), loadServices(), loadReviews()])
  selectedService.value = null
  selectedDate.value = ''
  slots.value = []
})

function serviceName(s: IService) {
  return localeText(s.nameEn, s.nameEt) || s.name
}
</script>

<template>
  <div class="container py-4">
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else-if="provider">
      <!-- Header -->
      <div class="row mb-4">
        <div class="col-md-3">
          <img
            v-if="provider.profilePictureUrl"
            :src="provider.profilePictureUrl"
            class="img-fluid rounded shadow"
            :alt="provider.profileName"
          />
          <div v-else class="bg-light rounded d-flex align-items-center justify-content-center" style="height:200px">
            <span class="fs-1">💆</span>
          </div>
        </div>
        <div class="col-md-9">
          <div class="d-flex align-items-start justify-content-between">
            <div>
              <h1>{{ provider.profileName }}</h1>
              <p class="text-muted">{{ provider.address }}</p>
              <div class="d-flex align-items-center gap-2">
                <RatingStars :model-value="Math.round(provider.averageRating)" />
                <span class="text-muted">{{ provider.averageRating.toFixed(1) }} ({{ provider.reviewCount }} {{ t('providers.reviews') }})</span>
              </div>
            </div>
            <button v-if="auth.isAuthenticated && auth.hasRole('Client')" class="btn btn-outline-danger" @click="addFavorite">♡ {{ t('providers.addFavorite') }}</button>
          </div>
          <p v-if="provider.bio" class="mt-3">{{ provider.bio }}</p>
        </div>
      </div>

      <!-- Services -->
      <h3>{{ t('providers.services') }}</h3>
      <div class="row g-2 mb-4">
        <div v-for="s in services ?? []" :key="s.id" class="col-md-6">
          <div
            class="card p-3 cursor-pointer"
            :class="selectedService?.id === s.id ? 'border-primary' : ''"
            style="cursor:pointer"
            @click="selectedService = s"
          >
            <div class="d-flex justify-content-between">
              <strong>{{ serviceName(s) }}</strong>
              <span class="text-primary">€{{ s.price }}</span>
            </div>
            <small class="text-muted">{{ s.durationMinutes }} min</small>
          </div>
        </div>
      </div>

      <!-- Date + Slots -->
      <div v-if="selectedService" class="mb-4">
        <h5>{{ t('providers.selectDate') }}</h5>
        <input v-model="selectedDate" type="date" class="form-control mb-3" style="max-width:220px" />
        <div v-if="selectedDate">
          <h6>{{ t('providers.availableSlots') }}</h6>
          <div v-if="slotsLoading"><LoadingSpinner /></div>
          <div v-else-if="slotsError" class="text-danger">{{ slotsError }}</div>
          <div v-else-if="slots.length" class="d-flex flex-wrap gap-2">
            <button
              v-for="(slot, i) in slots"
              :key="i"
              class="btn btn-outline-primary btn-sm"
              @click="selectSlot(slot)"
            >
              {{ slot.startTime.slice(0, 5) }} – {{ slot.endTime.slice(0, 5) }}
            </button>
          </div>
          <p v-else class="text-muted">{{ t('providers.noSlots') }}</p>
        </div>
      </div>

      <!-- Reviews -->
      <h3>{{ t('providers.reviews2') }}</h3>
      <div v-if="!(reviews ?? []).length" class="text-muted mb-3">{{ t('reviews.noReviews') }}</div>
      <div v-for="r in reviews ?? []" :key="r.id" class="card mb-2 p-3">
        <div class="d-flex justify-content-between">
          <strong>{{ r.userName }}</strong>
          <RatingStars :model-value="r.rating" />
        </div>
        <p v-if="r.comment" class="mb-0 mt-1">{{ r.comment }}</p>
        <small class="text-muted">{{ new Intl.DateTimeFormat(locale).format(new Date(r.createdAt)) }}</small>
      </div>
    </div>
  </div>
</template>
