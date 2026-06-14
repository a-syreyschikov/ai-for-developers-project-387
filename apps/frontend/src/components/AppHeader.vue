<template>
  <header class="app-header">
    <RouterLink class="brand" to="/" aria-label="Calendar">
      <span class="brand-mark"><i class="pi pi-calendar" /></span>
      <span>{{ t('app.name') }}</span>
    </RouterLink>

    <nav class="main-nav" aria-label="Главная навигация">
      <RouterLink to="/book" :class="{ 'section-active': route.path === '/book' || route.path.startsWith('/book/') }">{{ t('app.nav.book') }}</RouterLink>
      <RouterLink to="/admin" :class="{ 'section-active': route.path === '/admin' || route.path.startsWith('/admin/') }">{{ t('app.nav.admin') }}</RouterLink>
    </nav>
  </header>
</template>

<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'

const route = useRoute()
const { t } = useI18n()
</script>

<style scoped>
.app-header {
  position: sticky;
  top: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  width: min(1180px, calc(100% - 28px));
  margin: 12px auto 0;
  padding: 10px 14px;
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-panel);
  background: var(--surface-panel);
  box-shadow: var(--shadow-panel);
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 9px;
  color: var(--text-strong);
  font-size: 1.05rem;
  font-weight: 650;
}

.brand-mark {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border-radius: var(--radius-control);
  color: var(--p-primary-inverse-color, #ffffff);
  background: var(--brand);
}

.main-nav {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.main-nav a {
  border-radius: var(--radius-control);
  color: var(--text-muted);
  font-weight: 600;
  padding: 8px 12px;
}

.main-nav a.router-link-active,
.main-nav a.section-active {
  color: var(--brand);
  background: var(--brand-soft);
}

@media (max-width: 560px) {
  .app-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .main-nav {
    width: 100%;
  }

  .main-nav a {
    flex: 1;
    text-align: center;
  }
}
</style>
