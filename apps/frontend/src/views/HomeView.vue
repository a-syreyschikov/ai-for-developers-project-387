<template>
  <section class="home-page">
    <div class="hero-copy">
      <span class="eyebrow">Запись без лишних шагов</span>
      <h1>Запись на встречу за пару минут</h1>
      <p>
        Calendar помогает владельцу опубликовать типы встреч и недельное расписание, а гостям — быстро записаться на свободный слот.
      </p>
      <div class="hero-actions">
        <Button label="Записаться" icon="pi pi-arrow-right" icon-pos="right" as="router-link" to="/book" />
      </div>
    </div>

    <aside class="workflow-panel surface-card" aria-label="Как проходит запись">
      <div class="workflow-head">
        <span>Как это работает</span>
        <strong>Гость проходит запись за три шага</strong>
        <p>Страница ведёт от выбора формата до подтверждения без лишних переходов.</p>
      </div>

      <ol class="workflow-list">
        <li>
          <span class="step-number">01</span>
          <div>
            <strong>Выбор события</strong>
            <p>Гость видит доступные типы встреч и выбирает подходящий формат.</p>
          </div>
        </li>
        <li>
          <span class="step-number">02</span>
          <div>
            <strong>Свободное время</strong>
            <p>Календарь показывает только слоты из расписания владельца.</p>
          </div>
        </li>
        <li>
          <span class="step-number">03</span>
          <div>
            <strong>Подтверждение</strong>
            <p>После отправки данных выбранный слот закрепляется за бронированием.</p>
          </div>
        </li>
      </ol>
    </aside>
  </section>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
</script>

<style scoped>
.home-page {
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) minmax(320px, 0.9fr);
  gap: 28px;
  align-items: center;
  min-height: calc(100vh - 180px);
  padding: 36px 0 48px;
}

.hero-copy {
  display: grid;
  gap: 18px;
}

.eyebrow {
  width: fit-content;
  border-radius: var(--radius-control);
  padding: 7px 12px;
  color: var(--brand);
  background: var(--brand-soft);
  font-weight: 600;
}

h1 {
  color: var(--text-strong);
  font-size: clamp(2.2rem, 4.6vw, 4rem);
  font-weight: 650;
  letter-spacing: -0.04em;
  line-height: 1.04;
}

p {
  color: var(--text-muted);
  font-size: 1rem;
  max-width: 620px;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.workflow-panel {
  display: grid;
  gap: 18px;
  padding: 22px;
}

.workflow-head {
  display: grid;
  gap: 6px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--surface-border);
}

.workflow-head span {
  color: var(--brand);
  font-size: 0.82rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.workflow-head strong {
  color: var(--text-strong);
  font-size: 1.25rem;
  font-weight: 650;
}

.workflow-head p {
  color: var(--text-muted);
  font-size: 0.95rem;
  max-width: none;
}

.workflow-list {
  display: grid;
  list-style: none;
}

.workflow-list li {
  display: grid;
  grid-template-columns: 44px 1fr;
  gap: 14px;
  padding: 16px 0;
  border-bottom: 1px solid var(--surface-border);
}

.workflow-list li:first-child {
  padding-top: 0;
}

.workflow-list li:last-child {
  padding-bottom: 0;
  border-bottom: 0;
}

.step-number {
  display: grid;
  width: 42px;
  height: 42px;
  place-items: center;
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-control);
  color: var(--brand);
  background: var(--brand-soft);
  font-weight: 650;
}

.workflow-list strong {
  color: var(--text-strong);
  font-size: 1.05rem;
  font-weight: 650;
}

.workflow-list p {
  color: var(--text-muted);
  font-size: 0.95rem;
  max-width: none;
}

@media (max-width: 860px) {
  .home-page {
    align-items: start;
    grid-template-columns: 1fr;
    min-height: auto;
  }
}
</style>
