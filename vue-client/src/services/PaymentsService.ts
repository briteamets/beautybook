import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type { IPayment, IPaymentCreateRequest } from '@/types/IPayment'

export const PaymentsService = {
  async getAll(): Promise<IResultObject<IPayment[]>> {
    try {
      const { data } = await httpClient.get<IPayment[]>('payments')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: IPaymentCreateRequest): Promise<IResultObject<IPayment>> {
    try {
      const { data } = await httpClient.post<IPayment>('payments', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
