import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IService, IServiceCreateRequest, IServiceUpdateRequest } from '@/types/IService'

export const ServicesService = {
  async getAll(params?: {
    providerId?: string
    categoryId?: string
    serviceTypeId?: string
  }): Promise<IResultObject<IService[]>> {
    try {
      const { data } = await httpClient.get<IService[]>('services', { params })
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getMine(): Promise<IResultObject<IService[]>> {
    try {
      const { data } = await httpClient.get<IService[]>('services/mine')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getById(id: string): Promise<IResultObject<IService>> {
    try {
      const { data } = await httpClient.get<IService>(`services/${id}`)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: IServiceCreateRequest): Promise<IResultObject<IService>> {
    try {
      const { data } = await httpClient.post<IService>('services', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async update(id: string, body: IServiceUpdateRequest): Promise<IResultObject<IService>> {
    try {
      const { data } = await httpClient.put<IService>(`services/${id}`, body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async remove(id: string): Promise<IResultObject<void>> {
    try {
      await httpClient.delete(`services/${id}`)
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
