<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  id: string
  name: string
  path: string
  createdAt: string
}>()

const emit = defineEmits<{
  (e: 'enter', id: string): void
  (e: 'delete', id: string): void
}>()

// Formateamos la fecha a un string mas comprensible (DD/MM/YYYY)
const formattedDate = computed(() => {
  if (!props.createdAt) return 'Fecha desconocida'
  const dateObj = new Date(props.createdAt)
  return new Intl.DateTimeFormat(navigator.language).format(dateObj)
})

const onEnter = () => emit('enter', props.id)
const onDelete = () => emit('delete', props.id)
</script>

<template>
  <div class="flex flex-col bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow flex-grow rounded-lg overflow-hidden hover:shadow-lg transition-shadow duration-300 group">
    
    <!-- Decoración visual (Banner superior) -->
    <div class="h-20 bg-gradient-to-r from-blue-500 to-indigo-600 relative">
        <div class="absolute -bottom-6 left-4 bg-white dark:bg-gray-900 border-4 border-white dark:border-gray-900 p-2 rounded-full shadow-md text-2xl">
           🎮
        </div>
    </div>

    <!-- Contenido de la tarjeta -->
    <div class="p-5 mt-4 flex flex-col flex-grow">
      <h3 class="text-xl font-bold mb-2 text-gray-900 dark:text-white truncate">
        {{ name || 'Aventura sin nombre' }}
      </h3>
      
      <div class="text-sm text-gray-600 dark:text-gray-400 mb-4 space-y-1">
        <p class="flex items-center">
            <span class="mr-2">📁</span>
            <span class="truncate">{{ path }}</span>
        </p>
        <p class="flex items-center">
            <span class="mr-2">📅</span>
            <span>Iniciado el {{ formattedDate }}</span>
        </p>
      </div>

      <!-- Footer y Acciones -->
      <div class="mt-auto flex gap-2 pt-4 border-t border-gray-100 dark:border-gray-700">
        <button 
          @click="onEnter" 
          class="flex-1 py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 transition font-medium text-center"
        >
          Entrar a Jugar
        </button>
        <button 
          @click="onDelete" 
          class="py-2 px-3 text-red-500 hover:text-white bg-red-50 dark:bg-gray-700 hover:bg-red-500 rounded transition border border-red-200 dark:border-transparent dark:hover:border-red-500"
          title="Eliminar partida"
        >
          🗑️
        </button>
      </div>
    </div>
  </div>
</template>
