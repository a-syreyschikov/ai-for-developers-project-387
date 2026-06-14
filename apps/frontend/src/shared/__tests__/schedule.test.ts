import { describe, expect, it } from 'vitest'
import { createDefaultSchedule, disableScheduleDay, enableScheduleDay, timeOptions, weekdays } from '../schedule'

describe('schedule helpers', () => {
  it('creates default weekday schedule', () => {
    const schedule = createDefaultSchedule()

    expect(schedule.days.map((day) => day.weekday)).toEqual([...weekdays])
    expect(schedule.days.find((day) => day.weekday === 'monday')).toMatchObject({
      enabled: true,
      startsAtLocalTime: '09:00',
      endsAtLocalTime: '18:00',
    })
    expect(schedule.days.find((day) => day.weekday === 'sunday')).toMatchObject({
      enabled: false,
      startsAtLocalTime: null,
      endsAtLocalTime: null,
    })
  })

  it('uses 15 minute time options', () => {
    expect(timeOptions[0]).toBe('00:00')
    expect(timeOptions[1]).toBe('00:15')
    expect(timeOptions[timeOptions.length - 1]).toBe('23:45')
    expect(timeOptions).toHaveLength(96)
  })

  it('sets defaults when enabling and clears time when disabling', () => {
    const disabled = disableScheduleDay({
      weekday: 'monday',
      enabled: true,
      startsAtLocalTime: '10:00',
      endsAtLocalTime: '17:00',
    })
    const enabled = enableScheduleDay(disabled)

    expect(disabled).toMatchObject({ enabled: false, startsAtLocalTime: null, endsAtLocalTime: null })
    expect(enabled).toMatchObject({ enabled: true, startsAtLocalTime: '09:00', endsAtLocalTime: '18:00' })
  })
})
