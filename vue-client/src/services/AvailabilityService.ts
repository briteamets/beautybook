import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IAvailability } from '@/types/IAvailability'

export const AvailabilityService = {
  async getAll(params: {
    providerId: string
    serviceId?: string
    date?: string
  }): Promise<IResultObject<IAvailability[]>> {
    try {
      const { data } = await httpClient.get<IAvailability[]>('availability', { params })
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getMine(params?: { date?: string }): Promise<IResultObject<IAvailability[]>> {
    try {
      const { data } = await httpClient.get<IAvailability[]>('availability/mine', { params })
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: {
    providerId: string
    date: string
    startTime: string
    endTime: string
  }): Promise<IResultObject<IAvailability>> {
    try {
      const { data } = await httpClient.post<IAvailability>('availability', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async remove(id: string): Promise<IResultObject<void>> {
    try {
      await httpClient.delete(`availability/${id}`)
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
