<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{
  itemName?: string
}>()

const { t } = useI18n({ useScope: 'global' })

const emit = defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50">
    <div class="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-sm shadow-xl border border-gray-200 dark:border-gray-700 transform transition-all">
      <div class="flex items-center gap-3 mb-4 text-red-600 dark:text-red-400">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-8 h-8 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <h3 class="text-xl font-bold">{{ t('deleteModal.title') }}</h3>
      </div>
      
      <p class="text-gray-600 dark:text-gray-300 mb-6">
        {{ t('deleteModal.warning') }} <strong v-if="itemName" class="text-gray-900 dark:text-gray-100">{{ itemName }}</strong>. 
        {{ t('deleteModal.undone') }}
      </p>

      <div class="flex justify-end gap-3 mt-2">
        <button 
          @click="emit('cancel')"
          class="px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 dark:text-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 rounded transition font-medium"
        >
          {{ t('deleteModal.cancel') }}
        </button>
        <button 
          @click="emit('confirm')"
          class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded shadow transition font-medium"
        >
          {{ t('deleteModal.confirm') }}
        </button>
      </div>
    </div>
  </div>
</template>
