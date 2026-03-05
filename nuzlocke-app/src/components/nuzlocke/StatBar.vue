<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  label: string
  value: number
  max?: number
}>()

const computedMax = computed(() => props.max || 255) // Default max base stat is usually 255
const percentage = computed(() => Math.min(100, Math.max(0, (props.value / computedMax.value) * 100)))

const colorClass = computed(() => {
  if (percentage.value < 40) return 'bg-red-500' // Low
  if (percentage.value < 75) return 'bg-yellow-400' // Med
  return 'bg-green-500' // High
})
</script>

<template>
  <div class="flex items-center gap-2 text-xs w-full">
    <!-- Stat Label -->
    <span class="w-8 font-bold text-gray-600 dark:text-gray-300 uppercase tracking-tighter">{{ label }}</span>
    
    <!-- Bar Container -->
    <div class="flex-1 h-2.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden flex items-center shadow-inner">
      <!-- Fill -->
      <div 
        class="h-full transition-all duration-500 ease-out"
        :class="colorClass"
        :style="{ width: `${percentage}%` }"
      ></div>
    </div>
    
    <!-- Exact Value -->
    <span class="w-8 text-right font-mono text-gray-500 dark:text-gray-400">{{ value }}</span>
  </div>
</template>
