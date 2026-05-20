export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed'

export interface IBooking {
  id: string
  serviceName: string
  providerName: string
  salonName?: string
  appointmentDateTime: string
  status: BookingStatus
  totalPrice: number
  isPaid: boolean
  hasPayment: boolean
  notes?: string
  hasReview: boolean
  clientName?: string
}

export interface IBookingCreateRequest {
  serviceId: string
  availabilityId: string
  providerServiceId?: string
  date?: string
  startTime?: string
  salonId?: string
  notes?: string
}

export interface IBookingCancelRequest {
  reason?: string
}

export interface IBookingStatusUpdateRequest {
  status: BookingStatus
  reason?: string
}

export interface IBookingUpdateRequest {
  notes?: string
}
