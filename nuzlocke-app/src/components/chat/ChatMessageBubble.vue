<script setup lang="ts">
import { computed } from 'vue'
import type { ChatMessage } from '../../composables/useNuzlockeSocket'

const props = defineProps<{
  msg: ChatMessage
}>()

const isUser = computed(() => props.msg.sender === 'user')
const isSystem = computed(() => props.msg.sender === 'system')

// --- Estilos para la Burbuja de Chat Tradicional ---
const containerClasses = computed(() => {
    if (isSystem.value) return 'mx-auto max-w-[90%] text-center italic opacity-80'
    if (isUser.value) return 'self-end bg-blue-600 text-white ml-auto max-w-[85%] rounded-2xl rounded-tr-sm p-3 shadow-sm'
    
    // IA Base Box Classes
    return 'self-start bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 border border-gray-100 dark:border-gray-700 mr-auto max-w-[85%] rounded-2xl rounded-tl-sm p-3 shadow-md'
})

// --- Estilos Polimórficos para Eventos Nuzlocke (WorkflowEventMessage) ---
const workflowEventClasses = computed(() => {
    const id = props.msg.eventId?.toLowerCase() || ''
    
    if (id.includes('battle_start')) return 'bg-red-50 border-red-200 text-red-900 dark:bg-red-900/40 dark:border-red-800 dark:text-red-100 ring-1 ring-red-500/50'
    if (id.includes('pokemon_fainted') || id.includes('death')) return 'bg-gray-100 border-gray-300 text-gray-800 dark:bg-gray-800 dark:border-gray-600 dark:text-gray-200 shadow-inner opacity-90 grayscale'
    if (id.includes('pokemon_caught') || id.includes('capture')) return 'bg-green-50 border-green-200 text-green-900 dark:bg-green-900/40 dark:border-green-800 dark:text-green-100 ring-1 ring-green-500/50'
    if (id.includes('level_up') || id.includes('evolution')) return 'bg-yellow-50 border-yellow-200 text-yellow-900 dark:bg-yellow-900/40 dark:border-yellow-800 dark:text-yellow-100 shadow-[0_0_10px_rgba(250,204,21,0.2)]'
    if (id.includes('route_changed')) return 'bg-blue-50 border-blue-200 text-blue-900 dark:bg-blue-900/40 dark:border-blue-800 dark:text-blue-100'
    
    // Default
    return 'bg-gray-50 border-gray-200 text-gray-800 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-200'
})

const workflowEventIcon = computed(() => {
    const id = props.msg.eventId?.toLowerCase() || ''
    if (id.includes('battle_start')) return `<svg class="w-4 h-4 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>`
    if (id.includes('pokemon_fainted')) return `<svg class="w-4 h-4 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>`
    if (id.includes('pokemon_caught')) return `<svg class="w-4 h-4 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>`
    if (id.includes('route_changed')) return `<svg class="w-4 h-4 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" /></svg>`
    
    return `<svg class="w-4 h-4 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>`
})
</script>

<template>
  <!-- Renderizado Micro-Chip para Tool Calls del Agente (Consultando Base de datos, PokeAPI, etc) -->
  <div v-if="msg.type === 'agent_tool_call'" class="flex items-center justify-center my-2 opacity-60 text-xs text-gray-500">
      <svg class="animate-spin -ml-1 mr-2 h-3 w-3 text-gray-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
      </svg>
      <span>Consultando {{ msg.toolName }}...</span>
  </div>

  <!-- Renderizado Customizado para Hitos de Juego de Nuzlocke (WorkflowEvents) -->
  <div v-else-if="msg.type === 'workflow_event'" class="w-full flex justify-center my-3 text-sm">
      <div class="px-4 py-3 rounded-xl shadow-sm border max-w-[90%] w-full transition-colors" :class="workflowEventClasses">
          <div class="flex items-center gap-2 mb-1">
              <span v-html="workflowEventIcon" />
              <span class="font-extrabold uppercase tracking-wider text-xs">{{ msg.eventId?.replace(/_/g, ' ') }}</span>
          </div>
          <div class="whitespace-pre-wrap break-words opacity-90 text-sm mt-1 mb-2">{{ msg.content }}</div>
          
          <!-- Key-Values de Cambios de Estado si llegaron anexados al evento -->
          <div v-if="msg.stateChanges && Object.keys(msg.stateChanges).length > 0" class="mt-2 text-xs opacity-80 border-t border-black/10 dark:border-white/10 pt-2 flex flex-col gap-1">
             <div v-for="(val, key) in msg.stateChanges" :key="key" class="flex justify-between items-center bg-black/5 dark:bg-white/5 px-2 py-1 rounded">
                 <span class="font-semibold">{{ key }}</span>
                 <span class="font-mono">{{ val }}</span>
             </div>
          </div>
      </div>
  </div>

  <!-- Renderizado Estándar: Conversación de Chat (Usuario y Maestro IA) -->
  <div v-else class="flex flex-col mb-2 text-sm leading-relaxed antialiased relative" :class="containerClasses">
      <div class="whitespace-pre-wrap break-words" v-html="props.msg.content"></div>
      <span class="text-[0.65rem] opacity-50 block text-right mt-1 w-full" :class="isUser ? 'text-blue-100' : 'text-gray-400'">
         {{ props.msg.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }}
      </span>
  </div>
</template>
