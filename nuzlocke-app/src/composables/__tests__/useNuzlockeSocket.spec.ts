import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useNuzlockeSocket } from '../useNuzlockeSocket'

// Helper global mock WS para atrapar la instancia instanciada.
let mockWsInstance: any = null

class MockWebSocket {
    url: string
    onopen: (() => void) | null = null
    onmessage: ((event: any) => void) | null = null
    onerror: ((error: any) => void) | null = null
    onclose: (() => void) | null = null

    constructor(url: string) {
        this.url = url
        mockWsInstance = this // Capturamos la ref
    }

    send = vi.fn()
    close = vi.fn(() => {
        if (this.onclose) this.onclose()
    })
}

describe('useNuzlockeSocket (Global Singleton)', () => {
    let socketModule: ReturnType<typeof useNuzlockeSocket>

    beforeEach(() => {
        vi.stubGlobal('WebSocket', MockWebSocket)
        vi.stubEnv('VITE_WS_URL', 'ws://localhost:5000/ws')

        socketModule = useNuzlockeSocket()
        // Limpiamos el estado global reseteando las props para tener entorno limpio entre test y test
        socketModule.disconnect()
        socketModule.explorationMessages.value = []
        socketModule.battleMessages.value = []
        socketModule.romMode.value = 'exploration'
        socketModule.isAiThinking.value = false
        mockWsInstance = null
    })

    afterEach(() => {
        vi.unstubAllEnvs()
        vi.restoreAllMocks()
        socketModule.disconnect()
    })

    it('connects via explicit connect(sessionId) and opens connection', () => {
        socketModule.connect('session-123')

        expect(mockWsInstance).toBeDefined()
        expect(mockWsInstance.url).toBe('ws://localhost:5000/ws?nuzlockeId=session-123')
        expect(socketModule.isConnected.value).toBe(false)

        // Simular evento nativo de Socket Open
        mockWsInstance.onopen()

        expect(socketModule.isConnected.value).toBe(true)
    })

    it('parses mode_change effectively routing activeMessages', () => {
        socketModule.connect('s-mode')
        mockWsInstance.onopen()

        expect(socketModule.romMode.value).toBe('exploration')
        expect(socketModule.activeMessages.value).toBe(socketModule.explorationMessages.value)

        // Inject fake WS 'mode_change' event
        mockWsInstance.onmessage({
            data: JSON.stringify({ type: 'mode_change', mode: 'battle' })
        })

        expect(socketModule.romMode.value).toBe('battle')
        expect(socketModule.activeMessages.value).toBe(socketModule.battleMessages.value)
    })

    it('toggles isAiThinking on advice_start and advice_end', () => {
        socketModule.connect('s-think')
        mockWsInstance.onopen()

        expect(socketModule.isAiThinking.value).toBe(false)

        mockWsInstance.onmessage({ data: JSON.stringify({ type: 'advice_start', correlationId: '123' }) })
        expect(socketModule.isAiThinking.value).toBe(true)

        mockWsInstance.onmessage({ data: JSON.stringify({ type: 'advice_end', correlationId: '123' }) })
        expect(socketModule.isAiThinking.value).toBe(false)
    })

    it('pushes incoming workflow_event to the proper active mode queue', () => {
        socketModule.connect('s-content')
        mockWsInstance.onopen()

        // Aseguramos que estamos en battle para probar
        socketModule.romMode.value = 'battle'

        mockWsInstance.onmessage({
            data: JSON.stringify({
                type: 'workflow_event',
                eventId: 'battle_start',
                workflowId: 'w-123',
                success: true
            })
        })

        expect(socketModule.battleMessages.value.length).toBe(1)
        expect(socketModule.explorationMessages.value.length).toBe(0) // Exploracion limpia

        const storedMsg = socketModule.battleMessages.value[0]
        expect(storedMsg.type).toBe('workflow_event')
        expect(storedMsg.sender).toBe('system')
        expect(storedMsg.eventId).toBe('battle_start')
        expect(storedMsg.workflowId).toBe('w-123')
    })
})
