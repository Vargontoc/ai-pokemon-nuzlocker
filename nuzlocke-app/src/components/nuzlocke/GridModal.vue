<script setup lang="ts">

const props = defineProps<{
  show: boolean
  title: string
  isLoading?: boolean
}>()

const emit = defineEmits(['close'])

const handleClose = () => {
  emit('close')
}
</script>

<template>
  <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center p-4">
    <!-- Overlay backdrop -->
    <div 
      class="absolute inset-0 bg-gray-900/40 dark:bg-black/60 backdrop-blur-sm transition-opacity"
      @click="handleClose"
    ></div>

    <!-- Modal body -->
    <div class="relative bg-white dark:bg-gray-800 rounded-2xl shadow-xl w-full max-w-4xl max-h-[85vh] flex flex-col overflow-hidden animate-in fade-in zoom-in duration-200">
      
      <!-- Header -->
      <header class="px-6 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between bg-gray-50/50 dark:bg-gray-800/50">
        <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 flex items-center gap-3">
          <slot name="icon"></slot>
          {{ title }}
        </h2>
        <button 
          @click="handleClose"
          class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full transition-colors"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </header>

      <!-- Content Area with Grid Loading State -->
      <main class="flex-1 overflow-y-auto p-6">
        
        <!-- Loading Spinner -->
        <div v-if="isLoading" class="flex flex-col items-center justify-center py-12 text-gray-500 dark:text-gray-400">
          <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mb-4"></div>
          <p>Consultando base de datos neural...</p>
        </div>

        <!-- Dynamic Grid Slot -->
        <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
           <slot></slot>
        </div>

      </main>
      
    </div>
  </div>
</template>
