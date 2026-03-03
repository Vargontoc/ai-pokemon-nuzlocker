<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue3-toastify'
import { nuzlockeService, type NuzlockeSessionInfo } from '../services/nuzlockeService'
import { useNuzlockeSocket } from '../composables/useNuzlockeSocket'

const route = useRoute()
const router = useRouter()
const sessionId = route.params.id as string

const session = ref<NuzlockeSessionInfo | null>(null)
const isLoading = ref(true)

const { isConnected } = useNuzlockeSocket(sessionId)

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

onMounted(() => {
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
})
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
          {{ isConnected ? 'AI Online' : 'AI Offline' }}
        </span>
      </div>

      <div v-if="session" class="text-sm px-3 py-1 bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300 rounded-full font-medium ml-4">
        ID: {{ session.id }}
      </div>
    </header>

    <!-- Base Layout (Left Render, Right Chat/Tools) -->
    <main class="flex flex-1 overflow-hidden">
      
      <!-- Lado Izquierdo: Pantalla de Juego Transparente -->
      <section 
        ref="containerRef"
        class="flex-1 flex flex-col pt-6 bg-gray-100 dark:bg-gray-800 items-center justify-center relative overflow-hidden"
      >
        
        <div v-if="isLoading" class="flex flex-col items-center z-20">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mb-4"></div>
          <p>Sincronizando Estado...</p>
        </div>
        
        <div v-else class="w-full h-full relative pointer-events-none">
          <!-- Este es el contenedor REDIMENSIONABLE CUSTOM con borde NEON -->
          <div 
            class="absolute border border-blue-400/50 bg-transparent animate-neon-pulse pointer-events-auto rounded"
            :style="boxStyles"
          >
            <!-- Overlay interior -->
            <div class="absolute inset-0 flex items-center justify-center pointer-events-none opacity-20 transition-opacity hover:opacity-0">
               <span class="text-xs uppercase tracking-widest text-blue-500 font-bold mix-blend-difference select-none">Game Window</span>
            </div>
            
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
        
      </section>

      <!-- Lado Derecho: Contenido Interactivo Placeholder -->
      <aside class="w-96 border-l border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 flex flex-col">
          <div class="p-6 h-full flex items-center justify-center text-center text-gray-500 dark:text-gray-400 border-4 border-dashed border-gray-300 dark:border-gray-700 m-4 rounded-xl">
             <p class="text-lg">Próximamente:<br/>Chat Interactiva de IA</p>
          </div>
      </aside>

    </main>
  </div>
</template>
