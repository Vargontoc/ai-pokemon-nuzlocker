<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useNuzlockeSocket } from '../../composables/useNuzlockeSocket'
import { nuzlockeService } from '../../services/nuzlockeService'
import { toast } from 'vue3-toastify'
import ChatHeader from './ChatHeader.vue'
import ChatInput from './ChatInput.vue'
import ChatMessageBubble from './ChatMessageBubble.vue'

const props = defineProps<{
  sessionId: string
}>()

const { isConnected, isAiThinking, activeMessages, romMode } = useNuzlockeSocket()

const selectedLanguage = ref('es-ES')

const handleSendMessage = async (content: string) => {
  if (!content.trim() || isAiThinking.value) return
  
  // Agregar también el mensaje al cliente visualmente rápido (optimistic update)
  activeMessages.value.push({
    id: crypto.randomUUID(),
    sender: 'user',
    content,
    timestamp: new Date()
  })

  // Emitimos el Payload final hacia la infraestructura HTTP API de Advice
  try {
     isAiThinking.value = true // Pre-Bloqueamos visualmente por si tarda la latencia
     await nuzlockeService.askAdvice({
         nuzlockeId: props.sessionId,
         question: content,
         language: selectedLanguage.value
     })
  } catch(e) {
     console.error("Error pidiendo Advice", e)
     toast.error("El servidor de IA no pudo procesar el evento.")
     isAiThinking.value = false // Liberamos el input si falló la HTTP Res
  }
}

// Logica de Scrolling automático pegajoso
const chatContainer = ref<HTMLElement | null>(null)

watch(() => activeMessages.value.length, async () => {
    await nextTick()
    if (chatContainer.value) {
        chatContainer.value.scrollTop = chatContainer.value.scrollHeight + 100
    }
}, { deep: false })

watch(() => isAiThinking.value, async (thinking) => {
    // Si acaba de empezar a pensar (o termina), provocamos scroll suave abajo
    await nextTick()
    if (chatContainer.value && thinking) {
        chatContainer.value.scrollTop = chatContainer.value.scrollHeight + 100
    }
})

</script>

<template>
  <div class="h-full flex flex-col w-full bg-white dark:bg-gray-900 overflow-hidden relative">
      <!-- Chat Header: Contiene Select de Idioma y Toggle de Batalla/Exploración -->
      <ChatHeader :mode="romMode" :connected="isConnected" v-model:language="selectedLanguage" />

      <!-- Historial de Chat (Bubble List) -->
      <div ref="chatContainer" class="flex-1 overflow-y-auto px-4 py-4 space-y-4 scroll-smooth">
          <!-- TODO: ChatBubbleBox componentes renderizables por cada mensaje -->
          <div v-if="activeMessages.length === 0" class="h-full flex flex-col items-center justify-center text-gray-400 opacity-60">
             <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" /></svg>
             <p>El Profesor Oak IA te está escuchando...</p>
          </div>
          
          <!-- Renderizado Dinámico Polimórfico de Burbujas -->
          <ChatMessageBubble 
              v-for="msg in activeMessages" 
              :key="msg.id" 
              :msg="msg" 
          />
          
          <!-- Loading Bubble de la IA Pensando -->
          <div v-if="isAiThinking" class="p-3 rounded-lg w-[85%] bg-gray-100 dark:bg-gray-800 mr-auto text-gray-600 flex gap-2">
             <div class="animate-bounce delay-75 h-2 w-2 bg-gray-400 rounded-full mt-2"></div>
             <div class="animate-bounce delay-150 h-2 w-2 bg-gray-400 rounded-full mt-2"></div>
             <div class="animate-bounce delay-300 h-2 w-2 bg-gray-400 rounded-full mt-2"></div>
          </div>
      </div>

      <!-- Footer/Input Box -->
      <ChatInput :is-disabled="!isConnected || isAiThinking" @send="handleSendMessage" />
  </div>
</template>
