<script setup lang="ts">
import { computed } from 'vue'
import type { TeamMember } from '../../services/nuzlockeService'
import PartyPokemonNode from './PartyPokemonNode.vue'

const props = defineProps<{
  party: (TeamMember | null)[]
}>()

// Ensure we always render exactly 6 slots
const paddedParty = computed(() => {
  const arr = [...props.party]
  while (arr.length < 6) {
    arr.push(null)
  }
  return arr.slice(0, 6)
})

</script>

<template>
  <div class="w-full flex justify-center py-4 bg-gray-50 dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800 z-30">
    <div class="flex gap-2 sm:gap-4 md:gap-6 px-4">
      <PartyPokemonNode 
        v-for="(member, index) in paddedParty" 
        :key="index"
        :pokemon="member"
      />
    </div>
  </div>
</template>
