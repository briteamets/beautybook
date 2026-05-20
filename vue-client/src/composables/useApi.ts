import { ref } from 'vue'
import type { IResultObject } from '@/types/IResultObject'

export function useApi<T>(fn: () => Promise<IResultObject<T>>) {
  const data = ref<T | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function execute() {
    isLoading.value = true
    error.value = null
    const result = await fn()
    if (result.errors) {
      error.value = result.errors.join(', ')
    } else {
      data.value = result.data ?? null
    }
    isLoading.value = false
    return result
  }

  return { data, isLoading, error, execute }
}
