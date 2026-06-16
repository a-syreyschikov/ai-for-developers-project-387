import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { title: 'Главная' },
    },
    {
      path: '/book',
      name: 'book',
      component: () => import('../views/public/EventTypesView.vue'),
      meta: { title: 'Записаться на встречу' },
    },
    {
      path: '/book/success',
      name: 'booking-success',
      component: () => import('../views/public/BookingSuccessView.vue'),
      meta: { title: 'Запись создана' },
    },
    {
      path: '/book/:eventTypeId',
      name: 'book-event-type',
      component: () => import('../views/public/BookingTimeView.vue'),
      props: true,
      meta: { title: 'Выбор времени' },
    },
    {
      path: '/book/:eventTypeId/confirm',
      name: 'book-confirm',
      component: () => import('../views/public/BookingConfirmView.vue'),
      props: true,
      meta: { title: 'Подтверждение записи' },
    },
    {
      path: '/admin',
      redirect: '/admin/upcoming',
    },
    {
      path: '/admin',
      component: () => import('../views/admin/AdminLayout.vue'),
      children: [
        {
          path: 'upcoming',
          name: 'admin-upcoming',
          component: () => import('../views/admin/UpcomingBookingsView.vue'),
          meta: { title: 'Предстоящие встречи — Админка' },
        },
        {
          path: 'events',
          name: 'admin-events',
          component: () => import('../views/admin/EventTypesAdminView.vue'),
          meta: { title: 'Типы событий — Админка' },
        },
        {
          path: 'schedule',
          name: 'admin-schedule',
          component: () => import('../views/admin/ScheduleView.vue'),
          meta: { title: 'Расписание — Админка' },
        },
      ],
    },
  ],
})

router.afterEach((to) => {
  const baseTitle = 'Calendar'
  if (to.meta && typeof to.meta.title === 'string') {
    document.title = `${to.meta.title} — ${baseTitle}`
  } else {
    document.title = baseTitle
  }
})

export default router
