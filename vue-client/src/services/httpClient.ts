import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/stores/auth'
import i18n from '@/i18n'

const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})

httpClient.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.jwt) config.headers.Authorization = `Bearer ${auth.jwt}`
  config.headers['Accept-Language'] = i18n.global.locale.value
  return config
})

httpClient.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retried?: boolean }
    const auth = useAuthStore()

    if (
      error.response?.status === 401 &&
      !original._retried &&
      auth.jwt &&
      auth.refreshToken &&
      !original.url?.includes('identity/account/')
    ) {
      original._retried = true
      const ok = await auth.tryRefresh()
      if (ok) {
        original.headers.Authorization = `Bearer ${auth.jwt}`
        return httpClient(original)
      }
      auth.clear()
    }
    return Promise.reject(error)
  },
)

export default httpClient
