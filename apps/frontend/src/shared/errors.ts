import { isAxiosError } from 'axios'
import type { ErrorCode, ProblemDetails } from '@/api/calendar'

type Translate = (key: string) => string

export const getProblemDetails = (error: unknown): ProblemDetails | null => {
  if (!isAxiosError(error)) {
    return null
  }

  const data = error.response?.data
  if (!data || typeof data !== 'object' || !('code' in data)) {
    return null
  }

  return data as ProblemDetails
}

export const getErrorMessage = (error: unknown, t: Translate): string => {
  const problem = getProblemDetails(error)
  if (!problem) {
    return t('errors.generic')
  }

  const key = `errors.${problem.code as ErrorCode}`
  const translated = t(key)
  return translated === key ? (problem.detail ?? t('errors.generic')) : translated
}
