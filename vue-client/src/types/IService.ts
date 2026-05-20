export interface IService {
  id: string
  name: string
  nameEn: string
  nameEt: string
  description?: string
  descriptionEn?: string
  descriptionEt?: string
  price: number
  durationMinutes: number
  categoryId: string
  categoryName: string
  serviceTypeId?: string
  isActive: boolean
}

export interface IServiceCreateRequest {
  beautyProviderId: string
  serviceCategoryId: string
  serviceTypeId?: string
  name: Record<string, string>
  description?: Record<string, string>
  price: number
  durationMinutes: number
  isActive: boolean
}

export interface IServiceUpdateRequest {
  serviceCategoryId: string
  serviceTypeId?: string
  name: Record<string, string>
  description?: Record<string, string>
  price: number
  durationMinutes: number
  isActive: boolean
}
