import { createI18n } from 'vue-i18n'

export const messages = {
  ru: {
    app: {
      name: 'Calendar',
      nav: {
        book: 'Записаться',
        admin: 'Админка',
      },
    },
    common: {
      actions: {
        back: 'Назад',
        cancel: 'Отмена',
        close: 'Закрыть',
        continue: 'Продолжить',
        create: 'Создать',
        save: 'Сохранить',
      },
      states: {
        loading: 'Загрузка...',
        empty: 'Данных пока нет',
      },
    },
    errors: {
      generic: 'Что-то пошло не так. Попробуйте ещё раз.',
      VALIDATION_FAILED: 'Проверьте заполненные поля.',
      EVENT_TYPE_NOT_FOUND: 'Тип события не найден.',
      BOOKING_NOT_FOUND: 'Бронирование не найдено.',
      DUPLICATE_EVENT_TYPE_TITLE: 'Тип события с таким названием уже существует.',
      INVALID_SCHEDULE: 'Проверьте расписание.',
      SLOT_UNAVAILABLE: 'Это время уже занято. Выберите другое время.',
      SLOT_OUTSIDE_BOOKING_WINDOW: 'Выбранное время находится вне окна записи.',
      BOOKING_NOT_CANCELLABLE: 'Эту встречу уже нельзя отменить.',
      INTERNAL_ERROR: 'На сервере произошла ошибка. Попробуйте позже.',
    },
  },
} as const

export const i18n = createI18n({
  legacy: false,
  locale: 'ru',
  fallbackLocale: 'ru',
  messages,
})

export type MessageSchema = typeof messages.ru
