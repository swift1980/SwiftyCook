<template>
  <CardGrid :items="filtered" :loading="recipeStore.loading" />
  <div v-if="error" class="error">Error: {{ error }}</div>
</template>

<script setup lang="ts">
  import { computed, onMounted } from 'vue'
  import { useRecipeStore } from '@/stores/recipeStore'
  import { useCategoryStore } from '@/stores/categoryStore'
  import { storeToRefs } from 'pinia'
  import CardGrid from '@/components/shared/CardGrid.vue'

  const recipeStore = useRecipeStore();
  const categoryStore = useCategoryStore();

  const { recipes, error, loading } = storeToRefs(recipeStore);
  const { categories } = storeToRefs(categoryStore);

  const props = defineProps({
    nameQuery: { type: String, default: '' },
    ingredientList: { type: Array as () => string[], default: () => [] },
    matchMode: { type: String as () => 'and' | 'or', default: 'and' },
    selectedCategoryIds: { type: Array as () => number[], default: () => [] }
  })

  onMounted(() => {
    recipeStore.fetchAllRecipes();
    categoryStore.fetchActive();
  });


  const filtered = computed(() =>
    recipeStore.getFilteredRecipes(props.nameQuery, props.ingredientList, props.matchMode)
  )
</script>
