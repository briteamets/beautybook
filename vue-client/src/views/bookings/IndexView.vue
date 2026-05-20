<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { BookingsService } from '@/services/BookingsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import BookingStatusBadge from '@/components/BookingStatusBadge.vue'

const { t, locale } = useI18n()
const { data: bookings, isLoading, error, execute } = useApi(() => BookingsService.getAll())

const activeTab = ref<'upcoming' | 'past' | 'cancelled'>('upcoming')

const filtered = computed(() => {
  const now = new Date()
  return (bookings.value ?? []).filter((b) => {
    const dt = new Date(b.appointmentDateTime)
    if (activeTab.value === 'cancelled') return b.status === 'Cancelled'
    if (activeTab.value === 'past') return b.status !== 'Cancelled' && dt < now
    return b.status !== 'Cancelled' && dt >= now
  })
})

function fmtDate(iso: string) {
  return new Intl.DateTimeFormat(locale.value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}

async function cancel(id: string) {
  if (!confirm(t('bookings.confirmCancel'))) return
  const reason = prompt(t('bookings.cancelReason')) ?? undefined
  await BookingsService.cancel(id, { reason })
  execute()
}

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('bookings.title') }}</h2>

    <ul class="nav nav-tabs mb-3">
      <li class="nav-item">
        <button class="nav-link" :class="activeTab === 'upcoming' ? 'active' : ''" @click="activeTab = 'upcoming'">
          {{ t('bookings.upcoming') }}
        </button>
      </li>
      <li class="nav-item">
        <button class="nav-link" :class="activeTab === 'past' ? 'active' : ''" @click="activeTab = 'past'">
          {{ t('bookings.past') }}
        </button>
      </li>
      <li class="nav-item">
        <button class="nav-link" :class="activeTab === 'cancelled' ? 'active' : ''" @click="activeTab = 'cancelled'">
          {{ t('bookings.cancelled') }}
        </button>
      </li>
    </ul>

    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else>
      <div v-if="!filtered.length" class="text-muted">{{ t('bookings.noBookings') }}</div>
      <div class="table-responsive" v-else>
        <table class="table table-hover align-middle">
          <thead class="table-light">
            <tr>
              <th>{{ t('bookings.date') }}</th>
              <th>{{ t('bookings.service') }}</th>
              <th>{{ t('bookings.provider') }}</th>
              <th>{{ t('bookings.status') }}</th>
              <th>{{ t('bookings.price') }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="b in filtered" :key="b.id">
              <td>{{ fmtDate(b.appointmentDateTime) }}</td>
              <td>{{ b.serviceName }}</td>
              <td>{{ b.providerName }}</td>
              <td><BookingStatusBadge :status="b.status" /></td>
              <td>€{{ b.totalPrice }} <span class="badge" :class="b.isPaid ? 'bg-success' : 'bg-warning text-dark'">{{ b.isPaid ? t('bookings.paid') : t('bookings.unpaid') }}</span></td>
              <td class="d-flex gap-1">
                <RouterLink :to="{ name: 'BookingDetail', params: { id: b.id } }" class="btn btn-sm btn-outline-secondary">
                  {{ t('common.view') }}
                </RouterLink>
                <button
                  v-if="b.status === 'Pending' || b.status === 'Confirmed'"
                  class="btn btn-sm btn-outline-danger"
                  @click="cancel(b.id)"
                >
                  {{ t('bookings.cancel') }}
                </button>
                <RouterLink
                  v-if="b.status === 'Completed' && !b.isPaid"
                  :to="{ name: 'Payments' }"
                  class="btn btn-sm btn-success"
                >
                  {{ t('bookings.pay') }}
                </RouterLink>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
