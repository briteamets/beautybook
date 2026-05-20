import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IServiceCategory } from '@/types/IServiceCategory'

export const CategoriesService = {
  async getAll(): Promise<IResultObject<IServiceCategory[]>> {
    try {
      const { data } = await httpClient.get<IServiceCategory[]>('categories')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
