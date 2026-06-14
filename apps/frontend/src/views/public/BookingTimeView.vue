<template>
  <section class="page-stack">
    <Message v-if="error" severity="error">{{ error }}</Message>

    <div v-else-if="loading" class="booking-grid">
      <Skeleton v-for="item in 3" :key="item" height="22rem" />
    </div>

    <Message v-else-if="!owner || !eventType" severity="warn">Тип события недоступен.</Message>

    <div v-else class="booking-grid">
      <aside class="booking-panel surface-card">
        <div>
          <span class="panel-label">Владелец</span>
          <h2>{{ owner.displayName }}</h2>
          <p class="muted">Время указано в часовом поясе {{ owner.timeZone }}</p>
        </div>
        <Divider />
        <div>
          <span class="panel-label">Тип встречи</span>
          <h3>{{ eventType.title }}</h3>
          <p class="muted">{{ eventType.description || 'Описание не указано.' }}</p>
          <Tag :value="`${eventType.durationMinutes} минут`" severity="secondary" />
        </div>
        <Divider />
        <div class="selection-card">
          <span class="panel-label">Выбранный слот</span>
          <strong class="selection-time">{{ selectedSlot ? formatSlotRange(selectedSlot, owner.timeZone) : 'Время не выбрано' }}</strong>
          <span class="selection-date">{{ selectedDateKey ? formatDateLong(dateKeyToPickerDate(selectedDateKey), owner.timeZone) : 'Дата не выбрана' }}</span>
        </div>
      </aside>

      <section class="calendar-panel surface-card">
        <h2>Календарь</h2>
        <DatePicker
          v-model="dateModel"
          inline
          :manual-input="false"
          :min-date="minDate"
          :max-date="maxDate"
          :disabled-dates="disabledDates"
          :pt="datePickerPt"
        >
          <template #date="slotProps">
            <div class="date-cell">
              <span>{{ slotProps.date.day }}</span>
              <small v-if="availableSlotCount(slotProps.date) > 0">{{ availableSlotCount(slotProps.date) }}</small>
            </div>
          </template>
        </DatePicker>
      </section>

      <section class="slots-panel surface-card">
        <div class="slots-heading">
          <h2>Свободное время</h2>
          <span class="muted">{{ selectedDateSlots.length }} доступно</span>
        </div>

        <div v-if="slots.length === 0" class="empty-slots">
          <i class="pi pi-calendar-times" />
          <strong>Нет доступных слотов для записи</strong>
          <span>Попробуйте позже или выберите другой тип события.</span>
        </div>

        <div v-else class="slots-list">
          <button
            v-for="slot in selectedDateSlots"
            :key="slot.startsAt"
            class="slot-button"
            :class="{ selected: selectedSlot?.startsAt === slot.startsAt }"
            type="button"
            @click="selectedSlot = slot"
          >
            <span>{{ formatSlotRange(slot, owner.timeZone) }}</span>
            <Tag :value="selectedSlot?.startsAt === slot.startsAt ? 'Выбрано' : 'Свободно'" :severity="selectedSlot?.startsAt === slot.startsAt ? undefined : 'secondary'" />
          </button>
        </div>

        <div class="slot-actions">
          <Button label="Назад" severity="secondary" outlined as="router-link" to="/book" />
          <Button label="Продолжить" icon="pi pi-arrow-right" icon-pos="right" :disabled="!selectedSlot" @click="goToConfirm" />
        </div>
      </section>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import DatePicker from 'primevue/datepicker'
import Divider from 'primevue/divider'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { useI18n } from 'vue-i18n'
import { calendarApi, type EventType, type Owner, type Slot } from '@/api/calendar'
import { useBookingStore } from '@/stores/booking'
import {
  buildBookingWindowDateKeys,
  dateKeyToPickerDate,
  formatDateLong,
  formatSlotRange,
  groupSlotsByDate,
  pickerDateToDateKey,
  todayDateKeyInTimeZone,
  type DateKey,
} from '@/shared/datetime'
import { getErrorMessage } from '@/shared/errors'

const props = defineProps<{ eventTypeId: string }>()
const router = useRouter()
const bookingStore = useBookingStore()
const { t } = useI18n()

const owner = ref<Owner | null>(null)
const eventType = ref<EventType | null>(null)
const slots = ref<Slot[]>([])
const selectedDateKey = ref<DateKey>('')
const dateModel = ref<Date | null>(null)
const selectedSlot = ref<Slot | null>(null)
const loading = ref(true)
const error = ref('')

const slotsByDate = computed(() => (owner.value ? groupSlotsByDate(slots.value, owner.value.timeZone) : new Map<DateKey, Slot[]>()))
const bookingWindowKeys = computed(() => (owner.value ? buildBookingWindowDateKeys(todayDateKeyInTimeZone(owner.value.timeZone)) : []))
const calendarWindowKeys = computed(() => {
  const availableKeys = Array.from(slotsByDate.value.keys()).sort()
  const firstAvailable = availableKeys[0]
  const lastAvailable = availableKeys[availableKeys.length - 1]
  if (!firstAvailable || !lastAvailable) {
    return bookingWindowKeys.value
  }

  const daysCount = Math.floor((dateKeyToPickerDate(lastAvailable).getTime() - dateKeyToPickerDate(firstAvailable).getTime()) / 86_400_000) + 1
  return buildBookingWindowDateKeys(firstAvailable, daysCount)
})
const minDate = computed(() => (calendarWindowKeys.value[0] ? dateKeyToPickerDate(calendarWindowKeys.value[0]) : undefined))
const maxDate = computed(() => {
  const lastKey = calendarWindowKeys.value[calendarWindowKeys.value.length - 1]
  return lastKey ? dateKeyToPickerDate(lastKey) : undefined
})
const disabledDates = computed(() => calendarWindowKeys.value.filter((dateKey) => !slotsByDate.value.has(dateKey)).map(dateKeyToPickerDate))
const selectedDateSlots = computed(() => (selectedDateKey.value ? (slotsByDate.value.get(selectedDateKey.value) ?? []) : []))

