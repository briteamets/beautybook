import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { AccountService } from '@/services/AccountService'
import type { IRegisterInfo } from '@/types/IRegisterInfo'

function parseRoles(jwt: string): string[] {
  try {
    const payload = JSON.parse(atob((jwt.split('.')[1] ?? '').replace(/-/g, '+').replace(/_/g, '/')))
    const role = payload['role'] ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    if (!role) return []
    return Array.isArray(role) ? role : [role]
  } catch {
    return []
  }
}

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref(localStorage.getItem('jwt') ?? '')
  const refreshToken = ref(localStorage.getItem('refreshToken') ?? '')
  const email = ref(localStorage.getItem('email') ?? '')
  const firstName = ref(localStorage.getItem('firstName') ?? '')
  const lastName = ref(localStorage.getItem('lastName') ?? '')
  const roles = ref<string[]>(jwt.value ? parseRoles(jwt.value) : [])

  const isAuthenticated = computed(() => !!jwt.value)

  function hasRole(role: string): boolean {
    return roles.value.includes(role)
  }

  function _persist() {
    localStorage.setItem('jwt', jwt.value)
    localStorage.setItem('refreshToken', refreshToken.value)
    localStorage.setItem('email', email.value)
    localStorage.setItem('firstName', firstName.value)
    localStorage.setItem('lastName', lastName.value)
  }

  function _setTokens(newJwt: string, newRefresh: string) {
    jwt.value = newJwt
    refreshToken.value = newRefresh
    roles.value = parseRoles(newJwt)
  }

  function clear() {
    jwt.value = ''
    refreshToken.value = ''
    email.value = ''
    firstName.value = ''
    lastName.value = ''
    roles.value = []
    localStorage.removeItem('jwt')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('email')
    localStorage.removeItem('firstName')
    localStorage.removeItem('lastName')
  }

  async function loginAsync(
    userEmail: string,
    password: string,
  ): Promise<{ errors?: string[] }> {
    const result = await AccountService.login({ email: userEmail, password })
    if (result.errors) return { errors: result.errors }
    _setTokens(result.data!.jwt, result.data!.refreshToken)
    email.value = userEmail
    _persist()
    return {}
  }

  async function registerAsync(info: IRegisterInfo): Promise<{ errors?: string[] }> {
    const result = await AccountService.register(info)
    if (result.errors) return { errors: result.errors }
    _setTokens(result.data!.jwt, result.data!.refreshToken)
    email.value = info.email
    firstName.value = info.firstname
    lastName.value = info.lastname
    _persist()
    return {}
  }

  async function logoutAsync(): Promise<void> {
    if (refreshToken.value) {
      await AccountService.logout(refreshToken.value)
    }
    clear()
  }

  async function tryRefresh(): Promise<boolean> {
    if (!jwt.value || !refreshToken.value) return false
    const result = await AccountService.refreshToken({
      jwt: jwt.value,
      refreshToken: refreshToken.value,
    })
    if (result.errors || !result.data) return false
    _setTokens(result.data.jwt, result.data.refreshToken)
    _persist()
    return true
  }

  return {
    jwt,
    refreshToken,
    email,
    firstName,
    lastName,
    roles,
    isAuthenticated,
    hasRole,
    clear,
    loginAsync,
    registerAsync,
    logoutAsync,
    tryRefresh,
  }
})
