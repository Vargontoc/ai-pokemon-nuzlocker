import axios, { AxiosError } from 'axios'
import router from '../router'

const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL,
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json'
    }
})

// Variables para evitar múltiples redireccionamientos seguidos
let isOffline = false
let healthCheckInterval: ReturnType<typeof setInterval> | null = null

const startHealthCheckPolling = () => {
    if (healthCheckInterval) return

    healthCheckInterval = setInterval(async () => {
        try {
            // Un endpoint ligero existente como health o simplemente la raiz de la API
            // Usamos la misma instancia api en lugar de axios raw para que funcione 
            // con el baseURL ya configurado y además para facilitar el mock en testing.
            await api.get('/health', {
                timeout: 3000 // un timeout más corto para el check
            })

            // Si funciona, paramos el polling
            stopHealthCheckPolling()

            // Forzamos el comportamiento de reconexión haciendo una 
            // petición a la app que disparará el interceptor
            await api.get('/health')
        } catch (error) {
            // Continúa caido
        }
    }, 5000)
}

const stopHealthCheckPolling = () => {
    if (healthCheckInterval) {
        clearInterval(healthCheckInterval)
        healthCheckInterval = null
    }
}
api.interceptors.response.use(
    (response) => {
        // Si la conexión fue exitosa y antes estábamos offline, comprobamos si 
        // tenemos una ruta previa guardada para restaurar
        if (isOffline) {
            isOffline = false
            stopHealthCheckPolling()

            const lastRoute = localStorage.getItem('lastRouteBeforeOffline')
            if (lastRoute) {
                localStorage.removeItem('lastRouteBeforeOffline')
                router.push(lastRoute)
            } else {
                router.push('/')
            }
        }
        return response
    },
    async (error: AxiosError) => {
        // Comprobar si el error es de red (Network Error) o 5xx del servidor (Caída)
        const isNetworkError = !error.response || error.code === 'ERR_NETWORK' || error.response.status >= 500

        if (isNetworkError && !isOffline) {
            isOffline = true
            // Guardar la vista actual (si no es ya offline)
            const currentPath = router.currentRoute.value.fullPath
            if (currentPath !== '/offline') {
                localStorage.setItem('lastRouteBeforeOffline', currentPath)
            }

            // Forzar la redirección a la vista Offline/Configurada
            await router.push('/offline')

            // Empezar el polling de comprobacion
            startHealthCheckPolling()
        }

        return Promise.reject(error)
    }
)

export default api
