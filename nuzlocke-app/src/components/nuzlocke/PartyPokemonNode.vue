<script setup lang="ts">
import { computed } from 'vue'
import type { TeamMember } from '../../services/nuzlockeService'
import StatBar from './StatBar.vue'

const props = defineProps<{
  pokemon: TeamMember | null
}>()

// Determine if the node is empty
const isEmpty = computed(() => props.pokemon === null)

// Determine Status Colors for ring and effects
const borderClasses = computed(() => {
  if (isEmpty.value) return 'border-2 border-gray-400 dark:border-gray-500'
  
  const st = props.pokemon?.status?.toLowerCase() || ''
  if (st.includes('psn') || st.includes('poison')) return 'ring-4 ring-purple-500 animate-pulse'
  if (st.includes('brn') || st.includes('burn')) return 'ring-4 ring-red-500 animate-pulse'
  if (st.includes('par') || st.includes('paralyze')) return 'ring-4 ring-yellow-400 animate-bounce'
  if (st.includes('slp') || st.includes('sleep')) return 'ring-2 ring-blue-400'
  if (st.includes('frz') || st.includes('freeze')) return 'ring-2 ring-cyan-300'
  if (st.includes('fnt') || st.includes('fainted') || st.includes('dead')) return 'ring-2 ring-gray-600 dark:border-gray-600' // Fainted
  
  // Base circle styles for Pokemon
  return 'border-2 border-green-400 dark:border-green-600 hover:border-blue-500 transition-colors'
})

// Determine color tint overlay depending on status
const overlayClasses = computed(() => {
  if (isEmpty.value) return 'bg-gray-700/60 mix-blend-multiply'
  
  const st = props.pokemon?.status?.toLowerCase() || ''
  if (st.includes('psn') || st.includes('poison')) return 'bg-purple-600/40 mix-blend-multiply'
  if (st.includes('brn') || st.includes('burn')) return 'bg-red-500/40 mix-blend-multiply'
  if (st.includes('par') || st.includes('paralyze')) return 'bg-yellow-400/40 mix-blend-multiply'
  if (st.includes('slp') || st.includes('sleep')) return 'bg-blue-600/40 mix-blend-multiply'
  if (st.includes('frz') || st.includes('freeze')) return 'bg-cyan-300/40 mix-blend-multiply'
  if (st.includes('fnt') || st.includes('fainted') || st.includes('dead')) return 'bg-gray-800/70 mix-blend-multiply'
  
  return 'bg-transparent'
})



const imageClasses = computed(() => {
  const st = props.pokemon?.status?.toLowerCase() || ''
  if (st.includes('fnt') || st.includes('fainted') || st.includes('dead')) return 'grayscale opacity-50'
  return 'drop-shadow-md group-hover:scale-110 transition-transform'
})

// Mapeos limpios para la interfaz visual
const displayName = computed(() => props.pokemon?.nickname || props.pokemon?.species || '???')
const displaySpecies = computed(() => props.pokemon?.species || 'unknown')
const maxHp = computed(() => props.pokemon?.stats?.hpMax || props.pokemon?.stats?.hp || 1)
const currentHp = computed(() => props.pokemon?.stats?.hp || 0)

</script>

