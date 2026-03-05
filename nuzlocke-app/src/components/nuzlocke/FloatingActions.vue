<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits(['open:inventory', 'open:pc', 'open:graveyard'])

const tooltip = ref('')
const isOpen = ref(false)

const actions = [
  { id: 'inventory', icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10', name: 'Inventario' },
  { id: 'pc', icon: 'M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z', name: 'Pokémon PC' },
  { id: 'graveyard', icon: 'M12 4v16m8-8H4', name: 'Cementerio' }, // Simplified cross/grave icon
]

const handleClick = (id: string) => {
  emit(`open:${id}` as any)
  isOpen.value = false // Cerrar menú tras abrir el modal
}
</script>

<template>
  <div class="absolute -right-16 top-4 flex flex-col gap-3 z-40">
    
    <!-- Botón Toggle Principal -->
    <button 
      @click="isOpen = !isOpen"
      class="w-12 h-12 flex items-center justify-center rounded-xl bg-blue-600 hover:bg-blue-500 text-white shadow-lg transition-transform hover:scale-105"
      :class="{ 'ring-2 ring-blue-300 dark:ring-blue-800': isOpen }"
      title="Opciones Nuzlocke"
    >
      <svg v-if="!isOpen" xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
      </svg>
      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
      </svg>
    </button>

    <!-- Sub-menú Colapsable -->
    <div 
      class="flex flex-col gap-3 transition-all duration-300 origin-top bg-white/40 dark:bg-gray-800/80 backdrop-blur-sm p-2 rounded-2xl border border-gray-200/50 dark:border-gray-700/50 shadow-lg"
      :class="isOpen ? 'scale-y-100 opacity-100' : 'scale-y-0 opacity-0 pointer-events-none absolute top-14'"
    >
      <button 
        v-for="action in actions" 
        :key="action.id"
        @click="handleClick(action.id)"
        @mouseenter="tooltip = action.name"
        @mouseleave="tooltip = ''"
        class="group relative w-10 h-10 flex items-center justify-center rounded-xl bg-white dark:bg-gray-700 hover:bg-blue-50 dark:hover:bg-blue-900/50 text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-all duration-200 shadow-sm hover:shadow"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" :d="action.icon" />
        </svg>
        
        <!-- Tooltip -->
        <span 
          class="absolute right-full mr-4 px-2 py-1 bg-gray-900 text-white text-xs whitespace-nowrap rounded opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none"
        >
          {{ action.name }}
        </span>
      </button>
    </div>

  </div>
</template>
