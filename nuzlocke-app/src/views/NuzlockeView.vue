<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue3-toastify'
import { nuzlockeService, type NuzlockeSessionInfo } from '../services/nuzlockeService'
import { useNuzlockeSocket } from '../composables/useNuzlockeSocket'
import FloatingActions from '../components/nuzlocke/FloatingActions.vue'
import type { TeamMember } from '../services/nuzlockeService'

const route = useRoute()
const router = useRouter()
const sessionId = route.params.id as string

const session = ref<NuzlockeSessionInfo | null>(null)
const isLoading = ref(true)

const { isConnected, connect, disconnect } = useNuzlockeSocket()

const loadSession = async () => {
  try {
    session.value = await nuzlockeService.getSessionById(sessionId)
  } catch (error) {
    console.error("Failed to load nuzlocke session details:", error)
    toast.error('No se pudo cargar la partida desde la Base de Datos.')
    router.push({ name: 'dashboard' })
  } finally {
    isLoading.value = false
  }
}

const goBack = () => router.push({ name: 'dashboard' })

// Custom Resize Logic
const containerRef = ref<HTMLElement | null>(null)
const boxStyles = ref({
  width: '640px',
  height: '480px',
  left: '0px',
  top: '0px'
})

let activeDrag = ''
let startX = 0
let startY = 0
let startRect = { w: 0, h: 0, x: 0, y: 0 }

const startResize = (direction: string, e: MouseEvent) => {
  e.preventDefault()
  activeDrag = direction
  startX = e.clientX
  startY = e.clientY
  startRect = {
    w: parseInt(boxStyles.value.width),
    h: parseInt(boxStyles.value.height),
    x: parseInt(boxStyles.value.left),
    y: parseInt(boxStyles.value.top)
  }
  
  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
}

const onMouseMove = (e: MouseEvent) => {
  const dx = e.clientX - startX
  const dy = e.clientY - startY
  
  let { w, h, x, y } = startRect
  
  if (activeDrag.includes('e')) w += dx
  if (activeDrag.includes('s')) h += dy
  if (activeDrag.includes('w')) {
    w -= dx
    x += dx
  }
  if (activeDrag.includes('n')) {
    h -= dy
    y += dy
  }
  
  // Limites min-width min-height
  if (w < 320) {
    if (activeDrag.includes('w')) x -= (320 - w)
    w = 320
  }
  if (h < 240) {
    if (activeDrag.includes('n')) y -= (240 - h)
    h = 240
  }
  
  boxStyles.value = {
    width: `${w}px`,
    height: `${h}px`,
    left: `${x}px`,
    top: `${y}px`
  }
}

const onMouseUp = () => {
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('mouseup', onMouseUp)
}

// --- Split Resizer Logic ---
const rightPanelWidth = ref(384) // 96 * 4 = 384px (w-96)
let isDraggingSplit = false
let splitStartX = 0
let splitStartWidth = 0

const startSplitDrag = (e: MouseEvent) => {
  e.preventDefault()
  isDraggingSplit = true
  splitStartX = e.clientX
  splitStartWidth = rightPanelWidth.value
  
  window.addEventListener('mousemove', onSplitDrag)
  window.addEventListener('mouseup', onSplitDragEnd)
}

const onSplitDrag = (e: MouseEvent) => {
  if (!isDraggingSplit) return
  // dx negativo significa que raton va a izquierda, aumentando el panel derecho
  const dx = splitStartX - e.clientX
  const newWidth = Math.max(250, Math.min(800, splitStartWidth + dx))
  rightPanelWidth.value = newWidth
}

const onSplitDragEnd = () => {
  isDraggingSplit = false
  window.removeEventListener('mousemove', onSplitDrag)
  window.removeEventListener('mouseup', onSplitDragEnd)
}

// --- Pokemon Party Fetching ---
const activeParty = ref<TeamMember[]>([])

watch(() => isConnected.value, async (connected) => {
  if (connected) {
    try {
      console.log('Fetching active Party array from server...')
      activeParty.value = await nuzlockeService.getActiveParty(sessionId)
    } catch (err) {
      console.error('Failed to fetch active party:', err)
      activeParty.value = [] // Forzará a cargar círculos grises vacíos
    }
  }
}, { immediate: true })

