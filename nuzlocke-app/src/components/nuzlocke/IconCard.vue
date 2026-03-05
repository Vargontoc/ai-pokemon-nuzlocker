<script setup lang="ts">
defineProps<{
  name: string
  quantity?: number
  icon?: string // Opcional, reservado para SVG/URL
}>()
</script>

<template>
  <div class="flex flex-col items-center justify-center p-4 bg-gray-50 dark:bg-gray-700/50 hover:bg-white dark:hover:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-xl transition-all shadow-sm hover:shadow-md group">
    
    <div class="w-16 h-16 mb-3 rounded-full bg-blue-100 dark:bg-gray-800 flex items-center justify-center text-blue-500 dark:text-gray-400 group-hover:scale-110 transition-transform">
      <img v-if="icon" :src="icon" :alt="name" class="w-10 h-10 object-contain" />
      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
      </svg>
    </div>
    
    <h3 class="text-sm font-semibold text-gray-800 dark:text-gray-200 text-center line-clamp-2 truncate w-full" :title="name">
      {{ name }}
    </h3>
    
    <span v-if="quantity !== undefined" class="mt-1 flex items-center justify-center px-2 py-0.5 rounded-full bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 text-xs font-bold font-mono">
      ×{{ quantity }}
    </span>

  </div>
</template>
