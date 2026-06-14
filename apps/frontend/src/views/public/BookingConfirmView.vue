<template>
  <section class="page-stack">
    <Message v-if="pageError" severity="error">{{ pageError }}</Message>
    <div v-if="loading" class="confirm-grid">
      <Skeleton height="22rem" />
      <Skeleton height="22rem" />
    </div>

    <div v-else-if="!owner || !eventType || !slot" class="unavailable-state surface-card">
      <Message severity="warn">Выбранное время недоступно.</Message>
      <Button label="Выбрать другое время" as="router-link" :to="`/book/${eventTypeId}`" />
    </div>

    <div v-else class="confirm-grid">
      <aside class="summary-card surface-card">
        <span class="panel-label">Оформление записи</span>
        <h1>{{ eventType.title }}</h1>
        <p>{{ eventType.description || 'Описание не указано.' }}</p>
        <dl>
          <div><dt>Владелец</dt><dd>{{ owner.displayName }}</dd></div>
          <div><dt>Дата</dt><dd>{{ formatDate(slot.startsAt, owner.timeZone) }}</dd></div>
          <div><dt>Время</dt><dd>{{ formatSlotRange(slot, owner.timeZone) }}</dd></div>
          <div><dt>Длительность</dt><dd>{{ slot.durationMinutes }} минут</dd></div>
          <div><dt>Часовой пояс</dt><dd>{{ owner.timeZone }}</dd></div>
        </dl>
      </aside>

      <form class="booking-form surface-card" @submit.prevent="submit">
        <h2>Ваши данные</h2>
        <Message v-if="submitError" severity="error">{{ submitError }}</Message>
        <div class="form-field">
          <label for="guestName">Имя</label>
          <InputText id="guestName" v-model="guestName" v-bind="guestNameAttrs" :invalid="Boolean(errors.guestName)" />
          <span v-if="errors.guestName" class="field-error">{{ errors.guestName }}</span>
        </div>
        <div class="form-field">
          <label for="guestEmail">Email</label>
          <InputText id="guestEmail" v-model="guestEmail" v-bind="guestEmailAttrs" :invalid="Boolean(errors.guestEmail)" />
          <span v-if="errors.guestEmail" class="field-error">{{ errors.guestEmail }}</span>
        </div>
        <div class="form-actions">
          <Button label="Назад" severity="secondary" outlined as="router-link" :to="`/book/${eventTypeId}`" />
          <Button label="Записаться" type="submit" :loading="isSubmitting" />
        </div>
      </form>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { useI18n } from 'vue-i18n'
import { calendarApi, type EventType, type Owner, type Slot } from '@/api/calendar'
import { useBookingStore } from '@/stores/booking'
import { formatDate, formatSlotRange } from '@/shared/datetime'
import { getErrorMessage, getProblemDetails } from '@/shared/errors'

const props = defineProps<{ eventTypeId: string }>()
const route = useRoute()
const router = useRouter()
const bookingStore = useBookingStore()
const { t } = useI18n()

const owner = ref<Owner | null>(null)
const eventType = ref<EventType | null>(null)
const slot = ref<Slot | null>(null)
const loading = ref(true)
const pageError = ref('')
const submitError = ref('')

const eventTypeId = computed(() => props.eventTypeId)
const startsAt = computed(() => (typeof route.query.startsAt === 'string' ? route.query.startsAt : ''))

const bookingSchema = z.object({
  guestName: z.string().trim().min(1, 'Укажите имя').max(120, 'Максимум 120 символов'),
  guestEmail: z.string().trim().min(1, 'Укажите email').max(254, 'Максимум 254 символа').email('Укажите корректный email'),
})

const { defineField, errors, handleSubmit, isSubmitting } = useForm({
  validationSchema: toTypedSchema(bookingSchema),
  initialValues: {
    guestName: '',
    guestEmail: '',
  },
})

const [guestName, guestNameAttrs] = defineField('guestName')
const [guestEmail, guestEmailAttrs] = defineField('guestEmail')

const submit = handleSubmit(async (values) => {
  if (!slot.value) {
    return
  }

  submitError.value = ''
  try {
    const booking = await calendarApi.createPublicBooking({
      eventTypeId: props.eventTypeId,
      startsAt: slot.value.startsAt,
      guestName: values.guestName.trim(),
      guestEmail: values.guestEmail.trim().toLowerCase(),
    })

    if (owner.value && eventType.value) {
      bookingStore.setSelection(owner.value, eventType.value, slot.value)
    }
    bookingStore.setCreatedBooking(booking)
    router.push({ name: 'booking-success' })
  } catch (caught) {
    const problem = getProblemDetails(caught)
    submitError.value = getErrorMessage(caught, t)
    if (problem?.code === 'SLOT_UNAVAILABLE' || problem?.code === 'SLOT_OUTSIDE_BOOKING_WINDOW') {
      slot.value = null
    }
  }
})

onMounted(async () => {
  if (!startsAt.value) {
    loading.value = false
    return
  }

  try {
    const [ownerResult, eventTypeResult, slotsResult] = await Promise.all([
      calendarApi.getPublicOwner(),
      calendarApi.getPublicEventType(props.eventTypeId),
      calendarApi.listPublicEventTypeSlots(props.eventTypeId),
    ])
    owner.value = ownerResult
    eventType.value = eventTypeResult
    slot.value = slotsResult.items.find((item) => item.startsAt === startsAt.value) ?? null
  } catch (caught) {
    pageError.value = getErrorMessage(caught, t)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.confirm-grid {
  display: grid;
  grid-template-columns: 0.9fr 1fr;
  gap: 16px;
  align-items: stretch;
}

.summary-card,
.booking-form {
  display: grid;
  align-content: start;
  gap: 16px;
  padding: 24px;
}

.panel-label {
  color: var(--text-muted);
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
}

h1,
h2 {
  color: var(--text-strong);
  font-weight: 650;
  letter-spacing: -0.03em;
}

h1 {
  font-size: clamp(2rem, 4vw, 2.8rem);
  line-height: 1;
}

p {
  color: var(--text-muted);
}

dl {
  display: grid;
  gap: 10px;
}

dl div {
  display: grid;
  grid-template-columns: 140px 1fr;
  gap: 16px;
  padding-top: 10px;
  border-top: 1px solid var(--surface-border);
}

dt {
  color: var(--text-muted);
}

dd {
  color: var(--text-strong);
  font-weight: 600;
}

.form-actions {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding-top: 8px;
}

.unavailable-state {
  display: grid;
  justify-items: start;
  gap: 16px;
  padding: 24px;
}

@media (max-width: 820px) {
  .confirm-grid {
    grid-template-columns: 1fr;
  }

  dl div {
    grid-template-columns: 1fr;
    gap: 4px;
  }
}
</style>
