export interface IPayment {
  id: string
  amount: number
  currency: string
  status: string
  paidAt?: string
}

export interface IPaymentCreateRequest {
  bookingIds: string[]
  paymentMethod: string
}
