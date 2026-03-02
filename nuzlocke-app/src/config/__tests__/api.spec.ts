import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import api from '../api'
import router from '../../router'
import MockAdapter from 'axios-mock-adapter'

vi.mock('../../router', () => ({
    default: {
        push: vi.fn().mockResolvedValue(true),
        currentRoute: {
            value: { fullPath: '/test-route' }
        }
    }
}))

describe('Axios Interceptors', () => {
    let mock: MockAdapter

    beforeEach(async () => {
        vi.useFakeTimers()
        vi.clearAllMocks()
        localStorage.clear()
        mock = new MockAdapter(api)

        // Forzar reseteo del estado isOffline a false con una peticion exitosa
        mock.onGet('/_reset_test_').reply(200)
        await api.get('/_reset_test_').catch(() => { })
        vi.clearAllMocks() // limpiamos cualquier llamada a router generada por este reseteo
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    it('redirects to /offline on network error', async () => {
        mock.onGet('/test').networkError()

        try {
            await api.get('/test')
        } catch (e) {
            // Ignoramos el reject en el test
        }

        expect(router.push).toHaveBeenCalledWith('/offline')
        expect(localStorage.getItem('lastRouteBeforeOffline')).toBe('/test-route')
    })

    it('restores route on success if it was offline', async () => {
        // 1. Simular Network Error a la peticion inicial
        mock.onGet('/test-fail').networkError()
        try {
            await api.get('/test-fail')
        } catch (e) { }

        expect(router.push).toHaveBeenCalledWith('/offline')

        // 2. El polling interno hará GET de '/health' usando la misma instancia api.
        mock.onGet('/health').reply(200, { status: "ok" })

        // Simular que ha pasado 1 ciclo de polling (5s) para que dispare la petición
        await vi.advanceTimersByTimeAsync(5000)

        // 3. Simular la petición final con éxito
        // Como el reconector automático de polling gatilla api.get('/health'), esto debería
        // restaurar la ruta.
        expect(router.push).toHaveBeenCalledWith('/test-route')
    })
})
