<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import NuzlockeCard from '../components/dashboard/NuzlockeCard.vue'
import { nuzlockeService, type NuzlockeSessionInfo } from '../services/nuzlockeService'

const { t } = useI18n()

const sessions = ref<NuzlockeSessionInfo[]>([])
const isLoading = ref(true)
const showCreateModal = ref(false)

// Formularios
const newSessionName = ref('')
const newSessionPath = ref('')
const isCreating = ref(false)

const loadSessions = async () => {
  isLoading.value = true
  try {
    sessions.value = await nuzlockeService.getSessions()
  } catch (error) {
    console.error("Failed to load sessions:", error)
    // Se delega el error handling visible al interceptor de red o alertas futuras
  } finally {
    isLoading.value = false
  }
}

const handleCreate = async () => {
  if (!newSessionName.value || !newSessionPath.value) return
  isCreating.value = true
  try {
    const newSession = await nuzlockeService.createSession({
      name: newSessionName.value,
      directoryPath: newSessionPath.value
    })
    sessions.value.push(newSession)
    showCreateModal.value = false
    newSessionName.value = ''
    newSessionPath.value = ''
  } catch (error) {
    console.error("Failed to create session:", error)
  } finally {
    isCreating.value = false
  }
}

const handleDelete = async (id: string) => {
  if(!confirm('¿Estás seguro de que quieres borrar este Nuzlocke?')) return
  try {
    await nuzlockeService.deleteSession(id)
    sessions.value = sessions.value.filter(s => s.id !== id)
  } catch (error) {
    console.error("Failed to delete session:", error)
  }
}

const handleEnter = (id: string) => {
  console.log('Navegando al nuzlocke:', id)
  // TODO: Sprint posterior. Navegar a /nuzlocke/:id
  alert('Se conectará al dashboard del Nuzlocke: ' + id)
}

onMounted(() => {
  loadSessions()
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-3xl font-bold">{{ t('dashboard.title', 'Tus Nuzlockes') }}</h1>
      <button 
        @click="showCreateModal = true"
        class="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded shadow transition-colors flex items-center gap-2"
      >
        <span>+</span> Nuevo Nuzlocke
      </button>
    </div>

    <!-- Carga -->
    <div v-if="isLoading" class="flex justify-center items-center py-20">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="sessions.length === 0" class="text-center py-20 bg-white dark:bg-gray-800 rounded-lg shadow border border-gray-200 dark:border-gray-700">
      <h2 class="text-2xl font-semibold mb-2 text-gray-500 dark:text-gray-400">
        {{ t('dashboard.noNuzlockesFound', 'No tienes Nuzlockes activos') }}
      </h2>
      <p class="text-gray-500 mb-6">Crea uno nuevo para empezar tu aventura.</p>
      <button 
        @click="showCreateModal = true"
        class="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-6 rounded shadow transition-colors"
      >
        Empezar Nuzlocke
      </button>
    </div>

    <!-- Listado -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
      <NuzlockeCard 
        v-for="session in sessions" 
        :key="session.id"
        v-bind="session"
        @enter="handleEnter"
        @delete="handleDelete"
      />
    </div>

    <!-- Modal de Creación -->
    <div v-if="showCreateModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md shadow-xl border border-gray-200 dark:border-gray-700">
        <h2 class="text-2xl font-bold mb-4">Nuevo Nuzlocke</h2>
        
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1">Nombre de la partida</label>
            <input 
              v-model="newSessionName" 
              type="text" 
              class="w-full border rounded px-3 py-2 bg-gray-50 dark:bg-gray-900 border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Ej. FireRed Gen 1"
            />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">Ruta del emulador/Cagapuntos</label>
            <input 
              v-model="newSessionPath" 
              type="text" 
              class="w-full border rounded px-3 py-2 bg-gray-50 dark:bg-gray-900 border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="C:/saves/firered"
            />
          </div>
        </div>

        <div class="flex justify-end gap-3 mt-6">
          <button 
            @click="showCreateModal = false"
            class="px-4 py-2 text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white transition"
          >
            Cancelar
          </button>
          <button 
            @click="handleCreate"
            :disabled="!newSessionName || !newSessionPath || isCreating"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white rounded shadow transition"
          >
            {{ isCreating ? 'Creando...' : 'Crear Sesión' }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>
