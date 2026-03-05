import { ref, computed } from 'vue'
import { toast } from 'vue3-toastify'

export type RomMode = 'exploration' | 'battle'

export interface ChatMessage {
    id: string
    type?: string
    sender: 'ai' | 'user' | 'system'
    content: string
    timestamp: Date
    eventId?: string
    workflowId?: string
    toolName?: string
    source?: string
    stateChanges?: Record<string, any>
    success?: boolean
    correlationId?: string
}

// --- GLOBAL STATE ---
const isConnected = ref(false)
const isAiThinking = ref(false)
const romMode = ref<RomMode>('exploration')
const socket = ref<WebSocket | null>(null)

const explorationMessages = ref<ChatMessage[]>([])
const battleMessages = ref<ChatMessage[]>([])
let activeSessionId: string | null = null

export function useNuzlockeSocket() {

    // Getter helper: devuelve una computed con la lista de mensajes correspondiente al modo actual
    const activeMessages = computed(() => romMode.value === 'battle' ? battleMessages.value : explorationMessages.value)

    const connect = (sessionId: string) => {
        if (socket.value && activeSessionId === sessionId) return // Ya está conectado a la misma sesión

        // Cierra la anterior si es distinta
        if (socket.value) disconnect()

        activeSessionId = sessionId
        const wsUrl = import.meta.env.VITE_WS_URL
        if (!wsUrl) {
            console.error('VITE_WS_URL no está definido en el entorno')
            toast.error('Configuración de red incorrecta (WS_URL faltante)')
            return
        }

        try {
            const url = new URL(wsUrl)
            url.searchParams.append('nuzlockeId', sessionId)

            socket.value = new WebSocket(url.toString())

            socket.value.onopen = () => {
                isConnected.value = true
                console.log('Nuzlocke WebSocket connected')
                toast.success('Conectado a la Inteligencia Artificial', { autoClose: 2500 })
            }

            socket.value.onmessage = (event) => {
                try {
                    const payload = JSON.parse(event.data)
                    console.log('WS msg:', payload)
                    const msgList = romMode.value === 'battle' ? battleMessages : explorationMessages

                    if (payload.type === 'advice_start') {
                        isAiThinking.value = true
                    } else if (payload.type === 'mode_change') {
                        romMode.value = payload.mode as RomMode
                    } else if (payload.type === 'advice_end' || payload.type === 'advice_error' || payload.type === 'error') {
                        isAiThinking.value = false
                        if (payload.type === 'error' || payload.type === 'advice_error') toast.error(payload.message || 'Error AI')

                        // Si es advice_end, podemos opcionalmente empujar el FullAdvice si no hubo streaming previo de chunks 
                        // pero la versión de chunks pura construirá el mensaje paso a paso.

                    } else if (payload.type === 'agent_tool_call') {
                        msgList.value.push({
                            id: crypto.randomUUID(),
                            type: payload.type,
                            sender: 'system',
                            content: `Consultando ${payload.toolName}...`,
                            toolName: payload.toolName,
                            source: payload.source,
                            correlationId: payload.correlationId,
                            timestamp: new Date()
                        })
                    } else if (payload.type === 'workflow_event') {
                        msgList.value.push({
                            id: crypto.randomUUID(),
                            type: payload.type,
                            sender: 'system',
                            content: `Evento Nuzlocke: ${payload.eventId}`,
                            eventId: payload.eventId,
                            workflowId: payload.workflowId,
                            success: payload.success,
                            stateChanges: payload.stateChanges,
                            correlationId: payload.correlationId,
                            timestamp: new Date()
                        })
                    } else if (payload.type === 'advice_chunk') {
                        const lastMsg = msgList.value[msgList.value.length - 1]
                        if (lastMsg && lastMsg.sender === 'ai' && lastMsg.correlationId === payload.correlationId) {
                            lastMsg.content += payload.content || ''
                        } else {
                            msgList.value.push({
                                id: crypto.randomUUID(),
                                type: 'message',
                                sender: 'ai',
                                content: payload.content || '',
                                correlationId: payload.correlationId,
                                timestamp: new Date()
                            })
                        }
                    } else if (payload.content) {
                        msgList.value.push({
                            id: crypto.randomUUID(),
                            type: payload.type || 'message',
                            sender: payload.sender || 'ai',
                            content: payload.content,
                            timestamp: new Date(),
                            eventId: payload.eventId
                        })
                    }
                } catch (e) {
                    console.error('Error parseando WS event:', e, event.data)
                }
            }

            socket.value.onerror = (error) => {
                console.error('Nuzlocke WebSocket Error:', error)
                isAiThinking.value = false
                toast.error('Error general del Socket')
            }

            socket.value.onclose = () => {
                isConnected.value = false
                activeSessionId = null
                console.log('Nuzlocke WebSocket disconnected')
                toast.info('Conexión AI terminada', { autoClose: 3000 })
            }
        } catch (e) {
            console.error("No se pudo iniciar WebSocket", e)
        }
    }

    const disconnect = () => {
        if (socket.value) {
            socket.value.close()
            socket.value = null
            activeSessionId = null
            isConnected.value = false
            isAiThinking.value = false
        }
    }

    const sendMessage = (data: any) => {
        if (socket.value && isConnected.value) {
            socket.value.send(JSON.stringify(data))
        } else {
            console.warn("Se intentó enviar un payload sin conexión activa WS")
        }
    }

    return {
        isConnected,
        isAiThinking,
        romMode,
        explorationMessages,
        battleMessages,
        activeMessages,
        sendMessage,
        connect,
        disconnect
    }
}
