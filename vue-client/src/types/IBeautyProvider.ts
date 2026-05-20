import type { IAvailability } from './IAvailability'

export interface IBeautyProvider {
  id: string
  profileName: string
  bio?: string
  address: string
  lat: number
  lng: number
  profilePictureUrl?: string
  averageRating: number
  reviewCount: number
  todayAvailability: IAvailability[]
}

export interface IBeautyProviderCreate {
  profileName: string
  bio?: string
  address: string
  lat: number
  lng: number
  profilePictureUrl?: string
}

export interface IBeautyProviderUpdate {
  profileName: string
  bio?: string
  address: string
  lat: number
  lng: number
  profilePictureUrl?: string
}
