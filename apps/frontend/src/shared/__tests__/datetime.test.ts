import { describe, expect, it } from 'vitest'
import { formatDateLong, formatSlotRange, groupSlotsByDate, toDateKeyInTimeZone } from '../datetime'
import type { Slot } from '@/api/calendar'

describe('datetime helpers', () => {
  it('converts UTC timestamps to owner timezone date keys', () => {
    expect(toDateKeyInTimeZone('2026-06-15T21:30:00Z', 'Europe/Moscow')).toBe('2026-06-16')
  })

  it('groups slots by local date in owner timezone', () => {
    const slots: Slot[] = [
      {
        eventTypeId: '22222222-2222-2222-2222-222222222222',
        startsAt: '2026-06-15T07:00:00Z',
        endsAt: '2026-06-15T07:30:00Z',
        durationMinutes: 30,
      },
      {
        eventTypeId: '22222222-2222-2222-2222-222222222222',
        startsAt: '2026-06-15T21:30:00Z',
        endsAt: '2026-06-15T22:00:00Z',
        durationMinutes: 30,
      },
    ]

    const grouped = groupSlotsByDate(slots, 'Europe/Moscow')

    expect(grouped.get('2026-06-15')).toHaveLength(1)
    expect(grouped.get('2026-06-16')).toHaveLength(1)
  })

  it('formats slot ranges in owner timezone', () => {
    expect(
      formatSlotRange(
        {
          startsAt: '2026-06-15T07:00:00Z',
          endsAt: '2026-06-15T07:30:00Z',
        },
        'Europe/Moscow',
      ),
    ).toBe('10:00-10:30')
  })

  it('formats long dates with Russian weekday names', () => {
    expect(formatDateLong('2026-06-12T07:00:00Z', 'Europe/Moscow')).toBe('12.06.2026, пятница')
  })
})
