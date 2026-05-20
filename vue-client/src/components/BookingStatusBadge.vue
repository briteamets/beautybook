<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps<{ status: string }>()
const { t } = useI18n()

const badgeClass = computed(() => {
  switch (props.status) {
    case 'Pending': return 'bg-warning text-dark'
    case 'Confirmed': return 'bg-success'
    case 'Cancelled': return 'bg-danger'
    case 'Completed': return 'bg-secondary'
    default: return 'bg-light text-dark'
  }
})
</script>

<template>
  <span class="badge" :class="badgeClass">{{ t(`status.${status}`, status) }}</span>
</template>
