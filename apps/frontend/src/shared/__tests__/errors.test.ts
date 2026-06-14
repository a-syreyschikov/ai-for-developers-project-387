import { describe, expect, it } from 'vitest'
import { getErrorMessage, getProblemDetails } from '../errors'

const t = (key: string) =>
  (
    {
      'errors.generic': 'Общая ошибка',
      'errors.SLOT_UNAVAILABLE': 'Слот недоступен',
    } as Record<string, string>
  )[key] ?? key

describe('error helpers', () => {
  it('extracts RFC 7807 problem details from axios errors', () => {
    const problem = getProblemDetails({
      isAxiosError: true,
      response: {
        data: {
          type: 'https://calendar.local/problems/slot-unavailable',
          title: 'Slot is unavailable',
          status: 409,
          code: 'SLOT_UNAVAILABLE',
        },
      },
    })

    expect(problem?.code).toBe('SLOT_UNAVAILABLE')
  })

  it('maps problem code to localized message', () => {
    expect(
      getErrorMessage(
        {
          isAxiosError: true,
          response: {
            data: {
              type: 'https://calendar.local/problems/slot-unavailable',
              title: 'Slot is unavailable',
              status: 409,
              code: 'SLOT_UNAVAILABLE',
            },
          },
        },
        t,
      ),
    ).toBe('Слот недоступен')
  })

  it('falls back to generic message for non-API errors', () => {
    expect(getErrorMessage(new Error('network'), t)).toBe('Общая ошибка')
  })
})
