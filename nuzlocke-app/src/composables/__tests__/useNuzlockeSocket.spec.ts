import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useNuzlockeSocket } from '../useNuzlockeSocket'
import { mount } from '@vue/test-utils'
import { defineComponent } from 'vue'

const mockWebSocket = {
    send: vi.fn(),
    close: vi.fn(),
    onopen: null as any,
    onmessage: null as any,
    onerror: null as any,
    onclose: null as any
}

describe('useNuzlockeSocket', () => {
    beforeEach(() => {
        vi.stubGlobal('WebSocket', vi.fn(() => mockWebSocket))
        vi.stubEnv('VITE_WS_URL', 'ws://localhost:5000/ws')
    })

    afterEach(() => {
        vi.unstubAllEnvs()
        vi.restoreAllMocks()
    })

    it('conecta correctamente al montar el componente incluyendo el sessionId y se cierra al desmontar', () => {
        const TestComponent = defineComponent({
            template: '<div></div>',
            setup() {
                const { isConnected, sendMessage } = useNuzlockeSocket('session-123')
                return { isConnected, sendMessage }
            }
        })

        const wrapper = mount(TestComponent)

        // Se verifica que new WebSocket(...) fue llamado con la querystring de Id
        expect(global.WebSocket).toHaveBeenCalledWith('ws://localhost:5000/ws?nuzlockeId=session-123')
        expect(wrapper.vm.isConnected).toBe(false)

        // Simulamos la respuesta de apertura del servidor de AI
        if (mockWebSocket.onopen) mockWebSocket.onopen()
        expect(wrapper.vm.isConnected).toBe(true)

        // Test enviar mensaje
        wrapper.vm.sendMessage({ "cmd": "hello_ai" })
        expect(mockWebSocket.send).toHaveBeenCalledWith(JSON.stringify({ "cmd": "hello_ai" }))

        // Simulating the user leaving the screen (unmount components triggers native ws lifecycle cleaning)
        wrapper.unmount()
        expect(mockWebSocket.close).toHaveBeenCalled()
    })
})
