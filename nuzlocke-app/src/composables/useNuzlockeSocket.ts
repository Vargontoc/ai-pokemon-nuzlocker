import { ref, onMounted, onUnmounted } from 'vue'
import { toast } from 'vue3-toastify'

export function useNuzlockeSocket(sessionId: string) {
    const isConnected = ref(false)
    const socket = ref<WebSocket | null>(null)

    const connect = () => {
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
                console.log('Nuzlocke WebSocket mensaje recibido:', event.data)
                // TODO: Centralizar el parsing de DTOs en sprint venidero
            }

            socket.value.onerror = (error) => {
                console.error('Nuzlocke WebSocket Error:', error)
                toast.error('Error general del Socket')
            }

            socket.value.onclose = () => {
                isConnected.value = false
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
        }
    }

    const sendMessage = (data: any) => {
        if (socket.value && isConnected.value) {
            socket.value.send(JSON.stringify(data))
        } else {
            console.warn("Se intentó enviar un payload sin conexión activa WS")
        }
    }

    // Automáticamente ligamos el ciclo de vida de Vue
    onMounted(() => {
        connect()
    })

    onUnmounted(() => {
        disconnect()
    })

    return {
        isConnected,
        sendMessage,
        connect,
        disconnect
    }
}
