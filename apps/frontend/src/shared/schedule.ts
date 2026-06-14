import type { Schedule, ScheduleDay, Weekday } from '@/api/calendar'

export const weekdays = [
  'monday',
  'tuesday',
  'wednesday',
  'thursday',
  'friday',
  'saturday',
  'sunday',
] as const satisfies readonly Weekday[]

export const weekdayLabels: Record<Weekday, string> = {
  monday: 'Понедельник',
  tuesday: 'Вторник',
  wednesday: 'Среда',
  thursday: 'Четверг',
  friday: 'Пятница',
  saturday: 'Суббота',
  sunday: 'Воскресенье',
}

export const timeOptions = Array.from({ length: 96 }, (_, index) => {
  const totalMinutes = index * 15
  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
})

export const durationOptions = [15, 30, 45, 60, 90, 120, 180, 240] as const

export const createDefaultSchedule = (): Schedule => ({
  days: weekdays.map((weekday): ScheduleDay => {
    const enabled = !['saturday', 'sunday'].includes(weekday)
    return {
      weekday,
      enabled,
      startsAtLocalTime: enabled ? '09:00' : null,
      endsAtLocalTime: enabled ? '18:00' : null,
    }
  }),
})

export const enableScheduleDay = (day: ScheduleDay): ScheduleDay => ({
  ...day,
  enabled: true,
  startsAtLocalTime: day.startsAtLocalTime ?? '09:00',
  endsAtLocalTime: day.endsAtLocalTime ?? '18:00',
})

export const disableScheduleDay = (day: ScheduleDay): ScheduleDay => ({
  ...day,
  enabled: false,
  startsAtLocalTime: null,
  endsAtLocalTime: null,
})
