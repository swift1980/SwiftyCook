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
    <div v-else-if="nameSearchError" class="search-error">{{ nameSearchError }}</div>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted } from 'vue'
  import { useRecipeStore } from '@/stores/recipeStore'
  import { storeToRefs } from 'pinia'
  import CardGrid from '@/components/shared/CardGrid.vue'
  import type { PagedResultDto, RecipeSearchResultDto, RecipeCardItem } from '@/interfaces/ingredientSearch'
  import type { RecipeDto } from '@/interfaces/recipe'

  const props = defineProps({
    nameQuery: { type: String, default: '' },
    ingredientList: { type: Array as () => string[], default: () => [] },
    matchMode: { type: String as () => 'and' | 'or', default: 'and' },
    selectedCategoryIds: { type: Array as () => number[], default: () => [] },
    searchResults: { type: Object as () => PagedResultDto<RecipeSearchResultDto> | null, default: null },
    searchError: { type: String as () => string | null, default: null },
    searchLoading: { type: Boolean, default: false },
    nameSearchResults: { type: Array as () => RecipeDto[] | null, default: null },
    nameSearchError: { type: String as () => string | null, default: null },
    nameSearchLoading: { type: Boolean, default: false },
  })

  defineOptions({ name: 'RecipesView' })

  const emit = defineEmits<{ (e: 'retry'): void }>()

  const recipeStore = useRecipeStore()
  const { loading: storeLoading } = storeToRefs(recipeStore)

  onMounted(() => {
    if (!recipeStore.recipes.length) recipeStore.fetchAllRecipes()
  })

  const loading = computed(() => {
    if (props.searchResults !== null) return props.searchLoading
    if (props.nameQuery) return props.nameSearchLoading
    return storeLoading.value
  })

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

    if (props.nameQuery) {
      return (props.nameSearchResults ?? []).map((r) => ({ id: r.id, name: r.name, image: r.image }))
    }

    // Browse-all path — apply the ingredient-tag filter client-side over the full list
    return recipeStore
      .filterByIngredients(props.ingredientList, props.matchMode)
      .map((r) => ({ id: r.id, name: r.name, image: r.image }))
  })

  const emptyState = computed((): string | null => {
    if (loading.value || props.searchError || props.nameSearchError) return null

    if (props.searchResults !== null) {
      if (props.searchResults.totalCount === 0) {
        return 'No recipes matched these ingredients — try broadening your search.'
      }
      if (displayItems.value.length === 0 && props.nameQuery) {
        return `No results matching "${props.nameQuery}" in these ingredients — try clearing the name filter.`
      }
    } else if (props.nameQuery && props.nameSearchResults !== null && displayItems.value.length === 0) {
      return `No recipes matched "${props.nameQuery}".`
    }
    return null
  })
</script>
