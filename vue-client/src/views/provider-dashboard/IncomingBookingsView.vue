<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { BookingsService } from '@/services/BookingsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import BookingStatusBadge from '@/components/BookingStatusBadge.vue'
import type { BookingStatus } from '@/types/IBooking'

const { t, locale } = useI18n()
const { data: bookings, isLoading, error, execute } = useApi(() => BookingsService.getIncoming())
const actionError = ref<string | null>(null)
const actionSuccess = ref<string | null>(null)
const actionPending = ref<string | null>(null)

function fmtDate(iso: string) {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(iso))
}

async function setStatus(id: string, status: BookingStatus) {
  let reason: string | undefined
  if (status === 'Cancelled') {
    const input = prompt(t('providerDashboard.rejectReason'))
    if (input === null) return
    reason = input || undefined
  }
  actionError.value = null
  actionSuccess.value = null
  actionPending.value = id
  const result = await BookingsService.updateStatus(id, { status, reason })
  actionPending.value = null
  if (result.errors) {
    actionError.value = result.errors.join(', ')
  } else {
    actionSuccess.value = status
    await execute()
  }
}

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('providerDashboard.incoming2') }}</h2>
    <ErrorAlert v-if="actionError" :errors="[actionError]" class="mb-3" />
    <div v-if="actionSuccess" class="alert alert-success mb-3">
      {{ actionSuccess === 'Confirmed' ? t('providerDashboard.statusConfirmed', 'Booking confirmed.') :
         actionSuccess === 'Cancelled' ? t('providerDashboard.statusCancelled', 'Booking cancelled.') :
         t('providerDashboard.statusCompleted', 'Booking marked as completed.') }}
    </div>
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else>
      <p v-if="!(bookings ?? []).length" class="text-muted">{{ t('providerDashboard.noIncoming') }}</p>
      <table v-else class="table table-hover align-middle">
        <thead class="table-light">
          <tr>
            <th>{{ t('bookings.date') }}</th>
            <th>{{ t('bookings.service') }}</th>
            <th>{{ t('providerDashboard.client') }}</th>
            <th>{{ t('bookings.status') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="b in bookings ?? []" :key="b.id">
            <td>{{ fmtDate(b.appointmentDateTime) }}</td>
            <td>{{ b.serviceName }}</td>
            <td>{{ b.clientName }}</td>
            <td><BookingStatusBadge :status="b.status" /></td>
            <td>
              <div class="d-flex gap-1">
                <button
                  v-if="b.status === 'Pending'"
                  class="btn btn-sm btn-success"
                  :disabled="actionPending === b.id"
                  @click="setStatus(b.id, 'Confirmed')"
                >{{ t('providerDashboard.statusConfirm') }}</button>
                <button
                  v-if="b.status === 'Pending' || b.status === 'Confirmed'"
                  class="btn btn-sm btn-danger"
                  :disabled="actionPending === b.id"
                  @click="setStatus(b.id, 'Cancelled')"
                >{{ t('providerDashboard.statusReject') }}</button>
                <button
                  v-if="b.status === 'Confirmed'"
                  class="btn btn-sm btn-secondary"
                  :disabled="actionPending === b.id"
                  @click="setStatus(b.id, 'Completed')"
                >{{ t('providerDashboard.statusComplete') }}</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
