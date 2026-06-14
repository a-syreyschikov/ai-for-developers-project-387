<template>
  <section class="page-stack">
    <div class="page-heading">
      <h1>Расписание</h1>
      <p v-if="owner">Расписание указано в часовом поясе владельца: {{ owner.timeZone }}.</p>
      <p v-else>Недельный шаблон доступности владельца.</p>
    </div>

    <Message v-if="error" severity="error">{{ error }}</Message>
    <Message v-if="formError" severity="error">{{ formError }}</Message>

    <div v-if="loading" class="schedule-card surface-card">
      <Skeleton v-for="item in 7" :key="item" height="4rem" />
    </div>

    <form v-else class="schedule-card surface-card" @submit.prevent="saveSchedule">
      <div v-for="(day, index) in days" :key="day.weekday" class="schedule-row" :class="{ disabled: !day.enabled }">
        <div class="day-toggle">
          <ToggleSwitch :model-value="day.enabled" @update:model-value="(enabled) => setDayEnabled(index, enabled)" />
          <strong>{{ weekdayLabels[day.weekday] }}</strong>
        </div>
        <Select v-model="day.startsAtLocalTime" :options="timeOptions" :disabled="!day.enabled" placeholder="Начало" />
        <Select v-model="day.endsAtLocalTime" :options="timeOptions" :disabled="!day.enabled" placeholder="Конец" />
      </div>

      <small v-if="errors.days" class="field-error">{{ errors.days }}</small>

      <div class="schedule-actions">
        <Button label="Отмена" severity="secondary" outlined type="button" @click="resetSchedule" />
        <Button label="Сохранить" type="submit" :loading="saving" />
      </div>
    </form>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Skeleton from 'primevue/skeleton'
import ToggleSwitch from 'primevue/toggleswitch'
import { useToast } from 'primevue/usetoast'
import { useI18n } from 'vue-i18n'
import { calendarApi, type Owner, type Schedule, type ScheduleDay, type UpdateScheduleRequest } from '@/api/calendar'
import { getErrorMessage } from '@/shared/errors'
import { createDefaultSchedule, disableScheduleDay, enableScheduleDay, timeOptions, weekdayLabels, weekdays } from '@/shared/schedule'

const { t } = useI18n()
const toast = useToast()
const owner = ref<Owner | null>(null)
const days = ref<ScheduleDay[]>(createDefaultSchedule().days)
const savedSchedule = ref<Schedule>(createDefaultSchedule())
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')

const localTimeSchema = z.string().regex(/^([01][0-9]|2[0-3]):(00|15|30|45)$/)
const scheduleDaySchema = z.object({
  weekday: z.enum(weekdays),
  enabled: z.boolean(),
  startsAtLocalTime: localTimeSchema.nullable(),
  endsAtLocalTime: localTimeSchema.nullable(),
})

const scheduleSchema = z.object({
  days: z
    .array(scheduleDaySchema)
    .length(7, 'Расписание должно содержать все 7 дней')
    .superRefine((items, context) => {
      const orderMatches = items.every((day, index) => day.weekday === weekdays[index])
      if (!orderMatches) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Дни должны идти с понедельника по воскресенье' })
      }

      for (const [index, day] of items.entries()) {
        if (!day.enabled) {
          if (day.startsAtLocalTime !== null || day.endsAtLocalTime !== null) {
            context.addIssue({ code: z.ZodIssueCode.custom, path: [index], message: 'У выключенного дня время должно быть пустым' })
          }
          continue
        }

        if (!day.startsAtLocalTime || !day.endsAtLocalTime) {
          context.addIssue({ code: z.ZodIssueCode.custom, path: [index], message: 'Для включенного дня укажите начало и конец' })
          continue
        }

        if (day.endsAtLocalTime <= day.startsAtLocalTime) {
          context.addIssue({ code: z.ZodIssueCode.custom, path: [index], message: 'Время окончания должно быть позже начала' })
        }
      }
    }),
})

const { errors, setValues, validate } = useForm<UpdateScheduleRequest>({
  validationSchema: toTypedSchema(scheduleSchema),
  initialValues: createDefaultSchedule(),
})

const cloneSchedule = (schedule: Schedule): Schedule => ({
  days: schedule.days.map((day) => ({ ...day })),
})

const setDayEnabled = (index: number, enabled: boolean) => {
  const day = days.value[index]
  if (!day) {
    return
  }

  days.value[index] = enabled ? enableScheduleDay(day) : disableScheduleDay(day)
}

const resetSchedule = () => {
  const copy = cloneSchedule(savedSchedule.value)
  days.value = copy.days
  formError.value = ''
}

const saveSchedule = async () => {
  formError.value = ''
  setValues({ days: days.value })
  const validation = await validate()
  if (!validation.valid) {
    formError.value = 'Проверьте расписание.'
    return
  }

  saving.value = true
  try {
    const updated = await calendarApi.updateOwnerSchedule({ days: days.value })
    savedSchedule.value = cloneSchedule(updated)
    days.value = cloneSchedule(updated).days
    toast.add({ severity: 'success', summary: 'Расписание сохранено', life: 3000 })
  } catch (caught) {
    formError.value = getErrorMessage(caught, t)
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  try {
    const [ownerResult, scheduleResult] = await Promise.all([
      calendarApi.getOwner(),
      calendarApi.getOwnerSchedule(),
    ])
    owner.value = ownerResult
    savedSchedule.value = cloneSchedule(scheduleResult)
    days.value = cloneSchedule(scheduleResult).days
  } catch (caught) {
    error.value = getErrorMessage(caught, t)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.schedule-card {
  display: grid;
  gap: 10px;
  padding: 18px;
}

.schedule-row {
  display: grid;
  grid-template-columns: minmax(180px, 1fr) 160px 160px;
  gap: 10px;
  align-items: center;
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-control);
  padding: 12px 14px;
  background: var(--surface-panel);
}

.schedule-row.disabled {
  opacity: 0.62;
}

.day-toggle {
  display: flex;
  align-items: center;
  gap: 12px;
}

.day-toggle strong {
  color: var(--text-strong);
  font-weight: 600;
}

.schedule-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 8px;
}

@media (max-width: 720px) {
  .schedule-row {
    grid-template-columns: 1fr;
  }
}
</style>
