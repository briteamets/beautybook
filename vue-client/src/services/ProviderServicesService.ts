import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IAvailableSlot } from '@/types/IAvailableSlot'

export const ProviderServicesService = {
  async getSlots(providerServiceId: string, date: string): Promise<IResultObject<IAvailableSlot[]>> {
    try {
      const { data } = await httpClient.get<IAvailableSlot[]>(
        `provider-services/${providerServiceId}/slots`,
        { params: { date } },
      )
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
