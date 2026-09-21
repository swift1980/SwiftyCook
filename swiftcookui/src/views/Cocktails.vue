<template>
  <CardGrid :items="filtered" :loading="loading" />
  <div v-if="error" class="error">Error: {{ error }}</div>
</template>

<script setup lang="ts">
  import { onMounted, computed } from 'vue'
  import { useCocktailStore } from '@/stores/cocktailStore'
  import { storeToRefs } from 'pinia'
  import CardGrid from '@/components/shared/CardGrid.vue'

  defineOptions({ name: 'CocktailsView' })

  const cocktailStore = useCocktailStore();
  const { loading, error } = storeToRefs(cocktailStore);

  const props = defineProps({
    nameQuery: { type: String, default: '' },
  })

  onMounted(() => {
    cocktailStore.fetchCocktailAll();
  });

  const filtered = computed(() =>
    cocktailStore.getFilteredCocktails(props.nameQuery)
  )
</script>
