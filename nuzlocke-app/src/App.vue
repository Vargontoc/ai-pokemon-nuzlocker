<script setup lang="ts">
import { useTheme } from './composables/useTheme'
import { useI18n } from 'vue-i18n'
import { RouterView } from 'vue-router'

const { isDark, toggleTheme } = useTheme()
const { locale } = useI18n()

const toggleLanguage = () => {
  locale.value = locale.value === 'es' ? 'en' : 'es'
}
</script>

<template>
  <div class="min-h-screen transition-colors duration-300 bg-white dark:bg-gray-950">
    <header class="bg-blue-600 dark:bg-blue-900 text-white p-4 flex justify-between items-center shadow-md">
      <div class="flex items-center space-x-2 border-2 border-white/20 rounded-full px-4 py-1">
         <span class="font-bold">AI Nuzlocker</span>
      </div>
      
      <div class="flex space-x-4">
        <button 
          @click="toggleLanguage" 
          class="px-3 py-1 rounded bg-blue-700 dark:bg-blue-800 hover:bg-blue-800 dark:hover:bg-blue-700 transition"
        >
          {{ locale === 'es' ? 'EN' : 'ES' }}
        </button>
        <button 
          @click="toggleTheme" 
          class="px-3 py-1 rounded bg-blue-700 dark:bg-blue-800 hover:bg-blue-800 dark:hover:bg-blue-700 transition"
        >
          {{ isDark ? '☀ Light' : '🌙 Dark' }}
        </button>
      </div>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>
