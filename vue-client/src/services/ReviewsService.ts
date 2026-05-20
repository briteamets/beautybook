import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IReview, IReviewCreateRequest } from '@/types/IReview'

export const ReviewsService = {
  async getByProvider(providerId: string): Promise<IResultObject<IReview[]>> {
    try {
      const { data } = await httpClient.get<IReview[]>('reviews', { params: { providerId } })
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: IReviewCreateRequest): Promise<IResultObject<IReview>> {
    try {
      const { data } = await httpClient.post<IReview>('reviews', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async deleteComment(id: string): Promise<IResultObject<void>> {
    try {
      await httpClient.delete(`reviews/${id}/comment`)
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
