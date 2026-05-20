export interface IReview {
  id: string
  rating: number
  comment?: string
  userName: string
  createdAt: string
}

export interface IReviewCreateRequest {
  bookingId: string
  rating: number
  comment?: string
}
