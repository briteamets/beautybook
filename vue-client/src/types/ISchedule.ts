export interface IScheduleDay {
  dayOfWeek: number
  isWorkingDay: boolean
  startTime: string
  endTime: string
}

export interface IBreak {
  id?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  label?: string
}

export interface IBreakUpsertRequest {
  dayOfWeek: number
  startTime: string
  endTime: string
  label?: string
}

export interface IGenerateSlotsRequest {
  fromDate: string
  toDate: string
}

export interface IGenerateSlotsResponse {
  slotsGenerated: number
}

export interface IWeekBookingItem {
  id: string
  clientName: string
  serviceName: string
  startTime: string
  endTime: string
  status: string
}

export interface IOotelBookingItem {
  id: string
  label: string
  startTime: string
  endTime: string
}

export interface IWeekDayBundle {
  date: string
  slots: { startTime: string; endTime: string; isBooked: boolean; bookingId?: string }[]
  bookings: IWeekBookingItem[]
  ootel: IOotelBookingItem[]
}

export interface IWeekBundle {
  weekStart: string
  days: IWeekDayBundle[]
}

export interface IPatchDayRequest {
  date: string
  isWorkingDay?: boolean
  startTime?: string
  endTime?: string
}
