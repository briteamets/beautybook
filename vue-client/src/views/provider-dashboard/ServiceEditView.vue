<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ServicesService } from '@/services/ServicesService'
import { CategoriesService } from '@/services/CategoriesService'
import { ServiceTypesService } from '@/services/ServiceTypesService'
import { ProvidersService } from '@/services/ProvidersService'
import { useApi } from '@/composables/useApi'
import ErrorAlert from '@/components/ErrorAlert.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type { IServiceType } from '@/types/IServiceType'

const props = defineProps<{ id?: string }>()
const router = useRouter()
const { t } = useI18n()

const isEdit = computed(() => !!props.id)

const nameEn = ref('')
const nameEt = ref('')
const descEn = ref('')
const descEt = ref('')
const price = ref(0)
const durationMinutes = ref(60)
const categoryId = ref('')
const serviceTypeId = ref('')
const isActive = ref(true)
const providerId = ref('')

const errors = ref<string[]>([])
const isLoading = ref(false)
const initLoading = ref(true)

const { data: categories, execute: loadCats } = useApi(() => CategoriesService.getAll())
const { data: serviceTypes, execute: loadTypes } = useApi(() => ServiceTypesService.getAll())

const filteredTypes = computed<IServiceType[]>(() =>
  (serviceTypes.value ?? []).filter((st) => !categoryId.value || st.serviceCategoryId === categoryId.value),
)

onMounted(async () => {
  await Promise.all([loadCats(), loadTypes()])

  const provResult = await ProvidersService.getMine()
  if (provResult.data) providerId.value = provResult.data.id

  if (isEdit.value && props.id) {
    const result = await ServicesService.getById(props.id)
    if (result.data) {
      const s = result.data
      nameEn.value = s.nameEn
      nameEt.value = s.nameEt
      descEn.value = s.descriptionEn ?? ''
      descEt.value = s.descriptionEt ?? ''
      price.value = s.price
      durationMinutes.value = s.durationMinutes
      categoryId.value = s.categoryId
      serviceTypeId.value = s.serviceTypeId ?? ''
      isActive.value = s.isActive
    }
  }
  initLoading.value = false
})

async function submit() {
  errors.value = []
  isLoading.value = true

  const nameMap: Record<string, string> = {}
  if (nameEn.value) nameMap['en'] = nameEn.value
  if (nameEt.value) nameMap['et'] = nameEt.value

  const descMap: Record<string, string> | undefined =
    descEn.value || descEt.value
      ? { ...(descEn.value ? { en: descEn.value } : {}), ...(descEt.value ? { et: descEt.value } : {}) }
      : undefined

  let result
  if (isEdit.value && props.id) {
    result = await ServicesService.update(props.id, {
      serviceCategoryId: categoryId.value,
      serviceTypeId: serviceTypeId.value || undefined,
      name: nameMap,
      description: descMap,
      price: price.value,
      durationMinutes: durationMinutes.value,
      isActive: isActive.value,
    })
  } else {
    result = await ServicesService.create({
      beautyProviderId: providerId.value,
      serviceCategoryId: categoryId.value,
      serviceTypeId: serviceTypeId.value || undefined,
      name: nameMap,
      description: descMap,
      price: price.value,
      durationMinutes: durationMinutes.value,
      isActive: isActive.value,
    })
  }

  isLoading.value = false
  if (result.errors) errors.value = result.errors
  else router.push({ name: 'ProviderServices' })
}
</script>

<template>
  <div class="container py-4" style="max-width:640px">
    <h2 class="mb-4">{{ isEdit ? t('providerDashboard.editService') : t('providerDashboard.newService') }}</h2>
    <LoadingSpinner v-if="initLoading" />
    <div v-else>
      <ErrorAlert v-if="errors.length" :errors="errors" />
      <form @submit.prevent="submit">
        <div class="row g-3 mb-3">
          <div class="col-md-6">
            <label class="form-label">{{ t('providerDashboard.nameEn') }}</label>
            <input v-model="nameEn" type="text" class="form-control" required />
          </div>
          <div class="col-md-6">
            <label class="form-label">{{ t('providerDashboard.nameEt') }}</label>
            <input v-model="nameEt" type="text" class="form-control" />
          </div>
          <div class="col-md-6">
            <label class="form-label">{{ t('providerDashboard.descriptionEn') }}</label>
            <textarea v-model="descEn" class="form-control" rows="2" />
          </div>
          <div class="col-md-6">
            <label class="form-label">{{ t('providerDashboard.descriptionEt') }}</label>
            <textarea v-model="descEt" class="form-control" rows="2" />
          </div>
          <div class="col-md-4">
            <label class="form-label">{{ t('providerDashboard.price') }}</label>
            <input v-model.number="price" type="number" min="0" step="0.01" class="form-control" required />
          </div>
          <div class="col-md-4">
            <label class="form-label">{{ t('providerDashboard.duration') }}</label>
            <input v-model.number="durationMinutes" type="number" min="5" step="5" class="form-control" required />
          </div>
          <div class="col-md-4 d-flex align-items-end pb-1">
            <div class="form-check">
              <input id="isActive" v-model="isActive" type="checkbox" class="form-check-input" />
              <label for="isActive" class="form-check-label">{{ t('providerDashboard.isActive') }}</label>
            </div>
          </div>
          <div class="col-md-6">
            <label class="form-label">{{ t('providerDashboard.category') }}</label>
            <select v-model="categoryId" class="form-select" required>
              <option value="">Select…</option>
              <option v-for="c in categories ?? []" :key="c.id" :value="c.id">{{ c.nameEn || c.name }}</option>
            </select>
          </div>
          <div class="col-md-6">
            <label class="form-label">{{ t('providerDashboard.serviceType') }}</label>
            <select v-model="serviceTypeId" class="form-select">
              <option value="">None</option>
              <option v-for="st in filteredTypes" :key="st.id" :value="st.id">{{ st.nameEn }}</option>
            </select>
          </div>
        </div>
        <div class="d-flex gap-2">
          <button type="submit" class="btn btn-primary" :disabled="isLoading">
            <span v-if="isLoading" class="spinner-border spinner-border-sm me-1" />
            {{ t('common.save') }}
          </button>
          <RouterLink :to="{ name: 'ProviderServices' }" class="btn btn-outline-secondary">{{ t('common.cancel') }}</RouterLink>
        </div>
      </form>
    </div>
  </div>
</template>
