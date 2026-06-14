<template>
  <section class="admin-shell">
    <aside class="admin-sidebar surface-card">
      <span class="sidebar-title">Админка</span>
      <RouterLink to="/admin/upcoming"><i class="pi pi-clock" /> Предстоящие встречи</RouterLink>
      <RouterLink to="/admin/events"><i class="pi pi-list" /> События</RouterLink>
      <RouterLink to="/admin/schedule"><i class="pi pi-calendar" /> Расписание</RouterLink>
    </aside>
    <div class="admin-content">
      <RouterView />
    </div>
  </section>
</template>

<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<style scoped>
.admin-shell {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  gap: 18px;
  align-items: start;
}

.admin-sidebar {
  position: sticky;
  top: 86px;
  display: grid;
  gap: 8px;
  padding: 14px;
}

.sidebar-title {
  color: var(--text-muted);
  font-size: 0.82rem;
  font-weight: 600;
  text-transform: uppercase;
}

.admin-sidebar a {
  display: flex;
  align-items: center;
  gap: 10px;
  border-radius: var(--radius-control);
  padding: 10px 12px;
  color: var(--text-muted);
  font-weight: 600;
}

.admin-sidebar a.router-link-active {
  color: var(--brand);
  background: var(--brand-soft);
}

.admin-content {
  min-width: 0;
}

@media (max-width: 860px) {
  .admin-shell {
    grid-template-columns: 1fr;
  }

  .admin-sidebar {
    position: static;
  }
}
</style>