<template>
  <div class="relative flex flex-col items-center group cursor-default">
    <!-- Base Pokeball Node for BOTH Empty and Data -->
    <div class="w-20 h-20 sm:w-24 sm:h-24 md:w-28 md:h-28 rounded-full flex flex-col items-center justify-center p-2 sm:p-3 shadow-md relative overflow-hidden pokeball-node" :class="borderClasses">
       
       <!-- Status Overlay (Tint applied ON TOP of the Pokeball Canvas) -->
       <div class="absolute inset-0 z-10 transition-colors duration-300 pointer-events-none" :class="overlayClasses"></div>

       <!-- Empty State (Dash) -->
       <span v-if="isEmpty" class="z-20 relative text-4xl text-gray-800 dark:text-gray-300 font-black drop-shadow-md pb-1">-</span>

       <template v-else>
         <!-- The actual Pokemon Sprite -->
         <img :src="`https://img.pokemondb.net/sprites/home/normal/${displaySpecies.toLowerCase()}.png`" 
             :alt="displayName"
             class="max-w-full max-h-full object-contain relative z-20" :class="imageClasses"
             @error="($event.target as HTMLImageElement).src = 'https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/poke-ball.png'" />
         
         <!-- Status Badge (Micro Icon Overlap) -->
         <span v-if="pokemon?.status" class="absolute -bottom-1 -right-1 z-30 px-1.5 py-0.5 text-[9px] font-black uppercase rounded bg-gray-900 dark:bg-black text-white shadow">
           {{ pokemon.status }}
         </span>
       </template>
    </div>

    <!-- Under-label displayName -->
    <span class="mt-2 text-xs sm:text-sm font-semibold text-gray-800 dark:text-gray-200 max-w-[9rem] truncate text-center px-1" :title="displayName">
      {{ isEmpty ? '---' : displayName }}
    </span>

    <!-- ======================= -->
    <!-- HOVER MODAL (STATS BOX)  -->
    <!-- ======================= -->
    <div v-if="!isEmpty" class="absolute bottom-full mb-4 w-48 bg-white dark:bg-gray-800 rounded-xl shadow-2xl border border-gray-200 dark:border-gray-700 p-3 opacity-0 pointer-events-none scale-95 group-hover:opacity-100 group-hover:scale-100 group-hover:pointer-events-auto transition-all duration-200 z-50 origin-bottom">
      
      <!-- Top Header -->
      <div class="flex justify-between items-center mb-2 pb-2 border-b border-gray-100 dark:border-gray-700">
         <div class="flex flex-col">
            <span class="text-sm font-bold text-gray-800 dark:text-gray-100 truncate w-24" :title="displayName">{{ displayName }}</span>
            <span class="text-[10px] text-gray-500 dark:text-gray-400 capitalize">{{ displaySpecies }}</span>
         </div>
         <span class="px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 text-xs font-black rounded-lg">Lv.{{ pokemon?.level || '?' }}</span>
      </div>

      <!-- Health Bar -->
      <div class="mb-3">
        <div class="flex justify-between text-[10px] font-bold mb-1">
          <span class="text-gray-500">HP</span>
          <span :class="currentHp < (maxHp/2) ? 'text-red-500' : 'text-green-500'">{{ currentHp }} / {{ maxHp }}</span>
        </div>
        <div class="w-full h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
           <div class="h-full bg-green-500" :style="{ width: `${(currentHp / maxHp) * 100}%` }" />
        </div>
      </div>

      <!-- Stats Grid / Bars -->
      <div class="flex flex-col gap-1.5" v-if="pokemon?.stats">
        <StatBar label="ATK" :value="pokemon.stats.attack" />
        <StatBar label="DEF" :value="pokemon.stats.defense" />
        <StatBar label="SPA" :value="pokemon.stats.spAttack" />
        <StatBar label="SPD" :value="pokemon.stats.spDefense" />
        <StatBar label="SPE" :value="pokemon.stats.speed" />
      </div>

    </div>

  </div>
</template>

<style scoped>
/* Un único nodo que fuerza su propia estructura redonda a través de una máscara clip o un background sólido */
.pokeball-node {
  /* Fondo general que se vuelve el lienzo de la esfera */
  background: linear-gradient(
    to bottom,
    #ef4444 0%,
    #ef4444 47%,
    #000000 47%,
    #000000 53%,
    #ffffff 53%,
    #ffffff 100%
  );
  overflow: hidden;
  border-radius: 50% !important; /* Force priority over Tailwind */
  mask-image: radial-gradient(white, black); /* Fix para Webkit overflow bug en bordes redondeados */
  -webkit-mask-image: -webkit-radial-gradient(white, black);
  isolation: isolate; /* Crea un nuevo stacking context */
}

/* Círculo central negro de la Pokeball */
.pokeball-node::before {
  content: "";
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 25%;
  height: 25%;
  background-color: white;
  border: 4px solid black;
  border-radius: 50%;
  z-index: 1; /* Detrás de la capa overlay/sprite, pero delante del lienzo */
}

/* El botón interior blanco del centro del mecanismo */
.pokeball-node::after {
  content: "";
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 10%;
  height: 10%;
  background-color: white;
  border: 1px solid #777;
  border-radius: 50%;
  box-shadow: inset -1px -1px 2px rgba(0,0,0,0.2);
  z-index: 1; /* Al mismo nivel superponiendo al pseudo ::before */
}
</style>
