import type { AxiosError } from 'axios'

export function extractErrors(e: unknown): string[] {
  const err = e as AxiosError<{ error?: string; errors?: string[] } | string>
  if (err.response?.data) {
    const d = err.response.data
    if (typeof d === 'string') return [d]
    if (Array.isArray((d as { errors?: string[] }).errors)) return (d as { errors: string[] }).errors
    if ((d as { error?: string }).error) return [(d as { error: string }).error]
  }
  return [(e as Error).message ?? 'Unknown error']
}
