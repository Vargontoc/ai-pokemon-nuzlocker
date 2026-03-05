<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  isDisabled: boolean
}>()

const emit = defineEmits<{
  (e: 'send', content: string): void
}>()

const localMessage = ref('')

const doSend = () => {
    if (props.isDisabled || !localMessage.value.trim()) return
    emit('send', localMessage.value)
    localMessage.value = ''
}

const onKeyDown = (e: KeyboardEvent) => {
    // Enviar con Enter libre (sin Shift)
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault()
        doSend()
    }
}
</script>

<template>
  <div class="w-full shrink-0 p-3 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800 relative shadow-[0_-4px_6px_-2px_rgba(0,0,0,0.05)]">
      
      <!-- Overlay de Bloqueo Visual -->
      <div v-if="props.isDisabled" class="absolute inset-0 bg-gray-100/50 dark:bg-gray-900/60 z-20 flex items-center justify-center backdrop-blur-[1px] cursor-not-allowed">
      </div>

      <div class="flex items-end gap-2 relative z-10">
          <textarea 
            v-model="localMessage"
            @keydown="onKeyDown"
            rows="1"
            placeholder="Pregúntale a tu Maestro IA (Shift+Enter para salto de línea)..."
            class="no-scrollbar flex-1 max-h-32 min-h-[44px] bg-gray-50 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-xl px-4 py-2.5 outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-gray-800 dark:text-gray-100 resize-y shadow-inner text-sm transition-shadow disabled:opacity-50"
            :disabled="props.isDisabled"
          ></textarea>

          <button 
             @click="doSend"
             :disabled="props.isDisabled || !localMessage.trim()"
             class="h-11 w-11 shrink-0 bg-blue-600 hover:bg-blue-500 text-white rounded-xl shadow inline-flex items-center justify-center transition-colors disabled:opacity-50 disabled:bg-gray-400 disabled:cursor-not-allowed"
          >
             <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 ml-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" /></svg>
          </button>
      </div>
  </div>
</template>

<style scoped>
/* Ocultar barra de scroll y flechas (scrollbar pointers) para pulcritud visual */
.no-scrollbar {
    -ms-overflow-style: none;  /* IE and Edge */
    scrollbar-width: none;  /* Firefox */
}
.no-scrollbar::-webkit-scrollbar {
    display: none; /* Chrome, Safari and Opera */
}
</style>
