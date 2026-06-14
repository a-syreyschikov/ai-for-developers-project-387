<template>
  <section class="page-stack">
    <div class="admin-heading">
      <div class="page-heading">
        <h1>События</h1>
        <p>Типы встреч, которые гость может выбрать для записи.</p>
      </div>
      <Button label="Создать событие" icon="pi pi-plus" @click="openDialog" />
    </div>

    <Message v-if="error" severity="error">{{ error }}</Message>

    <div v-if="loading" class="event-grid">
      <Skeleton v-for="item in 3" :key="item" height="10rem" />
    </div>

    <div v-else-if="eventTypes.length === 0" class="empty-state surface-card">
      <i class="pi pi-list" />
      <strong>Событий пока нет</strong>
      <span>Создайте первый тип события для публичной записи.</span>
    </div>

    <div v-else class="event-grid">
      <Card v-for="eventType in eventTypes" :key="eventType.id" class="event-card">
        <template #title>{{ eventType.title }}</template>
        <template #subtitle>{{ eventType.durationMinutes }} минут</template>
        <template #content>
          <p>{{ eventType.description || 'Описание не указано.' }}</p>
        </template>
      </Card>
    </div>

    <Dialog v-model:visible="dialogVisible" header="Создать событие" modal :style="{ width: 'min(560px, calc(100vw - 32px))' }">
      <form class="event-form" @submit.prevent="submit">
        <Message v-if="submitError" severity="error">{{ submitError }}</Message>
        <div class="form-field">
          <label for="title">Название</label>
          <InputText id="title" v-model="title" v-bind="titleAttrs" :invalid="Boolean(errors.title)" />
          <span v-if="errors.title" class="field-error">{{ errors.title }}</span>
        </div>
        <div class="form-field">
          <label for="description">Описание</label>
          <Textarea id="description" v-model="description" v-bind="descriptionAttrs" rows="4" :invalid="Boolean(errors.description)" />
          <span v-if="errors.description" class="field-error">{{ errors.description }}</span>
        </div>
        <div class="form-field">
          <label for="duration">Длительность</label>
          <Select id="duration" v-model="durationMinutes" v-bind="durationAttrs" :options="durationSelectOptions" option-label="label" option-value="value" :invalid="Boolean(errors.durationMinutes)" />
          <span v-if="errors.durationMinutes" class="field-error">{{ errors.durationMinutes }}</span>
        </div>
        <div class="dialog-actions">
          <Button label="Отмена" severity="secondary" outlined type="button" @click="dialogVisible = false" />
          <Button label="Создать" type="submit" :loading="isSubmitting" />
        </div>
      </form>
    </Dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import Button from 'primevue/button'
import Card from 'primevue/card'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import Select from 'primevue/select'
import Skeleton from 'primevue/skeleton'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'
import { useI18n } from 'vue-i18n'
import { calendarApi, type EventType } from '@/api/calendar'
import { getErrorMessage } from '@/shared/errors'
import { durationOptions } from '@/shared/schedule'

const { t } = useI18n()
const toast = useToast()
const eventTypes = ref<EventType[]>([])
const loading = ref(true)
const error = ref('')
const submitError = ref('')
const dialogVisible = ref(false)

const durationSelectOptions = computed(() => durationOptions.map((value) => ({ label: `${value} минут`, value })))

const eventTypeSchema = z.object({
  title: z.string().trim().min(1, 'Укажите название').max(120, 'Максимум 120 символов'),
  description: z.string().max(1000, 'Максимум 1000 символов'),
  durationMinutes: z.number({ required_error: 'Выберите длительность' }).refine((value) => durationOptions.includes(value as (typeof durationOptions)[number]), 'Выберите длительность'),
})

const { defineField, errors, handleSubmit, isSubmitting, resetForm } = useForm({
  validationSchema: toTypedSchema(eventTypeSchema),
  initialValues: {
    title: '',
    description: '',
    durationMinutes: 30,
  },
})

const [title, titleAttrs] = defineField('title')
const [description, descriptionAttrs] = defineField('description')
const [durationMinutes, durationAttrs] = defineField('durationMinutes')

const loadEventTypes = async () => {
  const result = await calendarApi.listOwnerEventTypes()
  eventTypes.value = result.items
}

const openDialog = () => {
  submitError.value = ''
  resetForm()
  dialogVisible.value = true
}

const submit = handleSubmit(async (values) => {
  submitError.value = ''
  try {
    const created = await calendarApi.createOwnerEventType({
      title: values.title.trim(),
      description: values.description.trim(),
      durationMinutes: values.durationMinutes,
    })
    eventTypes.value = [...eventTypes.value, created].sort((left, right) => left.title.localeCompare(right.title, 'ru'))
    dialogVisible.value = false
    toast.add({ severity: 'success', summary: 'Событие создано', life: 3000 })
  } catch (caught) {
    submitError.value = getErrorMessage(caught, t)
  }
})

onMounted(async () => {
  try {
    await loadEventTypes()
  } catch (caught) {
    error.value = getErrorMessage(caught, t)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.admin-heading {
  display: flex;
  justify-content: space-between;
  align-items: start;
  gap: 16px;
}

.event-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 260px), 340px));
  gap: 16px;
  justify-content: start;
}

.event-card {
  height: 100%;
}

.event-card p {
  color: var(--text-muted);
}

.event-form {
  display: grid;
  gap: 16px;
}

.dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.empty-state {
  display: grid;
  place-items: center;
  gap: 8px;
  padding: 36px 18px;
  text-align: center;
}

.empty-state i {
  color: var(--brand);
  font-size: 2rem;
}

.empty-state strong {
  color: var(--text-strong);
  font-size: 1.25rem;
  font-weight: 650;
}

@media (max-width: 640px) {
  .admin-heading {
    flex-direction: column;
  }
}
</style>
