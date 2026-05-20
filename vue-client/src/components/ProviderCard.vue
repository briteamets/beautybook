<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { IBeautyProvider } from '@/types/IBeautyProvider'
import RatingStars from './RatingStars.vue'

defineProps<{ provider: IBeautyProvider }>()
const emit = defineEmits<{ (e: 'favorite', id: string): void }>()
const { t } = useI18n()
</script>

<template>
  <div class="card h-100 shadow-sm">
    <img
      v-if="provider.profilePictureUrl"
      :src="provider.profilePictureUrl"
      class="card-img-top"
      style="height: 180px; object-fit: cover"
      :alt="provider.profileName"
    />
    <div v-else class="card-img-top bg-light d-flex align-items-center justify-content-center" style="height: 180px">
      <span class="text-muted fs-1">💆</span>
    </div>
    <div class="card-body d-flex flex-column">
      <h5 class="card-title">{{ provider.profileName }}</h5>
      <p class="card-text text-muted small mb-1">{{ provider.address }}</p>
      <div class="mb-1">
        <RatingStars :model-value="Math.round(provider.averageRating)" />
        <span class="text-muted small ms-1">({{ provider.reviewCount }} {{ t('providers.reviews') }})</span>
      </div>
      <p class="text-muted small mb-2">
        {{ provider.todayAvailability.filter(a => !a.isBooked).length }} {{ t('providers.availableToday') }}
      </p>
      <div class="mt-auto d-flex gap-2">
        <RouterLink
          :to="{ name: 'ProviderDetail', params: { id: provider.id } }"
          class="btn btn-primary btn-sm flex-grow-1"
        >
          {{ t('providers.bookNow') }}
        </RouterLink>
        <button class="btn btn-outline-danger btn-sm" @click="emit('favorite', provider.id)" :title="t('providers.addFavorite')">
          ♡
        </button>
      </div>
    </div>
  </div>
</template>
