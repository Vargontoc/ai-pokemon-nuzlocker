import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'dashboard',
            component: DashboardView
        },
        {
            path: '/nuzlocke/:id',
            name: 'nuzlocke',
            component: () => import('../views/NuzlockeView.vue')
        },
        {
            path: '/offline',
            name: 'offline',
            component: () => import('../views/OfflineErrorView.vue')
        }
    ]
})

export default router
