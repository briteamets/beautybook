<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { BookingsService } from '@/services/BookingsService'
import { ReviewsService } from '@/services/ReviewsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import BookingStatusBadge from '@/components/BookingStatusBadge.vue'
import RatingStars from '@/components/RatingStars.vue'

const props = defineProps<{ id: string }>()
const { t, locale } = useI18n()

const { data: booking, isLoading, error, execute } = useApi(() => BookingsService.getById(props.id))

const cancelReason = ref('')
const cancelErrors = ref<string[]>([])
const cancelLoading = ref(false)

const reviewRating = ref(5)
const reviewComment = ref('')
const reviewErrors = ref<string[]>([])
const reviewLoading = ref(false)
const reviewDone = ref(false)

function fmtDate(iso: string) {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'long', timeStyle: 'short' }).format(new Date(iso))
}

async function submitCancel() {
  if (!confirm(t('bookings.confirmCancel'))) return
  cancelErrors.value = []
  cancelLoading.value = true
  const result = await BookingsService.cancel(props.id, { reason: cancelReason.value || undefined })
  cancelLoading.value = false
  if (result.errors) cancelErrors.value = result.errors
  else execute()
}

async function submitReview() {
  reviewErrors.value = []
  reviewLoading.value = true
  const result = await ReviewsService.create({
    bookingId: props.id,
    rating: reviewRating.value,
    comment: reviewComment.value || undefined,
  })
  reviewLoading.value = false
  if (result.errors) reviewErrors.value = result.errors
  else { reviewDone.value = true; execute() }
}

onMounted(execute)
watch(() => props.id, execute)
</script>

<template>
  <div class="container py-4" style="max-width:680px">
    <RouterLink :to="{ name: 'MyBookings' }" class="btn btn-link ps-0 mb-3">← {{ t('common.back') }}</RouterLink>
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else-if="booking">
      <div class="d-flex justify-content-between align-items-start mb-3">
        <h2>{{ booking.serviceName }}</h2>
        <BookingStatusBadge :status="booking.status" />
      </div>

      <dl class="row">
        <dt class="col-5">{{ t('bookings.provider') }}</dt>
        <dd class="col-7">{{ booking.providerName }}</dd>
        <dt class="col-5">{{ t('bookings.date') }}</dt>
        <dd class="col-7">{{ fmtDate(booking.appointmentDateTime) }}</dd>
        <dt class="col-5">{{ t('bookings.price') }}</dt>
        <dd class="col-7">€{{ booking.totalPrice }} — {{ booking.isPaid ? t('bookings.paid') : t('bookings.unpaid') }}</dd>
        <template v-if="booking.notes">
          <dt class="col-5">{{ t('bookings.notes') }}</dt>
          <dd class="col-7">{{ booking.notes }}</dd>
        </template>
      </dl>

      <!-- Cancel form -->
      <div v-if="booking.status === 'Pending' || booking.status === 'Confirmed'" class="card p-3 mb-3">
        <h5>{{ t('bookings.cancel') }}</h5>
        <ErrorAlert v-if="cancelErrors.length" :errors="cancelErrors" />
        <form @submit.prevent="submitCancel">
          <div class="mb-2">
            <input v-model="cancelReason" type="text" class="form-control" :placeholder="t('bookings.cancelReason')" />
          </div>
          <button type="submit" class="btn btn-danger btn-sm" :disabled="cancelLoading">
            <span v-if="cancelLoading" class="spinner-border spinner-border-sm me-1" />
            {{ t('bookings.cancel') }}
          </button>
        </form>
      </div>

      <!-- Review form -->
      <div v-if="booking.status === 'Completed' && !booking.hasReview && !reviewDone" class="card p-3 mb-3">
        <h5>{{ t('bookings.leaveReview') }}</h5>
        <ErrorAlert v-if="reviewErrors.length" :errors="reviewErrors" />
        <form @submit.prevent="submitReview">
          <div class="mb-2">
            <label class="form-label">{{ t('reviews.rating') }}</label>
            <RatingStars v-model="reviewRating" :editable="true" />
          </div>
          <div class="mb-2">
            <textarea v-model="reviewComment" class="form-control" rows="3" :placeholder="t('reviews.comment')" />
          </div>
          <button type="submit" class="btn btn-primary btn-sm" :disabled="reviewLoading">
            <span v-if="reviewLoading" class="spinner-border spinner-border-sm me-1" />
            {{ t('reviews.submit') }}
          </button>
        </form>
      </div>
      <div v-if="reviewDone" class="alert alert-success">{{ t('bookings.reviewSubmitted') }}</div>

      <!-- Pay -->
      <RouterLink
        v-if="booking.status === 'Completed' && !booking.isPaid"
        :to="{ name: 'Payments' }"
        class="btn btn-success"
      >
        {{ t('bookings.pay') }}
      </RouterLink>
    </div>
  </div>
</template>
