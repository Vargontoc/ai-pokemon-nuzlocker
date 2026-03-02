import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import DashboardView from '../../views/DashboardView.vue'
import { nuzlockeService, NuzlockeStatus } from '../../services/nuzlockeService'
import router from '../../router'
import i18n from '../../config/i18n'

// Mocking the router manually for tests that aren't integrated via plugins
vi.mock('vue-router', async () => {
    const actual = await vi.importActual('vue-router')
    return {
        ...actual,
        useRouter: () => ({
            push: vi.fn(),
            currentRoute: { value: { fullPath: '/' } }
        })
    }
})

// Mocking dependencies explicitly instead of nested calls inside components
vi.mock('../../services/nuzlockeService', async () => {
    const actual = await vi.importActual('../../services/nuzlockeService') as any
    return {
        ...actual,
        nuzlockeService: {
            getSessions: vi.fn(),
            createSession: vi.fn(),
            deleteSession: vi.fn()
        }
    }
})

describe('DashboardView.vue', () => {

    const fakeSessions = [
        { id: '1', name: 'Pokemon Rojo Fuego', path: 'C:/games/rf', createdAt: new Date().toISOString(), status: NuzlockeStatus.Active },
        { id: '2', name: 'Pokemon Esmeralda', path: 'C:/games/es', createdAt: new Date().toISOString(), status: NuzlockeStatus.Completed }
    ]

    beforeEach(() => {
        vi.clearAllMocks()
    })

    const mountComponent = () => mount(DashboardView, {
        global: {
            plugins: [i18n, router]
        }
    })

    it('displays loading state initially and empty state if no sessions', async () => {
        vi.mocked(nuzlockeService.getSessions).mockResolvedValueOnce([])

        const wrapper = mountComponent()

        // It should render loading spinner immediately
        expect(wrapper.find('.animate-spin').exists()).toBe(true)

        // flush all promises to resolve getSessions
        await flushPromises()

        // Loader should be gone
        expect(wrapper.find('.animate-spin').exists()).toBe(false)

        // Empty state showing (using translation output)
        expect(wrapper.text()).toContain('No se encontraron sesiones de Nuzlocke')
    })

    it('fetches and renders Nuzlocke sessions successfully', async () => {
        vi.mocked(nuzlockeService.getSessions).mockResolvedValueOnce(fakeSessions)

        const wrapper = mountComponent()
        await flushPromises()

        // Empty state is hidden
        expect(wrapper.text()).not.toContain('No se encontraron sesiones de Nuzlocke')

        // Found exactly 2 NuzlockeCards (titles)
        expect(wrapper.text()).toContain('Pokemon Rojo Fuego')
        expect(wrapper.text()).toContain('Pokemon Esmeralda')
    })
})
