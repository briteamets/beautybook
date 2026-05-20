import httpClient from './httpClient'
import { extractErrors } from './serviceHelpers'
import type { IResultObject } from '@/types/IResultObject'
import type {
  IScheduleDay,
  IBreak,
  IBreakUpsertRequest,
  IGenerateSlotsRequest,
  IGenerateSlotsResponse,
  IWeekBundle,
  IPatchDayRequest,
} from '@/types/ISchedule'

export const ScheduleService = {
  async getSchedule(): Promise<IResultObject<IScheduleDay[]>> {
    try {
      const { data } = await httpClient.get<IScheduleDay[]>('schedule')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async saveSchedule(
    days: IScheduleDay[],
  ): Promise<IResultObject<{ days: IScheduleDay[]; warnings: string[] }>> {
    try {
      const { data } = await httpClient.put<{ days: IScheduleDay[]; warnings: string[] }>(
        'schedule',
        days,
      )
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getBreaks(): Promise<IResultObject<IBreak[]>> {
    try {
      const { data } = await httpClient.get<IBreak[]>('schedule/breaks')
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async saveBreaks(breaks: IBreakUpsertRequest[]): Promise<IResultObject<IBreak[]>> {
    try {
      const { data } = await httpClient.put<IBreak[]>('schedule/breaks', breaks)
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async generateSlots(body: IGenerateSlotsRequest): Promise<IResultObject<IGenerateSlotsResponse>> {
    try {
      const { data } = await httpClient.post<IGenerateSlotsResponse>(
        'schedule/generate-slots',
        body,
      )
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async getWeek(weekStart: string): Promise<IResultObject<IWeekBundle>> {
    try {
      const { data } = await httpClient.get<IWeekBundle>('schedule/week', {
        params: { weekStart },
      })
      return { data }
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },

  async patchDay(body: IPatchDayRequest): Promise<IResultObject<void>> {
    try {
      await httpClient.patch('schedule/day', body)
      return {}
    } catch (e) {
      return { errors: extractErrors(e) }
    }
  },
}