onMounted(() => {
  connect(sessionId)
  loadSession()
  setTimeout(() => {
    // Centramos el recuadro cuando se monta el DOM
    if (containerRef.value) {
      const { clientWidth, clientHeight } = containerRef.value
      boxStyles.value.left = `${(clientWidth - 640) / 2}px`
      boxStyles.value.top = `${(clientHeight - 480) / 2}px`
    }
  }, 100)
})

onUnmounted(() => {
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('mouseup', onMouseUp)
  window.removeEventListener('mousemove', onSplitDrag)
  window.removeEventListener('mouseup', onSplitDragEnd)
  disconnect()
})
// --- Modals y Fetching API Logic ---
import GridModal from '../components/nuzlocke/GridModal.vue'
import IconCard from '../components/nuzlocke/IconCard.vue'
import PokemonCard from '../components/nuzlocke/PokemonCard.vue'
import PartyBar from '../components/nuzlocke/PartyBar.vue'
import ChatPanel from '../components/chat/ChatPanel.vue'

const isModalOpen = ref(false)
const modalTitle = ref('')
const isModalLoading = ref(false)
const modalType = ref<'inventory' | 'pc' | 'graveyard'>('inventory')

// Array polimórfico para reusar el slot del modal
const modalData = ref<any[]>([])

const handleOpenModal = async (type: 'inventory' | 'pc' | 'graveyard') => {
  modalType.value = type
  isModalOpen.value = true
  isModalLoading.value = true
  modalData.value = [] // Reset old data
  
  try {
    if (type === 'inventory') {
      modalTitle.value = 'Mi Mochila'
      modalData.value = await nuzlockeService.getInventory(sessionId)
    } else if (type === 'pc') {
      modalTitle.value = 'Sistema de Almacenamiento Pokémon'
      modalData.value = await nuzlockeService.getPC(sessionId)
    } else if (type === 'graveyard') {
      modalTitle.value = 'Cementerio de Caídos'
      modalData.value = await nuzlockeService.getGraveyard(sessionId)
    }
  } catch (err) {
    console.error(`Error al cargar ${type}:`, err)
    toast.error(`Error de red al consultar la Base de Datos (${type})`)
  } finally {
    isModalLoading.value = false
  }
}

</script>

