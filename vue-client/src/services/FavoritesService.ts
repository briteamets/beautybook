import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IFavorite, IFavoriteCreateRequest } from '@/types/IFavorite'

export const FavoritesService = {
  async getAll(): Promise<IResultObject<IFavorite[]>> {
    try {
      const { data } = await httpClient.get<IFavorite[]>('favorites')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: IFavoriteCreateRequest): Promise<IResultObject<IFavorite>> {
    try {
      const { data } = await httpClient.post<IFavorite>('favorites', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async remove(id: string): Promise<IResultObject<void>> {
    try {
      await httpClient.delete(`favorites/${id}`)
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
