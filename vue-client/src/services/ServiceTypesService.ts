import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IServiceType } from '@/types/IServiceType'

export const ServiceTypesService = {
  async getAll(): Promise<IResultObject<IServiceType[]>> {
    try {
      const { data } = await httpClient.get<IServiceType[]>('service-types')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
