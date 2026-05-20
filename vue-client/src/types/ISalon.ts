export interface ISalonStaffMember {
  providerId: string
  providerName: string
  role: string
  profilePictureUrl?: string
}

export interface ISalon {
  id: string
  name: string
  description?: string
  address: string
  lat: number
  lng: number
  profilePictureUrl?: string
  staffMembers: ISalonStaffMember[]
}
