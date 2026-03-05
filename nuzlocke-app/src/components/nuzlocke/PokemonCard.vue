<script setup lang="ts">
defineProps<{
  species: string
  nickname?: string | null
  level?: number
  isDead?: boolean
}>()
</script>

<template>
  <div 
    class="flex flex-col items-center p-3 rounded-xl border transition-all shadow-sm hover:shadow-md relative overflow-hidden group"
    :class="isDead ? 'bg-red-50/50 dark:bg-red-900/10 border-red-200 dark:border-red-800/50' : 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-700'"
  >
    
    <!-- Level Badge -->
    <div v-if="level" class="absolute top-2 left-2 px-1.5 py-0.5 rounded text-[10px] font-bold z-10"
         :class="isDead ? 'bg-red-600 text-white' : 'bg-blue-600 text-white dark:bg-blue-500'">
      Lv.{{ level }}
    </div>

    <!-- Dead Indicator Overlay -->
    <div v-if="isDead" class="absolute right-2 top-2 text-red-500 dark:text-red-400 opacity-60">
      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
    </div>

    <!-- Sprite Placeholder (Uses PKAPI dynamically or generic ball) -->
    <div class="h-20 w-20 mt-2 mb-2 flex items-center justify-center">
      <img :src="`https://img.pokemondb.net/sprites/home/normal/${species.toLowerCase()}.png`" 
           :alt="species"
           @error="($event.target as HTMLImageElement).src = 'https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/poke-ball.png'"
           class="max-w-full max-h-full object-contain drop-shadow transition-transform group-hover:scale-110" 
           :class="{ 'grayscale opacity-75': isDead }" />
    </div>

    <!-- Info -->
    <div class="text-center w-full">
      <h3 class="text-xs font-bold uppercase tracking-wider text-gray-500 dark:text-gray-400 truncate w-full" :title="species">
        {{ species }}
      </h3>
      <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 truncate w-full mt-0.5" :title="nickname || species">
        {{ nickname || species }}
      </p>
    </div>

  </div>
</template>
