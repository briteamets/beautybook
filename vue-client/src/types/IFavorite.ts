export interface IFavorite {
  id: string
  providerId: string
  profileName: string
  address: string
  averageRating: number
}

export interface IFavoriteCreateRequest {
  beautyProviderId: string
}
