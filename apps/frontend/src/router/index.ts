import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/book',
      name: 'book',
      component: () => import('../views/public/EventTypesView.vue'),
    },
    {
      path: '/book/success',
      name: 'booking-success',
      component: () => import('../views/public/BookingSuccessView.vue'),
    },
    {
      path: '/book/:eventTypeId',
      name: 'book-event-type',
      component: () => import('../views/public/BookingTimeView.vue'),
      props: true,
    },
    {
      path: '/book/:eventTypeId/confirm',
      name: 'book-confirm',
      component: () => import('../views/public/BookingConfirmView.vue'),
      props: true,
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
        },
        {
          path: 'events',
          name: 'admin-events',
          component: () => import('../views/admin/EventTypesAdminView.vue'),
        },
        {
          path: 'schedule',
          name: 'admin-schedule',
          component: () => import('../views/admin/ScheduleView.vue'),
        },
      ],
    },
  ],
})

export default router
