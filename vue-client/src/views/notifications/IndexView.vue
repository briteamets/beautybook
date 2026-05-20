<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { NotificationsService } from '@/services/NotificationsService'
import { useNotificationsStore } from '@/stores/notifications'
import { useApi } from '@/composables/useApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t, locale } = useI18n()
const notifStore = useNotificationsStore()
const { data: notifications, isLoading, error, execute } = useApi(() => NotificationsService.getAll())

async function markRead(id: string) {
  await NotificationsService.markRead(id)
  notifStore.decrement()
  execute()
}

function fmtDate(iso: string) {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(iso))
}

onMounted(execute)
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">{{ t('notifications.title') }}</h2>
    <LoadingSpinner v-if="isLoading" />
    <ErrorAlert v-else-if="error" :errors="[error]" />
    <div v-else>
      <p v-if="!(notifications ?? []).length" class="text-muted">{{ t('notifications.noNotifications') }}</p>
      <div class="list-group">
        <div
          v-for="n in notifications ?? []"
          :key="n.id"
          class="list-group-item"
          :class="!n.isRead ? 'list-group-item-warning' : ''"
        >
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <strong>{{ n.title }}</strong>
              <p class="mb-0">{{ n.body }}</p>
              <small class="text-muted">{{ fmtDate(n.createdAt) }}</small>
            </div>
            <button
              v-if="!n.isRead"
              class="btn btn-sm btn-outline-secondary ms-2"
              @click="markRead(n.id)"
            >
              {{ t('notifications.markRead') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
