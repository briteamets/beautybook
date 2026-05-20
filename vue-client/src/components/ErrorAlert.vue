<script setup lang="ts">
defineProps<{ errors: string[] }>()
</script>

<template>
  <div class="alert alert-danger" role="alert">
    <ul class="mb-0 ps-3">
      <li v-for="(e, i) in errors" :key="i">{{ e }}</li>
    </ul>
  </div>
</template>
