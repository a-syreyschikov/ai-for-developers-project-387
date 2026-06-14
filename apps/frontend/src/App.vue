<template>
  <Toast />
  <ConfirmDialog />
  <AppHeader />
  <main class="app-main">
    <RouterView />
  </main>
</template>

<script setup lang="ts">
import { RouterView } from 'vue-router'
import ConfirmDialog from 'primevue/confirmdialog'
import Toast from 'primevue/toast'
import AppHeader from '@/components/AppHeader.vue'
</script>

<style scoped>
.app-main {
  width: min(1180px, calc(100% - 28px));
  margin: 0 auto;
  padding: 24px 0 44px;
}
</style>
