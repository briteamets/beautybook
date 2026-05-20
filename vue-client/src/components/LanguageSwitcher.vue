<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { locale } = useI18n()

function toggle() {
  locale.value = locale.value === 'en' ? 'et' : 'en'
  localStorage.setItem('locale', locale.value)
}
</script>

<template>
  <button class="btn btn-outline-secondary btn-sm" @click="toggle">
    {{ locale === 'en' ? 'ET' : 'EN' }}
  </button>
</template>
