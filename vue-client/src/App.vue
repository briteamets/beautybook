<script setup lang="ts">
import TheNavbar from '@/components/TheNavbar.vue'
import TheFooter from '@/components/TheFooter.vue'
</script>

<template>
  <div class="d-flex flex-column min-vh-100">
    <TheNavbar />
    <main class="flex-grow-1">
      <RouterView />
    </main>
    <TheFooter />
  </div>
</template>
