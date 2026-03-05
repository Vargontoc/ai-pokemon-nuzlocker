<script setup lang="ts">
const props = defineProps<{
  mode: 'exploration' | 'battle'
  connected: boolean
  language: string
}>()

const emit = defineEmits<{
  (e: 'update:language', value: string): void
}>()

const languageMenu = [
    { code: 'es-ES', flag: '🇪🇸', nativeName: 'Español' },
    { code: 'en-US', flag: '🇺🇸', nativeName: 'English (US)' },
    { code: 'en-GB', flag: '🇬🇧', nativeName: 'English (UK)' },
    { code: 'pt-BR', flag: '🇧🇷', nativeName: 'Português' },
    { code: 'it-IT', flag: '🇮🇹', nativeName: 'Italiano' },
    { code: 'fr-FR', flag: '🇫🇷', nativeName: 'Français' },
]

const onLangChanged = (event: Event) => {
   const val = (event.target as HTMLSelectElement).value
   emit('update:language', val)
   console.log("AI Language preference changed to:", val)
}
</script>

<template>
  <header class="w-full shrink-0 flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900/50">
     <!-- Status / Room ID -->
     <div class="flex items-center gap-2">
         <div class="h-2.5 w-2.5 rounded-full" :class="props.connected ? 'bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)]' : 'bg-red-500'"></div>
         <span class="font-bold uppercase tracking-wider text-xs px-2 py-1 rounded select-none"
               :class="props.mode === 'exploration' ? 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' : 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300'">
            {{ props.mode === 'exploration' ? 'Modo Exploración' : 'Modo Batalla' }}
         </span>
     </div>

     <!-- Selector de Idioma -->
     <div class="relative group cursor-pointer text-sm">
         <select 
             :value="props.language"
             @change="onLangChanged"
             class="appearance-none bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-200 py-1 pl-8 pr-6 rounded-md shadow-sm outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer font-medium"
         >
            <option v-for="lang in languageMenu" :key="lang.code" :value="lang.code">
                {{ lang.nativeName }}
            </option>
         </select>
         
         <!-- Bandera Absoluta (Hack visual sobre el select nativo) -->
         <span class="absolute left-2.5 top-1/2 -translate-y-1/2 pointer-events-none text-base">
             {{ languageMenu.find(l => l.code === props.language)?.flag }}
         </span>
         
         <!-- Flechita para Custom Select -->
         <span class="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none text-gray-500">
             <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path></svg>
         </span>
     </div>
  </header>
</template>
