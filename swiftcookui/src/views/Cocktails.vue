<template>
  <CardGrid :items="filtered" :loading="loading" />
  <div v-if="error" class="error">Error: {{ error }}</div>
</template>

<script setup lang="ts">
  import { onMounted, computed } from 'vue'
  import { useCocktailStore } from '@/stores/cocktailStore'
  import { storeToRefs } from 'pinia'
  import CardGrid from '@/components/shared/CardGrid.vue'

  const cocktailStore = useCocktailStore();
  const { loading, error } = storeToRefs(cocktailStore);

  const props = defineProps({
    nameQuery: { type: String, default: '' },
    ingredientList: { type: Array as () => string[], default: () => [] },
    matchMode: { type: String as () => 'and' | 'or', default: 'and' }
  })

  onMounted(() => {
    cocktailStore.fetchCocktailAll();
  });

  const filtered = computed(() =>
    cocktailStore.getFilteredCocktails(props.nameQuery, props.ingredientList, props.matchMode)
  )
</script>
