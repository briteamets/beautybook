<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { BookingsService } from '@/services/BookingsService'
import { PaymentsService } from '@/services/PaymentsService'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t, locale } = useI18n()

const { data: bookings, isLoading: bookingsLoading, execute: loadBookings } = useApi(() => BookingsService.getAll())
const { data: payments, isLoading: paymentsLoading, execute: loadPayments } = useApi(() => PaymentsService.getAll())

const selectedIds = ref<Set<string>>(new Set())
const payMethod = ref('Card')
const payErrors = ref<string[]>([])
const payLoading = ref(false)

const unpaid = computed(() =>
  (bookings.value ?? []).filter((b) => b.status === 'Completed' && !b.isPaid),
)

function toggleSelect(id: string) {
  if (selectedIds.value.has(id)) selectedIds.value.delete(id)
  else selectedIds.value.add(id)
}

async function paySelected() {
  if (!selectedIds.value.size) return
  payErrors.value = []
  payLoading.value = true
  const result = await PaymentsService.create({
    bookingIds: Array.from(selectedIds.value),
    paymentMethod: payMethod.value,
  })
  payLoading.value = false
  if (result.errors) payErrors.value = result.errors
  else { selectedIds.value.clear(); await Promise.all([loadBookings(), loadPayments()]) }
}

function fmtDate(iso: string) {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium' }).format(new Date(iso))
}

onMounted(async () => { await Promise.all([loadBookings(), loadPayments()]) })
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-4">{{ t('payments.title') }}</h2>

    <!-- Unpaid bookings -->
    <h4>{{ t('payments.unpaidBookings') }}</h4>
    <LoadingSpinner v-if="bookingsLoading" />
    <div v-else>
      <p v-if="!unpaid.length" class="text-muted">{{ t('payments.noUnpaid') }}</p>
      <div v-else>
        <ErrorAlert v-if="payErrors.length" :errors="payErrors" />
        <table class="table table-hover mb-2">
          <thead class="table-light">
            <tr>
              <th></th>
              <th>{{ t('bookings.service') }}</th>
              <th>{{ t('bookings.provider') }}</th>
              <th>{{ t('bookings.date') }}</th>
              <th>{{ t('payments.amount') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="b in unpaid" :key="b.id">
              <td>
                <input
                  type="checkbox"
                  class="form-check-input"
                  :checked="selectedIds.has(b.id)"
                  @change="toggleSelect(b.id)"
                />
              </td>
              <td>{{ b.serviceName }}</td>
              <td>{{ b.providerName }}</td>
              <td>{{ fmtDate(b.appointmentDateTime) }}</td>
              <td>€{{ b.totalPrice }}</td>
            </tr>
          </tbody>
        </table>
        <div class="d-flex gap-2 align-items-center">
          <select v-model="payMethod" class="form-select" style="max-width:160px">
            <option value="Card">{{ t('payments.card') }}</option>
            <option value="Cash">{{ t('payments.cash') }}</option>
          </select>
          <button class="btn btn-success" :disabled="payLoading || !selectedIds.size" @click="paySelected">
            <span v-if="payLoading" class="spinner-border spinner-border-sm me-1" />
            {{ t('payments.paySelected') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Payment history -->
    <h4 class="mt-5">{{ t('payments.history') }}</h4>
    <LoadingSpinner v-if="paymentsLoading" />
    <div v-else>
      <p v-if="!(payments ?? []).length" class="text-muted">{{ t('payments.noPayments') }}</p>
      <table v-else class="table">
        <thead class="table-light">
          <tr>
            <th>{{ t('payments.amount') }}</th>
            <th>{{ t('payments.currency') }}</th>
            <th>{{ t('payments.status') }}</th>
            <th>{{ t('payments.paidAt') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in payments ?? []" :key="p.id">
            <td>{{ p.amount }}</td>
            <td>{{ p.currency }}</td>
            <td>{{ p.status }}</td>
            <td>{{ p.paidAt ? fmtDate(p.paidAt) : '—' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
