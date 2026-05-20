<script setup lang="ts">
const model = defineModel<number>({ default: 0 })
const props = withDefaults(defineProps<{ editable?: boolean }>(), { editable: false })
</script>

<template>
  <span class="rating-stars">
    <template v-for="n in 5" :key="n">
      <span
        :class="['star', n <= model ? 'filled' : 'empty', props.editable ? 'clickable' : '']"
        @click="props.editable ? (model = n) : undefined"
      >★</span>
    </template>
  </span>
</template>

<style scoped>
.star { font-size: 1.2rem; }
.filled { color: #ffc107; }
.empty { color: #dee2e6; }
.clickable { cursor: pointer; }
.clickable:hover { color: #ffa000; }
</style>
