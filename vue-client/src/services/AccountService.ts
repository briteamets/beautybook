import axios, { type AxiosError } from 'axios'
import type { IResultObject } from '@/types/IResultObject'
import type { IUserInfo } from '@/types/IUserInfo'
import type { ILoginInfo } from '@/types/ILoginInfo'
import type { IRegisterInfo } from '@/types/IRegisterInfo'
import type { ITokenRefreshInfo } from '@/types/ITokenRefreshInfo'

const accountClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})

function extractErrors(e: unknown): string[] {
  const err = e as AxiosError<{ error?: string } | string>
  if (err.response?.data) {
    const d = err.response.data
    if (typeof d === 'string') return [d]
    if (d.error) return [d.error]
  }
  return [(e as Error).message ?? 'Unknown error']
}

export const AccountService = {
  async login(info: ILoginInfo): Promise<IResultObject<IUserInfo>> {
    try {
      const { data } = await accountClient.post<IUserInfo>('identity/account/Login', info)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async register(info: IRegisterInfo): Promise<IResultObject<IUserInfo>> {
    try {
      const { data } = await accountClient.post<IUserInfo>('identity/account/Register', info)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async refreshToken(info: ITokenRefreshInfo): Promise<IResultObject<IUserInfo>> {
    try {
      const { data } = await accountClient.post<IUserInfo>(
        'identity/account/RefreshTokenData',
        info,
      )
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async logout(refreshToken: string): Promise<IResultObject<void>> {
    try {
      await accountClient.post('identity/account/Logout', { refreshToken })
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