<template>
  <div class="h-screen max-w-[100vw] overflow-hidden bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex flex-col">
    <!-- Header temporal a la espera del Layout -->
    <header class="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 z-10 shrink-0">
      <button 
        @click="goBack"
        class="bg-gray-200 hover:bg-gray-300 dark:bg-gray-800 dark:hover:bg-gray-700 p-2 rounded-full transition-colors"
        title="Volver al Panel"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
      </button>
      <h1 class="text-2xl font-bold flex-grow">
        <span v-if="isLoading" class="animate-pulse bg-gray-300 dark:bg-gray-700 h-8 w-48 rounded block"></span>
        <span v-else>{{ session?.name || 'Cargando Nuzlocke...' }}</span>
      </h1>
      
      <!-- WS Status Badge -->
      <div class="flex items-center gap-2">
        <span class="relative flex h-3 w-3">
          <span v-if="isConnected" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
          <span :class="['relative inline-flex rounded-full h-3 w-3', isConnected ? 'bg-green-500' : 'bg-red-500']"></span>
        </span>
        <span class="text-sm font-semibold text-gray-700 dark:text-gray-300">
          {{ isConnected ? 'Server Online' : 'Server Offline' }}
        </span>
      </div>
    </header>

    <!-- Base Layout (Left Render, Right Chat/Tools) -->
    <main class="flex flex-1 overflow-hidden">
      
      <!-- Lado Izquierdo: Pantalla de Juego Transparente -->
      <section 
        ref="containerRef"
        class="flex-1 flex flex-col pt-6 bg-gray-100 dark:bg-gray-800 items-center justify-center relative overflow-hidden"
      >
        
        <div v-if="isLoading" class="flex flex-col items-center z-20 flex-1 justify-center">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mb-4"></div>
          <p>Sincronizando Estado...</p>
        </div>
        
        <div v-else class="w-full flex-1 relative pointer-events-none flex items-center justify-center">
          
          <!-- Este es el contenedor REDIMENSIONABLE CUSTOM con borde NEON -->
          <div 
            class="absolute border border-blue-400/50 bg-transparent animate-neon-pulse pointer-events-auto rounded"
            :style="boxStyles"
          >
            <!-- Overlay interior -->
            <div class="absolute inset-0 flex items-center justify-center pointer-events-none opacity-20 transition-opacity hover:opacity-0">
               <span class="text-xs uppercase tracking-widest text-blue-500 font-bold mix-blend-difference select-none">Game Window</span>
            </div>
            
            <!-- Botonera Desplegable Anclada al Marco -->
            <FloatingActions 
              @open:inventory="handleOpenModal('inventory')"
              @open:pc="handleOpenModal('pc')"
              @open:graveyard="handleOpenModal('graveyard')"
            />

            
            <!-- 8 Puntos de Resize / Handles Custom -->
            <div class="absolute inset-x-0 top-0 h-2 cursor-n-resize hover:bg-white/10" @mousedown="startResize('n', $event)"></div>
            <div class="absolute inset-x-0 bottom-0 h-2 cursor-s-resize hover:bg-white/10 z-10" @mousedown="startResize('s', $event)"></div>
            <div class="absolute inset-y-0 left-0 w-2 cursor-w-resize hover:bg-white/10" @mousedown="startResize('w', $event)"></div>
            <div class="absolute inset-y-0 right-0 w-2 cursor-e-resize hover:bg-white/10 z-10" @mousedown="startResize('e', $event)"></div>
            
            <div class="absolute top-0 left-0 w-3 h-3 cursor-nw-resize hover:bg-white/20 z-20" @mousedown.stop="startResize('nw', $event)"></div>
            <div class="absolute top-0 right-0 w-3 h-3 cursor-ne-resize hover:bg-white/20 z-20" @mousedown.stop="startResize('ne', $event)"></div>
            <div class="absolute bottom-0 left-0 w-3 h-3 cursor-sw-resize hover:bg-white/20 z-20" @mousedown.stop="startResize('sw', $event)"></div>
            <div class="absolute bottom-0 right-0 w-3 h-3 cursor-se-resize hover:bg-white/20 z-20" @mousedown.stop="startResize('se', $event)"></div>
          </div>
        </div>
        
        <!-- Bottom Bar: Equipo Activo (Party) -->
        <div v-if="!isLoading" class="w-full shrink-0 flex items-end">
           <PartyBar :party="activeParty" class="pointer-events-auto shadow-[0_-4px_10px_-4px_rgba(0,0,0,0.1)]" />
        </div>
        
      </section>

      <!-- Grid Modal Instance -->
      <GridModal 
        :show="isModalOpen" 
        :title="modalTitle" 
        :is-loading="isModalLoading"
        @close="isModalOpen = false"
      >
        <template #icon>
           <svg v-if="modalType === 'inventory'" xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" /></svg>
           <svg v-else-if="modalType === 'pc'" xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
           <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
        </template>
        
        <!-- Renderizado dinámico dependiendo de la respuesta de Types -->
        <template v-if="modalType === 'inventory'">
           <IconCard 
              v-for="item in modalData" 
              :key="item.name" 
              :name="item.name" 
              :quantity="item.quantity" 
            />
        </template>
        <template v-else>
           <PokemonCard 
             v-for="(pkmn, idx) in modalData" 
             :key="idx"
             :species="pkmn.species"
             :nickname="pkmn.nickname"
             :level="pkmn.level"
             :isDead="modalType === 'graveyard'"
           />
        </template>
      </GridModal>

      <!-- Divisor Arrastrable Vertical -->
      <div 
        class="w-1 bg-gray-300 dark:bg-gray-700 hover:bg-blue-500 cursor-col-resize z-30 transition-colors"
        @mousedown="startSplitDrag"
      ></div>

      <!-- Lado Derecho: Contenido Interactivo y Chat AI -->
      <aside 
        class="bg-white dark:bg-gray-900 flex flex-col transition-all duration-0 ease-linear shrink-0 overflow-hidden shadow-[-4px_0_15px_-3px_rgba(0,0,0,0.1)] relative z-20"
        :style="{ width: `${rightPanelWidth}px` }"
      >
          <ChatPanel :session-id="sessionId" />
      </aside>

    </main>
  </div>
</template>
