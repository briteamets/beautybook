import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type {
  IBooking,
  IBookingCreateRequest,
  IBookingUpdateRequest,
  IBookingCancelRequest,
  IBookingStatusUpdateRequest,
} from '@/types/IBooking'

export const BookingsService = {
  async getAll(): Promise<IResultObject<IBooking[]>> {
    try {
      const { data } = await httpClient.get<IBooking[]>('bookings')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getIncoming(): Promise<IResultObject<IBooking[]>> {
    try {
      const { data } = await httpClient.get<IBooking[]>('bookings/incoming')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getById(id: string): Promise<IResultObject<IBooking>> {
    try {
      const { data } = await httpClient.get<IBooking>(`bookings/${id}`)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async create(body: IBookingCreateRequest): Promise<IResultObject<IBooking>> {
    try {
      const { data } = await httpClient.post<IBooking>('bookings', body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async update(id: string, body: IBookingUpdateRequest): Promise<IResultObject<IBooking>> {
    try {
      const { data } = await httpClient.patch<IBooking>(`bookings/${id}`, body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async cancel(id: string, body: IBookingCancelRequest): Promise<IResultObject<IBooking>> {
    try {
      const { data } = await httpClient.put<IBooking>(`bookings/${id}/cancel`, body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async updateStatus(
    id: string,
    body: IBookingStatusUpdateRequest,
  ): Promise<IResultObject<IBooking>> {
    try {
      const { data } = await httpClient.patch<IBooking>(`bookings/${id}/status`, body)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
