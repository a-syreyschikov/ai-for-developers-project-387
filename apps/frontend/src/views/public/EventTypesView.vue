<template>
  <section class="page-stack">
    <div class="page-heading">
      <h1>Записаться</h1>
      <p v-if="owner">Вы записываетесь к {{ owner.displayName }}. Время будет показано в часовом поясе {{ owner.timeZone }}.</p>
      <p v-else>Выберите тип события и перейдите к свободным слотам.</p>
    </div>

    <Message v-if="error" severity="error">{{ error }}</Message>

    <div v-if="loading" class="event-grid">
      <Skeleton v-for="item in 3" :key="item" height="10rem" />
    </div>

    <div v-else-if="eventTypes.length === 0" class="empty-state surface-card">
      <i class="pi pi-calendar-times" />
      <strong>Типы событий пока не созданы</strong>
      <span>Владелец сможет добавить их в админке.</span>
    </div>

    <div v-else class="event-grid">
      <RouterLink v-for="eventType in eventTypes" :key="eventType.id" class="event-card-link" :to="`/book/${eventType.id}`">
        <Card class="event-card">
          <template #title>{{ eventType.title }}</template>
          <template #subtitle>{{ eventType.durationMinutes }} минут</template>
          <template #content>
            <p>{{ eventType.description || 'Описание не указано.' }}</p>
            <span class="event-card-action">Выбрать время <i class="pi pi-arrow-right" /></span>
          </template>
        </Card>
      </RouterLink>
    </div>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { useI18n } from 'vue-i18n'
import { calendarApi, type EventType, type Owner } from '@/api/calendar'
import { getErrorMessage } from '@/shared/errors'

const { t } = useI18n()
const owner = ref<Owner | null>(null)
const eventTypes = ref<EventType[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    const [ownerResult, eventTypesResult] = await Promise.all([
      calendarApi.getPublicOwner(),
      calendarApi.listPublicEventTypes(),
    ])
    owner.value = ownerResult
    eventTypes.value = eventTypesResult.items
  } catch (caught) {
    error.value = getErrorMessage(caught, t)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.event-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 280px), 360px));
  gap: 16px;
  justify-content: start;
}

.event-card-link {
  display: block;
  min-width: 0;
}

.event-card {
  height: 100%;
  border: 1px solid transparent;
  transition:
    border-color 160ms ease,
    transform 160ms ease,
    box-shadow 160ms ease;
}

.event-card-link:hover .event-card,
.event-card-link:focus-visible .event-card {
  border-color: var(--brand);
  box-shadow: var(--shadow-panel);
  transform: translateY(-1px);
}

.event-card-link:focus-visible {
  border-radius: var(--radius-panel);
  outline: 2px solid var(--p-focus-ring-color, var(--brand));
  outline-offset: 3px;
}

.event-card p {
  min-height: 68px;
  color: var(--text-muted);
}

.event-card-action {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding-top: 10px;
  color: var(--brand);
  font-weight: 600;
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
</style>
