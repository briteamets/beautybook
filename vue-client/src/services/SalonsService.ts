import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { ISalon } from '@/types/ISalon'

export const SalonsService = {
  async getAll(): Promise<IResultObject<ISalon[]>> {
    try {
      const { data } = await httpClient.get<ISalon[]>('salons')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getById(id: string): Promise<IResultObject<ISalon>> {
    try {
      const { data } = await httpClient.get<ISalon>(`salons/${id}`)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
