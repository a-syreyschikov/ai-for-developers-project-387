import { addDays, format } from 'date-fns'
import { ru } from 'date-fns/locale/ru'
import { formatInTimeZone } from 'date-fns-tz'
import type { Slot } from '@/api/calendar'

export type DateKey = string

export const toDateKeyInTimeZone = (value: string | Date, timeZone: string): DateKey => {
  return formatInTimeZone(value, timeZone, 'yyyy-MM-dd')
}

export const todayDateKeyInTimeZone = (timeZone: string): DateKey => {
  return toDateKeyInTimeZone(new Date(), timeZone)
}

export const dateKeyToPickerDate = (dateKey: DateKey): Date => {
  const [year = '0', month = '1', day = '1'] = dateKey.split('-')
  return new Date(Number(year), Number(month) - 1, Number(day))
}

export const pickerDateToDateKey = (date: Date): DateKey => {
  return format(date, 'yyyy-MM-dd')
}

export const buildBookingWindowDateKeys = (startDateKey: DateKey, daysCount = 14): DateKey[] => {
  const start = dateKeyToPickerDate(startDateKey)
  return Array.from({ length: daysCount }, (_, index) => format(addDays(start, index), 'yyyy-MM-dd'))
}

export const groupSlotsByDate = (slots: Slot[], timeZone: string): Map<DateKey, Slot[]> => {
  const grouped = new Map<DateKey, Slot[]>()

  for (const slot of slots) {
    const dateKey = toDateKeyInTimeZone(slot.startsAt, timeZone)
    const items = grouped.get(dateKey) ?? []
    items.push(slot)
    grouped.set(dateKey, items)
  }

  for (const items of grouped.values()) {
    items.sort((left, right) => left.startsAt.localeCompare(right.startsAt))
  }

  return grouped
}

export const formatDate = (value: string | Date, timeZone: string): string => {
  return formatInTimeZone(value, timeZone, 'dd.MM.yyyy')
}

export const formatDateLong = (value: string | Date, timeZone: string): string => {
  return formatInTimeZone(value, timeZone, 'dd.MM.yyyy, EEEE', { locale: ru })
}

export const formatTime = (value: string | Date, timeZone: string): string => {
  return formatInTimeZone(value, timeZone, 'HH:mm')
}

export const formatSlotRange = (slot: Pick<Slot, 'startsAt' | 'endsAt'>, timeZone: string): string => {
  return `${formatTime(slot.startsAt, timeZone)}-${formatTime(slot.endsAt, timeZone)}`
}
