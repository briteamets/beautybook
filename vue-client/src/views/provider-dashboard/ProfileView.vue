<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ProvidersService } from '@/services/ProvidersService'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'

const { t } = useI18n()

const profileName = ref('')
const bio = ref('')
const address = ref('')
const lat = ref(0)
const lng = ref(0)
const profilePictureUrl = ref('')
const isLoading = ref(true)
const isSaving = ref(false)
const errors = ref<string[]>([])
const success = ref(false)

onMounted(async () => {
  const result = await ProvidersService.getMine()
  isLoading.value = false
  if (result.data) {
    const p = result.data
    profileName.value = p.profileName
    bio.value = p.bio ?? ''
    address.value = p.address
    lat.value = p.lat
    lng.value = p.lng
    profilePictureUrl.value = p.profilePictureUrl ?? ''
  } else if (result.errors) {
    errors.value = result.errors
  }
})

async function submit() {
  errors.value = []
  success.value = false
  isSaving.value = true
  const result = await ProvidersService.updateMine({
    profileName: profileName.value,
    bio: bio.value || undefined,
    address: address.value,
    lat: lat.value,
    lng: lng.value,
    profilePictureUrl: profilePictureUrl.value || undefined,
  })
  isSaving.value = false
  if (result.errors) errors.value = result.errors
  else success.value = true
}
</script>

<template>
  <div class="container py-4" style="max-width:600px">
    <h2 class="mb-4">{{ t('providerDashboard.profile') }}</h2>
    <LoadingSpinner v-if="isLoading" />
    <div v-else>
      <ErrorAlert v-if="errors.length" :errors="errors" />
      <div v-if="success" class="alert alert-success">{{ t('common.saved') }}</div>
      <form @submit.prevent="submit">
        <div class="form-floating mb-3">
          <input id="pname" v-model="profileName" type="text" class="form-control" placeholder="Name" required />
          <label for="pname">{{ t('providerDashboard.profile') }} Name</label>
        </div>
        <div class="mb-3">
          <label class="form-label">Bio</label>
          <textarea v-model="bio" class="form-control" rows="3" />
        </div>
        <div class="form-floating mb-3">
          <input id="addr" v-model="address" type="text" class="form-control" placeholder="Address" required />
          <label for="addr">Address</label>
        </div>
        <div class="row g-2 mb-3">
          <div class="col-6">
            <div class="form-floating">
              <input id="lat" v-model.number="lat" type="number" step="any" class="form-control" placeholder="lat" />
              <label for="lat">Latitude</label>
            </div>
          </div>
          <div class="col-6">
            <div class="form-floating">
              <input id="lng" v-model.number="lng" type="number" step="any" class="form-control" placeholder="lng" />
              <label for="lng">Longitude</label>
            </div>
          </div>
        </div>
        <div class="form-floating mb-3">
          <input id="pic" v-model="profilePictureUrl" type="url" class="form-control" placeholder="https://..." />
          <label for="pic">Profile picture URL</label>
        </div>
        <button type="submit" class="btn btn-primary" :disabled="isSaving">
          <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" />
          {{ t('common.save') }}
        </button>
      </form>
    </div>
  </div>
</template>
