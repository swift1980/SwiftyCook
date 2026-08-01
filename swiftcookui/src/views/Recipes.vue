<template>
  <div>
    <CardGrid v-if="displayItems.length || loading"
              :items="displayItems"
              :loading="loading" />

    <div v-else-if="emptyState" class="empty-state">{{ emptyState }}</div>
    <div v-else-if="searchError" class="search-error">
      {{ searchError }}
      <button class="retry-btn" @click="emit('retry')">Retry</button>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue'
  import { useRecipeStore } from '@/stores/recipeStore'
  import { storeToRefs } from 'pinia'
  import CardGrid from '@/components/shared/CardGrid.vue'
  import type { PagedResultDto, RecipeSearchResultDto, RecipeCardItem } from '@/interfaces/ingredientSearch'

  const props = defineProps({
    nameQuery: { type: String, default: '' },
    ingredientList: { type: Array as () => string[], default: () => [] },
    matchMode: { type: String as () => 'and' | 'or', default: 'and' },
    selectedCategoryIds: { type: Array as () => number[], default: () => [] },
    searchResults: { type: Object as () => PagedResultDto<RecipeSearchResultDto> | null, default: null },
    searchError: { type: String as () => string | null, default: null },
    searchLoading: { type: Boolean, default: false },
  })

  const emit = defineEmits<{ (e: 'retry'): void }>()

  const recipeStore = useRecipeStore()
  const { loading: storeLoading } = storeToRefs(recipeStore)

  const loading = computed(() =>
    props.searchResults !== null ? props.searchLoading : storeLoading.value
  )

  /** Normalise RecipeSearchResultDto → RecipeCardItem for CardGrid */
  function normalise(dto: RecipeSearchResultDto): RecipeCardItem {
    return {
      id: dto.recipeId,
      name: dto.name,
      image: dto.image,
      optionalMatchCount: dto.optionalMatchCount,
      optionalTotal: dto.optionalTotal,
      matchedOptionalIds: dto.matchedOptionalIds,
    }
  }

  const displayItems = computed((): RecipeCardItem[] => {
    if (props.searchResults !== null) {
      const normalised = props.searchResults.items.map(normalise)
      if (!props.nameQuery) return normalised
      const q = props.nameQuery.toLowerCase()
      return normalised.filter((r) => r.name.toLowerCase().includes(q))
    }

    // Store-list path — map RecipeDto to RecipeCardItem
    return recipeStore
      .getFilteredRecipes(props.nameQuery, props.ingredientList, props.matchMode)
      .map((r) => ({ id: r.id, name: r.name, image: r.image }))
  })

  const emptyState = computed((): string | null => {
    if (loading.value || props.searchError) return null

    if (props.searchResults !== null) {
      if (props.searchResults.totalCount === 0) {
        return 'No recipes matched these ingredients — try broadening your search.'
      }
      if (displayItems.value.length === 0 && props.nameQuery) {
        return `No results matching "${props.nameQuery}" in these ingredients — try clearing the name filter.`
      }
    }
    return null
  })
</script>
