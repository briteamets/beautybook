<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { BookingsService } from '@/services/BookingsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import BookingStatusBadge from '@/components/BookingStatusBadge.vue'
import type { BookingStatus } from '@/types/IBooking'

const { t, locale } = useI18n()
const { data: bookings, isLoading, error, execute } = useApi(() => BookingsService.getIncoming())

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
  await BookingsService.updateStatus(id, { status, reason })
  execute()
}

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('providerDashboard.incoming2') }}</h2>
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
                  @click="setStatus(b.id, 'Confirmed')"
                >{{ t('providerDashboard.statusConfirm') }}</button>
                <button
                  v-if="b.status === 'Pending' || b.status === 'Confirmed'"
                  class="btn btn-sm btn-danger"
                  @click="setStatus(b.id, 'Cancelled')"
                >{{ t('providerDashboard.statusReject') }}</button>
                <button
                  v-if="b.status === 'Confirmed'"
                  class="btn btn-sm btn-secondary"
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
