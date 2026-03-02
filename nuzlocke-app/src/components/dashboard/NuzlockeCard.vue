<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { NuzlockeStatus, type NuzlockeStatusType } from '../../services/nuzlockeService'

const props = defineProps<{
  id: string
  name: string
  path: string
  createdAt: string
  status?: NuzlockeStatusType
}>()

const { t } = useI18n({ useScope: 'global' })

const emit = defineEmits<{
  (e: 'enter', id: string): void
  (e: 'delete', id: string): void
}>()

// Formateamos la fecha a un string mas comprensible (DD/MM/YYYY)
const formattedDate = computed(() => {
  if (!props.createdAt) return t('card.unknownDate')
  const dateObj = new Date(props.createdAt)
  return new Intl.DateTimeFormat(navigator.language).format(dateObj)
})

const getStatusDetails = computed(() => {
  switch (props.status) {
    case NuzlockeStatus.Active: return { text: t('status.active'), colorClass: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300' }
    case NuzlockeStatus.Completed: return { text: t('status.completed'), colorClass: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300' }
    case NuzlockeStatus.Failed: return { text: t('status.failed'), colorClass: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300' }
    case NuzlockeStatus.Archived: return { text: t('status.archived'), colorClass: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300' }
    default: return { text: t('status.active'), colorClass: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300' } // Fallback por defecto si no viene
  }
})

const onEnter = () => emit('enter', props.id)
const onDelete = () => emit('delete', props.id)
</script>

<template>
  <div class="flex flex-col bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 font-sans shadow flex-grow rounded-lg overflow-hidden hover:shadow-lg transition-shadow duration-300 group">
    
    <!-- Decoración visual (Banner superior) y Título -->
    <div class="h-24 bg-gradient-to-r from-blue-600 to-indigo-700 dark:from-blue-800 dark:to-indigo-950 relative flex items-center justify-between px-5 pt-2 pb-6">
      
      <!-- Título Céntrico en el banner -->
      <h3 class="text-xl font-bold text-white truncate drop-shadow-md z-10 flex-grow pr-4">
        {{ name || t('card.unnamed') }}
      </h3>

      <!-- Status Badge -->
      <span :class="['px-2.5 py-0.5 rounded-full text-xs font-semibold uppercase tracking-wide border shadow-sm z-10', getStatusDetails.colorClass]">
        {{ getStatusDetails.text }}
      </span>

      <!-- Icono inferior colapsado sobre el contenedor interior -->
      <div class="absolute -bottom-6 left-5 bg-white dark:bg-gray-800 border-4 border-white dark:border-gray-800 p-1.5 rounded-full shadow-md text-2xl z-20">
        🎮 
      </div>
    </div>

    <!-- Contenido Textual de la tarjeta -->
    <div class="px-5 pb-5 pt-8 flex flex-col flex-grow">
      <!-- Los iconos se enmarcan mejor con espaciado -->
      <div class="text-sm text-gray-600 dark:text-gray-400 mb-6 space-y-2 font-medium">
        <p class="flex items-center">
            <span class="mr-2">📁</span>
            <span class="truncate">{{ path }}</span>
        </p>
        <p class="flex items-center">
            <span class="mr-2">📅</span>
            <span>{{ t('card.startedAt') }} {{ formattedDate }}</span>
        </p>
      </div>

      <!-- Footer y Acciones -->
      <div class="mt-auto flex gap-2 pt-4 border-t border-gray-100 dark:border-gray-700">
        <button 
          @click="onEnter" 
          class="flex-1 py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 transition font-medium text-center"
        >
          {{ t('card.enter') }}
        </button>
        <button 
          @click="onDelete" 
          class="py-2 px-3 text-red-500 hover:text-white bg-red-50 dark:bg-gray-700 hover:bg-red-500 rounded transition border border-red-200 dark:border-transparent dark:hover:border-red-500"
          :title="t('card.delete')"
        >
          🗑️
        </button>
      </div>
    </div>
  </div>
</template>
