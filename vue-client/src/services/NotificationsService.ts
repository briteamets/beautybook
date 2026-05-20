import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { INotification } from '@/types/INotification'

export const NotificationsService = {
  async getAll(): Promise<IResultObject<INotification[]>> {
    try {
      const { data } = await httpClient.get<INotification[]>('notifications')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async markRead(id: string): Promise<IResultObject<INotification>> {
    try {
      const { data } = await httpClient.put<INotification>(`notifications/${id}/read`)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
