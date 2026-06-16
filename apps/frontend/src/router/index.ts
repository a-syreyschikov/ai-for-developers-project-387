import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import { i18n } from '../shared/i18n'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { title: 'pages.home' },
    },
    {
      path: '/book',
      name: 'book',
      component: () => import('../views/public/EventTypesView.vue'),
      meta: { title: 'pages.book' },
    },
    {
      path: '/book/success',
      name: 'booking-success',
      component: () => import('../views/public/BookingSuccessView.vue'),
      meta: { title: 'pages.bookingSuccess' },
    },
    {
      path: '/book/:eventTypeId',
      name: 'book-event-type',
      component: () => import('../views/public/BookingTimeView.vue'),
      props: true,
      meta: { title: 'pages.bookEventType' },
    },
    {
      path: '/book/:eventTypeId/confirm',
      name: 'book-confirm',
      component: () => import('../views/public/BookingConfirmView.vue'),
      props: true,
      meta: { title: 'pages.bookConfirm' },
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
          meta: { title: 'pages.adminUpcoming' },
        },
        {
          path: 'events',
          name: 'admin-events',
          component: () => import('../views/admin/EventTypesAdminView.vue'),
          meta: { title: 'pages.adminEvents' },
        },
        {
          path: 'schedule',
          name: 'admin-schedule',
          component: () => import('../views/admin/ScheduleView.vue'),
          meta: { title: 'pages.adminSchedule' },
        },
      ],
    },
  ],
})

router.afterEach((to) => {
  const baseTitle = i18n.global.t('app.name')
  try {
    if (to.meta && typeof to.meta.title === 'string') {
      document.title = `${i18n.global.t(to.meta.title)} — ${baseTitle}`
    } else {
      document.title = baseTitle
    }
  } catch (error) {
    console.error('Ошибка при обновлении document.title:', error)
    document.title = baseTitle
  }
})

export default router
