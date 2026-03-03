<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { toast } from 'vue3-toastify'
import 'vue3-toastify/dist/index.css'
import NuzlockeCard from '../components/dashboard/NuzlockeCard.vue'
import DeleteConfirmModal from '../components/common/DeleteConfirmModal.vue'
import { nuzlockeService, type NuzlockeSessionInfo } from '../services/nuzlockeService'

const { t } = useI18n({ useScope: 'global' })
const router = useRouter()

const nuzlockes = ref<NuzlockeSessionInfo[]>([])
const isLoading = ref(true)
const showCreateModal = ref(false)

// Estado del Modal de Borrado
const showDeleteConfirmModal = ref(false)
const sessionToDelete = ref<string | null>(null)

// Formularios
const newNuzlockeName = ref('')
const newDescriptionNuzlocke = ref('')
const isCreating = ref(false)

const loadSessions = async () => {
  isLoading.value = true
  try {
    nuzlockes.value = await nuzlockeService.getSessions()
  } catch (error) {
    console.error("Failed to load sessions:", error)
    toast.error(t('dashboard.alerts.loadError'))
  } finally {
    isLoading.value = false
  }
}

const handleCreate = async () => {
  if (!newNuzlockeName.value || !newDescriptionNuzlocke.value) return
  isCreating.value = true
  try {
    const newSession = await nuzlockeService.createSession({
      name: newNuzlockeName.value,
      directoryPath: newDescriptionNuzlocke.value
    })
    nuzlockes.value.push(newSession)
    showCreateModal.value = false
    newNuzlockeName.value = ''
    newDescriptionNuzlocke.value = ''
    toast.success(t('dashboard.alerts.createSuccess'))
  } catch (error) {
    console.error("Failed to create session:", error)
    toast.error(t('dashboard.alerts.createError'))
  } finally {
    isCreating.value = false
  }
}

// Abre el modal de borrado
const promptDelete = (id: string) => {
  sessionToDelete.value = id
  showDeleteConfirmModal.value = true
}

const handleDelete = async () => {
  if(!sessionToDelete.value) return
  try {
    await nuzlockeService.deleteSession(sessionToDelete.value)
    nuzlockes.value = nuzlockes.value.filter(s => s.id !== sessionToDelete.value)
    toast.info(t('dashboard.alerts.deleteSuccess'))
  } catch (error) {
    console.error("Failed to delete session:", error)
    toast.error(t('dashboard.alerts.deleteError'))
  } finally {
    showDeleteConfirmModal.value = false
    sessionToDelete.value = null
  }
}

const handleEnter = (id: string) => {
  router.push({ name: 'nuzlocke', params: { id } })
}

onMounted(() => {
  loadSessions()
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-3xl font-bold">{{ t('dashboard.title') }}</h1>
      <button 
        @click="showCreateModal = true"
        class="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded shadow transition-colors flex items-center gap-2"
      >
        <span>+</span> {{ t('dashboard.newNuzlocke') }}
      </button>
    </div>

    <!-- Carga -->
    <div v-if="isLoading" class="flex justify-center items-center py-20">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="nuzlockes.length === 0" class="text-center py-20 bg-white dark:bg-gray-800 rounded-lg shadow border border-gray-200 dark:border-gray-700">
      <h2 class="text-2xl font-semibold mb-2 text-gray-500 dark:text-gray-400">
        {{ t('dashboard.noNuzlockesFound') }}
      </h2>
      <button 
        @click="showCreateModal = true"
        class="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-6 rounded shadow transition-colors mt-6"
      >
        {{ t('dashboard.startNuzlocke') }}
      </button>
    </div>

    <!-- Listado -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      <NuzlockeCard 
        v-for="session in nuzlockes" 
        :key="session.id"
        v-bind="session"
        @enter="handleEnter"
        @delete="promptDelete"
      />
    </div>

    <!-- Modal de Creación -->
    <div v-if="showCreateModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md shadow-xl border border-gray-200 dark:border-gray-700">
        <h2 class="text-2xl font-bold mb-4">{{ t('dashboard.newNuzlocke') }}</h2>
        
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('dashboard.form.nameLabel') }}</label>
            <input 
              v-model="newNuzlockeName"
              min="1"
              max="50"
              type="text" 
              class="w-full border rounded px-3 py-2 bg-gray-50 dark:bg-gray-900 border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
              :placeholder="t('dashboard.form.namePlaceholder')"
            />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">{{ t('dashboard.form.pathLabel') }}</label>
            <input 
              v-model="newDescriptionNuzlocke"
              max="100"
              type="text" 
              class="w-full border rounded px-3 py-2 bg-gray-50 dark:bg-gray-900 border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
              :placeholder="t('dashboard.form.pathPlaceholder')"
            />
          </div>
        </div>

        <div class="flex justify-end gap-3 mt-6">
          <button 
            @click="showCreateModal = false"
            class="px-4 py-2 text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white transition"
          >
            {{ t('dashboard.cancel') }}
          </button>
          <button 
            @click="handleCreate"
            :disabled="!newNuzlockeName || !newDescriptionNuzlocke || isCreating"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white rounded shadow transition"
          >
            {{ isCreating ? t('dashboard.creating') : t('dashboard.createSession') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Confirmación Borrado Modal -->
    <DeleteConfirmModal 
      v-if="showDeleteConfirmModal"
      :item-name="nuzlockes.find(s => s.id === sessionToDelete)?.name"
      @confirm="handleDelete"
      @cancel="showDeleteConfirmModal = false; sessionToDelete = null;"
    />

  </div>
</template>
