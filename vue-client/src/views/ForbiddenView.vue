<template>
  <div class="container py-5 text-center">
    <h1 class="display-1">403</h1>
    <h2>{{ $t('forbidden.title') }}</h2>
    <p class="text-muted">{{ $t('forbidden.message') }}</p>
    <RouterLink class="btn btn-primary" :to="{ name: 'Home' }">{{ $t('notFound.home') }}</RouterLink>
  </div>
</template>