const datePickerPt = {
  panel: { class: 'booking-datepicker-panel' },
  day: { class: 'booking-datepicker-day' },
}

const slotDateKey = (date: { year: number; month: number; day: number }): DateKey => {
  return pickerDateToDateKey(new Date(date.year, date.month, date.day))
}

const availableSlotCount = (date: { year: number; month: number; day: number }): number => {
  const dateKey = slotDateKey(date)
  return slotsByDate.value.get(dateKey)?.length ?? 0
}

const selectFirstSlotForDate = (dateKey: DateKey) => {
  selectedSlot.value = slotsByDate.value.get(dateKey)?.[0] ?? null
}

const chooseInitialDate = () => {
  const firstAvailable = Array.from(slotsByDate.value.keys()).sort()[0]
  selectedDateKey.value = firstAvailable ?? ''
  dateModel.value = firstAvailable ? dateKeyToPickerDate(firstAvailable) : null
  selectFirstSlotForDate(selectedDateKey.value)
}

watch(dateModel, (date) => {
  selectedDateKey.value = date ? pickerDateToDateKey(date) : ''
  selectFirstSlotForDate(selectedDateKey.value)
})

const goToConfirm = () => {
  if (!owner.value || !eventType.value || !selectedSlot.value) {
    return
  }

  bookingStore.setSelection(owner.value, eventType.value, selectedSlot.value)
  router.push({ name: 'book-confirm', params: { eventTypeId: props.eventTypeId }, query: { startsAt: selectedSlot.value.startsAt } })
}

onMounted(async () => {
  try {
    const [ownerResult, eventTypeResult, slotsResult] = await Promise.all([
      calendarApi.getPublicOwner(),
      calendarApi.getPublicEventType(props.eventTypeId),
      calendarApi.listPublicEventTypeSlots(props.eventTypeId),
    ])
    owner.value = ownerResult
    eventType.value = eventTypeResult
    slots.value = slotsResult.items
    chooseInitialDate()
  } catch (caught) {
    error.value = getErrorMessage(caught, t)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.booking-grid {
  display: grid;
  grid-template-columns: 0.9fr 1fr 1fr;
  gap: 16px;
  align-items: stretch;
}

.booking-panel,
.calendar-panel,
.slots-panel {
  display: flex;
  flex-direction: column;
  min-height: 470px;
  padding: 20px;
}

.panel-label {
  display: block;
  color: var(--text-muted);
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
}

h2,
h3,
strong {
  color: var(--text-strong);
  font-weight: 650;
}

.selection-card {
  display: grid;
  gap: 6px;
  margin-top: auto;
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-panel);
  padding: 16px;
  background: var(--brand-soft);
}

.selection-time {
  color: var(--brand);
  font-size: clamp(1.35rem, 3vw, 1.75rem);
  line-height: 1.1;
}

.selection-date {
  color: var(--text-body);
  font-weight: 600;
}

.date-cell {
  display: grid;
  min-width: 2.2rem;
  min-height: 2.25rem;
  align-content: center;
  gap: 3px;
  justify-items: center;
  line-height: 1;
}

.date-cell span {
  font-weight: 600;
}

.date-cell small {
  color: var(--brand);
  font-size: 0.68rem;
  font-weight: 600;
  line-height: 1;
}

:deep(.p-datepicker-day-selected .date-cell small) {
  color: var(--p-datepicker-date-selected-color, #ffffff);
}

:deep(.booking-datepicker-panel) {
  width: 100%;
  border: 0;
  box-shadow: none;
}

:deep(.booking-datepicker-day) {
  border-radius: var(--radius-control);
}

.slots-heading {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.slots-list {
  display: grid;
  gap: 8px;
  max-height: 320px;
  overflow: auto;
  padding: 4px;
}

.slot-button {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-control);
  padding: 12px 14px;
  color: var(--text-strong);
  background: var(--surface-panel);
  cursor: pointer;
  transition:
    border-color 160ms ease,
    background-color 160ms ease;
}

.slot-button span {
  font-weight: 600;
}

.slot-button.selected {
  border-color: var(--brand);
  background: var(--brand-soft);
}

.empty-slots {
  display: grid;
  place-items: center;
  gap: 8px;
  flex: 1;
  text-align: center;
}

.empty-slots i {
  color: var(--brand);
  font-size: 2rem;
}

.empty-slots strong {
  font-size: 1.1rem;
}

.slot-actions {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-top: auto;
  padding-top: 16px;
}

@media (max-width: 980px) {
  .booking-grid {
    grid-template-columns: 1fr;
  }

  .booking-panel,
  .calendar-panel,
  .slots-panel {
    min-height: auto;
  }
}
</style>
