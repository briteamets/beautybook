import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import HomeView from '@/views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'Home', component: HomeView },
    { path: '/login', name: 'Login', component: () => import('@/views/account/LoginView.vue') },
    {
      path: '/register',
      name: 'Register',
      component: () => import('@/views/account/RegisterView.vue'),
    },
    { path: '/providers', name: 'Providers', component: () => import('@/views/providers/IndexView.vue') },
    {
      path: '/providers/:id',
      name: 'ProviderDetail',
      component: () => import('@/views/providers/DetailView.vue'),
      props: true,
    },
    {
      path: '/providers/:id/book',
      name: 'BookProvider',
      component: () => import('@/views/bookings/CreateView.vue'),
      props: true,
      meta: { requiresAuth: true, role: 'Client' },
    },
    {
      path: '/bookings',
      name: 'MyBookings',
      component: () => import('@/views/bookings/IndexView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/bookings/:id',
      name: 'BookingDetail',
      component: () => import('@/views/bookings/DetailView.vue'),
      props: true,
      meta: { requiresAuth: true },
    },
    {
      path: '/favorites',
      name: 'Favorites',
      component: () => import('@/views/favorites/IndexView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/notifications',
      name: 'Notifications',
      component: () => import('@/views/notifications/IndexView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/payments',
      name: 'Payments',
      component: () => import('@/views/payments/IndexView.vue'),
      meta: { requiresAuth: true },
    },
    { path: '/salons', name: 'Salons', component: () => import('@/views/salons/IndexView.vue') },
    {
      path: '/salons/:id',
      name: 'SalonDetail',
      component: () => import('@/views/salons/DetailView.vue'),
      props: true,
    },
    {
      path: '/provider/profile',
      name: 'ProviderProfile',
      component: () => import('@/views/provider-dashboard/ProfileView.vue'),
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    {
      path: '/provider/services',
      name: 'ProviderServices',
      component: () => import('@/views/provider-dashboard/ServicesView.vue'),
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    {
      path: '/provider/services/new',
      name: 'ProviderServiceNew',
      component: () => import('@/views/provider-dashboard/ServiceEditView.vue'),
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    {
      path: '/provider/services/:id/edit',
      name: 'ProviderServiceEdit',
      component: () => import('@/views/provider-dashboard/ServiceEditView.vue'),
      props: true,
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    {
      path: '/provider/schedule',
      name: 'ProviderSchedule',
      component: () => import('@/views/provider-dashboard/ScheduleView.vue'),
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    {
      path: '/provider/availability',
      name: 'ProviderAvailability',
      component: () => import('@/views/provider-dashboard/AvailabilityView.vue'),
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    {
      path: '/provider/incoming',
      name: 'ProviderIncoming',
      component: () => import('@/views/provider-dashboard/IncomingBookingsView.vue'),
      meta: { requiresAuth: true, role: 'ServiceProvider' },
    },
    { path: '/forbidden', name: 'Forbidden', component: () => import('@/views/ForbiddenView.vue') },
    { path: '/:pathMatch(.*)*', name: 'NotFound', component: () => import('@/views/NotFoundView.vue') },
  ],
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return next({ name: 'Login', query: { redirect: to.fullPath } })
  }
  const requiredRole = to.meta.role as string | undefined
  if (requiredRole && !auth.hasRole(requiredRole)) {
    return next({ name: 'Forbidden' })
  }
  next()
})

export default router
