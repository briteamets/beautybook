import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type {
  IBeautyProvider,
  IBeautyProviderCreate,
  IBeautyProviderUpdate,
} from '@/types/IBeautyProvider'

export const ProvidersService = {
  async getAll(params?: {
    serviceTypeId?: string
    providerType?: string
    search?: string
  }): Promise<IResultObject<IBeautyProvider[]>> {
    try {
      const { data } = await httpClient.get<IBeautyProvider[]>('providers', { params })
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getMine(): Promise<IResultObject<IBeautyProvider>> {
    try {
      const { data } = await httpClient.get<IBeautyProvider>('providers/mine')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getById(id: string): Promise<IResultObject<IBeautyProvider>> {
    try {
      const { data } = await httpClient.get<IBeautyProvider>(`providers/${id}`)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: IBeautyProviderCreate): Promise<IResultObject<IBeautyProvider>> {
    try {
      const { data } = await httpClient.post<IBeautyProvider>('providers', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async updateMine(body: IBeautyProviderUpdate): Promise<IResultObject<IBeautyProvider>> {
    try {
      const { data } = await httpClient.put<IBeautyProvider>('providers/mine', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async update(id: string, body: IBeautyProviderUpdate): Promise<IResultObject<IBeautyProvider>> {
    try {
      const { data } = await httpClient.put<IBeautyProvider>(`providers/${id}`, body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async remove(id: string): Promise<IResultObject<void>> {
    try {
      await httpClient.delete(`providers/${id}`)
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
