import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { NotificationsService } from '@/services/NotificationsService'

export const useNotificationsStore = defineStore('notifications', () => {
  const unreadCount = ref(0)

  const hasUnread = computed(() => unreadCount.value > 0)

  async function fetchUnreadCount() {
    const result = await NotificationsService.getAll()
    if (result.data) {
      unreadCount.value = result.data.filter((n) => !n.isRead).length
    }
  }

  function decrement() {
    if (unreadCount.value > 0) unreadCount.value--
  }

  return { unreadCount, hasUnread, fetchUnreadCount, decrement }
})
